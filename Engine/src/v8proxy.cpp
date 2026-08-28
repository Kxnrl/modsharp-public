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

#include "v8proxy.h"
#include "address.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "memory/zydis_utility.h"
#include "module.h"
#include "sdkproxy.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/type/Vector.h"

#include <unordered_map>
#include <vector>

static thread_local std::vector<v8::Local<v8::Value>> s_HandleTable;

uint32_t PushHandle(v8::Local<v8::Value> value)
{
    s_HandleTable.push_back(value);
    return static_cast<uint32_t>(s_HandleTable.size() - 1);
}

v8::Local<v8::Value> ResolveHandle(uint32_t handle)
{
    if (handle >= s_HandleTable.size())
        return {};

    return s_HandleTable[handle];
}

int32_t HandleFrameBegin()
{
    return static_cast<int32_t>(s_HandleTable.size());
}

void HandleFrameEnd(int32_t base)
{
    if (base >= 0 && static_cast<size_t>(base) <= s_HandleTable.size())
        s_HandleTable.resize(static_cast<size_t>(base));
}

void PurgeHandles()
{
    s_HandleTable.clear();
}

const ScriptWrapper* GetWrapperFromThis(const v8::FunctionCallbackInfo<v8::Value>& info)
{
    v8::Local<v8::Object> self = info.This();

    if (self->InternalFieldCount() != 3)
        return nullptr;

    return static_cast<const ScriptWrapper*>(self->GetAlignedPointerFromInternalField(1));
}

CBaseEntity* GetEntityFromThis(const v8::FunctionCallbackInfo<v8::Value>& info)
{
    const auto* wrapper = GetWrapperFromThis(info);
    if (!wrapper || wrapper->type != static_cast<int32_t>(ScriptWrapperType::Entity))
        return nullptr;

    CBaseHandle handle(wrapper->handle);
    if (!handle.IsValid())
        return nullptr;

    // FindEntityByEHandle re-validates the serial (the engine does the same via
    // `cmp [rax+10h], ecx`), so a forged handle resolves to null rather than garbage.
    return g_pGameEntitySystem->FindEntityByEHandle(handle);
}

bool V8ToNumber(v8::Local<v8::Value> val, double& out)
{
    if (!val->IsNumber())
        return false;

    out = val.As<v8::Number>()->Value();
    return true;
}

bool V8ToVector(v8::Local<v8::Context> ctx, v8::Local<v8::Value> val, Vector& out)
{
    if (!val->IsObject() || val->IsArray())
        return false;

    v8::Isolate*          isolate = ctx->GetIsolate();
    v8::Local<v8::Object> obj     = val.As<v8::Object>();

    v8::Local<v8::Value> vx{}, vy{}, vz{};
    double               x{}, y{}, z{};
    if (!obj->Get(ctx, v8::String::NewFromUtf8Literal(isolate, "x")).ToLocal(&vx) || !V8ToNumber(vx, x)) return false;
    if (!obj->Get(ctx, v8::String::NewFromUtf8Literal(isolate, "y")).ToLocal(&vy) || !V8ToNumber(vy, y)) return false;
    if (!obj->Get(ctx, v8::String::NewFromUtf8Literal(isolate, "z")).ToLocal(&vz) || !V8ToNumber(vz, z)) return false;

    out.Init(static_cast<float>(x), static_cast<float>(y), static_cast<float>(z));
    return true;
}

static bool TryGetEntityHandle(v8::Local<v8::Value> val, EHandle_t& out)
{
    if (val.IsEmpty() || !val->IsObject())
        return false;

    auto obj = val.As<v8::Object>();
    if (obj->InternalFieldCount() != 3)
        return false;

    const auto* wrapper = static_cast<const ScriptWrapper*>(obj->GetAlignedPointerFromInternalField(1));
    if (wrapper == nullptr || wrapper->type != static_cast<int32_t>(ScriptWrapperType::Entity))
        return false;

    out = wrapper->handle;
    return true;
}

void V8ValueToScript(v8::Isolate* isolate, v8::Local<v8::Context> ctx, v8::Local<v8::Value> val, ScriptValue& out, char* strBuf, int strBufSize)
{
    out.type = ScriptValueType::Null;

    if (val.IsEmpty() || val->IsNullOrUndefined())
        return;

    if (val->IsBoolean())
    {
        out.type = ScriptValueType::Bool;
        out.b    = val.As<v8::Boolean>()->Value();
        return;
    }

    if (val->IsNumber())
    {
        out.type   = ScriptValueType::Number;
        out.number = val.As<v8::Number>()->Value();
        return;
    }

    if (val->IsString())
    {
        out.type = ScriptValueType::String;
        if (strBuf == nullptr || strBufSize <= 0)
        {
            out.str = nullptr;
            return;
        }

        auto      str = val.As<v8::String>();
        const int len = str->Utf8Length(isolate);
        if (len < strBufSize)
        {
            str->WriteUtf8(isolate, strBuf, strBufSize);
            strBuf[strBufSize - 1] = 0;
            out.str                = strBuf;
        }
        else
        {
            // Oversized: fall back to a growable thread-local so a valid string is never truncated. Same
            // synchronous-consume lifetime as strBuf — the caller copies out.str before the next marshal.
            static thread_local std::string overflow;
            overflow.resize(static_cast<size_t>(len) + 1);
            str->WriteUtf8(isolate, overflow.data(), static_cast<int>(overflow.size()));
            out.str = overflow.c_str();
        }
        return;
    }

    EHandle_t handle;
    if (TryGetEntityHandle(val, handle))
    {
        out.type   = ScriptValueType::Entity;
        out.entity = handle;
        return;
    }

    // Arrays are ALWAYS Array handles — a JS array is never a Vector (Valve's Vector is the object
    // {x,y,z}). Checked before the Vector probe so a numeric array round-trips as an Array.
    if (val->IsArray())
    {
        out.type   = ScriptValueType::Array;
        out.handle = PushHandle(val);
        return;
    }

    Vector vec;
    if (V8ToVector(ctx, val, vec))
    {
        out.type   = ScriptValueType::Vector;
        out.vec[0] = vec.x;
        out.vec[1] = vec.y;
        out.vec[2] = vec.z;
        return;
    }

    if (val->IsObject())
    {
        out.type   = ScriptValueType::Object;
        out.handle = PushHandle(val);
        return;
    }

    WARN("V8ValueToScript -> unsupported val type");
}

