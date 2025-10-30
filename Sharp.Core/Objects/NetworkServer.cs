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

using System.Collections.Generic;
using Sharp.Core.CStrike;
using Sharp.Core.Helpers;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Objects;

internal sealed unsafe partial class NetworkServer : NativeObject, INetworkServer
{
    private readonly CUtlVector<nint>* _clients;

    private NetworkServer(nint ptr) : base(ptr)
        => _clients = (CUtlVector<nint>*) nint.Add(ptr, CoreGameData.CNetworkGameServer.VecClients);

    public int GetClientCount()
    {
        CheckDisposed();

        return _clients->Size;
    }

    public IGameClient? GetGameClient(PlayerSlot slot)
        => GameClient.Create(Bridges.Natives.Client.GetClientBySlot(slot));

    public IGameClient? GetGameClient(UserID userId)
        => GameClient.Create(Bridges.Natives.Client.GetClientByUserId(userId));

    public IGameClient? GetGameClient(SteamID steamId)
        => GameClient.Create(Bridges.Natives.Client.GetClientBySteamId(steamId));

    public CUtlVector<nint>* GetGameClientPointers()
    {
        CheckDisposed();

        return _clients;
    }

    public IReadOnlyList<IGameClient> GetGameClients()
        => GetGameClients(true);

    public List<IGameClient> GetGameClients(bool connected, bool inGame = false)
    {
        CheckDisposed();

        var builder = new List<IGameClient>(_clients->Size);

        for (var index = _clients->Size - 1; index >= 0; index--)
        {
            var client = GameClient.Create(_clients->Element(index));

            if (client is null || client.SignOnState < SignOnState.Connected)
            {
                continue;
            }

            builder.Add(client);
        }

        return builder;
    }

    public int GetGameClientCount(bool fullyInGame = false)
    {
        CheckDisposed();

        var count = 0;
        var state = fullyInGame ? SignOnState.Full : SignOnState.Connected;

        for (var index = _clients->Size - 1; index >= 0; index--)
        {
            var actual = _clients->Element(index);

            if (actual == nint.Zero)
            {
                continue;
            }

            var signOnState = (SignOnState) actual.GetInt32(CoreGameData.GameClient.SignonState);

            if (signOnState < state)
            {
                continue;
            }

            count++;
        }

        return count;
    }
}
