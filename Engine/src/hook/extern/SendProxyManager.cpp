/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

#include "bridge/natives/SendProxyManager.h"
#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "manager/HookManager.h"
#include "murmurhash.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CServerSideClient.h"

#include <cstring>
#include <memory>
#include <string>
#include <string_view>
#include <unordered_map>
#include <unordered_set>
#include <vector>

#include <safetyhook.hpp>

// SendProxy — per-client net-var override during the flattened-serializer encode. Managed code registers
// (entity, field) via natives; the shared snapshot pack stays untouched and each recipient's stream is
// corrected at the per-client BitCopy stage, which runs on the main thread.

// Installed lazily on the first Hook so a server not using SendProxy pays nothing on the per-client encode.
static bool InstallSendProxyHooks();

static bool g_installed     = false;
static bool g_installFailed = false;

constexpr int       MAX_ENTITY_COUNT = 16384;
constexpr uintptr_t USER_PTR_MIN     = 0x10000;

enum class SpFieldType : uint8_t
{
    Unsupported = 0,
    Int32,
    UInt32,
    Int64,
    Fixed32,
    Fixed64,
    Bool,
    Float32,
    QuantizedFloat,
    QAngle3,
    Vector3,
    Coord3,
    Normal3,
    CoordIntegral3,
    String,
    ByteArray,
};

constexpr int KIND_INT    = 0;
constexpr int KIND_FLOAT  = 1;
constexpr int KIND_BOOL   = 2;
constexpr int KIND_VECTOR = 3;
constexpr int KIND_STRING = 4;

static bool IsUserPtr(uintptr_t p)
{
    return p >= USER_PTR_MIN;
}

static bool IsUserPtr(const void* p)
{
    return reinterpret_cast<uintptr_t>(p) >= USER_PTR_MIN;
}

using SpEncodeFn = void (*)(void* bf, void* fieldInfo, void* params, void* valuePtr, uint32_t extra);

struct SpStringHash
{
    using is_transparent = void;
    size_t operator()(std::string_view s) const noexcept { return std::hash<std::string_view>{}(s); }
};

constexpr int MAX_SLOTS    = 64;
constexpr int MAX_DISTINCT = 16;  // distinct override values per (entity,field)/tick before we stop deduping
constexpr int BLOB_CAP     = 320; // max encoded field size (string ≤ 256 + slack)

// One batched callback per (entity, field) per tick fills every slot; each distinct value is encoded ONCE into a blob and equal-value slots reuse it via bit-splice.
struct SpFieldBatch
{
    int32_t        tick    = -1;
    int32_t        kind    = 0; // SendProxyValueKind (also passed to the callback via the forward arg)
    uint64_t       hasMask = 0;
    SendProxyValue values[MAX_SLOTS]{};     // managed writes up to here (offset 16); the rest is native-only.
    void*          encodeLeafRec = nullptr; // the leaf the blobs were encoded with (a same-named leaf differs)
    int8_t         slotBlob[MAX_SLOTS]{};   // per set slot: blob index, or -1 (passthrough)
    int32_t        blobBits[MAX_DISTINCT]{};
    int32_t        blobCount = 0;
    uint8_t        blobData[MAX_DISTINCT][BLOB_CAP]{};
};

using SpFieldMap = std::unordered_map<std::string, SpFieldBatch, SpStringHash, std::equal_to<>>;

static SpFieldMap* g_pHooks[MAX_ENTITY_COUNT] = {};
static bool        g_hasAnyHook               = false;

// A callback can Unhook/UnhookEntity itself mid-dispatch; queue erasures and flush after, else erasing `batch` while native still reads it is a use-after-free.
static bool                                     g_dispatching = false;
static std::vector<std::pair<int, std::string>> g_pendingUnhook;
static std::vector<int>                         g_pendingClear;
static void                                     FlushPending();

static std::unordered_map<uintptr_t, SpFieldType> g_encoderTypes;

