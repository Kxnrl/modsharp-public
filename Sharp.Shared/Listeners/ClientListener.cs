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

using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Shared.Listeners;

public interface IClientListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     接口实现的版本号
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     优先级
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     是否允许检查Admin
    /// </summary>
    /// <returns>True = Block Check</returns>
    bool OnClientPreAdminCheck(IGameClient client)
        => false;

    void OnClientConnected(IGameClient client)
    {
    }

    void OnClientPutInServer(IGameClient client)
    {
    }

    void OnClientPostAdminCheck(IGameClient client)
    {
    }

    void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
    }

    void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
    }

    void OnClientSettingChanged(IGameClient client)
    {
    }

    ECommandAction OnClientSayCommand(IGameClient client, bool teamOnly, bool isCommand, string commandName, string message)
        => ECommandAction.Skipped;

    void OnAdminCacheReload()
    {
    }
}
