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

using Sharp.Core.Bridges;
using Sharp.Core.CStrike;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Core.Objects;

internal partial class NetworkingStringTable : NativeObject, INetworkingStringTable
{
    public string GetName()
        => CoreBridge.NetworkingStringTableHelperInstance.GetName(_this);

    public int GetId()
        => CoreBridge.NetworkingStringTableHelperInstance.GetId(_this);

    public int GetStringCount()
        => CoreBridge.NetworkingStringTableHelperInstance.GetStringCount(_this);

    public int AddString(bool server, string value, byte[]? data)
        => CoreBridge.NetworkingStringTableHelperInstance.AddString(_this, server, value, data);

    public string GetString(int index)
        => CoreBridge.NetworkingStringTableHelperInstance.GetString(_this, index);

    public unsafe StringTableUserData* GetStringUserData(int index)
        => CoreBridge.NetworkingStringTableHelperInstance.GetStringUserData(_this, index);

    public void SetStringUserData(int index, byte[]? data)
        => CoreBridge.NetworkingStringTableHelperInstance.SetStringUserData(_this, index, data);

    public int FindStringIndex(string value)
        => CoreBridge.NetworkingStringTableHelperInstance.FindStringIndex(_this, value);
}
