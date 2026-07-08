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

#include "address.h"
#include "bridge/natives/SendProxyManager.h"
#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "manager/HookManager.h"
#include "memory/zydis_utility.h"
#include "module.h"
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

#include <Zydis.h>
#include <safetyhook.hpp>

// SendProxy — per-client net-var override during the flattened-serializer encode. Managed code registers
// (entity, field) via natives; the shared snapshot pack stays untouched and each recipient's stream is
// corrected at the per-client BitCopy stage, which runs on the main thread.

// Installed lazily on the first Hook so a server not using SendProxy pays nothing on the per-client encode.
static bool InstallSendProxyHooks();

static bool g_installed     = false;
static bool g_installFailed = false;

// Pack-context entity-index offset, resolved once in InstallSendProxyHooks (before any detour is installed).
// Read by BOTH entity-index detours (WriteDeltaEntity_Internal + WriteEnterPVS), so it lives at file scope.
static int g_offEntityIndex = -1;

// SendProxy struct offsets auto-resolved from the binary in InstallSendProxyHooks (before any encode), so a game
// update that shifts these layouts needs no gamedata edit. Each falls back to its gamedata literal if the
// disassembly anchor is gone (never a silent 0); -1 = unresolved. The typed accessors below read these.
// bf_write scratch + bf_read cursor — derived together from CFlattenedSerializer::BitCopyPrimitive.
static int g_offBfWriteData     = -1;
static int g_offBfWriteByteCap  = -1;
static int g_offBfWriteBitCap   = -1;
static int g_offBfWriteCurBit   = -1;
static int g_offBfWriteOverflow = -1;
static int g_offBfReadCurBit    = -1;
// WriteFieldList field descriptor (arg5) — derived from CFlattenedSerializer::WriteFieldList.
static int g_offWriteInfoBitTable   = -1;
static int g_offWriteInfoSnapshot   = -1;
static int g_offWriteInfoFieldCount = -1;

// Offset-drift safety. The boot-resolved offsets are trusted over the gamedata literals, so before ANY bit is
// substituted they must prove themselves once against live encode data — else a wrong derivation on a future game
// build would silently corrupt every hooked client's stream. Until verified, real bits pass through untouched;
// a failed check latches substitution off with a loud warning. One-time, so no steady-state cost.
static bool g_offsetsVerified = false;
static bool g_offsetsBad      = false;

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
constexpr int MAX_DISTINCT = MAX_SLOTS; // 64 slots => at most 64 distinct values, so the blob pool can never overflow
constexpr int BLOB_CAP     = 320;       // max encoded field size (string ≤ 256 + slack)

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
static SafetyHookInline g_enterPvsHook{};
static SafetyHookInline g_wflHook{};
static SafetyHookInline g_bitCopyHook{};

// ─── Typed accessors over the engine's flattened-serializer structures ───
// Each reads its layout from the SendProxy_* gamedata offsets (same names/values as before — only the
// access is named now), mirroring the house pattern in cstrike/type/CServerSideClient.h so the scattered
// +offset math stays out of the substitution logic.

// A flattened-serializer tree node, walked in the same DFS order as the bit-offset table.
struct SpSerializerNode
{
    uint8_t* base;
    explicit SpSerializerNode(void* p) : base(reinterpret_cast<uint8_t*>(p)) {}
    [[nodiscard]] int FieldCount() const
    {
        static auto off = g_pGameData->GetOffset("SendProxy_Serializer_FieldCount");
        return *reinterpret_cast<int*>(base + off);
    }
    [[nodiscard]] void* FieldsArray() const
    {
        static auto off = g_pGameData->GetOffset("SendProxy_Serializer_FieldsArray");
        return *reinterpret_cast<void**>(base + off);
    }
    static int FieldStride()
    {
        static auto off = g_pGameData->GetOffset("SendProxy_Serializer_FieldStride");
        return off;
    }
};

// One field record inside a node's array. record[0] == fieldInfo; a non-null Child means a sub-serializer.
struct SpSerializerField
{
    uint8_t* rec;
    explicit SpSerializerField(void* p) : rec(reinterpret_cast<uint8_t*>(p)) {}
    [[nodiscard]] void* Child() const
    {
        static auto off = g_pGameData->GetOffset("SendProxy_SerializerField_Child");
        return *reinterpret_cast<void**>(rec + off);
    }
    [[nodiscard]] void* FieldInfo() const { return *reinterpret_cast<void**>(rec); }
};

