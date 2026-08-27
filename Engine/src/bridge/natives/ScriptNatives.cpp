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

#include "bridge/natives/ScriptNatives.h"
#include "bridge/adapter.h"
#include "v8proxy.h"

#include "cstrike/type/ScriptValue.h"

#include <cstdint>

// RegisterManagedScriptMethod is defined in hook/cs_script.cpp (it appends to the managed-method slot
// table the InitV8ClassTemplates hook injects). Everything else here adapts a v8proxy helper.
extern void RegisterManagedScriptMethod(const char* className, const char* methodName, int32_t managedId);

// ============================================================
// Script.* native entry points — thin adapters over v8proxy. The managed-callback natives
// (Create/Release/IsAlive/Invoke) are v8proxy interfaces, registered directly below.
// ============================================================

static CBaseEntity* ScriptCallGetScript()
{
    return GetCurrentPointScriptEntity();
}

static CBaseEntity* ScriptCallGetCaller(v8::FunctionCallbackInfo<v8::Value>* info)
{
    if (info == nullptr)
        return nullptr;

    const auto& args = *info;

    // entity receiver (weapon.XXX etc.) resolves to that entity; script receiver (Instance.XXX) is the
    // script object itself — resolve it to the owning point_script.
    if (auto* entity = GetEntityFromThis(args))
        return entity;

    if (const auto* wrapper = GetWrapperFromThis(args);
        wrapper != nullptr && wrapper->type == static_cast<int32_t>(ScriptWrapperType::ScriptObject))
        return GetCurrentPointScriptEntity();

    return nullptr;
}

static int32_t ScriptCallGetArgCount(v8::FunctionCallbackInfo<v8::Value>* info)
{
    return info != nullptr ? info->Length() : 0;
}

static void ScriptCallMarshalArg(v8::FunctionCallbackInfo<v8::Value>* info, int32_t index, ScriptValue* out)
{
    out->type = ScriptValueType::Null;

    if (info == nullptr)
        return;

    const auto& args = *info;
    if (index < 0 || index >= args.Length())
        return;

    static thread_local char strBuf[kArgStringBufferSize];

    auto isolate = v8::Isolate::GetCurrent();
    auto ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    V8ValueToScript(isolate, ctx, args[index], *out, strBuf, kArgStringBufferSize);
    if (tryCatch.HasCaught())
        tryCatch.Reset();
}

// values_ is FunctionCallbackInfo's 2nd member (v8-function-callback.h: implicit_args_ @0, values_ @
// kApiSystemPointerSize).
// GC-safe: values_[i] IS the slot operator[](i) reads (Local::FromSlot(values_+i)), a GC-updated root, so
// writing another tagged Address there (the form operator[] reads) leaves it a root GC keeps rewriting.
static void ScriptCallSetArg(v8::FunctionCallbackInfo<v8::Value>* info, int32_t index, ScriptValue* in)
{
    if (info == nullptr)
        return;

    const auto& args = *info;
    if (index < 0 || index >= args.Length())
        return;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    const v8::Local<v8::Value> value = ScriptToV8Value(isolate, ctx, *in);

    auto* values  = *reinterpret_cast<v8::internal::Address**>(reinterpret_cast<char*>(info) + sizeof(v8::internal::Address));
    values[index] = v8::internal::ValueHelper::ValueAsAddress(v8::internal::ValueHelper::HandleAsValue(value));
}

static uint32_t ScriptCallNewObject()
{
    return PushHandle(v8::Object::New(v8::Isolate::GetCurrent()));
}

static uint32_t ScriptCallNewArray()
{
    return PushHandle(v8::Array::New(v8::Isolate::GetCurrent()));
}

