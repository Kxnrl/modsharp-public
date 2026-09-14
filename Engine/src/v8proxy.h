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

#ifndef MS_ROOT_V8PROXY_H
#define MS_ROOT_V8PROXY_H

#include "definitions.h"

#include "cstrike/type/ScriptValue.h"

#include <v8.h>

#include <cstdint>

class CBaseEntity;
class Vector;

// Reusable V8 <-> ScriptValue bridge shared by the cs_script hook and the Script.* natives.
// Free functions, no namespace; state is file-static in v8proxy.cpp.

// ------------------------------------------------------------
// Per-call V8 handle table. JS objects/arrays cross the boundary by index into a per-thread stack,
// never by value. Each call records the base on entry and truncates back on exit, so a nested call
// gets handles above the outer's and reclaims only its own — the outer's stay valid (its HandleScope
// is still open). A clear()-per-call design corrupted nesting, hence the stack.
// ------------------------------------------------------------

uint32_t PushHandle(v8::Local<v8::Value> value);

v8::Local<v8::Value> ResolveHandle(uint32_t handle);

int32_t HandleFrameBegin();
void    HandleFrameEnd(int32_t base);

void PurgeHandles();
void PurgeCallbacks();

struct ScopedHandleFrame
{
    int32_t base;

    ScopedHandleFrame() : base(HandleFrameBegin()) {}
    ~ScopedHandleFrame() { HandleFrameEnd(base); }

    ScopedHandleFrame(const ScopedHandleFrame&)            = delete;
    ScopedHandleFrame& operator=(const ScopedHandleFrame&) = delete;
};

// ------------------------------------------------------------
// V8 script wrapper — the engine's 3-internal-field object for entities / script objects.
// Payload is at +0x08; do NOT index it as an int32 array.
// ------------------------------------------------------------

enum class ScriptWrapperType : int32_t
{
    ScriptObject = 1,
    Entity       = 2,
};

struct ScriptWrapper
{
    int32_t type;   // 0x00
    int32_t unused; // 0x04
    union           // 0x08
    {
        void*     object; // ScriptObject → CCSBaseScript*
        EHandle_t handle; // Entity       → entity handle
    };
};

static_assert(offsetof(ScriptWrapper, object) == 0x08);
static_assert(offsetof(ScriptWrapper, handle) == 0x08);

// Only the field count is checked; callers must still validate the type tag / payload.
const ScriptWrapper* GetWrapperFromThis(const v8::FunctionCallbackInfo<v8::Value>& info);

CBaseEntity* GetEntityFromThis(const v8::FunctionCallbackInfo<v8::Value>& info);

// Fast-path size of the thread-local buffer a marshalled string is written into; an oversized string
// grows a fallback buffer instead of truncating (see V8ValueToScript).
constexpr int kArgStringBufferSize = 1024;

bool V8ToNumber(v8::Local<v8::Value> val, double& out);
bool V8ToVector(v8::Local<v8::Context> ctx, v8::Local<v8::Value> val, Vector& out);

// strBuf is caller-owned; the written string is valid only for the current call.
void V8ValueToScript(v8::Isolate* isolate, v8::Local<v8::Context> ctx, v8::Local<v8::Value> val, ScriptValue& out, char* strBuf, int strBufSize);

bool ResolveObjectHandle(uint32_t handle, v8::Local<v8::Object>& out);
bool ResolveArrayHandle(uint32_t handle, v8::Local<v8::Array>& out);

v8::Local<v8::Value> ScriptToV8Value(v8::Isolate* isolate, v8::Local<v8::Context> ctx, const ScriptValue& v);

// ------------------------------------------------------------
// Engine entry points + version gate. InitV8 resolves the entry points itself and returns false on any
// failure — a V8 ABI/version mismatch or an unresolved entry point; the caller treats false as fatal.
// ------------------------------------------------------------

bool InitV8();

const char* InternGlobalSymbol(const char* name);

CBaseEntity* GetCurrentPointScriptEntity();

// Call PopOwnerScript only when PushOwnerScript returned true.
bool PushOwnerScript(CBaseEntity* owner);
void PopOwnerScript();

// ------------------------------------------------------------
// Managed callback subsystem — a JS function a registered C# method captures and invokes later. Owns
// the registry + v8::Global lifetime + owner-death listener internally; ScriptNatives exposes these
// as the Script.*Callback natives. Invoke takes C#'s flat pre-order arg descriptor (nodes/nodeCount).
// ------------------------------------------------------------

uint32_t CreateManagedCallback(v8::FunctionCallbackInfo<v8::Value>* info, int32_t index);
void     ReleaseManagedCallback(uint32_t id);
bool     IsManagedCallbackAlive(uint32_t id);
bool     InvokeManagedCallback(uint32_t id, ScriptValue* nodes, int32_t nodeCount, int32_t argc, ScriptValue* result);

#endif