// Per-thread captures, set by hooks on this thread and consumed at BitCopy (field identity resolved via ResolveFieldIndex below).
static thread_local void*          t_client     = nullptr;
static thread_local int            t_entityIdx  = -1;
static thread_local void*          t_serializer = nullptr;
static thread_local const int32_t* t_bitTable   = nullptr; // start bits: table[idx+1] for field idx (table[0] is skew)
static thread_local void*          t_srcData    = nullptr; // snapshot buffer; gate out BitCopy calls on other buffers
static thread_local int            t_fieldCount = 0;

struct SpResolveKey
{
    void* serializer;
    int   index;
    bool  operator==(const SpResolveKey& o) const { return serializer == o.serializer && index == o.index; }
};
struct SpResolveKeyHash
{
    size_t operator()(const SpResolveKey& k) const
    {
        return std::hash<void*>{}(k.serializer) ^ (static_cast<size_t>(k.index) * 0x9E3779B97F4A7C15ull);
    }
};
struct SpResolved
{
    void*       leafRec = nullptr;
    SpFieldType type    = SpFieldType::Unsupported;
    int         kind    = -1;
    uint32_t    hash    = 0; // murmur of the field name — carried to managed so it routes by hash, not string
    std::string name;
};
static std::unordered_map<SpResolveKey, SpResolved, SpResolveKeyHash> g_resolve;

static SafetyHookInline g_perClientHook{};
static SafetyHookInline g_wdeHook{};
static SafetyHookInline g_wflHook{};
static SafetyHookInline g_bitCopyHook{};

// Walks leaf records in the same flattened DFS order as the bit-offset table, so `target` equals the cursor-matched field index.
static void* WalkToLeafRec(void* serializer, int& current, int target, int depth)
{
    if (depth > 4 || !IsUserPtr(serializer))
        return nullptr;

    int count = *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(serializer) + 0x28);
    if (count <= 0 || count > 4096)
        return nullptr;

    void* arr = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(serializer) + 0x30);
    if (!IsUserPtr(arr))
        return nullptr;

    for (int i = 0; i < count; i++)
    {
        void* rec   = reinterpret_cast<uint8_t*>(arr) + i * 0x2E;
        void* child = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(rec) + 0x08);
        if (IsUserPtr(child))
        {
            if (void* found = WalkToLeafRec(child, current, target, depth + 1))
                return found;
            continue;
        }

        if (current == target)
            return rec;
        current++;
    }

    return nullptr;
}

static const char* ResolveFieldNameByIndex(void* serializer, int index, void** leafRecOut)
{
    *leafRecOut = nullptr;
    if (index < 0)
        return nullptr;

    int   current = 0;
    void* rec     = WalkToLeafRec(serializer, current, index, 0);
    if (!IsUserPtr(rec))
        return nullptr;

    void* fieldInfo = *reinterpret_cast<void**>(rec);
    if (!IsUserPtr(fieldInfo))
        return nullptr;

    *leafRecOut = rec;
    auto name   = *reinterpret_cast<char**>(reinterpret_cast<uint8_t*>(fieldInfo) + 0x08);
    return IsUserPtr(name) ? name : nullptr;
}

static SpFieldType ClassifyLeaf(void* leafRec)
{
    if (!IsUserPtr(leafRec))
        return SpFieldType::Unsupported;
    void* fieldInfo = *reinterpret_cast<void**>(leafRec);
    if (!IsUserPtr(fieldInfo))
        return SpFieldType::Unsupported;
    void* dispatch = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(fieldInfo) + 0x38);
    if (!IsUserPtr(dispatch))
        return SpFieldType::Unsupported;
    auto encFn = *reinterpret_cast<uintptr_t*>(dispatch);
    auto it    = g_encoderTypes.find(encFn);
    return it == g_encoderTypes.end() ? SpFieldType::Unsupported : it->second;
}