// String payloads land in a thread-local buffer valid only until the next marshal on this thread.
static void ScriptCallObjGet(uint32_t handle, const char* key, ScriptValue* out)
{
    out->type = ScriptValueType::Null;

    v8::Local<v8::Object> obj;
    if (key == nullptr || !ResolveObjectHandle(handle, obj))
        return;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    auto         jsKey = v8::String::NewFromUtf8(isolate, key).ToLocalChecked();

    // Own-key only (matches ObjHas/ObjDelete/ObjKeys/ObjCount): an inherited prototype member such as
    // "toString"/"constructor" is not an entry of this dictionary and must read as absent (Null).
    bool own = false;
    if (!obj->HasOwnProperty(ctx, jsKey).To(&own) || !own)
    {
        tryCatch.Reset();
        return;
    }

    v8::Local<v8::Value> member;
    if (!obj->Get(ctx, jsKey).ToLocal(&member))
    {
        tryCatch.Reset();
        return;
    }

    static thread_local char strBuf[kArgStringBufferSize];
    V8ValueToScript(isolate, ctx, member, *out, strBuf, kArgStringBufferSize);
}

static void ScriptCallObjSet(uint32_t handle, const char* key, ScriptValue* in)
{
    v8::Local<v8::Object> obj;
    if (key == nullptr || !ResolveObjectHandle(handle, obj))
        return;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    auto         jsKey = v8::String::NewFromUtf8(isolate, key).ToLocalChecked();
    auto         jsVal = ScriptToV8Value(isolate, ctx, *in);
    if (obj->CreateDataProperty(ctx, jsKey, jsVal).IsNothing())
        tryCatch.Reset();
}

static bool ScriptCallObjHas(uint32_t handle, const char* key)
{
    v8::Local<v8::Object> obj;
    if (key == nullptr || !ResolveObjectHandle(handle, obj))
        return false;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    auto         jsKey = v8::String::NewFromUtf8(isolate, key).ToLocalChecked();
    const bool   has   = obj->HasOwnProperty(ctx, jsKey).FromMaybe(false);
    tryCatch.Reset();
    return has;
}

static uint32_t ScriptCallObjKeys(uint32_t handle)
{
    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::Local<v8::Object> obj;
    if (!ResolveObjectHandle(handle, obj))
        return PushHandle(v8::Array::New(isolate));

    v8::TryCatch         tryCatch(isolate);
    v8::Local<v8::Array> names;
    // own ENUMERABLE string keys (Object.keys semantics) — not getOwnPropertyNames' non-enumerable set.
    if (!obj->GetOwnPropertyNames(ctx, static_cast<v8::PropertyFilter>(v8::ONLY_ENUMERABLE | v8::SKIP_SYMBOLS)).ToLocal(&names))
    {
        tryCatch.Reset();
        return PushHandle(v8::Array::New(isolate));
    }

    return PushHandle(names);
}

// Dictionary.Remove semantics: absent key -> false (raw JS `delete` returns true for absent keys).
static bool ScriptCallObjDelete(uint32_t handle, const char* key)
{
    v8::Local<v8::Object> obj;
    if (key == nullptr || !ResolveObjectHandle(handle, obj))
        return false;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    auto         jsKey = v8::String::NewFromUtf8(isolate, key).ToLocalChecked();

    bool has = false;
    if (!obj->HasOwnProperty(ctx, jsKey).To(&has) || !has)
    {
        tryCatch.Reset();
        return false;
    }

    bool deleted = false;
    if (!obj->Delete(ctx, jsKey).To(&deleted))
    {
        tryCatch.Reset();
        return false;
    }

    return deleted;
}

static int32_t ScriptCallObjCount(uint32_t handle)
{
    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::Local<v8::Object> obj;
    if (!ResolveObjectHandle(handle, obj))
        return 0;

    v8::TryCatch         tryCatch(isolate);
    v8::Local<v8::Array> names;
    if (!obj->GetOwnPropertyNames(ctx, static_cast<v8::PropertyFilter>(v8::ONLY_ENUMERABLE | v8::SKIP_SYMBOLS)).ToLocal(&names))
    {
        tryCatch.Reset();
        return 0;
    }

    return static_cast<int32_t>(names->Length());
}

static void ScriptCallObjClear(uint32_t handle)
{
    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::Local<v8::Object> obj;
    if (!ResolveObjectHandle(handle, obj))
        return;

    v8::TryCatch         tryCatch(isolate);
    v8::Local<v8::Array> names;
    if (!obj->GetOwnPropertyNames(ctx).ToLocal(&names))
    {
        tryCatch.Reset();
        return;
    }

    const uint32_t count = names->Length();
    for (uint32_t i = 0; i < count; ++i)
    {
        v8::Local<v8::Value> key;
        if (!names->Get(ctx, i).ToLocal(&key))
        {
            tryCatch.Reset();
            continue;
        }

        if (obj->Delete(ctx, key).IsNothing())
            tryCatch.Reset();
    }
}

