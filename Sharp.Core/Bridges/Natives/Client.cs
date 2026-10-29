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
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Core.Bridges.Natives;

public static partial class Client
{
    public static partial IntPtr GetClientBySlot(PlayerSlot slot);

    public static partial IntPtr GetClientByUserId(UserID steamId);

    public static partial IntPtr GetClientBySteamId(SteamID steamId);

    public static partial string GetNetWorkAddress(IGameClient client);

    public static partial float GetTimeConnected(IGameClient ptr);

    public static partial void ConsolePrint(IGameClient ptr, string message);

    public static partial void SetName(IGameClient ptr, string name);

    public static partial bool SayChatMessage(IGameClient ptr, bool teamOnly, string message);

    public static partial void AddCommandCallback(string command);

    public static partial void AddCommandListener(string command);

    public static partial void KickClient(IGameClient ptr,
        string                                        internalReason,
        NetworkDisconnectionReason                    networkDisconnectionReason);

    public static partial IntPtr GetConVarValue(IGameClient ptr, string name);

    public static partial void ClientCommand(IGameClient ptr, string command);

    public static partial void FakeClientCommand(IGameClient ptr, string command);

    public static partial void ExecuteStringCommand(IGameClient ptr, string command);
}