bool ResolveObjectHandle(uint32_t handle, v8::Local<v8::Object>& out)
{
    auto value = ResolveHandle(handle);
    if (value.IsEmpty() || !value->IsObject())
        return false;

    out = value.As<v8::Object>();
    return true;
}

bool ResolveArrayHandle(uint32_t handle, v8::Local<v8::Array>& out)
{
    auto value = ResolveHandle(handle);
    if (value.IsEmpty() || !value->IsArray())
        return false;

    out = value.As<v8::Array>();
    return true;
}

// The factory returns v8::Local<v8::Object>. MSVC returns that non-trivial aggregate through a
// hidden pointer (first arg); the Itanium ABI returns the single pointer member in rax (the
// dispatcher does `return factory(&symbol, entity)`).
#ifdef PLATFORM_WINDOWS
using ScriptEntityWrapperFn = v8::Object** (*)(v8::Object** out, const char** className, CBaseEntity* entity);
#else
using ScriptEntityWrapperFn = v8::Object* (*)(const char** className, CBaseEntity* entity);
#endif
using GetCurrentScriptFn = CBaseEntity* (*)();
using ScriptStackPushFn  = void (*)(void* scriptSubObject);
using ScriptStackPopFn   = void (*)();
using MakeGlobalSymbolFn = const char* (*)(const char* name);

static ScriptEntityWrapperFn s_CreateEntityWrapper = nullptr;
static GetCurrentScriptFn    s_GetCurrentScript    = nullptr;
static ScriptStackPushFn     s_ScriptStackPush     = nullptr;
static ScriptStackPopFn      s_ScriptStackPop      = nullptr;
static MakeGlobalSymbolFn    s_MakeGlobalSymbol    = nullptr;

const char* InternGlobalSymbol(const char* name)
{
    if (s_MakeGlobalSymbol == nullptr) [[unlikely]]
        return nullptr;

    return s_MakeGlobalSymbol(name);
}

// The point_script's CCSScript_EntityScript is COMPOSED into the entity at a fixed offset. It is NOT a
// base class, so the getter hands us the sub-object,
// not the entity. Discover the composition offset once by scanning a live point_script for the
// CCSScript_EntityScript vtable via the module RTTI table (IsPointerDerivedFrom reads a single qword
// and looks it up — never follows a garbage vtable), then cache it. SIZE_MAX = not resolved yet.
static size_t s_CsScriptSubobjectOffset = static_cast<size_t>(-1);