static int32_t ScriptCallArrLength(uint32_t handle)
{
    v8::Local<v8::Array> arr;
    if (!ResolveArrayHandle(handle, arr))
        return 0;

    return static_cast<int32_t>(arr->Length());
}

static void ScriptCallArrGet(uint32_t handle, int32_t index, ScriptValue* out)
{
    out->type = ScriptValueType::Null;

    v8::Local<v8::Array> arr;
    if (index < 0 || !ResolveArrayHandle(handle, arr))
        return;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch         tryCatch(isolate);
    v8::Local<v8::Value> member;
    if (!arr->Get(ctx, static_cast<uint32_t>(index)).ToLocal(&member))
    {
        tryCatch.Reset();
        return;
    }

    static thread_local char strBuf[kArgStringBufferSize];
    V8ValueToScript(isolate, ctx, member, *out, strBuf, kArgStringBufferSize);
}

static void ScriptCallArrSet(uint32_t handle, int32_t index, ScriptValue* in)
{
    v8::Local<v8::Array> arr;
    if (index < 0 || !ResolveArrayHandle(handle, arr))
        return;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    auto         jsVal = ScriptToV8Value(isolate, ctx, *in);
    if (arr->Set(ctx, static_cast<uint32_t>(index), jsVal).IsNothing())
        tryCatch.Reset();
}

static void ScriptCallArrPush(uint32_t handle, ScriptValue* in)
{
    v8::Local<v8::Array> arr;
    if (!ResolveArrayHandle(handle, arr))
        return;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    auto         jsVal = ScriptToV8Value(isolate, ctx, *in);
    if (arr->Set(ctx, arr->Length(), jsVal).IsNothing())
        tryCatch.Reset();
}

static bool ScriptCallArrRemove(uint32_t handle, int32_t index)
{
    v8::Local<v8::Array> arr;
    if (index < 0 || !ResolveArrayHandle(handle, arr))
        return false;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    if (static_cast<uint32_t>(index) >= arr->Length())
        return false;

    v8::TryCatch tryCatch(isolate);

    v8::Local<v8::Value> spliceVal;
    if (!arr->Get(ctx, v8::String::NewFromUtf8(isolate, "splice").ToLocalChecked()).ToLocal(&spliceVal)
        || !spliceVal->IsFunction())
    {
        tryCatch.Reset();
        return false;
    }

    v8::Local<v8::Value> args[2] = {
        v8::Integer::New(isolate, index),
        v8::Integer::New(isolate, 1),
    };

    v8::Local<v8::Value> result;
    if (!spliceVal.As<v8::Function>()->Call(ctx, arr, 2, args).ToLocal(&result))
    {
        tryCatch.Reset();
        return false;
    }

    return true;
}

static void ScriptCallArrClear(uint32_t handle)
{
    v8::Local<v8::Array> arr;
    if (!ResolveArrayHandle(handle, arr))
        return;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    v8::TryCatch tryCatch(isolate);
    if (arr->Set(ctx, v8::String::NewFromUtf8(isolate, "length").ToLocalChecked(), v8::Integer::New(isolate, 0)).IsNothing())
        tryCatch.Reset();
}

static bool ScriptCallArrInsert(uint32_t handle, int32_t index, ScriptValue* in)
{
    v8::Local<v8::Array> arr;
    if (index < 0 || !ResolveArrayHandle(handle, arr))
        return false;

    auto* isolate = v8::Isolate::GetCurrent();
    auto  ctx     = isolate->GetCurrentContext();

    if (static_cast<uint32_t>(index) > arr->Length())
        return false;

    v8::TryCatch tryCatch(isolate);

    v8::Local<v8::Value> spliceVal;
    if (!arr->Get(ctx, v8::String::NewFromUtf8(isolate, "splice").ToLocalChecked()).ToLocal(&spliceVal)
        || !spliceVal->IsFunction())
    {
        tryCatch.Reset();
        return false;
    }

    v8::Local<v8::Value> args[3] = {
        v8::Integer::New(isolate, index),
        v8::Integer::New(isolate, 0),
        ScriptToV8Value(isolate, ctx, *in),
    };

    v8::Local<v8::Value> result;
    if (!spliceVal.As<v8::Function>()->Call(ctx, arr, 3, args).ToLocal(&result))
    {
        tryCatch.Reset();
        return false;
    }

    return true;
}