static int KindForType(SpFieldType t)
{
    switch (t)
    {
    case SpFieldType::Int32:
    case SpFieldType::UInt32:
    case SpFieldType::Int64:
    case SpFieldType::Fixed32:
    case SpFieldType::Fixed64:
        return KIND_INT;
    case SpFieldType::Bool:
        return KIND_BOOL;
    case SpFieldType::Float32:
        return KIND_FLOAT;
    case SpFieldType::QAngle3:
    case SpFieldType::Vector3:
        return KIND_VECTOR;
    case SpFieldType::String:
        return KIND_STRING;
    // Quantized/coord/normal need a count/mode word this path doesn't read; return -1 so no forward fires.
    default:
        return -1;
    }
}

static uint8_t (*g_origBitCopy)(void* dst, void* src, uint32_t bits) = nullptr;

static bool EncodeToBlob(void* leafRec, SpFieldType type, const SendProxyValue& v, uint8_t* outBlob, int outCap, int& outBits)
{
    void* fieldInfo = *reinterpret_cast<void**>(leafRec);
    if (!IsUserPtr(fieldInfo))
        return false;
    void* dispatch = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(fieldInfo) + 0x38);
    if (!IsUserPtr(dispatch))
        return false;
    auto encFn = *reinterpret_cast<SpEncodeFn*>(dispatch);
    if (!IsUserPtr(reinterpret_cast<void*>(encFn)))
        return false;

    uint8_t paramOff  = *(reinterpret_cast<uint8_t*>(fieldInfo) + 0xC9);
    void*   paramsPtr = nullptr;
    if (paramOff != 0xFF)
    {
        void* base = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(fieldInfo) + 0x40);
        if (!IsUserPtr(base))
            return false;
        paramsPtr = reinterpret_cast<uint8_t*>(base) + paramOff;
    }

    uint8_t scratch[0x30]{};
    void*   strSlot  = nullptr;
    void*   valuePtr = scratch;
    switch (type)
    {
    case SpFieldType::UInt32: *reinterpret_cast<uint64_t*>(scratch) = static_cast<uint32_t>(v.i); break;
    case SpFieldType::Int32:
    case SpFieldType::Int64:
    case SpFieldType::Fixed32:
    case SpFieldType::Fixed64: *reinterpret_cast<int64_t*>(scratch) = v.i; break;
    case SpFieldType::Bool: scratch[0] = v.i != 0 ? 1 : 0; break;
    case SpFieldType::Float32: *reinterpret_cast<double*>(scratch) = v.f; break;
    case SpFieldType::QAngle3:
    case SpFieldType::Vector3:
        reinterpret_cast<float*>(scratch)[0] = v.x;
        reinterpret_cast<float*>(scratch)[1] = v.y;
        reinterpret_cast<float*>(scratch)[2] = v.z;
        break;
    case SpFieldType::String:
        if (v.strLen < 0 || v.strLen >= static_cast<int>(sizeof(v.str)))
            return false;
        strSlot  = const_cast<char*>(v.str); // encoder reads *valuePtr as char*
        valuePtr = &strSlot;
        break;
    default:
        return false; // quantized/coord need the live count/mode word — unsupported here.
    }

    // Local bf_write layout: data ptr +0x00, byte size +0x08, bit size +0x0C, cursor bits +0x10.
    uint8_t data[512]{};
    uint8_t bw[0x40]{};
    *reinterpret_cast<void**>(bw + 0x00) = data;
    *reinterpret_cast<int*>(bw + 0x08)   = sizeof(data);
    *reinterpret_cast<int*>(bw + 0x0C)   = sizeof(data) * 8;
    *reinterpret_cast<int*>(bw + 0x10)   = 0;

    encFn(bw, fieldInfo, paramsPtr, valuePtr, 0);

    int     encodedBits = *reinterpret_cast<int*>(bw + 0x10);
    uint8_t overflow    = *(bw + 0x20);
    int     bytes       = (encodedBits + 7) / 8;
    if (overflow != 0 || encodedBits <= 0 || bytes > outCap)
        return false;

    memcpy(outBlob, data, bytes);
    outBits = encodedBits;
    return true;
}

