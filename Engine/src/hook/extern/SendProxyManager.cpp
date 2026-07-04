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

constexpr int kMaxSlots = 64;

// Per-slot override values filled by ONE batched callback per (entity, field) per tick, then served to every
// receiver in that tick's per-client loop with no further managed call. `tick` is stamped from
// gpGlobals->nTickCount so the first BitCopy of a (entity, field) in a tick refills it and the rest reuse it.
struct FieldBatch
{
    int32_t        tick = -1;
    int32_t        type = 0; // FieldType, filled by native before the callback
    uint64_t       hasMask = 0;
    SendProxyValue values[kMaxSlots]{};
};

using FieldMap = std::unordered_map<std::string, FieldBatch, string_hash, std::equal_to<>>;

// ── Registration store: pointer array indexed by entity index. All access is on the main thread
//    (registration natives, the per-client encode, and entity-delete cleanup all run there). ────────────────
FieldMap* g_hooks[kMaxEdicts] = {};
bool      g_hasAnyHook        = false;

// Encoder fn -> FieldType, built once at install from the encoder-registry bucket table. Read-only after.
std::unordered_map<uintptr_t, FieldType> g_encoderTypes;

// Per-thread captures. Only the main thread's copies are ever read at a substitution (gated on recipient).
thread_local void* t_client     = nullptr;
thread_local int   t_entityIdx  = -1;
thread_local void* t_serializer = nullptr;
thread_local void* t_fieldPath  = nullptr;

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

// Encode the overridden value into a scratch bf_write via the field's own encoder, then emit those bits into
// dst through the original BitCopy after skipping the real value in src. Returns false to pass the real value.
uint8_t (*g_origBitCopy)(void* dst, void* src, uint32_t bits) = nullptr;

// Returns true if the fake value was emitted (outResult holds the engine's BitCopy return); false means
// "pass the real value" and the caller must do the original copy.
bool Substitute(void* leafRec, FieldType type, const SendProxyValue& v, void* dst, void* src, uint32_t bitcount, uint8_t& outResult)
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
            return false; // encoder expects params but the base is unreadable — send the real value.
        paramsPtr = reinterpret_cast<uint8_t*>(base) + paramOff;
    }

    // Build the value in the layout this encoder reads.
    uint8_t scratch[0x30]{};

    // String encoder reads *valuePtr as a char*; point it at the inline (null-terminated) buffer. Handled
    // separately from the numeric scratch because its value pointer is a pointer-to-pointer.
    if (type == FieldType::String)
    {
        const char* strPtr = v.str;
        // Ensure null-terminated within bounds even if strLen is bogus.
        if (v.strLen < 0 || v.strLen >= static_cast<int>(sizeof(v.str)))
            return false;

        void*   sv          = const_cast<char*>(strPtr);
        uint8_t data[512]{};
        uint8_t bw[0x40]{};
        *reinterpret_cast<void**>(bw + 0x00) = data;
        *reinterpret_cast<int*>(bw + 0x08)   = sizeof(data);
        *reinterpret_cast<int*>(bw + 0x0C)   = sizeof(data) * 8;
        *reinterpret_cast<int*>(bw + 0x10)   = 0;

        encFn(bw, fieldInfo, paramsPtr, &sv, 0);

        int     encodedBits = *reinterpret_cast<int*>(bw + 0x10);
        uint8_t overflow    = *(bw + 0x20);
        if (overflow != 0 || encodedBits <= 0)
            return false;

        *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(src) + 0x10) += static_cast<int>(bitcount);
        *reinterpret_cast<int*>(bw + 0x10) = 0;
        outResult                          = g_origBitCopy(dst, bw, static_cast<uint32_t>(encodedBits));
        return true;
    }

    switch (type)
    {
        case FieldType::UInt32:
            *reinterpret_cast<uint64_t*>(scratch) = static_cast<uint32_t>(v.i);
            break;
        case FieldType::Int32:
        case FieldType::Int64:
        case FieldType::Fixed32:
        case FieldType::Fixed64:
            *reinterpret_cast<int64_t*>(scratch) = v.i;
            break;
        case FieldType::Bool:
            scratch[0] = v.i != 0 ? 1 : 0;
            break;
        case FieldType::Float32:
            *reinterpret_cast<double*>(scratch) = v.f;
            break;
        case FieldType::QAngle3:
        case FieldType::Vector3:
            reinterpret_cast<float*>(scratch)[0] = v.x;
            reinterpret_cast<float*>(scratch)[1] = v.y;
            reinterpret_cast<float*>(scratch)[2] = v.z;
            break;
        default:
            // Quantized/coord/normal need the field's count/mode word from the live value; not supported for
            // per-client without a live-value read. Pass through rather than emit a wrong quantization.
            return false;
    }

    // Scratch bf_write (data +0x00, nDataBytes +0x08, nDataBits +0x0c, cursor +0x10, overflow +0x20, flag +0x22).
    constexpr int kBound   = 64;
    constexpr int kBfSize  = 0x40;
    uint8_t       data[kBound]{};
    uint8_t       bw[kBfSize]{};
    *reinterpret_cast<void**>(bw + 0x00) = data;
    *reinterpret_cast<int*>(bw + 0x08)   = kBound;
    *reinterpret_cast<int*>(bw + 0x0C)   = kBound * 8;
    *reinterpret_cast<int*>(bw + 0x10)   = 0;

    encFn(bw, fieldInfo, paramsPtr, scratch, 0);

    int     encodedBits = *reinterpret_cast<int*>(bw + 0x10);
    uint8_t overflow    = *(bw + 0x20);
    if (overflow != 0 || encodedBits <= 0)
        return false;

    // Skip the real value in src, rewind scratch, copy the fake bits (propagating the engine's return so an
    // over-long substitution that overflows dst is reported, not masked).
    *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(src) + 0x10) += static_cast<int>(bitcount);
    *reinterpret_cast<int*>(bw + 0x10) = 0;
    outResult = g_origBitCopy(dst, bw, static_cast<uint32_t>(encodedBits));
    return true;
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
    t_fieldPath = reinterpret_cast<void*>(ctx.rdi);
}

