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

#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"
#include "bridge/natives/SendProxyManager.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "manager/HookManager.h"

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

// SendProxy — per-client net-var value override during the flattened-serializer encode.
//
// Managed plugins register (entity, field) via natives; the shared snapshot pack is left untouched, and each
// recipient's stream is corrected at the per-client BitCopy stage. That stage runs on the MAIN thread (the
// per-client send loop is synchronous, after the parallel PackEntities join), so the value is resolved by
// firing the OnSendProxyBatch forward into managed there — a plain main-thread callback, no worker-thread
// re-entrancy.
//
// Captures needed for a per-field substitution, each set by a hook on this thread and consumed at BitCopy:
//   PerClientEncode        -> current recipient (CServerSideClient*)
//   WriteDeltaEntity        -> current entity index
//   WriteFieldList          -> current serializer
//   GetBitRange (linux)     -> current CFieldPath* (Windows: WriteFieldList_FieldPathSite -> field index)
//   BitCopyPrimitive        -> substitute: resolve field name, gate, fire forward, re-encode, emit fake bits.

namespace
{
constexpr int       kMaxEdicts   = 16384;
constexpr uintptr_t kUserMin     = 0x10000;
constexpr int       kFieldPathMax = 3;

enum class FieldType : uint8_t
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

// SendProxyValue.kind
constexpr int kKindInt    = 0;
constexpr int kKindFloat  = 1;
constexpr int kKindBool   = 2;
constexpr int kKindVector = 3;
constexpr int kKindString = 4;

bool IsUserPtr(uintptr_t p)
{
    return p >= kUserMin;
}

bool IsUserPtr(const void* p)
{
    return reinterpret_cast<uintptr_t>(p) >= kUserMin;
}

using EncodeFn = void (*)(void* bf, void* fieldInfo, void* params, void* valuePtr, uint32_t extra);

struct string_hash
{
    using is_transparent = void;
    size_t operator()(std::string_view s) const noexcept { return std::hash<std::string_view>{}(s); }
};

constexpr int kMaxSlots    = 64;
constexpr int kMaxDistinct = 16;  // distinct override values per (entity,field)/tick before we stop deduping
constexpr int kBlobCap     = 320; // max encoded field size (string ≤ 256 + slack)

// Per-slot override values filled by ONE batched callback per (entity, field) per tick, then served to every
// receiver in that tick's per-client loop with no further managed call. `tick` is stamped from
// gpGlobals->nTickCount so the first BitCopy of a (entity, field) in a tick refills it and the rest reuse it.
// After the callback fills the typed values, each DISTINCT value is encoded ONCE into a blob (blobData) and
// every slot points at its blob (slotBlob) — receivers 2..N just bit-splice the cached blob, never re-encode.
struct FieldBatch
{
    int32_t        tick = -1;
    int32_t        kind = 0; // SendProxyValueKind (also passed to the callback via the forward arg)
    uint64_t       hasMask = 0;
    SendProxyValue values[kMaxSlots]{};    // managed writes up to here (offset 16); the rest is native-only.
    void*          encodeLeafRec = nullptr; // the leaf the blobs were encoded with (a same-named leaf differs)
    int8_t         slotBlob[kMaxSlots]{};  // per set slot: blob index, or -1 (passthrough)
    int32_t        blobBits[kMaxDistinct]{};
    int32_t        blobCount = 0;
    uint8_t        blobData[kMaxDistinct][kBlobCap]{};
};

using FieldMap = std::unordered_map<std::string, FieldBatch, string_hash, std::equal_to<>>;

// ── Registration store: pointer array indexed by entity index. All access is on the main thread
//    (registration natives, the per-client encode, and entity-delete cleanup all run there). ────────────────
FieldMap* g_hooks[kMaxEdicts] = {};
bool      g_hasAnyHook        = false;

// A batch callback can synchronously Unhook/UnhookEntity itself; erasing the map node while native is still
// reading `batch` would be a use-after-free. While dispatching we queue erasures and flush them afterward.
bool                                     g_dispatching = false;
std::vector<std::pair<int, std::string>> g_pendingUnhook;
std::vector<int>                         g_pendingClear;
void                                     FlushPending();

// Encoder fn -> FieldType, built once at install from the encoder-registry bucket table. Read-only after.
std::unordered_map<uintptr_t, FieldType> g_encoderTypes;

// Per-thread captures. Only the main thread's copies are ever read at a substitution (gated on recipient).
thread_local void*    t_client     = nullptr;
thread_local int      t_entityIdx  = -1;
thread_local void*    t_serializer = nullptr;
thread_local void*    t_fieldPath  = nullptr; // linux: CFieldPath* captured at GetBitRange (used on cache miss)
thread_local int      t_fieldIndex = -1;      // windows: flattened-leaf index (used on cache miss)
thread_local uint32_t t_fieldToken = 0;       // packed field-path token — the stable per-field cache key

// Resolving a field name at every BitCopy (CFieldPath walk + string) is the dominant per-tick cost. The field
// is identified by the engine's packed path token, so cache the resolve per (serializer, token): the walk runs
// once per (serializer, field) and the hot path is an integer-keyed lookup after that.
struct ResolveKey
{
    void*    serializer;
    uint32_t token;
    bool     operator==(const ResolveKey& o) const { return serializer == o.serializer && token == o.token; }
};
struct ResolveKeyHash
{
    size_t operator()(const ResolveKey& k) const
    {
        return std::hash<void*>{}(k.serializer) ^ (static_cast<size_t>(k.token) * 0x9E3779B97F4A7C15ull);
    }
};
struct Resolved
{
    void*       leafRec = nullptr;
    FieldType   type    = FieldType::Unsupported;
    int         kind    = -1;
    std::string name;
};
std::unordered_map<ResolveKey, Resolved, ResolveKeyHash> g_resolve;

// Hook objects.
SafetyHookInline g_perClientHook{};
SafetyHookInline g_wdeHook{};
SafetyHookInline g_wflHook{};
SafetyHookMid    g_bitRangeHook{};
SafetyHookInline g_bitCopyHook{};

// ── Field-path resolution (linux CFieldPath* walk) ──────────────────────────────────────────────────────
bool IndexInBounds(void* serializer, int idx)
{
    if (!IsUserPtr(serializer))
        return false;
    int count = *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(serializer) + 0x28);
    return count > 0 && count <= 4096 && idx >= 0 && idx < count;
}

