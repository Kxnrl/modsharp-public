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

    /// <summary>
    ///     Number of arguments passed by the JS caller.
    /// </summary>
    int ArgCount { get; }

    /// <summary>
    ///     Gets the <see cref="ScriptValueType" /> of the argument at <paramref name="index" />.
    /// </summary>
    /// <remarks>JS functions are reported as <see cref="ScriptValueType.Object" /> (use <see cref="GetFunction" /> to capture them).</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    ScriptValueType GetArgType(int index);

    /// <summary>
    ///     Reads a boolean argument, or <see langword="null" /> if the JS value is <c>null</c>/<c>undefined</c>.
    ///     Raises a JS <c>Error</c> if the argument has a different type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    bool? GetBool(int index);

    /// <summary>
    ///     Reads a number argument, or <see langword="null" /> if the JS value is <c>null</c>/<c>undefined</c>.
    ///     Raises a JS <c>Error</c> if the argument has a different type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    double? GetNumber(int index);

    /// <summary>
    ///     Reads a string argument, or <see langword="null" /> if the JS value is <c>null</c>/<c>undefined</c>.
    ///     Raises a JS <c>Error</c> if the argument has a different type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    string? GetString(int index);

    /// <summary>
    ///     Reads a vector argument, or <see langword="null" /> if the JS value is <c>null</c>/<c>undefined</c>.
    ///     Raises a JS <c>Error</c> if the argument has a different type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    Vector? GetVector(int index);

    /// <summary>
    ///     Reads an entity argument, or <see langword="null" /> if the JS value is <c>null</c>/<c>undefined</c> or the entity no longer exists.
    ///     Raises a JS <c>Error</c> if the argument has a different type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    IBaseEntity? GetEntity(int index);

    /// <summary>
    ///     Captures a JS function argument as a persistent <see cref="IScriptCallback" />, or returns <see langword="null" /> if the argument is not a JS function (does not raise a JS <c>Error</c>).
    /// </summary>
    /// <remarks>The returned callback owns a <c>v8::Global</c> handle and must be disposed on the game main thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    IScriptCallback? GetFunction(int index);

    // ---------------------------------------------------------------------------------------------
    //  Object / Array
    //  Arg: JS object/array → ScriptObject / ScriptArray
    //  Ret: NewObject / NewArray / JS null/undefined → null
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    ///     Reads a JS object argument, or <see langword="null" /> if the JS value is <c>null</c>/<c>undefined</c>.
    ///     Raises a JS <c>Error</c> if the argument has a different type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    ScriptObject? GetObject(int index);

    /// <summary>
    ///     Reads a JS array argument, or <see langword="null" /> if the JS value is <c>null</c>/<c>undefined</c>.
    ///     Raises a JS <c>Error</c> if the argument has a different type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
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
