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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

public interface IScriptManager
{
    public delegate ScriptReturnValue? ScriptMethodDelegate(IScriptCallContext call);

    /// <summary>
    ///     Register a C# method on the global <c>Instance</c> cs_script class.
    /// </summary>
    /// <returns>The managed method id.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Not called on the game thread, or a live registration already exists for <c>("Instance", methodName)</c> (including from the same module).
    /// </exception>
    int RegisterMethod(string methodName, ScriptMethodDelegate callback);

    /// <summary>
    ///     Register a C# method on a cs_script JS class (e.g. "Entity", "CSPlayerController").
    /// </summary>
    /// <returns>The managed method id.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Not called on the game thread, or a live registration already exists for <c>(className, methodName)</c> (including from the same module).
    /// </exception>
    int RegisterMethod(string className, string methodName, ScriptMethodDelegate callback);

    /// <summary>
    ///     Create CallContext from native pointer. Useful for detouring.
    ///     <br /> <see langword="using" /> is required.
    /// </summary>
    /// <exception cref="InvalidOperationException">Not called on the game thread.</exception>
    IScriptCallContext CreateCallContext(nint pointer);

    /// <summary>
    ///     Read a variable by name from a running <c>point_script</c>; reaches module-scope top-level
    ///     <c>let</c>/<c>const</c>/<c>var</c> (global scope is skipped). A dotted path (<c>obj.field.sub</c>) walks properties from a
    ///     module-scope object.
    /// </summary>
    /// <remarks>
    ///     <paramref name="script" /> must derive from <c>CCSPointScriptEntity</c> and have at least one registered engine callback.
    ///     Missing leaf properties on an existing object, JS <c>null</c>/<c>undefined</c>, and object/array values return <see langword="true" /> with <paramref name="value" /> set to <see langword="null" />.
    /// </remarks>
    /// <returns><see langword="false" /> when the root binding or intermediate path segment is not resolved.</returns>
    /// <exception cref="InvalidOperationException">Not called on the game thread.</exception>
    bool TryReadVariable(IBaseEntity script, string name, out ScriptReturnValue? value);

    /// <summary>
    ///     Write a variable by name into a running <c>point_script</c>; reaches module-scope top-level bindings.
    ///     A dotted path (<c>obj.field.sub</c>) assigns a property on a module-scope object.
    /// </summary>
    /// <remarks>
    ///     Uses V8 <c>ScopeIterator::SetVariableValue</c> directly without enforcing <c>const</c> checks, so writing a top-level
    ///     <c>const</c> can silently mutate it. Object/array <see cref="ScriptReturnValue" /> handles are only valid inside an active call frame.
    /// </remarks>
    /// <returns><see langword="false" /> when the name cannot be resolved or the write fails.</returns>
    /// <exception cref="InvalidOperationException">Not called on the game thread.</exception>
    bool TryWriteVariable(IBaseEntity script, string name, ScriptReturnValue value);
}