// Skips the real value in src, then emits the cached blob via a fresh bf_write through the engine's own BitCopy.
static uint8_t SpliceBlob(void* dst, void* src, uint32_t bitcount, const uint8_t* blob, int bits)
{
    uint8_t bw[0x40]{};
    *reinterpret_cast<const void**>(bw + 0x00) = blob;
    *reinterpret_cast<int*>(bw + 0x08)         = (bits + 7) / 8;
    *reinterpret_cast<int*>(bw + 0x0C)         = bits;
    *reinterpret_cast<int*>(bw + 0x10)         = 0;

    *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(src) + 0x10) += static_cast<int>(bitcount);
    return g_origBitCopy(dst, bw, static_cast<uint32_t>(bits));
}

static bool ValuesEqual(int kind, const SendProxyValue& a, const SendProxyValue& b)
{
    switch (kind)
    {
    case KIND_INT:
    case KIND_BOOL: return a.i == b.i;
    case KIND_FLOAT: return a.f == b.f;
    case KIND_VECTOR: return a.x == b.x && a.y == b.y && a.z == b.z;
    case KIND_STRING:
        return a.strLen >= 0 && a.strLen < static_cast<int>(sizeof(a.str)) && a.strLen == b.strLen
               && memcmp(a.str, b.str, static_cast<size_t>(a.strLen)) == 0;
    default: return false;
    }
}

static void* Detour_PerClientEncode(void* a, void* b, void* c, void* d, void* e, void* f)
{
    // The whole substitution path assumes main-thread execution (no locks on g_pHooks/g_resolve) — assert it.
    AssertBool(g_nMainThreadId == static_cast<uint64_t>(GetCurrentThreadId()));

    t_client   = b;
    auto* orig = g_perClientHook.original<void* (*)(void*, void*, void*, void*, void*, void*)>();
    auto  ret  = orig(a, b, c, d, e, f);
    t_client   = nullptr;
    return ret;
}

static void* Detour_WriteDeltaEntity(void* a, void* b, void* c, void* d, void* e, void* f)
{
    int prev = t_entityIdx;
    if (IsUserPtr(b))
        t_entityIdx = *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(b) + 0x34);
    auto* orig  = g_wdeHook.original<void* (*)(void*, void*, void*, void*, void*, void*)>();
    auto  ret   = orig(a, b, c, d, e, f);
    t_entityIdx = prev;
    return ret;
}

static void* Detour_WriteFieldList(void* a, void* b, void* c, void* d, void* e, uint32_t p6, uint32_t p7, void* p8, uint32_t p9)
{
    void*          prevSer   = t_serializer;
    const int32_t* prevTable = t_bitTable;
    void*          prevData  = t_srcData;
    int            prevCount = t_fieldCount;

    t_serializer = a;
    // e = param_5: the field descriptor. table @+0x08 (start bits), snapshot buffer @+0x18, count @+0x30.
    if (IsUserPtr(e))
    {
        t_bitTable   = *reinterpret_cast<const int32_t**>(reinterpret_cast<uint8_t*>(e) + 0x08);
        t_srcData    = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(e) + 0x18);
        t_fieldCount = *reinterpret_cast<int32_t*>(reinterpret_cast<uint8_t*>(e) + 0x30);
    }
    else
    {
        // Clear, not leave stale — else this frame resolves against the outer frame's table + a fresh serializer.
        t_bitTable   = nullptr;
        t_srcData    = nullptr;
        t_fieldCount = 0;
    }

    auto* orig = g_wflHook.original<void* (*)(void*, void*, void*, void*, void*, uint32_t, uint32_t, void*, uint32_t)>();
    auto  ret  = orig(a, b, c, d, e, p6, p7, p8, p9);

    t_serializer = prevSer;
    t_bitTable   = prevTable;
    t_srcData    = prevData;
    t_fieldCount = prevCount;
    return ret;
}