// Walk the CFieldPath (filled by GetBitRange) to the leaf record; returns the field-name char* and the leaf
// record. Every deref is guarded — any bad pointer returns nullptr and the caller passes the real value.
const char* ResolveFieldName(void* serializer, void* hdr, void** leafRecOut)
{
    *leafRecOut = nullptr;
    if (!IsUserPtr(serializer) || !IsUserPtr(hdr))
        return nullptr;

    auto  h     = reinterpret_cast<uint8_t*>(hdr);
    short count = *reinterpret_cast<short*>(h + 0x18);
    if (count < 1 || count > kFieldPathMax)
        return nullptr;

    void* idxArr;
    if (*(h + 0x1A) != 0)
    {
        idxArr = *reinterpret_cast<void**>(h);
        if (!IsUserPtr(idxArr))
            return nullptr;
    }
    else
    {
        idxArr = hdr;
    }

    void* serArr = serializer;
    void* rec    = nullptr;

    for (int k = 0; k < count; k++)
    {
        short idxK = *reinterpret_cast<short*>(reinterpret_cast<uint8_t*>(idxArr) + k * 2);
        if (idxK == 0x7FFF)
        {
            if (k == 0)
                return nullptr;
            break;
        }

        if (k > 0)
        {
            void* child = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(rec) + 0x08);
            if (!IsUserPtr(child))
                return nullptr;
            serArr = child;
        }

        if (!IndexInBounds(serArr, idxK))
            return nullptr;

        void* arr = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(serArr) + 0x30);
        if (!IsUserPtr(arr))
            return nullptr;

        rec = reinterpret_cast<uint8_t*>(arr) + idxK * 0x2E;
        if (!IsUserPtr(rec))
            return nullptr;
    }

    void* pInfo = *reinterpret_cast<void**>(rec);
    if (!IsUserPtr(pInfo))
        return nullptr;

    *leafRecOut = rec;
    auto name   = *reinterpret_cast<char**>(reinterpret_cast<uint8_t*>(pInfo) + 0x08);
    return IsUserPtr(name) ? name : nullptr;
}

