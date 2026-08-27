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

using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Script
{
    public static partial void RegisterMethod(string className, string methodName, int managedId);

    public static partial nint GetScript();

    public static partial nint GetCaller(nint info);

    public static partial int GetArgCount(nint info);

    public static partial void MarshalArg(nint info, int index, ScriptValue* value);

    public static partial void SetArg(nint info, int index, ScriptValue* value);

    // Object / Array composite ops. `handle` indexes the per-call V8 handle table (see cs_script.cpp
    // s_HandleTable). New* return a fresh handle; ObjGet/ArrGet write *value (a nested object/array
    // yields another handle in value->Handle); ObjSet/ArrSet/ArrPush read *value.
    public static partial uint NewObject();

    public static partial uint NewArray();

    public static partial void ObjGet(uint handle, string key, ScriptValue* value);

    public static partial void ObjSet(uint handle, string key, ScriptValue* value);

    public static partial bool ObjHas(uint handle, string key);

    public static partial uint ObjKeys(uint handle);

    public static partial bool ObjDelete(uint handle, string key);

    public static partial int ObjCount(uint handle);

    public static partial void ObjClear(uint handle);

    public static partial int ArrLength(uint handle);

    public static partial void ArrGet(uint handle, int index, ScriptValue* value);

    public static partial void ArrSet(uint handle, int index, ScriptValue* value);

    public static partial void ArrPush(uint handle, ScriptValue* value);

    public static partial bool ArrRemove(uint handle, int index);

    public static partial void ArrClear(uint handle);

    public static partial bool ArrInsert(uint handle, int index, ScriptValue* value);

    public static partial int HandleFrameBegin();

    public static partial void HandleFrameEnd(int baseIndex);

    // Persistent callbacks. CreateCallback captures arg[index] (must be a JS function) as a callback
    // owned by the current point_script and returns its id (0 = failure). InvokeCallback runs it now
    // (false = unknown id / dead owner): `nodes`/`nodeCount` is the flat pre-order arg descriptor C#
    // built, `argc` the top-level count, and the JS return marshals into `result`. Release is
    // idempotent; IsCallbackAlive folds in owner liveness.
    public static partial uint CreateCallback(nint info, int index);

    public static partial void ReleaseCallback(uint id);

    public static partial bool IsCallbackAlive(uint id);

    public static partial bool InvokeCallback(uint id, ScriptValue* nodes, int nodeCount, int argc, ScriptValue* result);
}
