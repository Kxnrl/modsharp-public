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

using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal unsafe partial class NetworkingStringTableHelper : NativeObject
{
    public partial string GetName(nint table);

    public partial int GetId(nint table);

    public partial int GetStringCount(nint table);

    // ReSharper disable once MemberCanBePrivate.Global
    public partial int AddString(nint table, bool server, string value, byte* data, int size);

    public int AddString(nint table, bool server, string value, byte[]? data)
    {
        if (data is null)
        {
            return AddString(table, server, value, null, 0);
        }

        fixed (byte* ptr = data)
        {
            return AddString(table, server, value, ptr, data.Length);
        }
    }

    public partial string GetString(nint table, int index);

    public partial StringTableUserData* GetStringUserData(nint pTable, int index);

    // ReSharper disable once MemberCanBePrivate.Global
    public partial void SetStringUserData(nint table, int index, byte* data, int size);

    public void SetStringUserData(nint table, int index, byte[]? data)
    {
        if (data is null)
        {
            SetStringUserData(table, index, null, 0);

            return;
        }

        fixed (byte* ptr = data)
        {
            SetStringUserData(table, index, ptr, data.Length);
        }
    }

    public partial int FindStringIndex(nint table, string value);
}