// Windows has no standalone GetBitRange (inlined), so the field is identified by a flattened-leaf INDEX (DFS
// order) captured at the WriteFieldList inner site. Walk the serializer's leaf records in the same order and
// return the record at `target`. Pure pointer walk (compiled on both platforms; only used on Windows).
void* WalkToLeafRec(void* serializer, int& current, int target, int depth)
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

const char* ResolveFieldNameByIndex(void* serializer, int index, void** leafRecOut)
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

FieldType ClassifyLeaf(void* leafRec)
{
    if (!IsUserPtr(leafRec))
        return FieldType::Unsupported;
    void* fieldInfo = *reinterpret_cast<void**>(leafRec);
    if (!IsUserPtr(fieldInfo))
        return FieldType::Unsupported;
    void* dispatch = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(fieldInfo) + 0x38);
    if (!IsUserPtr(dispatch))
        return FieldType::Unsupported;
    auto encFn = *reinterpret_cast<uintptr_t*>(dispatch);
    auto it    = g_encoderTypes.find(encFn);
    return it == g_encoderTypes.end() ? FieldType::Unsupported : it->second;
}

int KindForType(FieldType t)
{
    switch (t)
    {
        case FieldType::Int32:
        case FieldType::UInt32:
        case FieldType::Int64:
        case FieldType::Fixed32:
        case FieldType::Fixed64:
            return kKindInt;
        case FieldType::Bool:
            return kKindBool;
        case FieldType::Float32:
            return kKindFloat;
        case FieldType::QAngle3:
        case FieldType::Vector3:
            return kKindVector;
        case FieldType::String:
            return kKindString;
        // Quantized/coord/normal need the field's count/mode word from the live value, which the per-client
        // path does not read — don't fire the forward for a kind Substitute can't emit (silent no-op otherwise).
        default:
            return -1;
    }
}

uint8_t (*g_origBitCopy)(void* dst, void* src, uint32_t bits) = nullptr;

// Encode a value ONCE via the field's own encoder into `outBlob` (wire bits), returning the bit count in
// `outBits`. Done at batch-fill time (not per receiver) so N receivers with the same value cost one encode and
// then a bit-splice each — this is the maintainer's `SendProxyOverride{bitCount, bits}`. false = don't override.
bool EncodeToBlob(void* leafRec, FieldType type, const SendProxyValue& v, uint8_t* outBlob, int outCap, int& outBits)
{
    void* fieldInfo = *reinterpret_cast<void**>(leafRec);
    if (!IsUserPtr(fieldInfo))
        return false;
    void* dispatch = *reinterpret_cast<void**>(reinterpret_cast<uint8_t*>(fieldInfo) + 0x38);
    if (!IsUserPtr(dispatch))
        return false;
    auto encFn = *reinterpret_cast<EncodeFn*>(dispatch);
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

    // Build the value in the layout this encoder reads, and the value pointer to hand it.
    uint8_t scratch[0x30]{};
    void*   strSlot   = nullptr;
    void*   valuePtr  = scratch;
    switch (type)
    {
        case FieldType::UInt32: *reinterpret_cast<uint64_t*>(scratch) = static_cast<uint32_t>(v.i); break;
        case FieldType::Int32:
        case FieldType::Int64:
        case FieldType::Fixed32:
        case FieldType::Fixed64: *reinterpret_cast<int64_t*>(scratch) = v.i; break;
        case FieldType::Bool: scratch[0] = v.i != 0 ? 1 : 0; break;
        case FieldType::Float32: *reinterpret_cast<double*>(scratch) = v.f; break;
        case FieldType::QAngle3:
        case FieldType::Vector3:
            reinterpret_cast<float*>(scratch)[0] = v.x;
            reinterpret_cast<float*>(scratch)[1] = v.y;
            reinterpret_cast<float*>(scratch)[2] = v.z;
            break;
        case FieldType::String:
            if (v.strLen < 0 || v.strLen >= static_cast<int>(sizeof(v.str)))
                return false;
            strSlot  = const_cast<char*>(v.str); // encoder reads *valuePtr as char*
            valuePtr = &strSlot;
            break;
        default:
            return false; // quantized/coord need the live count/mode word — unsupported here.
    }

    // Encode into a local bf_write, then copy the used bytes into outBlob.
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

// Splice a pre-encoded blob into dst: skip the real value in src, then copy the cached bits via the engine's
// own BitCopy (a fresh bf_write over the blob at cursor 0). No re-encode.
uint8_t SpliceBlob(void* dst, void* src, uint32_t bitcount, const uint8_t* blob, int bits)
{
    uint8_t bw[0x40]{};
    *reinterpret_cast<const void**>(bw + 0x00) = blob;
    *reinterpret_cast<int*>(bw + 0x08)         = (bits + 7) / 8;
    *reinterpret_cast<int*>(bw + 0x0C)         = bits;
    *reinterpret_cast<int*>(bw + 0x10)         = 0;

    *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(src) + 0x10) += static_cast<int>(bitcount);
    return g_origBitCopy(dst, bw, static_cast<uint32_t>(bits));
}

