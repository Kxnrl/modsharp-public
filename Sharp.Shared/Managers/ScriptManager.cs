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
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

public interface IScriptManager
{
    public delegate ScriptReturnValue? ScriptMethodDelegate(IScriptCallContext call);

    /// <summary>
    ///     Register a C# method on a global Instance.
    /// </summary>
    /// <returns>the managed method id</returns>
    /// <exception cref="InvalidOperationException">
    ///     another live module already registered the same (className, methodName).
    /// </exception>
    int RegisterMethod(string methodName, ScriptMethodDelegate callback);

    /// <summary>
    ///     Register a C# method on a cs_script JS class (e.g. "Entity", "CSPlayerController").
    /// </summary>
    /// <returns>the managed method id</returns>
    /// <exception cref="InvalidOperationException">
    ///     another live module already registered the same (className, methodName).
    /// </exception>
    int RegisterMethod(string className, string methodName, ScriptMethodDelegate callback);

    /// <summary>
    ///     Create CallContext from native pointer. useful for detour.
    ///     <br /> <see langword="using" /> is required.
    /// </summary>
    /// <exception cref="InvalidOperationException">not called on the game thread.</exception>
    IScriptCallContext CreateCallContext(nint pointer);
}