// FieldInfo: encoder dispatch / params / name for a leaf field.
struct SpFieldInfo
{
    uint8_t* base;
    explicit SpFieldInfo(void* p) : base(reinterpret_cast<uint8_t*>(p)) {}
    [[nodiscard]] char* Name() const
    {
        static auto off = g_pGameData->GetOffset("SendProxy_FieldInfo_Name");
        return *reinterpret_cast<char**>(base + off);
    }
    [[nodiscard]] void* EncoderDispatch() const
    {
        static auto off = g_pGameData->GetOffset("SendProxy_FieldInfo_EncoderDispatch");
        return *reinterpret_cast<void**>(base + off);
    }
    [[nodiscard]] void* EncoderBase() const
    {
        static auto off = g_pGameData->GetOffset("SendProxy_FieldInfo_EncoderBase");
        return *reinterpret_cast<void**>(base + off);
    }
    [[nodiscard]] uint8_t ParamOffset() const
    {
        static auto off = g_pGameData->GetOffset("SendProxy_FieldInfo_ParamOffset");
        return *(base + off);
    }
};

// WriteFieldList's per-call field descriptor (detour arg5): source snapshot + start-bit table + field count.
struct SpWriteInfo
{
    uint8_t* base;
    explicit SpWriteInfo(void* p) : base(reinterpret_cast<uint8_t*>(p)) {}
    [[nodiscard]] const int32_t* BitOffsetTable() const { return *reinterpret_cast<const int32_t**>(base + g_offWriteInfoBitTable); }
    [[nodiscard]] void*          SnapshotBuffer() const { return *reinterpret_cast<void**>(base + g_offWriteInfoSnapshot); }
    [[nodiscard]] int32_t        FieldCount() const { return *reinterpret_cast<int32_t*>(base + g_offWriteInfoFieldCount); }
};

// Per-entity pack context (arg2 of the delta / EnterPVS writers). The entity-index displacement is resolved
// at load into g_offEntityIndex (ResolveEntityIndexOffset, gamedata fallback), so it's read via that, not a name.
struct SpPackContext
{
    uint8_t* base;
    explicit SpPackContext(void* p) : base(reinterpret_cast<uint8_t*>(p)) {}
    [[nodiscard]] int EntityIndex() const { return *reinterpret_cast<int*>(base + g_offEntityIndex); }
};

// Local bf_write scratch we build to drive the engine encoder — layout mirrors the engine's bf_write.
struct SpBfWrite
{
    uint8_t* base;
    explicit SpBfWrite(void* p) : base(reinterpret_cast<uint8_t*>(p)) {}
    void SetData(const void* d) { *reinterpret_cast<const void**>(base + g_offBfWriteData) = d; }
    void SetByteCap(int v) { *reinterpret_cast<int*>(base + g_offBfWriteByteCap) = v; }
    void SetBitCap(int v) { *reinterpret_cast<int*>(base + g_offBfWriteBitCap) = v; }
    void SetCurBit(int v) { *reinterpret_cast<int*>(base + g_offBfWriteCurBit) = v; }
    [[nodiscard]] int     CurBit() const { return *reinterpret_cast<int*>(base + g_offBfWriteCurBit); }
    [[nodiscard]] uint8_t Overflow() const { return *(base + g_offBfWriteOverflow); }
};

// Engine bf_read cursor on the snapshot buffer — only the read-position field is needed.
struct SpBfRead
{
    uint8_t* base;
    explicit SpBfRead(void* p) : base(reinterpret_cast<uint8_t*>(p)) {}
    [[nodiscard]] int CurBit() const { return *reinterpret_cast<int*>(base + g_offBfReadCurBit); }
    void              Advance(int bits) { *reinterpret_cast<int*>(base + g_offBfReadCurBit) += bits; }
};

