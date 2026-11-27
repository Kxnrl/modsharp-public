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
using System.Collections.Generic;

namespace Sharp.Shared.Hooks;

public interface IVirtualHook : IRuntimeNativeHook
{
    /// <summary>
    ///     Prepare virtual hook using a GameData vfunc index.
    /// </summary>
    /// <remarks>
    ///     An invalid <paramref name="module" /> alias or missing vtable for <paramref name="class" /> triggers a native <c>FatalError</c>.
    /// </remarks>
    /// <param name="module">Engine module short alias (see <see cref="IModSharp.FindPattern" />)</param>
    /// <param name="class">C++ class name</param>
    /// <param name="function">GameData virtual function name</param>
    /// <param name="hookFn">UnmanagedCallersOnly static function</param>
    /// <exception cref="KeyNotFoundException">GameData virtual function index not found</exception>
    /// <exception cref="InvalidOperationException">The hook is already installed</exception>
    void Prepare(string module, string @class, string function, nint hookFn);

    /// <summary>
    ///     Prepare virtual hook using a 0-based vtable slot index.
    /// </summary>
    /// <remarks>
    ///     An invalid <paramref name="module" /> alias or missing vtable for <paramref name="class" /> triggers a native <c>FatalError</c>.
    /// </remarks>
    /// <param name="module">Engine module short alias (see <see cref="IModSharp.FindPattern" />)</param>
    /// <param name="class">C++ class name</param>
    /// <param name="offset">0-based virtual function slot index (not byte offset)</param>
    /// <param name="hookFn">UnmanagedCallersOnly static function</param>
    /// <exception cref="InvalidOperationException">The hook is already installed</exception>
    void Prepare(string module, string @class, int offset, nint hookFn);

    /// <summary>
    ///     Prepare virtual hook from an existing vtable pointer and 0-based vtable slot index.
    /// </summary>
    /// <param name="vTable">Virtual table address</param>
    /// <param name="offset">0-based virtual function slot index (not byte offset)</param>
    /// <param name="hookFn">UnmanagedCallersOnly static function</param>
    /// <exception cref="InvalidOperationException">The hook is already installed</exception>
    void Prepare(nint vTable, int offset, nint hookFn);
}