bool ValuesEqual(int kind, const SendProxyValue& a, const SendProxyValue& b)
{
    switch (kind)
    {
        case kKindInt:
        case kKindBool: return a.i == b.i;
        case kKindFloat: return a.f == b.f;
        case kKindVector: return a.x == b.x && a.y == b.y && a.z == b.z;
        case kKindString:
            return a.strLen >= 0 && a.strLen < static_cast<int>(sizeof(a.str)) && a.strLen == b.strLen
                && memcmp(a.str, b.str, static_cast<size_t>(a.strLen)) == 0;
        default: return false;
    }
}

// ── Hooks ────────────────────────────────────────────────────────────────────────────────────────────────
void* PerClientEncode_Detour(void* a, void* b, void* c, void* d, void* e, void* f)
{
    t_client   = b;
    auto* orig = g_perClientHook.original<void* (*)(void*, void*, void*, void*, void*, void*)>();
    auto  ret  = orig(a, b, c, d, e, f);
    t_client   = nullptr;
    return ret;
}

void* WriteDeltaEntity_Detour(void* a, void* b, void* c, void* d, void* e, void* f)
{
    int prev = t_entityIdx;
    if (IsUserPtr(b))
        t_entityIdx = *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(b) + 0x34);
    auto* orig  = g_wdeHook.original<void* (*)(void*, void*, void*, void*, void*, void*)>();
    auto  ret   = orig(a, b, c, d, e, f);
    t_entityIdx = prev;
    return ret;
}

void* WriteFieldList_Detour(void* a, void* b, void* c, void* d, void* e, uint32_t p6, uint32_t p7, void* p8, uint32_t p9)
{
    void* prev   = t_serializer;
    t_serializer = a;
    auto* orig   = g_wflHook.original<void* (*)(void*, void*, void*, void*, void*, uint32_t, uint32_t, void*, uint32_t)>();
    auto  ret    = orig(a, b, c, d, e, p6, p7, p8, p9);
    t_serializer = prev;
    return ret;
}

void GetBitRange_Mid(SafetyHookContext& ctx)
{
    t_fieldPath  = reinterpret_cast<void*>(ctx.rdi);
    t_fieldToken = static_cast<uint32_t>(ctx.rdx); // 3rd arg = the packed field-path token
}

// Windows: WriteFieldList inner site, R12 = current flattened-leaf index (survives the following call).
void WindowsFieldIndexHook(SafetyHookContext& ctx)
{
    t_fieldIndex = static_cast<int>(ctx.r12);
    t_fieldToken = static_cast<uint32_t>(ctx.r12); // the index is the stable per-field key on Windows
}