// Walks leaf records in the same flattened DFS order as the bit-offset table, so `target` equals the cursor-matched field index.
static void* WalkToLeafRec(void* serializer, int& current, int target, int depth)
{
    if (depth > 4 || !IsUserPtr(serializer))
        return nullptr;

    const SpSerializerNode node(serializer);
    const int              count = node.FieldCount();
    if (count <= 0 || count > 4096)
        return nullptr;

    void* arr = node.FieldsArray();
    if (!IsUserPtr(arr))
        return nullptr;

    const int stride = SpSerializerNode::FieldStride();
    for (int i = 0; i < count; i++)
    {
        void* rec   = reinterpret_cast<uint8_t*>(arr) + i * stride;
        void* child = SpSerializerField(rec).Child();
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

    void* fieldInfo = SpSerializerField(rec).FieldInfo();
    if (!IsUserPtr(fieldInfo))
        return nullptr;

    *leafRecOut = rec;
    auto name   = SpFieldInfo(fieldInfo).Name();
    if (!IsUserPtr(name))
        return nullptr;
    // A real field name starts with an identifier char. If SendProxy_FieldInfo_Name drifts, this reads a garbage
    // pointer; the leading-byte check plus the exact match against the hooked-field set below makes an accidental
    // hit effectively impossible, so a wrong Name offset degrades to "no substitution", never a wrong one.
    const char c = *name;
    if (c != '_' && !(c >= 'A' && c <= 'Z') && !(c >= 'a' && c <= 'z'))
        return nullptr;
    return name;
}

static SpFieldType ClassifyLeaf(void* leafRec)
{
    if (!IsUserPtr(leafRec))
        return SpFieldType::Unsupported;
    void* fieldInfo = SpSerializerField(leafRec).FieldInfo();
    if (!IsUserPtr(fieldInfo))
        return SpFieldType::Unsupported;
    void* dispatch = SpFieldInfo(fieldInfo).EncoderDispatch();
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
    void* fieldInfo = SpSerializerField(leafRec).FieldInfo();
    if (!IsUserPtr(fieldInfo))
        return false;
    const SpFieldInfo info(fieldInfo);
    void*             dispatch = info.EncoderDispatch();
    if (!IsUserPtr(dispatch))
        return false;
    auto encFn = *reinterpret_cast<SpEncodeFn*>(dispatch);
    if (!IsUserPtr(reinterpret_cast<void*>(encFn)))
        return false;

    uint8_t paramOff  = info.ParamOffset();
    void*   paramsPtr = nullptr;
    // Plausibility guard: a real param offset is either the 0xFF "no params" sentinel or a small index into the
    // params blob. A value in between means the ParamOffset/EncoderBase layout is off — refuse rather than build a
    // bogus params pointer and hand it to the engine encoder. Returning false leaves the real value in the stream.
    if (paramOff != 0xFF && paramOff >= 0x80)
        return false;
    if (paramOff != 0xFF)
    {
        void* base = info.EncoderBase();
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

    // Local bf_write scratch we build to drive the engine encoder (layout mirrors the engine's bf_write).
    uint8_t   data[512]{};
    uint8_t   bwBuf[0x40]{};
    SpBfWrite bw(bwBuf);
    bw.SetData(data);
    bw.SetByteCap(sizeof(data));
    bw.SetBitCap(sizeof(data) * 8);
    bw.SetCurBit(0);

    encFn(bwBuf, fieldInfo, paramsPtr, valuePtr, 0);

    int     encodedBits = bw.CurBit();
    uint8_t overflow    = bw.Overflow();
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
    uint8_t   bwBuf[0x40]{};
    SpBfWrite bw(bwBuf);
    bw.SetData(blob);
    bw.SetByteCap((bits + 7) / 8);
    bw.SetBitCap(bits);
    bw.SetCurBit(0);

    SpBfRead(src).Advance(static_cast<int>(bitcount)); // skip the real value in the source stream
    return g_origBitCopy(dst, bwBuf, static_cast<uint32_t>(bits));
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

// CNetworkGameServer::PerClientEncode(this, recipient, ..): arg1 is the server (this), arg2 the CServerSideClient
// recipient we capture as t_client. The remaining args are the engine's per-frame OO-PVS encode context —
// pFrameSnapshot (the shared packed frame), pClientFrame (this client's frame state), pPackBuffers (the pack/scratch
// buffers, member read at +0x30), and transmitFlags (a flags word passed in r9d); all opaque to the substitution
// path and forwarded to the original verbatim.
static void* Detour_PerClientEncode(void* pServer, void* pClient, void* pFrameSnapshot, void* pClientFrame, void* pPackBuffers, void* transmitFlags)
{
    // The whole substitution path assumes main-thread execution (no locks on g_pHooks/g_resolve) — assert it.
    AssertBool(g_nMainThreadId == static_cast<uint64_t>(GetCurrentThreadId()));

    t_client   = pClient;
    auto* orig = g_perClientHook.original<void* (*)(void*, void*, void*, void*, void*, void*)>();
    auto  ret  = orig(pServer, pClient, pFrameSnapshot, pClientFrame, pPackBuffers, transmitFlags);
    t_client   = nullptr;
    return ret;
}

// CNetworkGameServerBase::WriteDeltaEntity_Internal(server, entityCtx, ..): arg1 is the server (this), arg2 the
// per-entity pack context we read the entity index from. pFrameSnapshot is consumed by the engine but opaque to us;
// pBaselineData/pWriteContext/writeOptions are the trailing delta-write context the function itself never reads (RE
// verified) — all forwarded to the original verbatim.
static void* Detour_WriteDeltaEntity(void* pServer, void* pEntityCtx, void* pFrameSnapshot, void* pBaselineData, void* pWriteContext, void* writeOptions)
{
    int prev = t_entityIdx;
    if (IsUserPtr(pEntityCtx))
        t_entityIdx = SpPackContext(pEntityCtx).EntityIndex();
    auto* orig  = g_wdeHook.original<void* (*)(void*, void*, void*, void*, void*, void*)>();
    auto  ret   = orig(pServer, pEntityCtx, pFrameSnapshot, pBaselineData, pWriteContext, writeOptions);
    t_entityIdx = prev;
    return ret;
}

// The from-baseline writer for a client's initial full snapshot. It never enters WriteDeltaEntity_Internal, so
// without this t_entityIdx stays -1 and BitCopy passes real bits through. pEntityCtx is the SAME per-entity ctx the
// delta writer gets (entity index at +0x34, dst bf_write at +0x88 — both paths write the same per-client buffer).
// void(server, entityCtx): both call sites set only rdi/rsi and discard the return (verified in libengine2).
static void Detour_WriteEnterPVS(void* pServer, void* pEntityCtx)
{
    int prev = t_entityIdx;
    if (IsUserPtr(pEntityCtx))
        t_entityIdx = SpPackContext(pEntityCtx).EntityIndex();
    auto* orig = g_enterPvsHook.original<void (*)(void*, void*)>();
    orig(pServer, pEntityCtx);
    t_entityIdx = prev;
}

// CFlattenedSerializer::WriteFieldList(serializer, dstBitWrite, fieldList, fieldContext, fieldDesc, ..). arg1 is the
// serializer being written; pFieldDesc (arg5) is the per-call field descriptor (start-bit table + source snapshot +
// field count) we read. The others are the engine's write context, opaque to the substitution path and forwarded
// verbatim: pDstBitWrite (destination bf_write), pFieldList (CUtlVector of field records to write, {data@+0x10,
// count@+8}), pFieldContext (per-write context, count read at +0x10), nFieldFlags/nFieldCount (words in r9d/arg7),
// pFieldPathFilter (an optional int*: null-checked, its int compared), and nWriteFlags (trailing flags word).
static void* Detour_WriteFieldList(void* pSerializer, void* pDstBitWrite, void* pFieldList, void* pFieldContext, void* pFieldDesc, uint32_t nFieldFlags, uint32_t nFieldCount, void* pFieldPathFilter, uint32_t nWriteFlags)
{
    void*          prevSer   = t_serializer;
    const int32_t* prevTable = t_bitTable;
    void*          prevData  = t_srcData;
    int            prevCount = t_fieldCount;

    t_serializer = pSerializer;
    if (IsUserPtr(pFieldDesc))
    {
        const SpWriteInfo info(pFieldDesc);
        t_bitTable   = info.BitOffsetTable();
        t_srcData    = info.SnapshotBuffer();
        t_fieldCount = info.FieldCount();
    }
    else
    {
        // Clear, not leave stale — else this frame resolves against the outer frame's table + a fresh serializer.
        t_bitTable   = nullptr;
        t_srcData    = nullptr;
        t_fieldCount = 0;
    }

    auto* orig = g_wflHook.original<void* (*)(void*, void*, void*, void*, void*, uint32_t, uint32_t, void*, uint32_t)>();
    auto  ret  = orig(pSerializer, pDstBitWrite, pFieldList, pFieldContext, pFieldDesc, nFieldFlags, nFieldCount, pFieldPathFilter, nWriteFlags);

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

    // Prove the boot-resolved offsets against live data before ever substituting (once). V3: the src cursor
    // advances by exactly bitcount → BfRead/BfWrite_CurBit is right. V1: the serializer walk yields one leaf per
    // bit-table entry → the tree offsets + t_fieldCount (hence WriteInfo_FieldCount) are right. A wrong offset
    // fails one of these → substitution stays off (inert), never corrupts. Latches on the first hooked field.
    if (g_offsetsBad)
        return g_origBitCopy(dst, src, bitcount);
    if (!g_offsetsVerified)
    {
        const int     before = SpBfRead(src).CurBit();
        const uint8_t out    = g_origBitCopy(dst, src, bitcount);
        const int     after  = SpBfRead(src).CurBit();
        if (after - before != static_cast<int>(bitcount))
        {
            g_offsetsBad = true;
            WARN("SendProxy: bf cursor offset failed live check (advanced %d, expected %u) — SendProxy_Bf* offsets look drifted; substitution disabled.", after - before, bitcount);
            return out;
        }
        int leaves = 0;
        WalkToLeafRec(t_serializer, leaves, 0x7fffffff, 0);
        if (t_fieldCount > 0 && leaves == t_fieldCount)
        {
            g_offsetsVerified = true; // proven consistent — substitute from the next field on
        }
        else
        {
            g_offsetsBad = true;
            WARN("SendProxy: serializer walk yielded %d leaves for %d fields — SendProxy_Serializer/WriteInfo offsets look drifted; substitution disabled.", leaves, t_fieldCount);
        }
        return out; // this field passes through real; substitution begins once verified
    }

    int index = ResolveFieldIndex(SpBfRead(src).CurBit());
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

    static auto bucketCountOff = g_pGameData->GetOffset("SendProxy_EncoderBucket_Count");
    static auto entryFuncOff    = g_pGameData->GetOffset("SendProxy_EncoderEntry_Func");
    static auto entryNameOff    = g_pGameData->GetOffset("SendProxy_EncoderEntry_Name");
    static auto bucketStride    = g_pGameData->GetOffset("SendProxy_EncoderBucket_Stride");
    static auto entryStride      = g_pGameData->GetOffset("SendProxy_EncoderEntry_Stride");

    for (int i = 0; i < 7; i++)
    {
        int bucket = i + 1;
        // Bucket 6 (byte-array) has no supported value kind, so it needs no type entry.
        if (bucket == 6)
            continue;

        auto handler = g_pGameData->GetAddress<uintptr_t>(keys[i]);
        if (!IsUserPtr(handler))
            continue;

        int count = *reinterpret_cast<int*>(registry + bucket * bucketStride + bucketCountOff);
        if (count <= 0 || count > 32)
            continue;

        for (int e = 0; e < count; e++)
        {
            auto entry = handler + static_cast<uintptr_t>(e) * entryStride;
            auto fn    = *reinterpret_cast<uintptr_t*>(entry + entryFuncOff);
            if (!IsUserPtr(fn))
                continue;
            auto name = *reinterpret_cast<char**>(entry + entryNameOff);
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

// Auto-derive the pack-context entity-index offset from libengine2 so a game update that shifts it needs no
// gamedata edit. Anchor: CNetworkGameServerBase::WriteEnterPVS reads the entity index as the FIRST dword load
// from its entityCtx arg (arg1) right in the prologue — `mov r32, [arg1 + 0x34]` — then immediately masks it
// with 0x3ff to index the edict-chunk table, a stable Valve idiom. Returns the displacement, or -1 if the
// anchor is gone (the caller then falls back to the gamedata literal). Verified derived==52 on both the current
// libengine2.so and engine2.dll; this is the only SendProxy offset with an unambiguous single-instruction anchor
// — the rest stay on gamedata because a wrong displacement here corrupts the stream silently rather than crashing.
static int ResolveEntityIndexOffset()
{
    const auto addr = g_pGameData->GetAddress<uintptr_t>("CNetworkGameServerBase::WriteEnterPVS");
    if (!IsUserPtr(addr))
        return -1;

    const auto* range = modules::engine->GetFunctionRange(addr);
    if (range == nullptr)
        return -1;

#ifdef PLATFORM_WINDOWS
    constexpr auto kArg1 = ZYDIS_REGISTER_RDX; // WriteEnterPVS(server=rcx, entityCtx=rdx)
#else
    constexpr auto kArg1 = ZYDIS_REGISTER_RSI; // WriteEnterPVS(server=rdi, entityCtx=rsi)
#endif

    // Bound the scan to the prologue: the read is at +0x17/+0x19, so a match past this window would be an
    // unrelated arg1 access on a shifted binary — better to miss (and fall back) than to grab a wrong offset.
    const uintptr_t windowEnd = range->start + 0x60;
    const uintptr_t scanEnd    = windowEnd < range->end ? windowEnd : range->end;

    int result = -1;
    ZydisUtility::ScanInstructions(range->start, scanEnd, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
        // first `mov r32, dword [entityCtx + disp]`
        if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
            && instr.operand_count_visible == 2
            && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
            && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
            && operands[1].size == 32
            && ZydisUtility::GetBaseRegister(operands[1].mem.base) == kArg1
            && operands[1].mem.index == ZYDIS_REGISTER_NONE
            && operands[1].mem.disp.has_displacement
            && operands[1].mem.disp.value > 0
            && operands[1].mem.disp.value < 0x1000)
        {
            result = static_cast<int>(operands[1].mem.disp.value);
            return true;
        }
        return false;
    });

    return result;
}

// Commits a boot-derived offset into `slot`: prefer the value auto-resolved from the binary (zero-touch across
// game updates), fall back to the gamedata literal if the anchor is gone, and WARN on any mismatch so a stale
// gamedata value is visible. Never leaves a silent 0 — the gamedata path FatalErrors if the key is absent.
static void AssignOffset(int& slot, int resolved, const char* gdKey)
{
    int        gd     = 0;
    const bool haveGd = g_pGameData->GetOffset(gdKey, &gd);
    if (resolved >= 0)
    {
        if (haveGd && resolved != gd)
            WARN("SendProxy: %s auto-resolved=%d differs from gamedata=%d — using resolved (gamedata literal may be stale).", gdKey, resolved, gd);
        slot = resolved;
    }
    else
    {
        WARN("SendProxy: %s auto-resolve failed — falling back to gamedata.", gdKey);
        slot = g_pGameData->GetOffset(gdKey);
    }
}

// Derive the engine bitbuf (CBitWrite/CBitRead) field offsets from CFlattenedSerializer::BitCopyPrimitive so a
// game update that shifts the bitbuf layout needs no gamedata edit. BitCopy(dst, src, bitCount) treats its src
// arg (a register on both platforms — reg-copied here, never spilled) as a bitbuf and reads: data ptr at +0
// (first qword load), byte cap at +8 (dword compare), bit cap at +0xC (first dword load), cur-bit cursor at
// +0x10 (first dword store from a register), overflow flag at +0x20 (byte store of an immediate). The scratch
// bf_write we build shares this exact layout and the snapshot bf_read's cursor is the same +0x10, so one pass
// yields all six. Verified derived==gamedata on both libnetworksystem.so and networksystem.dll; any field the
// scan misses stays -1 and falls back to gamedata.
static void ResolveBitBufOffsets(int& data, int& byteCap, int& bitCap, int& curBit, int& overflow)
{
    data = byteCap = bitCap = curBit = overflow = -1;

    const auto addr = g_pGameData->GetAddress<uintptr_t>("CFlattenedSerializer::BitCopyPrimitive");
    if (!IsUserPtr(addr))
        return;
    const auto* range = modules::network->GetFunctionRange(addr);
    if (range == nullptr)
        return;

#ifdef PLATFORM_WINDOWS
    constexpr auto kSrc = ZYDIS_REGISTER_RDX; // BitCopy(dst=rcx, src=rdx, bitCount=r8)
#else
    constexpr auto kSrc = ZYDIS_REGISTER_RSI; // BitCopy(dst=rdi, src=rsi, bitCount=edx)
#endif

    std::unordered_set<ZydisRegister> src{kSrc};
    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* ops) {
        // store into the src struct: cursor (dword from a register) / overflow flag (byte immediate)
        if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && instr.operand_count_visible == 2 && ops[0].type == ZYDIS_OPERAND_TYPE_MEMORY
            && ops[0].mem.index == ZYDIS_REGISTER_NONE && src.contains(ZydisUtility::GetBaseRegister(ops[0].mem.base)))
        {
            const auto disp = static_cast<int>(ops[0].mem.disp.value);
            if (ops[0].size == 32 && ops[1].type == ZYDIS_OPERAND_TYPE_REGISTER && curBit < 0)
                curBit = disp;
            else if (ops[0].size == 8 && ops[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE && overflow < 0)
                overflow = disp;
        }
        // load from the src struct: data ptr (qword) / bit cap (dword) via MOV, byte cap via CMP
        if (instr.operand_count_visible == 2 && ops[1].type == ZYDIS_OPERAND_TYPE_MEMORY
            && ops[1].mem.index == ZYDIS_REGISTER_NONE && src.contains(ZydisUtility::GetBaseRegister(ops[1].mem.base)))
        {
            const auto disp = static_cast<int>(ops[1].mem.disp.value);
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && ops[0].type == ZYDIS_OPERAND_TYPE_REGISTER)
            {
                if (ops[1].size == 64 && data < 0)
                    data = disp;
                else if (ops[1].size == 32 && bitCap < 0)
                    bitCap = disp;
            }
            else if (instr.mnemonic == ZYDIS_MNEMONIC_CMP && ops[1].size == 32 && byteCap < 0)
                byteCap = disp;
        }
        // follow reg-copies of src; drop a tracked reg when it is overwritten by a non-copy
        if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && instr.operand_count_visible == 2 && ops[0].type == ZYDIS_OPERAND_TYPE_REGISTER)
        {
            const auto dst = ZydisUtility::GetBaseRegister(ops[0].reg.value);
            if (ops[1].type == ZYDIS_OPERAND_TYPE_REGISTER && src.contains(ZydisUtility::GetBaseRegister(ops[1].reg.value)))
                src.insert(dst);
            else if (ops[1].type != ZYDIS_OPERAND_TYPE_REGISTER)
                src.erase(dst);
        }
        return false; // scan the whole function
    });
}

