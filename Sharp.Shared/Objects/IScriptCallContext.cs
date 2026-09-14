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

using System;
using System.Diagnostics.CodeAnalysis;
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.Objects;

public interface IScriptCallContext : IContextObject, IDisposable
{
    /// <summary>
    ///     The point_script itself (the current-script).
    /// </summary>
    IBaseEntity Script { get; }

    /// <summary>
    ///     The receiver of this call (V8's This). An entity-class method returns that entity; a point_script
    ///     method (Instance) has the script itself as its receiver and returns the point_script
    ///     (always equal to <see cref="Script" />).<br />type==2 (entity) → handle;<br />
    ///     type==1 (script) → current-script.
    ///     <para>
    ///         <b>Why nullable (a normal dispatch is always non-null; the three edge cases below return null):</b><br />
    ///         ① <b>Entity already removed</b>: a script-side use-after-remove, legitimately reachable.<br />
    ///         ② <b>Foreign this call</b>: <c>Class.prototype.M.call({}, …)</c> <br />
    ///         ③ <b>Degenerate</b>: info is null (does not happen on a normal dispatch), or the current-script getter did not resolve at startup.
    ///     </para>
    /// </summary>
    IBaseEntity? Caller { get; }

    int ArgCount { get; }

    ScriptValueType GetArgType(int index);

    bool? GetBool(int index);

    double? GetNumber(int index);

    string? GetString(int index);

    Vector? GetVector(int index);

    IBaseEntity? GetEntity(int index);

    IScriptCallback? GetFunction(int index);

    // ---------------------------------------------------------------------------------------------
    //  Object / Array
    //  Arg: JS object/array → ScriptObject / ScriptArray
    //  Ret: NewObject / NewArray / JS null/undefined → null
    // ---------------------------------------------------------------------------------------------

    ScriptObject? GetObject(int index);

    ScriptArray? GetArray(int index);

    ScriptObject NewObject();

    ScriptArray NewArray();

    ScriptValue ObjGet(uint handle, string key);

    void ObjSet(uint handle, string key, in ScriptValue value);

    bool ObjHas(uint handle, string key);

    uint ObjKeys(uint handle);

    bool ObjDelete(uint handle, string key);

    int ObjCount(uint handle);

    void ObjClear(uint handle);

    int ArrLength(uint handle);

    ScriptValue ArrGet(uint handle, int index);

    void ArrSet(uint handle, int index, in ScriptValue value);

    void ArrPush(uint handle, in ScriptValue value);

    bool ArrRemove(uint handle, int index);

    void ArrClear(uint handle);

    bool ArrInsert(uint handle, int index, in ScriptValue value);

    void SetArgNull(int index);

    void SetArg(int index, bool value);

    void SetArg(int index, double value);

    void SetArg(int index, string value);

    void SetArg(int index, Vector value);

    void SetArg(int index, IBaseEntity value);

    void SetArg(int index, ScriptObject value);

    void SetArg(int index, ScriptArray value);

    IBaseEntity? ResolveEntity(in ScriptValue value);

    /// <summary>
    ///     Raise a JS <c>Error</c> in the calling script, with <paramref name="message" /> as the message.<br />
    ///     <b>Does not return normally</b>: it throws to abort the current callback, and the framework then raises the Error in V8.
    /// </summary>
    [DoesNotReturn]
    void ThrowError(string message);
}