uint8_t BitCopy_Detour(void* dst, void* src, uint32_t bitcount)
{
    // Substitute only during a per-client send. t_client is a thread_local set only by the per-client encode
    // (main thread); it is null on the shared-pack worker threads, so this check gates everything below to the
    // main thread — no lock needed for g_hasAnyHook / g_hooks, which are only touched there.
    if (t_client == nullptr || !g_hasAnyHook || !IsUserPtr(t_serializer) || t_entityIdx < 0 || t_entityIdx >= kMaxEdicts)
        return g_origBitCopy(dst, src, bitcount);

    auto* fieldMap = g_hooks[t_entityIdx];
    if (fieldMap == nullptr)
        return g_origBitCopy(dst, src, bitcount);

    // Resolve (serializer, token) -> {leafRec, kind, name} once; integer-keyed lookup on every BitCopy after.
    auto rit = g_resolve.find(ResolveKey{t_serializer, t_fieldToken});
    if (rit == g_resolve.end())
    {
        void*       leafRec = nullptr;
        const char* name;
#ifdef PLATFORM_WINDOWS
        name = ResolveFieldNameByIndex(t_serializer, t_fieldIndex, &leafRec);
#else
        name = IsUserPtr(t_fieldPath) ? ResolveFieldName(t_serializer, t_fieldPath, &leafRec) : nullptr;
#endif
        Resolved r;
        if (name != nullptr && leafRec != nullptr)
        {
            r.leafRec = leafRec;
            r.type    = ClassifyLeaf(leafRec);
            r.kind    = KindForType(r.type);
            r.name    = name;
        }
        rit = g_resolve.emplace(ResolveKey{t_serializer, t_fieldToken}, std::move(r)).first;
    }

    const Resolved& rf = rit->second;
    if (rf.leafRec == nullptr || rf.kind < 0)
        return g_origBitCopy(dst, src, bitcount);

    auto it = fieldMap->find(std::string_view(rf.name));
    if (it == fieldMap->end())
        return g_origBitCopy(dst, src, bitcount);

    void*     leafRec = rf.leafRec;
    int       kind    = rf.kind;
    FieldType type    = rf.type;

    FieldBatch& batch = it->second;

    // First BitCopy of this (entity, field) in the tick: fire ONE callback that fills the per-slot table;
    // every other receiver this tick reads it below with no further managed call.
    int tick = gpGlobals->nTickCount;
    if (batch.tick != tick)
    {
        batch.tick    = tick;
        batch.kind    = kind;
        batch.hasMask = 0;
        g_dispatching = true;
        forwards::OnSendProxyBatch->Invoke(t_entityIdx, rf.name.c_str(), kind, &batch);
        g_dispatching = false;

        // Encode each DISTINCT overridden value ONCE; every slot points at its blob (reused for equal values).
        batch.encodeLeafRec = leafRec;
        batch.blobCount     = 0;
        for (int s = 0; s < kMaxSlots; s++)
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
            else if (batch.blobCount < kMaxDistinct
                     && EncodeToBlob(leafRec, type, batch.values[s], batch.blobData[batch.blobCount], kBlobCap, batch.blobBits[batch.blobCount]))
            {
                batch.slotBlob[s] = static_cast<int8_t>(batch.blobCount++);
            }
            else
            {
                batch.slotBlob[s] = -1; // encode failed or pool full — this slot gets the real value.
            }
        }
    }

    int     slot   = reinterpret_cast<CServerSideClient*>(t_client)->GetSlot();
    uint8_t result = 0;
    bool    substituted = false;
    // Only substitute the exact leaf the blobs were encoded with — a same-named leaf elsewhere in the tree can
    // use a different encoder, and its blob would be malformed bits.
    if (slot >= 0 && slot < kMaxSlots && (batch.hasMask & (1ull << slot)) != 0 && leafRec == batch.encodeLeafRec)
    {
        int bi = batch.slotBlob[slot];
        if (bi >= 0 && bi < batch.blobCount)
        {
            result      = SpliceBlob(dst, src, bitcount, batch.blobData[bi], batch.blobBits[bi]);
            substituted = true;
        }
    }

    // `batch`/`it` may be erased by a deferred Unhook the callback requested — done using them now.
    FlushPending();

    return substituted ? result : g_origBitCopy(dst, src, bitcount);
}

