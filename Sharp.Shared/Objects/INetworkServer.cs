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
using Sharp.Shared.CStrike;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;

namespace Sharp.Shared.Objects;

public interface INetworkServer : INativeObject
{
    /// <summary>
    ///     Get total number of clients in pool
    /// </summary>
    int GetClientCount();

    /// <summary>
    ///     Get number of clients in pool with filters
    ///     <remarks><paramref name="inGame" /> will override <paramref name="connected" /></remarks>
    /// </summary>
    int GetClientCount(bool connected, bool inGame = false);

    /// <summary>
    ///     Get client by player slot
    /// </summary>
    IGameClient? GetGameClient(PlayerSlot slot);

    /// <summary>
    ///     Get client by user ID
    /// </summary>
    IGameClient? GetGameClient(UserID userId);

    /// <summary>
    ///     Get client by Steam ID
    /// </summary>
    IGameClient? GetGameClient(SteamID steamId);

    /// <summary>
    ///     Get all clients from pool
    /// </summary>
    [Obsolete("Use overload GetGameClients(bool, bool) instead, will be removed in 2.2")]
    IReadOnlyList<IGameClient> GetGameClients();

    /// <summary>
    ///     Get filtered list of clients <br />
    ///     <remarks><paramref name="inGame" /> will override <paramref name="connected" /></remarks>
    /// </summary>
    List<IGameClient> GetGameClients(bool connected, bool inGame = false);

    /// <summary>
    ///     Retrieves a pointer to the internal server client list container.
    /// </summary>
    /// <returns></returns>
    unsafe CUtlVector<nint>* GetGameClientPointers();

    /// <summary>
    ///     Get number of in-game clients
    /// </summary>
    [Obsolete("Use GetClientCount(bool, bool) instead, will be removed in 2.2")]
    int GetGameClientCount(bool fullyInGame = false);
}
