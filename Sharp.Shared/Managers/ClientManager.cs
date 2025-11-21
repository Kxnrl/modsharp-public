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
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

public interface IClientManager
{
    public delegate ECommandAction DelegateClientCommand(IGameClient client, StringCommand command);

    /// <summary>
    ///     添加ClientListener以监听事件
    /// </summary>
    void InstallClientListener(IClientListener listener);

    /// <summary>
    ///     移除ClientListener
    /// </summary>
    void RemoveClientListener(IClientListener listener);

    /// <summary>
    ///     监听IClient发送的StringCommand (Virtual Command) <br />
    ///     会自动注册ms_开头
    /// </summary>
    void InstallCommandCallback(string command, DelegateClientCommand callback);

    /// <summary>
    ///     移除监听IClient发送的StringCommand (Virtual Command) <br />
    ///     会自动注册ms_开头
    /// </summary>
    void RemoveCommandCallback(string command, DelegateClientCommand callback);

    /// <summary>
    ///     监听IClient发送的ConCommand
    /// </summary>
    void InstallCommandListener(string command, DelegateClientCommand callback);

    /// <summary>
    ///     移除监听IClient发送的ConCommand
    /// </summary>
    void RemoveCommandListener(string command, DelegateClientCommand callback);

    /// <summary>
    ///     通过Slot查找某个IClient
    /// </summary>
    IGameClient? GetGameClient(PlayerSlot slot);

    /// <summary>
    ///     通过UserId查找某个IClient
    /// </summary>
    IGameClient? GetGameClient(UserID userId);

    /// <summary>
    ///     通过SteamId查找某个IClient
    /// </summary>
    IGameClient? GetGameClient(SteamID steamId);

    /// <summary>
    ///     获取已建立链接的IClient
    /// </summary>
    IEnumerable<IGameClient> GetGameClients(bool inGame = false);

    /// <summary>
    ///     获取已建立链接的IClient
    /// </summary>
    List<IGameClient> GetGameClientList(bool inGame = false);

    /// <summary>
    ///     获取Client池的IClient数量
    /// </summary>
    int GetClientCount(bool inGame = false);

    /// <summary>
    ///     把某个玩家立即踢出游戏
    /// </summary>
    /// <param name="client">IClient</param>
    /// <param name="internalReason">只在游戏内部Log中记录</param>
    /// <param name="msgId">踢人的理由会直接根据ID展示给IClient</param>
    void KickClient(IGameClient    client,
        string                     internalReason,
        NetworkDisconnectionReason msgId = NetworkDisconnectionReason.Invalid);

    /// <summary>
    ///     远程查询客户ConVar
    /// </summary>
    /// <param name="client">IClient</param>
    /// <param name="name">ConVar名</param>
    /// <param name="callback">Callback</param>
    /// <returns>Cookie</returns>
    int QueryConVar(IGameClient client, string name, Action<IGameClient, QueryConVarValueStatus, string, string> callback);

    /// <summary>
    ///     清空Admins并重新触发OnAdminReload
    /// </summary>
    void ReloadAdmins();

    /// <summary>
    ///     通过SteamID查找Admin
    /// </summary>
    IAdmin? FindAdmin(SteamID identity);

    /// <summary>
    ///     通过名字查找Admin
    /// </summary>
    IAdmin? FindAdmin(string name);

    /// <summary>
    ///     创建Admin
    /// </summary>
    IAdmin CreateAdmin(SteamID identity, string name, byte immunity = 0);

    /// <summary>
    ///     删除Admin
    /// </summary>
    void DeleteAdmin(IAdmin admin);

    /// <summary>
    ///     获取所有的Admins
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<IAdmin> GetAdmins();
}