// ── Encoder enumeration (for ClassifyLeaf) ───────────────────────────────────────────────────────────────
FieldType ClassifyEntry(int bucket, const char* name)
{
    auto eq  = [&](const char* s) { return name != nullptr && strcmp(name, s) == 0; };
    bool def = eq("default");
    switch (bucket)
    {
        case 1: return def ? FieldType::Int32 : eq("fixed32") ? FieldType::Fixed32 : eq("fixed64") ? FieldType::Fixed64 : FieldType::Unsupported;
        case 2: return def ? FieldType::UInt32 : eq("fixed32") ? FieldType::Fixed32 : eq("fixed64") ? FieldType::Fixed64 : FieldType::Unsupported;
        case 3:
            return def                                                               ? FieldType::QuantizedFloat
                   : (eq("qangle") || eq("qangle_pitch_yaw") || eq("qangle_precise")) ? FieldType::QAngle3
                   : eq("normal")                                                     ? FieldType::Normal3
                   : eq("coord")                                                      ? FieldType::Coord3
                   : eq("coord_integral")                                             ? FieldType::CoordIntegral3
                                                                                      : FieldType::Unsupported;
        case 4: return def ? FieldType::Float32 : FieldType::Unsupported;
        case 5: return def ? FieldType::String : FieldType::Unsupported;
        case 7: return def ? FieldType::Bool : FieldType::Unsupported;
        default: return FieldType::Unsupported;
    }
}

void BuildEncoderMap()
{
    auto registry = g_pGameData->GetAddress<uintptr_t>("CFlattenedSerializer::EncoderRegistry");
    if (!IsUserPtr(registry))
    {
        WARN("SendProxy: EncoderRegistry not resolved — field classification disabled.");
        return;
    }

    const char* keys[] = {
        "CFlattenedSerializer::EncoderBucket1", "CFlattenedSerializer::EncoderBucket2",
        "CFlattenedSerializer::EncoderBucket3", "CFlattenedSerializer::EncoderBucket4",
        "CFlattenedSerializer::EncoderBucket5", "CFlattenedSerializer::EncoderBucket6",
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
            if (type != FieldType::Unsupported)
                g_encoderTypes.emplace(fn, type);
        }
    }
}

// ── Natives ──────────────────────────────────────────────────────────────────────────────────────────────
void RecomputeHasAny()
{
    for (auto* p : g_hooks)
    {
        if (p != nullptr)
        {
            g_hasAnyHook = true;
            return;
        }
    }
    g_hasAnyHook = false;
}

void SendProxyHookField(int entityIndex, const char* field)
{
    if (entityIndex <= 0 || entityIndex >= kMaxEdicts || field == nullptr)
        return;
    if (g_hooks[entityIndex] == nullptr)
        g_hooks[entityIndex] = new FieldMap();
    g_hooks[entityIndex]->try_emplace(field);
    g_hasAnyHook = true;
}

bool SendProxyUnhookField(int entityIndex, const char* field)
{
    if (entityIndex <= 0 || entityIndex >= kMaxEdicts || field == nullptr)
        return false;
    if (g_dispatching)
    {
        g_pendingUnhook.emplace_back(entityIndex, field);
        return true;
    }
    if (g_hooks[entityIndex] == nullptr)
        return false;
    bool removed = g_hooks[entityIndex]->erase(field) > 0;
    if (g_hooks[entityIndex]->empty())
    {
        delete g_hooks[entityIndex];
        g_hooks[entityIndex] = nullptr;
        RecomputeHasAny();
    }
    return removed;
}

void SendProxyClearEntity(int entityIndex)
{
    if (entityIndex <= 0 || entityIndex >= kMaxEdicts)
        return;
    if (g_dispatching)
    {
        g_pendingClear.push_back(entityIndex);
        return;
    }
    if (g_hooks[entityIndex] == nullptr)
        return;
    delete g_hooks[entityIndex];
    g_hooks[entityIndex] = nullptr;
    RecomputeHasAny();
}

void FlushPending()
{
    if (g_pendingUnhook.empty() && g_pendingClear.empty())
        return;

    // g_dispatching is false here, so these run the real erase path.
    auto unhook = std::move(g_pendingUnhook);
    auto clear  = std::move(g_pendingClear);
    g_pendingUnhook.clear();
    g_pendingClear.clear();

    for (auto& [entityIndex, field] : unhook)
        SendProxyUnhookField(entityIndex, field.c_str());
    for (int entityIndex : clear)
        SendProxyClearEntity(entityIndex);
}