// Derive the WriteFieldList field-descriptor (arg5) offsets from CFlattenedSerializer::WriteFieldList so a game
// update that shifts them needs no gamedata edit. In its per-field write loop the function reads, off arg5: the
// start-bit table pointer (its result is indexed with a scale-4 subscript), the snapshot source buffer, and the
// field count (a dword compare). arg5 is a register on Linux (r8) and a stack argument on Windows (recovered from
// the frame); we track its reg-copies and stack spills through the function and classify the reads. Verified
// derived==gamedata on both libnetworksystem.so and networksystem.dll; if the full {table, snapshot, count} shape
// isn't recovered unambiguously the outputs stay -1 and each falls back to gamedata.
static void ResolveWriteInfoOffsets(int& table, int& snapshot, int& count)
{
    table = snapshot = count = -1;

    const auto addr = g_pGameData->GetAddress<uintptr_t>("CFlattenedSerializer::WriteFieldList");
    if (!IsUserPtr(addr))
        return;
    const auto* range = modules::network->GetFunctionRange(addr);
    if (range == nullptr)
        return;

    std::unordered_set<ZydisRegister>      argRegs;   // registers currently aliasing arg5
    std::unordered_set<int64_t>            argSlots;  // rbp/rsp-relative slots holding arg5
    std::unordered_map<ZydisRegister, int> tableCand; // reg -> disp it was loaded from off arg5 (bit-table candidate)
    std::unordered_set<int>                qwords;    // non-zero qword-read displacements off arg5

#ifdef PLATFORM_WINDOWS
    // arg5 is the 5th (first stack) argument: [entry_rsp + 0x28] becomes [rbp + N*8 + 0x28] once the frame ptr is
    // set by `lea rbp, [rsp - D]` after N pushes. Recover its rbp-relative slot from the prologue.
    {
        int  pushes = 0;
        bool found  = false;
        ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* ops) {
            if (instr.mnemonic == ZYDIS_MNEMONIC_PUSH)
                pushes++;
            else if (instr.mnemonic == ZYDIS_MNEMONIC_LEA && ops[0].type == ZYDIS_OPERAND_TYPE_REGISTER && ops[0].reg.value == ZYDIS_REGISTER_RBP
                     && ops[1].type == ZYDIS_OPERAND_TYPE_MEMORY && ops[1].mem.base == ZYDIS_REGISTER_RSP)
            {
                argSlots.insert(-ops[1].mem.disp.value + 8LL * pushes + 0x28);
                found = true;
                return true;
            }
            return false;
        });
        if (!found)
            return;
    }