static int32_t ScriptCallHandleFrameBegin()
{
    return HandleFrameBegin();
}

static void ScriptCallHandleFrameEnd(int32_t base)
{
    HandleFrameEnd(base);
}

namespace natives::script
{
static void RegisterMethod(const char* className, const char* methodName, int32_t managedId)
{
    RegisterManagedScriptMethod(className, methodName, managedId);
}

void Init()
{
    bridge::CreateNative("Script.RegisterMethod", reinterpret_cast<void*>(RegisterMethod));
    bridge::CreateNative("Script.GetScript", reinterpret_cast<void*>(ScriptCallGetScript));
    bridge::CreateNative("Script.GetCaller", reinterpret_cast<void*>(ScriptCallGetCaller));
    bridge::CreateNative("Script.GetArgCount", reinterpret_cast<void*>(ScriptCallGetArgCount));
    bridge::CreateNative("Script.MarshalArg", reinterpret_cast<void*>(ScriptCallMarshalArg));
    bridge::CreateNative("Script.SetArg", reinterpret_cast<void*>(ScriptCallSetArg));

    bridge::CreateNative("Script.NewObject", reinterpret_cast<void*>(ScriptCallNewObject));
    bridge::CreateNative("Script.NewArray", reinterpret_cast<void*>(ScriptCallNewArray));
    bridge::CreateNative("Script.ObjGet", reinterpret_cast<void*>(ScriptCallObjGet));
    bridge::CreateNative("Script.ObjSet", reinterpret_cast<void*>(ScriptCallObjSet));
    bridge::CreateNative("Script.ObjHas", reinterpret_cast<void*>(ScriptCallObjHas));
    bridge::CreateNative("Script.ObjKeys", reinterpret_cast<void*>(ScriptCallObjKeys));
    bridge::CreateNative("Script.ObjDelete", reinterpret_cast<void*>(ScriptCallObjDelete));
    bridge::CreateNative("Script.ObjCount", reinterpret_cast<void*>(ScriptCallObjCount));
    bridge::CreateNative("Script.ObjClear", reinterpret_cast<void*>(ScriptCallObjClear));
    bridge::CreateNative("Script.ArrLength", reinterpret_cast<void*>(ScriptCallArrLength));
    bridge::CreateNative("Script.ArrGet", reinterpret_cast<void*>(ScriptCallArrGet));
    bridge::CreateNative("Script.ArrSet", reinterpret_cast<void*>(ScriptCallArrSet));
    bridge::CreateNative("Script.ArrPush", reinterpret_cast<void*>(ScriptCallArrPush));
    bridge::CreateNative("Script.ArrRemove", reinterpret_cast<void*>(ScriptCallArrRemove));
    bridge::CreateNative("Script.ArrClear", reinterpret_cast<void*>(ScriptCallArrClear));
    bridge::CreateNative("Script.ArrInsert", reinterpret_cast<void*>(ScriptCallArrInsert));
    bridge::CreateNative("Script.HandleFrameBegin", reinterpret_cast<void*>(ScriptCallHandleFrameBegin));
    bridge::CreateNative("Script.HandleFrameEnd", reinterpret_cast<void*>(ScriptCallHandleFrameEnd));

    bridge::CreateNative("Script.CreateCallback", reinterpret_cast<void*>(CreateManagedCallback));
    bridge::CreateNative("Script.ReleaseCallback", reinterpret_cast<void*>(ReleaseManagedCallback));
    bridge::CreateNative("Script.IsCallbackAlive", reinterpret_cast<void*>(IsManagedCallbackAlive));
    bridge::CreateNative("Script.InvokeCallback", reinterpret_cast<void*>(InvokeManagedCallback));
}
} // namespace natives::script
