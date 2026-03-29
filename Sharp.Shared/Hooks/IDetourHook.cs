/* 
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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
using System.Collections.Generic;

namespace Sharp.Shared.Hooks;

public interface IDetourHook : IRuntimeNativeHook, IDisposable
{
    /// <summary>
    ///     Prepare hook
    /// </summary>
    /// <param name="gamedata">gamedata key</param>
    /// <param name="hookFn">UnmanagedCallersOnly static function</param>
    /// <exception cref="EntryPointNotFoundException">Thrown when GameData is not found / null pointer / invalid field</exception>
    /// <exception cref="KeyNotFoundException">Thrown when faiils to find gamedata</exception>
    void Prepare(string gamedata, nint hookFn);

    /// <summary>
    ///     Prepare Hook <br />
    ///     <remarks>
    ///         Need to manually convert to nint <br />
    ///     </remarks>
    /// </summary>
    /// <example>
    ///     <code>
    ///         (nint) (delegate* unmanaged<void>) &Test
    ///  </code>
    /// </example>
    /// <param name="pTargetFn">Native function address</param>
    /// <param name="hookFn">UnmanagedCallersOnly static function</param>
    void Prepare(nint pTargetFn, nint hookFn);
}