// Resolve the entity->JS-wrapper factory by behavior, not a byte signature: the engine's
// generic entity dispatcher references all three JS class-name literals, and inside it the factory is the
// only internal function called exactly twice — once per class-name branch (behavior is
// codegen-stable where a byte pattern is not). IAT/PLT calls are rejected as non-local (tier0's
// MakeGlobalSymbol is also called twice on some codegens); a direct tail jmp leaving the function body is
// counted as a call (GCC compiles `return factory(...)` to a jmp, invisible to ResolveCallTarget).
static uintptr_t ExtractWrapperFactory(CModule* mod, uintptr_t dispatch)
{
    const auto* range = mod->GetFunctionRange(dispatch);
    if (range == nullptr)
    {
        WARN("cs_script: dispatcher candidate server+%llx has no function range, skipping",
             static_cast<uint64_t>(dispatch - mod->Base()));
        return 0;
    }

    // Shape gate: the dispatcher is a small branch-and-call stub (~0x80 bytes);
    // the class-template registration body — which matches the same string refs on some
    // builds — runs thousands of bytes. Same thin-function idiom as
    // GetInitV8ClassTemplatesAddress; 0x400 leaves an order of magnitude on each side.
    constexpr uintptr_t kDispatcherMaxSize = 0x400;

    const auto size = static_cast<uintptr_t>(range->end - range->start);
    if (size > kDispatcherMaxSize)
    {
        FLOG("cs_script: dispatcher candidate server+%llx skipped (0x%llx bytes > 0x%llx)",
             static_cast<uint64_t>(dispatch - mod->Base()),
             static_cast<uint64_t>(size),
             static_cast<uint64_t>(kDispatcherMaxSize));
        return 0;
    }

    std::unordered_map<uintptr_t, int> callCounts;

    ZydisUtility::ScanInstructions(range->start, range->end,
                                   [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
                                       auto target = ZydisUtility::ResolveCallTarget(&instr, operands, ip);

                                       if (target == 0 && instr.mnemonic == ZYDIS_MNEMONIC_JMP
                                           && operands[0].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
                                       {
                                           if (const auto jmp = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                                               jmp != 0 && (jmp < range->start || jmp >= range->end))
                                               target = jmp;
                                       }

                                       if (target != 0 && mod->IsInModule(target))
                                           ++callCounts[target];

                                       return false;
                                   });

    uintptr_t twiceCalled = 0;
    int       candidates  = 0;
    for (const auto& [target, count] : callCounts)
    {
        if (count == 2)
        {
            twiceCalled = target;
            ++candidates;
        }
    }

    FLOG("cs_script: dispatcher candidate server+%llx (0x%llx bytes): %d twice-called local targets",
         static_cast<uint64_t>(dispatch - mod->Base()),
         static_cast<uint64_t>(size),
         candidates);

    return candidates == 1 ? twiceCalled : 0;
}

static uintptr_t GetCreateEntityWrapperAddress()
{
    auto* mod = modules::server;

    // The string-ref anchor can match more than one function (the class-template registration
    // body references the same class-name literals). Analyze
    // every match; accept only if all analyzable candidates agree on one factory.
    {
        uintptr_t factory       = 0;
        int       factoryOwners = 0;

        for (const auto dispatch : mod->FindAllFunctionsFromStringRefs({"CSPlayerController", "PointTemplate", "Entity"}))
        {
            const auto candidate = ExtractWrapperFactory(mod, dispatch);
            if (candidate == 0)
                continue;

            if (factory != 0 && factory != candidate)
            {
                // two dispatcher candidates disagree on the factory — refuse to guess
                factoryOwners = 2;
                break;
            }

            factory = candidate;
            ++factoryOwners;
        }

        if (factory != 0 && factoryOwners == 1)
        {
            FLOG("Found cs_script entity wrapper factory at server+%llx", static_cast<uint64_t>(factory - mod->Base()));
            return factory;
        }

        WARN("cs_script: entity factory not uniquely identified by Zydis (%d owners), falling back to signature", factoryOwners);
    }

    // Fallback: on_demand gamedata signatures. Query only the key THIS platform authors — GetAddress
    // FErrors on a missing key, and a single-platform entry is dropped on the other platform at parse.
    // Windows authors the factory directly; Linux the dispatcher (factory derived from it).
#ifdef PLATFORM_WINDOWS
    if (uintptr_t address = 0; g_pGameData != nullptr && g_pGameData->GetAddress("CCSScript::CreateEntityWrapper", address))
        return address;
#else
    if (uintptr_t dispatcher = 0; g_pGameData != nullptr && g_pGameData->GetAddress("CCSScript::CreateEntityWrapperDispatcher", dispatcher))
    {
        if (const auto factory = ExtractWrapperFactory(mod, dispatcher); factory != 0)
        {
            FLOG("Found cs_script entity wrapper factory at server+%llx (via gamedata dispatcher signature)",
                 static_cast<uint64_t>(factory - mod->Base()));
            return factory;
        }
    }
#endif

    return 0;
}

// V8 is linked shared (USING_V8_SHARED), but much of the API we depend on is inlined
// from these headers instead of dispatched through the DLL: GetAlignedPointerFromInternalField
// compiles down to a bare [obj+0x17] / [obj+0x1F] load, guarded by instance-type
// constants baked in at compile time. If the game ships a v8.dll from a different
// revision, those inlined offsets read the wrong memory and hand back garbage — no
// crash, no error, no log. Pin the version so a mismatch fails loudly at startup
// rather than silently corrupting every unwrap.
static bool CheckV8Version()
{
    const auto* runtime = v8::V8::GetVersion();
    if (runtime == nullptr)
    {
        WARN("v8::V8::GetVersion() returned null");
        return false;
    }

    // Parse the leading "major.minor.build.patch"; GetVersion() may append a suffix.
    int  parsed[4] = {0, 0, 0, 0};
    auto cursor    = runtime;

    for (auto& value : parsed)
    {
        if (*cursor < '0' || *cursor > '9')
        {
            WARN("Unparsable V8 version string '%s'", runtime);
            return false;
        }

        // Bound the digit count. Real V8 components are tiny, and an unbounded
        // accumulate would be signed overflow (UB) on a malformed string. Nine digits
        // keeps the running value below INT_MAX with room to spare.
        int digits = 0;

        while (*cursor >= '0' && *cursor <= '9')
        {
            if (++digits > 9)
            {
                WARN("Implausible V8 version component in '%s'", runtime);
                return false;
            }

            value = value * 10 + (*cursor++ - '0');
        }

        if (*cursor == '.')
            ++cursor;
    }

    if (parsed[0] != V8_MAJOR_VERSION || parsed[1] != V8_MINOR_VERSION
        || parsed[2] != V8_BUILD_NUMBER || parsed[3] != V8_PATCH_LEVEL)
    {
        WARN("V8 version mismatch: headers are %d.%d.%d.%d but v8.dll reports '%s'. "
             "Inlined V8 accessors would silently read wrong offsets.",
             V8_MAJOR_VERSION,
             V8_MINOR_VERSION,
             V8_BUILD_NUMBER,
             V8_PATCH_LEVEL,
             runtime);
        return false;
    }

    FLOG("V8 runtime version '%s' matches headers", runtime);

    return true;
}

// Resolve the current-script getter (indexes the global current-script stack; its result is the
// point_script that owns the running call). Zydis-first and cross-platform: every cs_script method
// callback opens with
//     call v8::Isolate::GetCurrent
//     call v8::HandleScope::HandleScope    (1st resolvable call after GetCurrent)
//     call <current-script getter>         (2nd — its result is dynamic_cast to
//                                           CCSScript_EntityScript, else "invoked in incorrect scope")
// This layout is shared across entity- and point_script-method wrappers. Take that 2nd call
// from every "Calling %s.%s"
// wrapper and vote: the getter is shared by ~all of them, so it wins outright and one odd wrapper
// cannot swing it. A signature belongs in gamedata (server.games.jsonc), never hard-coded here.
static uintptr_t GetCurrentScriptAddress()
{
    auto* mod = modules::server;

    if (const auto wrappers = mod->FindAllFunctionsFromStringRefs({"Calling %s.%s\n"}); !wrappers.empty())
    {
        const auto v8GetCurrent = reinterpret_cast<uintptr_t>(v8::Isolate::GetCurrent);

        std::unordered_map<uintptr_t, int> votes;

        for (const auto wrapper : wrappers)
        {
            const auto* range = mod->GetFunctionRange(wrapper);
            if (range == nullptr)
                continue;

            // -1 until the GetCurrent call is seen; then count resolvable calls after it:
            // 1 = HandleScope ctor, 2 = the current-script getter.
            int       resolved  = -1;
            uintptr_t candidate = 0;

            ZydisUtility::ScanInstructions(range->start, range->end,
                                           [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
                                               const auto target = ZydisUtility::ResolveCallTarget(&instr, operands, ip);
                                               if (target == 0)
                                                   return false;

                                               if (resolved < 0)
                                               {
                                                   if (target == v8GetCurrent)
                                                       resolved = 0;

                                                   return false;
                                               }

                                               if (++resolved == 2)
                                               {
                                                   candidate = target;

                                                   return true;
                                               }

                                               return false;
                                           });

            if (candidate != 0)
                ++votes[candidate];
        }

        uintptr_t getter    = 0;
        int       bestVotes = 0;
        bool      tied      = false;
        for (const auto& [target, count] : votes)
        {
            if (count > bestVotes)
            {
                getter    = target;
                bestVotes = count;
                tied      = false;
            }
            else if (count == bestVotes)
            {
                tied = true;
            }
        }

        // A tied top vote is ambiguous — fail closed to the gamedata fallback rather than bind a
        // nondeterministic getter (mirrors ResolveScriptStackFns::pickBest).
        if (getter != 0 && !tied)
        {
            FLOG("Found cs_script current-script getter at server+%llx (%d/%llu wrappers agree)",
                 static_cast<uint64_t>(getter - mod->Base()),
                 bestVotes,
                 static_cast<uint64_t>(wrappers.size()));

            return getter;
        }

        WARN("cs_script: current-script getter not resolved by Zydis, trying gamedata fallback");
    }

    // Fallback: on_demand gamedata signature (both platforms authored, so GetAddress returns false
    // gracefully on drift rather than hitting the fatal missing-key path).
    uintptr_t address = 0;
    if (g_pGameData != nullptr && g_pGameData->GetAddress("CCSScript::GetCurrentScript", address))
        return address;

    return 0;
}

// Current-script stack push/pop — for invoking a persisted JS callback with its owning script made
// current, the way the engine's own dispatchers do. This is the same stack the getter above reads:
// push is a CUtlVector AddToTail taking the script sub-object;
// pop just decrements the depth. Bracketing func->Call with push…pop lets any script
// API the callback re-enters resolve to the right script (matches CCSScript's method dispatcher).

// The current-script getter opens with `mov eax, [rip+depth]`; return that global's address (its
// first RIP-relative memory operand). Push and pop are the only other functions that touch it, which
// is how we pick them out of the dispatcher below.
static uintptr_t ExtractCurrentScriptDepthGlobal(CModule* mod, uintptr_t getter)
{
    const auto* range = mod->GetFunctionRange(getter);
    if (range == nullptr)
        return 0;

    uintptr_t depthGlobal = 0;
    ZydisUtility::ScanInstructions(range->start, range->end,
                                   [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
                                       // The depth load is `mov eax, [rip+depth]` — a 32-bit dword read.
                                       // Require MOV + 32-bit width so an earlier 64-bit RIP load (e.g. a /GS
                                       // security-cookie fetch) or a non-load RIP operand is not mistaken for it.
                                       if (instr.mnemonic != ZYDIS_MNEMONIC_MOV || instr.operand_width != 32)
                                           return false;

                                       for (int i = 0; i < instr.operand_count; ++i)
                                       {
                                           const auto& op = operands[i];
                                           if (op.type == ZYDIS_OPERAND_TYPE_MEMORY && op.mem.base == ZYDIS_REGISTER_RIP)
                                           {
                                               const auto abs = ZydisUtility::GetAbsoluteAddress(instr, op, ip);
                                               if (abs != 0)
                                               {
                                                   depthGlobal = abs;
                                                   return true;
                                               }
                                           }
                                       }
                                       return false;
                                   });
    return depthGlobal;
}

enum class ScriptStackFnKind
{
    None,
    Push,
    Pop,
};

// Classify a dispatcher callee by how it WRITES the depth global. Pop's only depth access is the
// decrement (`dec [depth]` on MSVC, `sub dword [depth], 1` on GCC); push (AddToTail) grows the
// count with any other write; the getter (and anything else) only reads it, so read-only accesses
// classify as None rather than polluting the push votes.
static ScriptStackFnKind ClassifyScriptStackFn(CModule* mod, uintptr_t fn, uintptr_t depthGlobal)
{
    const auto* range = mod->GetFunctionRange(fn);
    if (range == nullptr)
        return ScriptStackFnKind::None;

    bool sawDecrementWrite = false;
    bool sawOtherWrite     = false;
    ZydisUtility::ScanInstructions(range->start, range->end,
                                   [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
                                       for (int i = 0; i < instr.operand_count; ++i)
                                       {
                                           const auto& op = operands[i];
                                           if (op.type != ZYDIS_OPERAND_TYPE_MEMORY || op.mem.base != ZYDIS_REGISTER_RIP
                                               || ZydisUtility::GetAbsoluteAddress(instr, op, ip) != depthGlobal)
                                               continue;

                                           // Destination operand is index 0 for every write form we care about
                                           // (mov/add/sub/inc/dec) — a depth access anywhere else is a read.
                                           if (i != 0)
                                               continue;

                                           const bool isDecrement =
                                               instr.mnemonic == ZYDIS_MNEMONIC_DEC
                                               || (instr.mnemonic == ZYDIS_MNEMONIC_SUB && instr.operand_count >= 2
                                                   && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                                                   && operands[1].imm.value.u == 1);

                                           if (isDecrement)
                                               sawDecrementWrite = true;
                                           else
                                               sawOtherWrite = true;
                                       }
                                       return false;
                                   });

    if (sawDecrementWrite && !sawOtherWrite)
        return ScriptStackFnKind::Pop;

    if (sawOtherWrite)
        return ScriptStackFnKind::Push;

    return ScriptStackFnKind::None;
}

// Resolve push+pop off the method-call dispatcher (string "PublicMethod:%s"), which brackets its
// invoke with push…pop; classify its callees by the getter-derived depth global. Zydis first, then the
// on_demand gamedata signatures "CCSScript::ScriptStackPush"/"ScriptStackPop" (win+linux); either left
// null on failure.
static void ResolveScriptStackFns(uintptr_t getter, ScriptStackPushFn& outPush, ScriptStackPopFn& outPop)
{
    auto* mod = modules::server;

    outPush = nullptr;
    outPop  = nullptr;

    const uintptr_t depthGlobal = getter != 0 ? ExtractCurrentScriptDepthGlobal(mod, getter) : 0;
    if (depthGlobal == 0)
    {
        WARN("cs_script: current-script depth global not extracted from getter server+%llx",
             getter != 0 ? static_cast<uint64_t>(getter - mod->Base()) : 0ULL);
    }
    else
    {
        FLOG("cs_script: current-script depth global at server+%llx",
             static_cast<uint64_t>(depthGlobal - mod->Base()));

        std::unordered_map<uintptr_t, int> pushVotes;
        std::unordered_map<uintptr_t, int> popVotes;

        for (const auto dispatcher : mod->FindAllFunctionsFromStringRefs({"PublicMethod:%s"}))
        {
            const auto* range = mod->GetFunctionRange(dispatcher);
            if (range == nullptr)
                continue;

            ZydisUtility::ScanInstructions(range->start, range->end,
                                           [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
                                               auto target = ZydisUtility::ResolveCallTarget(&instr, operands, ip);

                                               // GCC turns trailing calls into direct tail jmps (see
                                               // ExtractWrapperFactory) — treat a jmp that leaves the
                                               // function body as a call.
                                               if (target == 0 && instr.mnemonic == ZYDIS_MNEMONIC_JMP
                                                   && operands[0].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
                                               {
                                                   if (const auto jmp = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                                                       jmp != 0 && (jmp < range->start || jmp >= range->end))
                                                       target = jmp;
                                               }

                                               if (target != 0 && mod->IsInModule(target))
                                               {
                                                   switch (ClassifyScriptStackFn(mod, target, depthGlobal))
                                                   {
                                                   case ScriptStackFnKind::Push: ++pushVotes[target]; break;
                                                   case ScriptStackFnKind::Pop: ++popVotes[target]; break;
                                                   default: break;
                                                   }
                                               }
                                               return false;
                                           });
        }

        FLOG("cs_script: script-stack vote pools: %zu push candidates, %zu pop candidates",
             pushVotes.size(),
             popVotes.size());

        const auto pickBest = [](const std::unordered_map<uintptr_t, int>& votes) -> uintptr_t {
            uintptr_t best = 0;
            int       n    = 0;
            bool      tied = false;
            for (const auto& [addr, count] : votes)
            {
                if (count > n)
                {
                    best = addr;
                    n    = count;
                    tied = false;
                }
                else if (count == n)
                {
                    tied = true;
                }
            }
            // A tied top vote is ambiguous — binding the wrong push/pop as an engine stack primitive
            // corrupts state. Return none so the gamedata fallback (or fail-closed) is used instead.
            return tied ? 0 : best;
        };

        outPush = reinterpret_cast<ScriptStackPushFn>(pickBest(pushVotes));
        outPop  = reinterpret_cast<ScriptStackPopFn>(pickBest(popVotes));
    }

    if (outPush == nullptr && g_pGameData != nullptr)
    {
        uintptr_t address = 0;
        if (g_pGameData->GetAddress("CCSScript::ScriptStackPush", address))
            outPush = reinterpret_cast<ScriptStackPushFn>(address);
    }
    if (outPop == nullptr && g_pGameData != nullptr)
    {
        uintptr_t address = 0;
        if (g_pGameData->GetAddress("CCSScript::ScriptStackPop", address))
            outPop = reinterpret_cast<ScriptStackPopFn>(address);
    }

    if (outPush != nullptr && outPop != nullptr)
        FLOG("Found cs_script current-script push at server+%llx, pop at server+%llx",
             static_cast<uint64_t>(reinterpret_cast<uintptr_t>(outPush) - mod->Base()),
             static_cast<uint64_t>(reinterpret_cast<uintptr_t>(outPop) - mod->Base()));
    else
        WARN("cs_script: current-script push/pop not resolved");
}

bool InitV8()
{
    if (!CheckV8Version())
        return false;

    // Export spelling differs between tier0 builds; try both. Runtime lookup instead of a
    // link-time import so a tier0 without the export fails here, not at dlopen of the module.
    {
        auto address = modules::tier0->GetFunctionByName("MakeGlobalSymbol");
        if (!address.IsValid())
            address = modules::tier0->GetFunctionByName("_MakeGlobalSymbol");

        if (!address.IsValid())
        {
            WARN("cs_script: tier0 does not export MakeGlobalSymbol");
            return false;
        }

        s_MakeGlobalSymbol = address.As<MakeGlobalSymbolFn>();
    }

    s_CreateEntityWrapper = reinterpret_cast<ScriptEntityWrapperFn>(GetCreateEntityWrapperAddress());
    if (s_CreateEntityWrapper == nullptr)
    {
        WARN("cs_script: entity wrapper factory not resolved");
        return false;
    }

    s_GetCurrentScript = reinterpret_cast<GetCurrentScriptFn>(GetCurrentScriptAddress());
    if (s_GetCurrentScript == nullptr)
    {
        WARN("cs_script: current-script getter not resolved");
        return false;
    }

    ResolveScriptStackFns(reinterpret_cast<uintptr_t>(s_GetCurrentScript), s_ScriptStackPush, s_ScriptStackPop);
    if (s_ScriptStackPush == nullptr || s_ScriptStackPop == nullptr)
        return false;

    return true;
}

// Pick the most-derived JS class name for an entity so the wrapper carries the right prototype
// (spec §6 "full chain"). Uses RTTI-name checks rather than hard-coded vtable offsets — the names
// are stable across game updates, the offsets are not. Most-derived first.
static const char* PickScriptClassName(CBaseEntity* entity)
{
    auto* mod = modules::server;
    void* p   = entity;

    if (mod->IsPointerDerivedFrom(p, "CCSPlayerController")) return "CSPlayerController";
    if (mod->IsPointerDerivedFrom(p, "CCSObserverPawn")) return "CSObserverPawn";
    if (mod->IsPointerDerivedFrom(p, "CCSPlayerPawn")) return "CSPlayerPawn";
    if (mod->IsPointerDerivedFrom(p, "CCSWeaponBase")) return "CSWeaponBase";
    if (mod->IsPointerDerivedFrom(p, "CPlantedC4")) return "CSPlantedC4";
    if (mod->IsPointerDerivedFrom(p, "CBaseCSGrenadeProjectile")) return "CSGrenadeProjectileBase";
    if (mod->IsPointerDerivedFrom(p, "CPointTemplate")) return "PointTemplate";
    if (mod->IsPointerDerivedFrom(p, "CBaseModelEntity")) return "BaseModelEntity";

    return "Entity";
}

v8::Local<v8::Value> ScriptToV8Value(v8::Isolate* isolate, v8::Local<v8::Context> ctx, const ScriptValue& v)
{
    switch (v.type)
    {
    case ScriptValueType::Bool:
        return v8::Boolean::New(isolate, v.b);
    case ScriptValueType::Number:
        return v8::Number::New(isolate, v.number);
    case ScriptValueType::String: {
        if (v.str == nullptr)
            return v8::Null(isolate);
        // ToLocal (not ToLocalChecked): an oversized (> String::kMaxLength) or OOM string yields JS null
        // instead of CHECK-aborting the whole server on this data-controlled return path.
        v8::Local<v8::String> str;
        if (!v8::String::NewFromUtf8(isolate, v.str).ToLocal(&str))
            return v8::Null(isolate);
        return str;
    }
    case ScriptValueType::Vector: {
        // Valve's Vector is the object {x,y,z} (Source2ZE cs_script types), not an array. CreateDataProperty
        // (not Set) so an inherited/prototype setter on x/y/z cannot intercept the write or throw — matches
        // the hardening in BuildScriptArgValue.
        auto obj = v8::Object::New(isolate);
        obj->CreateDataProperty(ctx, v8::String::NewFromUtf8Literal(isolate, "x"), v8::Number::New(isolate, v.vec[0])).IsJust();
        obj->CreateDataProperty(ctx, v8::String::NewFromUtf8Literal(isolate, "y"), v8::Number::New(isolate, v.vec[1])).IsJust();
        obj->CreateDataProperty(ctx, v8::String::NewFromUtf8Literal(isolate, "z"), v8::Number::New(isolate, v.vec[2])).IsJust();
        return obj;
    }
    case ScriptValueType::Entity: {
        if (s_CreateEntityWrapper == nullptr)
            return v8::Null(isolate);

        CBaseHandle handle(v.entity);
        if (!handle.IsValid())
            return v8::Null(isolate);

        auto* entity = g_pGameEntitySystem->FindEntityByEHandle(handle);
        if (entity == nullptr)
            return v8::Null(isolate);

        const char* sym = InternGlobalSymbol(PickScriptClassName(entity));
        if (sym == nullptr)
            return v8::Null(isolate);

        // v8::Local<Object> is a single-pointer wrapper; &wrapper aliases v8::Object**.
        v8::Local<v8::Object> wrapper;
#ifdef PLATFORM_WINDOWS
        s_CreateEntityWrapper(reinterpret_cast<v8::Object**>(&wrapper), &sym, entity);
#else
        *reinterpret_cast<v8::Object**>(&wrapper) = s_CreateEntityWrapper(&sym, entity);
#endif

        if (wrapper.IsEmpty())
            return v8::Null(isolate);

        return wrapper;
    }
    case ScriptValueType::Object:
    case ScriptValueType::Array: {
        // Rooted by the caller (GetReturnValue().Set / ObjSet) before this HandleScope closes.
        auto value = ResolveHandle(v.handle);
        if (value.IsEmpty())
            return v8::Null(isolate);

        return value;
    }
    case ScriptValueType::Null:
    default:
        return v8::Null(isolate);
    }
}

CBaseEntity* GetCurrentPointScriptEntity()
{
    if (s_GetCurrentScript == nullptr)
        return nullptr;

    auto* script = reinterpret_cast<uint8_t*>(s_GetCurrentScript());
    if (script == nullptr)
        return nullptr;

    if (s_CsScriptSubobjectOffset == static_cast<size_t>(-1))
    {
        auto* probe = g_pGameEntitySystem->FindByClassname(nullptr, "point_script");
        if (probe == nullptr)
            return nullptr; // no point_script to learn the layout from yet; retry on a later call

        auto* base = reinterpret_cast<uint8_t*>(probe);
        for (size_t i = 0; i < 0x800; i += sizeof(void*))
        {
            if (modules::server->IsPointerDerivedFrom(base + i, "CCSScript_EntityScript"))
            {
                s_CsScriptSubobjectOffset = i;
                FLOG("cs_script: CCSScript_EntityScript sub-object at entity+0x%llx", static_cast<uint64_t>(i));
                break;
            }
        }

        if (s_CsScriptSubobjectOffset == static_cast<size_t>(-1))
        {
            WARN("cs_script: failed to discover CCSScript_EntityScript sub-object offset; Script/Caller will be null");
            return nullptr;
        }
    }

    return reinterpret_cast<CBaseEntity*>(script - s_CsScriptSubobjectOffset);
}

// The offset must already be discovered (CreateCallback resolves it first via GetCurrentPointScriptEntity).
bool PushOwnerScript(CBaseEntity* owner)
{
    if (s_ScriptStackPush == nullptr || s_ScriptStackPop == nullptr || s_CsScriptSubobjectOffset == static_cast<size_t>(-1))
        return false;

    s_ScriptStackPush(reinterpret_cast<uint8_t*>(owner) + s_CsScriptSubobjectOffset);
    return true;
}

void PopOwnerScript()
{
    if (s_ScriptStackPop != nullptr)
        s_ScriptStackPop();
}

// Persistent JS callbacks — a v8::Function a script hands to a registered C# method, kept alive across
// calls and invoked later from C#. Each is owned by the point_script that created it (EHandle) and
// released on Dispose, owner death, or map end. The isolate is process-level and single-threaded, so a
// plain map on the script thread needs no locking.

struct ScriptCallback
{
    v8::Isolate*             isolate;
    v8::Global<v8::Function> function;
    v8::Global<v8::Context>  context;
    CBaseHandle              owner;

    ScriptCallback(v8::Isolate* isolate, v8::Local<v8::Function> function, v8::Local<v8::Context> context, CBaseHandle owner) : isolate(isolate), function(isolate, function), context(isolate, context), owner(owner)
    {
    }
};

static std::unordered_map<uint32_t, ScriptCallback> s_Callbacks;
static uint32_t                                     s_NextCallbackId = 1;

// Release a point_script's callbacks the instant it is deleted. Dispose from C# is the primary path and
// the invoke-time liveness guard already blocks stale invokes; this just reclaims promptly. A missed
// match (serial already recycled) is harmless — the map-end sweep is the final backstop.
class ScriptOwnerListener final : public IEntityListener
{
public:
    void OnEntityCreated(CBaseEntity*) override {}
    void OnEntitySpawned(CBaseEntity*) override {}
    void OnEntityFollowed(CBaseEntity*, CBaseEntity*) override {}
    void OnEntityDeleted(CBaseEntity* entity) override
    {
        if (s_Callbacks.empty() || entity == nullptr)
            return;

        const CBaseHandle handle = entity->GetEHandle();
        std::erase_if(s_Callbacks, [&](const auto& kv) { return kv.second.owner == handle; });
    }
};

static ScriptOwnerListener s_ScriptOwnerListener;

uint32_t CreateManagedCallback(v8::FunctionCallbackInfo<v8::Value>* info, int32_t index)
{
    auto* isolate = v8::Isolate::GetCurrent();
    if (isolate == nullptr)
        return 0;

    if (info == nullptr)
        return 0;

    const auto& args = *info;
    if (index < 0 || index >= args.Length())
        return 0;

    v8::Local<v8::Value> value = args[index];
    if (!value->IsFunction())
        return 0;

    CBaseEntity* owner = GetCurrentPointScriptEntity();
    if (owner == nullptr)
        return 0;

    // Install the owner-death listener once. We only reach here from inside a script method (owner
    // resolved above), so g_pGameEntitySystem is live — it is not at hook-install time. The function
    // static runs its initializer exactly once for the process.
    [[maybe_unused]] static const bool listenerInstalled = [] {
        g_pGameEntitySystem->AddListenerEntity(&s_ScriptOwnerListener);
        return true;
    }();

    const uint32_t id = s_NextCallbackId++;

    s_Callbacks.try_emplace(id, isolate, value.As<v8::Function>(), isolate->GetCurrentContext(), owner->GetEHandle());
    return id;
}

void ReleaseManagedCallback(uint32_t id)
{
    s_Callbacks.erase(id);
}

bool IsManagedCallbackAlive(uint32_t id)
{
    const auto it = s_Callbacks.find(id);
    if (it == s_Callbacks.end())
        return false;

    return g_pGameEntitySystem->FindEntityByEHandle(it->second.owner) != nullptr;
}

void PurgeCallbacks()
{
    s_Callbacks.clear();
}

// A string result lives here only until InvokeManagedCallback returns; C# copies it immediately.
static thread_local char s_InvokeResultStrBuf[kArgStringBufferSize];

// Materialize one descriptor node (advancing cursor) into a V8 value. Object/Array nodes carry their
// child count in the uint slot; children follow in pre-order (Object as count×(key,value) pairs).
static v8::Local<v8::Value> BuildScriptArgValue(v8::Isolate* isolate, v8::Local<v8::Context> ctx,
                                                const ScriptValue* nodes, int32_t count, int32_t& cursor)
{
    if (cursor < 0 || cursor >= count)
        return v8::Undefined(isolate);

    const ScriptValue& node = nodes[cursor++];

    switch (node.type)
    {
    case ScriptValueType::Array: {
        // Clamp the child count to the nodes actually remaining: a malformed descriptor with a bogus count
        // must not request a multi-GB Array or spin billions of iterations. A well-formed descriptor from
        // the C# writer always has count <= remaining, so this never affects valid input.
        uint32_t n = node.handle;
        if (n > static_cast<uint32_t>(count - cursor))
            n = static_cast<uint32_t>(count - cursor);
        auto array = v8::Array::New(isolate, static_cast<int>(n));
        for (uint32_t i = 0; i < n; ++i)
            array->Set(ctx, i, BuildScriptArgValue(isolate, ctx, nodes, count, cursor)).IsJust();
        return array;
    }
    case ScriptValueType::Object: {
        uint32_t m = node.handle;
        if (m > static_cast<uint32_t>(count - cursor))
            m = static_cast<uint32_t>(count - cursor);
        auto obj = v8::Object::New(isolate);
        for (uint32_t i = 0; i < m; ++i)
        {
            auto key = BuildScriptArgValue(isolate, ctx, nodes, count, cursor);
            auto val = BuildScriptArgValue(isolate, ctx, nodes, count, cursor);
            // CreateDataProperty (not Set): a key like "__proto__" must become an own data property, not
            // invoke the inherited prototype setter. Skip a non-string key (Undefined/Null/etc. from a
            // malformed descriptor) rather than an unchecked As<Name> downcast that would crash.
            if (key->IsName())
                obj->CreateDataProperty(ctx, key.As<v8::Name>(), val).IsJust();
        }
        return obj;
    }
    default:
        return ScriptToV8Value(isolate, ctx, node);
    }
}

struct ScopedOwnerScript
{
    bool pushed;

    explicit ScopedOwnerScript(CBaseEntity* owner) : pushed(PushOwnerScript(owner)) {}
    ~ScopedOwnerScript() { if (pushed) PopOwnerScript(); }

    ScopedOwnerScript(const ScopedOwnerScript&)            = delete;
    ScopedOwnerScript& operator=(const ScopedOwnerScript&) = delete;
};

// C# releases the callback when this returns false (unknown id / dead owner).
bool InvokeManagedCallback(uint32_t id, ScriptValue* nodes, int32_t nodeCount, int32_t argc, ScriptValue* result)
{
    const auto it = s_Callbacks.find(id);
    if (it == s_Callbacks.end())
        return false;

    ScriptCallback& cb = it->second;

    CBaseEntity* owner = g_pGameEntitySystem->FindEntityByEHandle(cb.owner);
    if (owner == nullptr)
        return false;

    v8::Isolate* isolate = cb.isolate;
    if (isolate == nullptr)
        return false;

    v8::Isolate::Scope isolateScope(isolate);
    v8::HandleScope    handleScope(isolate);

    v8::Local<v8::Context> context = cb.context.Get(isolate);
    if (context.IsEmpty())
        return false;

    v8::Context::Scope contextScope(context);

    v8::Local<v8::Function> function = cb.function.Get(isolate);
    if (function.IsEmpty())
        return false;

    ScopedHandleFrame handleFrame;
    ScopedOwnerScript ownerScope(owner);

    std::vector<v8::Local<v8::Value>> argv;
    argv.reserve(static_cast<size_t>(argc > 0 ? argc : 0));
    const ScriptValue* nodeArray = nodes;
    int32_t            cursor    = 0;
    for (int32_t i = 0; i < argc; ++i)
        argv.push_back(BuildScriptArgValue(isolate, context, nodeArray, nodeCount, cursor));

    v8::TryCatch         tryCatch(isolate);
    v8::Local<v8::Value> callResult;
    bool                 called = function->Call(context, context->Global(), static_cast<int>(argv.size()), argv.data()).ToLocal(&callResult);
    if (tryCatch.HasCaught())
    {
        v8::String::Utf8Value text(isolate, tryCatch.Exception());
        WARN("cs_script: callback threw: %s", *text != nullptr ? *text : "<exception>");
        tryCatch.Reset();
        called = false;
    }

    // Scalars/string/vector/entity survive past this frame; an object/array result is a handle that dies
    // with this HandleScope, so it is reported as Null (return structured data via out-params instead).
    if (result != nullptr)
    {
        auto& out = *result;
        if (called)
        {
            V8ValueToScript(isolate, context, callResult, out, s_InvokeResultStrBuf, kArgStringBufferSize);
            if (out.type == ScriptValueType::Object || out.type == ScriptValueType::Array)
                out.type = ScriptValueType::Null;
        }
        else
        {
            out.type = ScriptValueType::Null;
        }
    }

    return true;
}
