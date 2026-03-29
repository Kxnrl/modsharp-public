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

using Sharp.Shared.CStrike;
using Sharp.Shared.Types;

namespace Sharp.Shared.Objects;

public interface INetworkingStringTable : INativeObject
{
    /// <summary>
    ///     Table name
    /// </summary>
    string GetName();

    /// <summary>
    ///     Table ID
    /// </summary>
    int GetId();

    /// <summary>
    ///     Get How many entries this table has
    /// </summary>
    /// <returns></returns>
    int GetStringCount();

    /// <summary>
    ///     Insert a new string to current string table
    /// </summary>
    /// <param name="server">True if this string data is server side</param>
    /// <param name="value">the string key</param>
    /// <param name="data">the string value</param>
    int AddString(bool server, string value, byte[]? data);

    /// <summary>
    ///     Get string value by index
    /// </summary>
    string GetString(int index);

    /// <summary>
    ///     Get UserData from given index
    /// </summary>
    unsafe StringTableUserData* GetStringUserData(int index);

    /// <summary>
    ///     Override UserData
    /// </summary>
    void SetStringUserData(int index, byte[]? data);

    /// <summary>
    ///     Get the index from given string value
    /// </summary>
    /// <returns>-1 if not found</returns>
    int FindStringIndex(string value);
}
