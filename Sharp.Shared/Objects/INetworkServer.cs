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
using Sharp.Shared.CStrike;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;

namespace Sharp.Shared.Objects;

public interface INetworkServer : INativeObject
{
    /// <summary>
    ///     获取Client池的IClient数量
    /// </summary>
    int GetClientCount();

    /// <summary>
    ///     通过PlayerSlot获取IClient
    /// </summary>
    IGameClient? GetGameClient(PlayerSlot slot);

    /// <summary>
    ///     通过UserId获取IClient
    /// </summary>
    IGameClient? GetGameClient(UserID userId);

    /// <summary>
    ///     通过SteamId获取IClient
    /// </summary>
    IGameClient? GetGameClient(SteamID steamId);

    /// <summary>
    ///     获取IClient池
    /// </summary>
    IReadOnlyList<IGameClient> GetGameClients();

    /// <summary>
    ///     获取IClient
    /// </summary>
    List<IGameClient> GetGameClients(bool connected, bool inGame = false);

    /// <summary>
    ///     获取sv->m_vecClients
    /// </summary>
    /// <returns></returns>
    unsafe CUtlVector<nint>* GetGameClientPointers();

    /// <summary>
    ///     获取游戏内Client数量
    /// </summary>
    int GetGameClientCount(bool fullyInGame = false);
}