// Matches the src read cursor (each field's absolute start bit, seeked by WriteFieldList) against the bit-offset table to recover the flattened-leaf index. table[1..count] are strictly increasing start bits.
static int ResolveFieldIndex(int startBit)
{
    if (!IsUserPtr(t_bitTable) || t_fieldCount <= 0 || t_fieldCount > 4096)
        return -1;
    int lo = 1, hi = t_fieldCount;
    while (lo <= hi)
    {
        int mid = (lo + hi) / 2;
        int v   = t_bitTable[mid];
        if (v == startBit)
        {
            // Zero-width fields share a start bit; the one actually being copied is the last of the run.
            while (mid < t_fieldCount && t_bitTable[mid + 1] == startBit)
                mid++;
            return mid - 1; // table[idx+1] = start bit of field idx
        }
        if (v < startBit)
            lo = mid + 1;
        else
            hi = mid - 1;
    }
    return -1;
}

static uint8_t Detour_BitCopy(void* dst, void* src, uint32_t bitcount)
{
    // t_client is set only during the per-client encode (main thread) and null on shared-pack worker threads, so this gate makes the whole path main-thread-only — no locks needed on g_hasAnyHook/g_pHooks.
    if (t_client == nullptr || !g_hasAnyHook || bitcount == 0 || !IsUserPtr(t_serializer) || t_entityIdx < 0 || t_entityIdx >= MAX_ENTITY_COUNT)
        return g_origBitCopy(dst, src, bitcount);

    auto* fieldMap = g_pHooks[t_entityIdx];
    if (fieldMap == nullptr)
        return g_origBitCopy(dst, src, bitcount);

    // Gate out BitCopy calls on other buffers (merge/scratch) — only the snapshot buffer carries our fields.
    if (!IsUserPtr(src) || *reinterpret_cast<void**>(src) != t_srcData)
        return g_origBitCopy(dst, src, bitcount);

    int index = ResolveFieldIndex(*reinterpret_cast<int32_t*>(reinterpret_cast<uint8_t*>(src) + 0x10));
    if (index < 0)
        return g_origBitCopy(dst, src, bitcount);

    // Resolve (serializer, index) -> {leafRec, kind, name} once; integer-keyed lookup on every BitCopy after.
    auto rit = g_resolve.find(SpResolveKey{t_serializer, index});
    if (rit == g_resolve.end())
    {
        void*       leafRec = nullptr;
        const char* name    = ResolveFieldNameByIndex(t_serializer, index, &leafRec);
        SpResolved  r;
        if (name != nullptr && leafRec != nullptr)
        {
            r.leafRec = leafRec;
            r.type    = ClassifyLeaf(leafRec);
            r.kind    = KindForType(r.type);
            r.name    = name;
            r.hash    = MurmurHash2(name, MURMURHASH_SEED);
        }
        rit = g_resolve.emplace(SpResolveKey{t_serializer, index}, std::move(r)).first;
    }

    const SpResolved& rf = rit->second;
    if (rf.leafRec == nullptr || rf.kind < 0)
        return g_origBitCopy(dst, src, bitcount);

    auto it = fieldMap->find(std::string_view(rf.name));
    if (it == fieldMap->end())
        return g_origBitCopy(dst, src, bitcount);

    void*       leafRec = rf.leafRec;
    int         kind    = rf.kind;
    SpFieldType type    = rf.type;

    SpFieldBatch& batch = it->second;

    // First BitCopy this tick for (entity, field): fire the callback once; later receivers just reuse the batch.
    int tick = gpGlobals->nTickCount;
    if (batch.tick != tick)
    {
        batch.tick    = tick;
        batch.kind    = kind;
        batch.hasMask = 0;
        g_dispatching = true;
        forwards::OnSendProxyBatch->Invoke(t_entityIdx, rf.hash, kind, &batch);
        g_dispatching = false;

        batch.encodeLeafRec = leafRec;
        batch.blobCount     = 0;
        for (int s = 0; s < MAX_SLOTS; s++)
        {
            if ((batch.hasMask & (1ull << s)) == 0)
                continue;

            int reuse = -1;
            for (int p = 0; p < s; p++)
            {
                if ((batch.hasMask & (1ull << p)) != 0 && batch.slotBlob[p] >= 0
                    && ValuesEqual(kind, batch.values[s], batch.values[p]))
                {
                    reuse = batch.slotBlob[p];
                    break;
                }
            }

            if (reuse >= 0)
            {
                batch.slotBlob[s] = static_cast<int8_t>(reuse);
            }
            else if (batch.blobCount < MAX_DISTINCT
                     && EncodeToBlob(leafRec, type, batch.values[s], batch.blobData[batch.blobCount], BLOB_CAP, batch.blobBits[batch.blobCount]))
            {
                batch.slotBlob[s] = static_cast<int8_t>(batch.blobCount++);
            }
            else
            {
                batch.slotBlob[s] = -1; // encode failed or pool full — this slot gets the real value.
            }
        }
    }

    int     slot        = reinterpret_cast<CServerSideClient*>(t_client)->GetSlot();
    uint8_t result      = 0;
    bool    substituted = false;
    // Only substitute the exact leaf the blobs were encoded with — a same-named leaf elsewhere could use a different encoder and produce malformed bits.
    if (slot >= 0 && slot < MAX_SLOTS && (batch.hasMask & (1ull << slot)) != 0 && leafRec == batch.encodeLeafRec)
    {
        int bi = batch.slotBlob[slot];
        if (bi >= 0 && bi < batch.blobCount)
        {
            result      = SpliceBlob(dst, src, bitcount, batch.blobData[bi], batch.blobBits[bi]);
            substituted = true;
        }
    }

    FlushPending();

    return substituted ? result : g_origBitCopy(dst, src, bitcount);
}