uint8_t BitCopy_Detour(void* dst, void* src, uint32_t bitcount)
{
    // Substitute only during a per-client send. t_client is a thread_local set only by the per-client encode
    // (main thread); it is null on the shared-pack worker threads, so this check gates everything below to the
    // main thread — no lock needed for g_hasAnyHook / g_hooks, which are only touched there.
    if (t_client == nullptr || !g_hasAnyHook || !IsUserPtr(t_serializer) || t_entityIdx < 0 || t_entityIdx >= kMaxEdicts || !IsUserPtr(t_fieldPath))
        return g_origBitCopy(dst, src, bitcount);

    auto* fieldMap = g_hooks[t_entityIdx];
    if (fieldMap == nullptr)
        return g_origBitCopy(dst, src, bitcount);

    void*       leafRec   = nullptr;
    const char* fieldName = ResolveFieldName(t_serializer, t_fieldPath, &leafRec);
    if (fieldName == nullptr || leafRec == nullptr)
        return g_origBitCopy(dst, src, bitcount);

    auto it = fieldMap->find(std::string_view(fieldName));
    if (it == fieldMap->end())
        return g_origBitCopy(dst, src, bitcount);

    FieldType type = ClassifyLeaf(leafRec);
    int       kind = KindForType(type);
    if (kind < 0)
        return g_origBitCopy(dst, src, bitcount);

    FieldBatch& batch = it->second;

    // First BitCopy of this (entity, field) in the tick: fire ONE callback that fills the per-slot table;
    // every other receiver this tick reads it below with no further managed call.
    int tick = gpGlobals->nTickCount;
    if (batch.tick != tick)
    {
        batch.tick    = tick;
        batch.type    = kind;
        batch.hasMask = 0;
        forwards::OnSendProxyBatch->Invoke(t_entityIdx, fieldName, kind, &batch);
    }

    int slot = *reinterpret_cast<int*>(reinterpret_cast<uint8_t*>(t_client) + 0x48);
    if (slot < 0 || slot >= kMaxSlots || (batch.hasMask & (1ull << slot)) == 0)
        return g_origBitCopy(dst, src, bitcount);

    uint8_t result;
    if (Substitute(leafRec, type, batch.values[slot], dst, src, bitcount, result))
        return result;

    return g_origBitCopy(dst, src, bitcount);
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
    if (entityIndex <= 0 || entityIndex >= kMaxEdicts || field == nullptr || g_hooks[entityIndex] == nullptr)
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
    if (entityIndex <= 0 || entityIndex >= kMaxEdicts || g_hooks[entityIndex] == nullptr)
        return;
    delete g_hooks[entityIndex];
    g_hooks[entityIndex] = nullptr;
    RecomputeHasAny();
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
#ifdef PLATFORM_WINDOWS
    // Windows inlines GetBitRange, so the field path must be resolved from a field index via a serializer
    // leaf-DFS (as the standalone plugin does but never verified on hardware). Not ported — per-client override
    // is Linux-only until that path is added and tested. Don't install unverified detours for zero function.
    WARN("SendProxy: per-client override is Linux-only for now (Windows field-path resolution not implemented).");
    return;
#else
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

    auto bitRangeAddr = g_pGameData->GetAddress<void*>("CFlattenedSerializer::GetBitRange");
    if (!IsUserPtr(bitRangeAddr))
    {
        WARN("SendProxy: GetBitRange not resolved — SendProxy disabled.");
        return;
    }
    if (auto mid = safetyhook::MidHook::create(bitRangeAddr, GetBitRange_Mid))
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

    FLOG("SendProxy: per-client hooks installed (%zu encoder types classified).", g_encoderTypes.size());
#endif
}
