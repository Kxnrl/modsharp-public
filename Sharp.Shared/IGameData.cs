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
    ///     Get offset by key, throws exception if not found
    /// </summary>
    /// <param name="name">Key name</param>
    /// <returns>Offset value</returns>
    int GetOffset(string name);

    /// <summary>
    ///     Get offset using class::member format
    /// </summary>
    /// <param name="classname">Class name</param>
    /// <param name="name">Member name</param>
    /// <returns>Offset value</returns>
    int GetOffset(string classname, string name);

    /// <summary>
    ///     Get address by key, throws exception if not found
    /// </summary>
    /// <param name="name">Key name</param>
    /// <returns>Address value</returns>
    nint GetAddress(string name);

    /// <summary>
    ///     Get address using class::member format
    /// </summary>
    /// <param name="classname">Class name</param>
    /// <param name="name">Member name</param>
    /// <returns>Address value</returns>
    nint GetAddress(string classname, string name);

    /// <summary>
    ///     Get virtual function index by key
    /// </summary>
    /// <param name="name"></param>
    /// <returns>Index value</returns>
    int GetVFuncIndex(string name);

    /// <summary>
    ///     Get virtual function index using class::member format
    /// </summary>
    /// <param name="classname">Class name</param>
    /// <param name="name">Member name</param>
    /// <returns>Index value</returns>
    int GetVFuncIndex(string classname, string name);

    /// <summary>
    ///     Register GameData file
    /// </summary>
    /// <param name="path">File path (automatically searches files in sharp/gamedata directory)</param>
    void Register(string path);

    /// <summary>
    ///     Unregister GameData file
    /// </summary>
    /// <param name="path">File path (automatically searches files in sharp/gamedata directory)</param>
    void Unregister(string path);
}