static SpFieldType ClassifyEntry(int bucket, const char* name)
{
    auto eq  = [&](const char* s) { return name != nullptr && strcmp(name, s) == 0; };
    bool def = eq("default");
    switch (bucket)
    {
    case 1: return def ? SpFieldType::Int32 : eq("fixed32") ? SpFieldType::Fixed32 :
                                          eq("fixed64")     ? SpFieldType::Fixed64 :
                                                              SpFieldType::Unsupported;
    case 2: return def ? SpFieldType::UInt32 : eq("fixed32") ? SpFieldType::Fixed32 :
                                           eq("fixed64")     ? SpFieldType::Fixed64 :
                                                               SpFieldType::Unsupported;
    case 3:
        return def ? SpFieldType::QuantizedFloat : (eq("qangle") || eq("qangle_pitch_yaw") || eq("qangle_precise")) ? SpFieldType::QAngle3 :
                                               eq("normal")                                                         ? SpFieldType::Normal3 :
                                               eq("coord")                                                          ? SpFieldType::Coord3 :
                                               eq("coord_integral")                                                 ? SpFieldType::CoordIntegral3 :
                                                                                                                      SpFieldType::Unsupported;
    case 4: return def ? SpFieldType::Float32 : SpFieldType::Unsupported;
    case 5: return def ? SpFieldType::String : SpFieldType::Unsupported;
    case 7: return def ? SpFieldType::Bool : SpFieldType::Unsupported;
    default: return SpFieldType::Unsupported;
    }
}

