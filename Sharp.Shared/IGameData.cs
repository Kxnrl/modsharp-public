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

namespace Sharp.Shared;

public interface IGameData
{
    /// <summary>
    ///     Try to get offset by key
    /// </summary>
    /// <param name="name">Key name</param>
    /// <param name="offset">Found offset value</param>
    /// <returns>True if found, false otherwise</returns>
    bool GetOffset(string name, out int offset);

    /// <summary>
    ///     Try to get address by key
    /// </summary>
    /// <param name="name">Key name</param>
    /// <param name="address">Found address value</param>
    /// <returns>True if found, false otherwise</returns>
    bool GetAddress(string name, out nint address);

    /// <summary>
    ///     Try to get virtual function index by key
    /// </summary>
    /// <param name="name">Key name</param>
    /// <param name="index">Found index value</param>
    /// <returns>True if found, false otherwise</returns>
    bool GetVFuncIndex(string name, out int index);

    /// <summary>
    ///     Get offset by key.
    /// </summary>
    /// <param name="name">Key name</param>
    /// <returns>Offset value</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="name" /> is not found.</exception>
    int GetOffset(string name);

    /// <summary>
    ///     Get offset using <c>classname::name</c> format, walking schema base classes if needed.
    /// </summary>
    /// <param name="classname">Schema class name</param>
    /// <param name="name">Member name</param>
    /// <returns>Offset value</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">Key not found in <paramref name="classname" /> or its schema base classes.</exception>
    int GetOffset(string classname, string name);

    /// <summary>
    ///     Get address by key.
    /// </summary>
    /// <param name="name">Key name</param>
    /// <returns>Address value</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="name" /> is not found.</exception>
    nint GetAddress(string name);

    /// <summary>
    ///     Get address using <c>classname::name</c> format, walking schema base classes if needed.
    /// </summary>
    /// <param name="classname">Schema class name</param>
    /// <param name="name">Member name</param>
    /// <returns>Address value</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">Key not found in <paramref name="classname" /> or its schema base classes.</exception>
    nint GetAddress(string classname, string name);

    /// <summary>
    ///     Get virtual function index by key.
    /// </summary>
    /// <param name="name">Key name</param>
    /// <returns>Index value</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="name" /> is not found.</exception>
    int GetVFuncIndex(string name);

    /// <summary>
    ///     Get virtual function index using <c>classname::name</c> format, walking schema base classes if needed.
    /// </summary>
    /// <param name="classname">Schema class name</param>
    /// <param name="name">Member name</param>
    /// <returns>Index value</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">Key not found in <paramref name="classname" /> or its schema base classes.</exception>
    int GetVFuncIndex(string classname, string name);

    /// <summary>
    ///     Register and eagerly resolve a GameData file (and apply any declared <c>Patches</c>).
    /// </summary>
    /// <remarks>
    ///     Appends <c>.jsonc</c> automatically if missing. Registration is tracked globally (case-insensitive, not reference-counted).
    /// </remarks>
    /// <param name="path">File path relative to <c>sharp/gamedata</c> (without needing the <c>.jsonc</c> suffix).</param>
    /// <exception cref="System.InvalidOperationException">The file is already registered or native loading failed.</exception>
    void Register(string path);

    /// <summary>
    ///     Unregister a GameData file and restore any <c>Patches</c> it applied.
    /// </summary>
    /// <remarks>Appends <c>.jsonc</c> automatically if missing. Registration is not reference-counted.</remarks>
    /// <param name="path">File path relative to <c>sharp/gamedata</c>.</param>
    void Unregister(string path);
}