class SendProxyEntityListener : public IEntityListener
{
public:
    void OnEntityCreated(CBaseEntity*) override {}
    void OnEntityDeleted(CBaseEntity* pEntity) override
    {
        if (g_hasAnyHook)
            SendProxyClearEntity(pEntity->GetEntityIndex());
    }
    void OnEntitySpawned(CBaseEntity*) override {}
    void OnEntityFollowed(CBaseEntity*, CBaseEntity*) override {}
} static s_entityListener;

template <typename Fn>
bool InstallDetour(SafetyHookInline& hook, const char* key, Fn detour)
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
} // namespace

namespace natives::sendproxy
{
void Init()
{
    bridge::CreateNative("SendProxy.HookField", reinterpret_cast<void*>(SendProxyHookField));
    bridge::CreateNative("SendProxy.UnhookField", reinterpret_cast<void*>(SendProxyUnhookField));
    bridge::CreateNative("SendProxy.ClearEntity", reinterpret_cast<void*>(SendProxyClearEntity));
}
} // namespace natives::sendproxy

void InstallSendProxyHooks()
{
    BuildEncoderMap();
    if (g_encoderTypes.empty())
    {
        WARN("SendProxy: no encoders classified — SendProxy disabled.");
        return;
    }

    if (!InstallDetour(g_perClientHook, "CNetworkGameServer::PerClientEncode", PerClientEncode_Detour)
        || !InstallDetour(g_wdeHook, "CNetworkGameServerBase::WriteDeltaEntity_Internal", WriteDeltaEntity_Detour)
        || !InstallDetour(g_wflHook, "CFlattenedSerializer::WriteFieldList", WriteFieldList_Detour))
    {
        WARN("SendProxy: capture hooks incomplete — SendProxy disabled.");
        return;
    }

    // Field identity: linux hooks the standalone GetBitRange (CFieldPath* in rdi); Windows inlines it, so hook
    // the WriteFieldList inner site (field index in r12) instead.
#ifdef PLATFORM_WINDOWS
    auto fieldPathAddr = g_pGameData->GetAddress<void*>("CFlattenedSerializer::WriteFieldList_FieldPathSite");
    auto fieldPathFn   = WindowsFieldIndexHook;
    const char* fieldPathKey = "WriteFieldList_FieldPathSite";
#else
    auto fieldPathAddr = g_pGameData->GetAddress<void*>("CFlattenedSerializer::GetBitRange");
    auto fieldPathFn   = GetBitRange_Mid;
    const char* fieldPathKey = "GetBitRange";
#endif
    if (!IsUserPtr(fieldPathAddr))
    {
        WARN("SendProxy: %s not resolved — SendProxy disabled.", fieldPathKey);
        return;
    }
    if (auto mid = safetyhook::MidHook::create(fieldPathAddr, fieldPathFn))
    {
        g_bitRangeHook = std::move(*mid);
        g_pHookManager->Register(&g_bitRangeHook);
    }
    else
    {
        WARN("SendProxy: field-path mid-hook failed: %s", g_szMidFuncHookErrors[mid.error().type]);
        return;
    }

    auto bitCopyAddr = g_pGameData->GetAddress<void*>("CFlattenedSerializer::BitCopyPrimitive");
    if (!IsUserPtr(bitCopyAddr))
    {
        WARN("SendProxy: BitCopyPrimitive not resolved — SendProxy disabled.");
        return;
    }
    auto bc = safetyhook::InlineHook::create(bitCopyAddr, reinterpret_cast<void*>(BitCopy_Detour));
    if (!bc)
    {
        WARN("SendProxy: failed to hook BitCopyPrimitive: %s", g_szInlineHookErrors[bc.error().type]);
        return;
    }
    g_bitCopyHook = std::move(*bc);
    g_origBitCopy = g_bitCopyHook.original<uint8_t (*)(void*, void*, uint32_t)>();
    g_pHookManager->Register(&g_bitCopyHook);

    g_pGameEntitySystem->AddListenerEntity(&s_entityListener);

    // Serializers (and their leaf records) are rebuilt on a map change; drop the resolve cache so a reused
    // serializer pointer can't map an old token to the wrong field.
    g_pHookManager->Hook_GameDeactivate(HookType_Post, [] { g_resolve.clear(); });

    FLOG("SendProxy: per-client hooks installed (%zu encoder types classified).", g_encoderTypes.size());
}