static void BuildEncoderMap()
{
    auto registry = g_pGameData->GetAddress<uintptr_t>("CFlattenedSerializer::EncoderRegistry");
    if (!IsUserPtr(registry))
    {
        WARN("SendProxy: EncoderRegistry not resolved — field classification disabled.");
        return;
    }

    const char* keys[] = {
        "CFlattenedSerializer::EncoderBucket1",
        "CFlattenedSerializer::EncoderBucket2",
        "CFlattenedSerializer::EncoderBucket3",
        "CFlattenedSerializer::EncoderBucket4",
        "CFlattenedSerializer::EncoderBucket5",
        "CFlattenedSerializer::EncoderBucket6",
        "CFlattenedSerializer::EncoderBucket7",
    };

    for (int i = 0; i < 7; i++)
    {
        int bucket = i + 1;
        // Bucket 6 (byte-array) has no supported value kind, so it needs no type entry.
        if (bucket == 6)
            continue;

        auto handler = g_pGameData->GetAddress<uintptr_t>(keys[i]);
        if (!IsUserPtr(handler))
            continue;

        int count = *reinterpret_cast<int*>(registry + bucket * 16 + 0x08);
        if (count <= 0 || count > 32)
            continue;

        for (int e = 0; e < count; e++)
        {
            auto entry = handler + static_cast<uintptr_t>(e) * 0x80;
            auto fn    = *reinterpret_cast<uintptr_t*>(entry + 0x30);
            if (!IsUserPtr(fn))
                continue;
            auto name = *reinterpret_cast<char**>(entry + 0x00);
            auto type = ClassifyEntry(bucket, IsUserPtr(reinterpret_cast<void*>(name)) ? name : nullptr);
            if (type != SpFieldType::Unsupported)
                g_encoderTypes.emplace(fn, type);
        }
    }
}

static void RecomputeHasAny()
{
    for (auto* p : g_pHooks)
    {
        if (p != nullptr)
        {
            g_hasAnyHook = true;
            return;
        }
    }
    g_hasAnyHook = false;
}

static void SendProxyManagerHookField(int entityIndex, const char* field)
{
    if (entityIndex <= 0 || entityIndex >= MAX_ENTITY_COUNT || field == nullptr)
        return;
    if (!InstallSendProxyHooks())
        return;
    if (g_pHooks[entityIndex] == nullptr)
        g_pHooks[entityIndex] = new SpFieldMap();
    g_pHooks[entityIndex]->try_emplace(field);
    g_hasAnyHook = true;
}

static bool SendProxyManagerUnhookField(int entityIndex, const char* field)
{
    if (entityIndex <= 0 || entityIndex >= MAX_ENTITY_COUNT || field == nullptr)
        return false;
    if (g_dispatching)
    {
        g_pendingUnhook.emplace_back(entityIndex, field);
        return true;
    }
    if (g_pHooks[entityIndex] == nullptr)
        return false;
    bool removed = g_pHooks[entityIndex]->erase(field) > 0;
    if (g_pHooks[entityIndex]->empty())
    {
        delete g_pHooks[entityIndex];
        g_pHooks[entityIndex] = nullptr;
        RecomputeHasAny();
    }
    return removed;
}

static void SendProxyManagerClearEntity(int entityIndex)
{
    if (entityIndex <= 0 || entityIndex >= MAX_ENTITY_COUNT)
        return;
    if (g_dispatching)
    {
        g_pendingClear.push_back(entityIndex);
        return;
    }
    if (g_pHooks[entityIndex] == nullptr)
        return;
    delete g_pHooks[entityIndex];
    g_pHooks[entityIndex] = nullptr;
    RecomputeHasAny();
}

static void FlushPending()
{
    if (g_pendingUnhook.empty() && g_pendingClear.empty())
        return;

    auto unhook = std::move(g_pendingUnhook);
    auto clear  = std::move(g_pendingClear);
    g_pendingUnhook.clear();
    g_pendingClear.clear();

    for (auto& [entityIndex, field] : unhook)
        SendProxyManagerUnhookField(entityIndex, field.c_str());
    for (int entityIndex : clear)
        SendProxyManagerClearEntity(entityIndex);
}

class SendProxyEntityListener : public IEntityListener
{
public:
    void OnEntityCreated(CBaseEntity* pEntity) override
    {
        // Entity indices are reused; a create over a still-hooked index means the previous delete was missed — clear stale hooks defensively (as TransmitManager does).
        if (!g_hasAnyHook)
            return;
        const int index = pEntity->GetEntityIndex();
        if (index > 0 && index < MAX_ENTITY_COUNT && g_pHooks[index] != nullptr)
        {
            WARN("SendProxy: entity created over hooked index %d — clearing stale hooks.", index);
            SendProxyManagerClearEntity(index);
        }
    }
    void OnEntityDeleted(CBaseEntity* pEntity) override
    {
        if (g_hasAnyHook)
            SendProxyManagerClearEntity(pEntity->GetEntityIndex());
    }
    void OnEntitySpawned(CBaseEntity*) override {}
    void OnEntityFollowed(CBaseEntity*, CBaseEntity*) override {}
} static s_entityListener;