#else
    argRegs.insert(ZYDIS_REGISTER_R8); // WriteFieldList(serializer=rdi, .., arg5=r8, ..)
#endif

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* ops) {
        // reads off arg5: qword field (table candidate / snapshot) via MOV, field count via dword CMP
        if (instr.operand_count_visible == 2 && ops[1].type == ZYDIS_OPERAND_TYPE_MEMORY
            && ops[1].mem.index == ZYDIS_REGISTER_NONE && argRegs.contains(ZydisUtility::GetBaseRegister(ops[1].mem.base)))
        {
            const auto disp = static_cast<int>(ops[1].mem.disp.value);
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && ops[0].type == ZYDIS_OPERAND_TYPE_REGISTER && ops[1].size == 64 && disp != 0)
            {
                tableCand[ZydisUtility::GetBaseRegister(ops[0].reg.value)] = disp;
                qwords.insert(disp);
            }
            else if (instr.mnemonic == ZYDIS_MNEMONIC_CMP && ops[1].size == 32 && count < 0)
                count = disp;
        }
        // a scale-4 index off a table candidate confirms the bit-offset table
        for (int i = 0; i < instr.operand_count_visible; i++)
        {
            if (ops[i].type == ZYDIS_OPERAND_TYPE_MEMORY && ops[i].mem.scale == 4 && ops[i].mem.index != ZYDIS_REGISTER_NONE)
            {
                auto it = tableCand.find(ZydisUtility::GetBaseRegister(ops[i].mem.base));
                if (it != tableCand.end() && table < 0)
                    table = it->second;
            }
        }
        // propagate arg5 through reg-copies and stack spills/reloads
        if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && instr.operand_count_visible == 2)
        {
            if (ops[0].type == ZYDIS_OPERAND_TYPE_REGISTER)
            {
                const auto dst = ZydisUtility::GetBaseRegister(ops[0].reg.value);
                const bool fromReg  = ops[1].type == ZYDIS_OPERAND_TYPE_REGISTER && argRegs.contains(ZydisUtility::GetBaseRegister(ops[1].reg.value));
                const bool fromSlot = ops[1].type == ZYDIS_OPERAND_TYPE_MEMORY && ops[1].mem.index == ZYDIS_REGISTER_NONE
                                      && (ops[1].mem.base == ZYDIS_REGISTER_RBP || ops[1].mem.base == ZYDIS_REGISTER_RSP)
                                      && argSlots.contains(ops[1].mem.disp.value);
                // a qword field load off arg5 makes dst a fresh table candidate (set just above) — keep it; it holds
                // the table pointer, not arg5 itself, so it still leaves argRegs.
                const bool loadedField = ops[1].type == ZYDIS_OPERAND_TYPE_MEMORY && ops[1].mem.index == ZYDIS_REGISTER_NONE
                                         && argRegs.contains(ZydisUtility::GetBaseRegister(ops[1].mem.base));
                if (fromReg || fromSlot)
                    argRegs.insert(dst);
                else
                {
                    argRegs.erase(dst);
                    if (!loadedField)
                        tableCand.erase(dst); // reassigned to a non-arg5 value — invalidate any stale table pointer
                }
            }
            else if (ops[0].type == ZYDIS_OPERAND_TYPE_MEMORY && ops[0].mem.index == ZYDIS_REGISTER_NONE
                     && (ops[0].mem.base == ZYDIS_REGISTER_RBP || ops[0].mem.base == ZYDIS_REGISTER_RSP)
                     && ops[1].type == ZYDIS_OPERAND_TYPE_REGISTER && argRegs.contains(ZydisUtility::GetBaseRegister(ops[1].reg.value)))
                argSlots.insert(ops[0].mem.disp.value); // spill arg5 to stack
        }
        return false;
    });

    // The bit-offset table is the *4-indexed qword read; the snapshot is the single OTHER non-zero qword read.
    if (table >= 0)
        qwords.erase(table);
    if (qwords.size() == 1)
        snapshot = *qwords.begin();

    // Ship only a complete, unambiguous shape; otherwise fall back to gamedata for every field.
    if (table < 0 || snapshot < 0 || count < 0)
        table = snapshot = count = -1;
}

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

    // Resolve once here (before the detours below are installed) so both entity-index detours read a plain int.
    // Prefer the value auto-derived from libengine2 (zero-touch across game updates); fall back to the gamedata
    // literal if the disassembly anchor is gone, and WARN on any mismatch so a stale gamedata value is visible.
    {
        int        gd       = 0;
        const bool haveGd   = g_pGameData->GetOffset("SendProxy_PackContext_EntityIndex", &gd);
        const int  resolved = ResolveEntityIndexOffset();
        if (resolved > 0)
        {
            if (haveGd && resolved != gd)
                WARN("SendProxy: PackContext_EntityIndex auto-resolved=%d differs from gamedata=%d — using resolved (gamedata literal may be stale).", resolved, gd);
            g_offEntityIndex = resolved;
        }
        else
        {
            // Anchor missing — fall back to gamedata (FatalErrors if that too is absent; never a silent 0).
            WARN("SendProxy: PackContext_EntityIndex auto-resolve failed — falling back to gamedata.");
            g_offEntityIndex = g_pGameData->GetOffset("SendProxy_PackContext_EntityIndex");
        }
    }

    // Auto-resolve the bitbuf + WriteInfo struct offsets from the binary too (same zero-touch-across-updates goal;
    // AssignOffset prefers the derived value, falls back to gamedata if the anchor is gone, and WARNs on mismatch).
    // Safe to prefer the derived value because Detour_BitCopy gates all substitution on a one-time live check of
    // these offsets — a wrong derivation makes SendProxy inert, not corrupt (g_offsetsVerified/g_offsetsBad).
    {
        int data = -1, byteCap = -1, bitCap = -1, curBit = -1, overflow = -1;
        ResolveBitBufOffsets(data, byteCap, bitCap, curBit, overflow);
        AssignOffset(g_offBfWriteData, data, "SendProxy_BfWrite_Data");
        AssignOffset(g_offBfWriteByteCap, byteCap, "SendProxy_BfWrite_ByteCap");
        AssignOffset(g_offBfWriteBitCap, bitCap, "SendProxy_BfWrite_BitCap");
        AssignOffset(g_offBfWriteCurBit, curBit, "SendProxy_BfWrite_CurBit");
        AssignOffset(g_offBfWriteOverflow, overflow, "SendProxy_BfWrite_Overflow");
        AssignOffset(g_offBfReadCurBit, curBit, "SendProxy_BfRead_CurBit"); // same cursor field as bf_write

        int table = -1, snapshot = -1, fieldCount = -1;
        ResolveWriteInfoOffsets(table, snapshot, fieldCount);
        AssignOffset(g_offWriteInfoBitTable, table, "SendProxy_WriteInfo_BitOffsetTable");
        AssignOffset(g_offWriteInfoSnapshot, snapshot, "SendProxy_WriteInfo_SnapshotBuffer");
        AssignOffset(g_offWriteInfoFieldCount, fieldCount, "SendProxy_WriteInfo_FieldCount");
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

    // Best-effort: extends override coverage to a client's initial from-baseline snapshot. Unlike the trio above
    // we don't gate on it — if the sig fails to resolve the delta path is unaffected (late joiners just miss the
    // override on their first snapshot). TryInstallDetour already WARNs the specifics on failure.
    TryInstallDetour(g_enterPvsHook, "CNetworkGameServerBase::WriteEnterPVS", Detour_WriteEnterPVS);

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