template <typename Fn>
static bool TryInstallDetour(SafetyHookInline& hook, const char* key, Fn detour)
{
    auto addr = g_pGameData->GetAddress<void*>(key);
    if (!IsUserPtr(addr))
    {
        WARN("SendProxy: %s not resolved.", key);
        return false;
    }
    auto result = safetyhook::InlineHook::create(addr, reinterpret_cast<void*>(detour));
    if (!result)
    {
        WARN("SendProxy: failed to hook %s: %s", key, g_szInlineHookErrors[result.error().type]);
        return false;
    }
    hook = std::move(*result);
    g_pHookManager->Register(&hook);
    return true;
}

namespace natives::sendproxy
{
void Init()
{
    bridge::CreateNative("SendProxy.HookField", reinterpret_cast<void*>(SendProxyManagerHookField));
    bridge::CreateNative("SendProxy.UnhookField", reinterpret_cast<void*>(SendProxyManagerUnhookField));
    bridge::CreateNative("SendProxy.ClearEntity", reinterpret_cast<void*>(SendProxyManagerClearEntity));
}
} // namespace natives::sendproxy

static bool InstallSendProxyHooks()
{
    if (g_installed)
        return true;
    if (g_installFailed)
        return false;

    // A failed install is latched so we don't re-attempt (and re-warn) on every Hook call.
    g_installFailed = true;

    BuildEncoderMap();
    if (g_encoderTypes.empty())
    {
        WARN("SendProxy: no encoders classified — SendProxy disabled.");
        return false;
    }

    if (!TryInstallDetour(g_perClientHook, "CNetworkGameServer::PerClientEncode", Detour_PerClientEncode)
        || !TryInstallDetour(g_wdeHook, "CNetworkGameServerBase::WriteDeltaEntity_Internal", Detour_WriteDeltaEntity)
        || !TryInstallDetour(g_wflHook, "CFlattenedSerializer::WriteFieldList", Detour_WriteFieldList))
    {
        WARN("SendProxy: capture hooks incomplete — SendProxy disabled.");
        return false;
    }

    auto bitCopyAddr = g_pGameData->GetAddress<void*>("CFlattenedSerializer::BitCopyPrimitive");
    if (!IsUserPtr(bitCopyAddr))
    {
        WARN("SendProxy: BitCopyPrimitive not resolved — SendProxy disabled.");
        return false;
    }
    auto bc = safetyhook::InlineHook::create(bitCopyAddr, reinterpret_cast<void*>(Detour_BitCopy));
    if (!bc)
    {
        WARN("SendProxy: failed to hook BitCopyPrimitive: %s", g_szInlineHookErrors[bc.error().type]);
        return false;
    }
    g_bitCopyHook = std::move(*bc);
    g_origBitCopy = g_bitCopyHook.original<uint8_t (*)(void*, void*, uint32_t)>();
    g_pHookManager->Register(&g_bitCopyHook);

    g_pGameEntitySystem->AddListenerEntity(&s_entityListener);

    // A map change rebuilds serializers (staling the resolve cache) and entity teardown isn't guaranteed per hooked entity, so drop everything here — else leaked FieldMaps keep g_hasAnyHook true across maps.
    g_pHookManager->Hook_GameDeactivate(HookType_Post, [] {
        for (auto*& p : g_pHooks)
        {
            delete p;
            p = nullptr;
        }
        g_hasAnyHook = false;
        g_resolve.clear();
    });

    g_installFailed = false;
    g_installed     = true;
    return true;
}
