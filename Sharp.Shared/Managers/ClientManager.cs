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
    ///     Add <see cref="IClientListener"/> to listen for events
    /// </summary>
    void InstallClientListener(IClientListener listener);

    /// <summary>
    ///     Remove <see cref="IClientListener"/>
    /// </summary>
    void RemoveClientListener(IClientListener listener);

    /// <summary>
    ///     Listen for StringCommand (Virtual Command) sent by <see cref="IGameClient"/> <br />
    ///     Automatically registers with ms_ prefix
    /// </summary>
    void InstallCommandCallback(string command, DelegateClientCommand callback);

    /// <summary>
    ///     Remove listener for StringCommand (Virtual Command) sent by <see cref="IGameClient"/> <br />
    ///     Automatically registers with ms_ prefix
    /// </summary>
    void RemoveCommandCallback(string command, DelegateClientCommand callback);

    /// <summary>
    ///     Listen for ConCommand sent by <see cref="IGameClient"/>
    /// </summary>
    void InstallCommandListener(string command, DelegateClientCommand callback);

    /// <summary>
    ///     Remove listener for ConCommand sent by <see cref="IGameClient"/>
    /// </summary>
    void RemoveCommandListener(string command, DelegateClientCommand callback);

    /// <summary>
    ///     Find an <see cref="IGameClient"/> by Slot
    /// </summary>
    IGameClient? GetGameClient(PlayerSlot slot);

    /// <summary>
    ///     Find an <see cref="IGameClient"/> by UserId
    /// </summary>
    IGameClient? GetGameClient(UserID userId);

    /// <summary>
    ///     Find an <see cref="IGameClient"/> by SteamId
    /// </summary>
    IGameClient? GetGameClient(SteamID steamId);

    /// <summary>
    ///     Get connected <see cref="IGameClient"/>s
    /// </summary>
    IEnumerable<IGameClient> GetGameClients(bool inGame = false);

    /// <summary>
    ///     Get connected <see cref="IGameClient"/>s
    /// </summary>
    List<IGameClient> GetGameClientList(bool inGame = false);

    /// <summary>
    ///     Get the number of <see cref="IGameClient"/>s in the client pool
    /// </summary>
    int GetClientCount(bool inGame = false);

    /// <summary>
    ///     Immediately kick a player from the game
    /// </summary>
    /// <param name="client"><see cref="IGameClient"/></param>
    /// <param name="internalReason">Only recorded in internal game log</param>
    /// <param name="msgId">The kick reason will be displayed to the <see cref="IGameClient"/> based on ID</param>
    void KickClient(IGameClient    client,
        string                     internalReason,
        NetworkDisconnectionReason msgId = NetworkDisconnectionReason.Invalid);

    /// <summary>
    ///     Query the value of a given cvar from the client
    /// </summary>
    /// <param name="client"><see cref="IGameClient"/></param>
    /// <param name="name">ConVar name</param>
    /// <param name="callback">Callback</param>
    /// <returns>Cookie</returns>
    /// <remarks>
    ///     <para>Can query any cvar that does not have flag <see cref="ConVarFlags.ServerCannotQuery"/> set.</para>
    ///     <para>Note that client-side values can be manipulated by cheats/hacks and should not be trusted for security-critical decisions.</para>
    /// </remarks>
    int QueryConVar(IGameClient client, string name, Action<IGameClient, QueryConVarValueStatus, string, string> callback);

    /// <summary>
    ///     Clear Admins and re-trigger OnAdminReload
    /// </summary>
    void ReloadAdmins();

    /// <summary>
    ///     Find Admin by SteamID
    /// </summary>
    IAdmin? FindAdmin(SteamID identity);

    /// <summary>
    ///     Find Admin by name
    /// </summary>
    IAdmin? FindAdmin(string name);

    /// <summary>
    ///     Create Admin
    /// </summary>
    IAdmin CreateAdmin(SteamID identity, string name, byte immunity = 0);

    /// <summary>
    ///     Delete Admin
    /// </summary>
    void DeleteAdmin(IAdmin admin);

    /// <summary>
    ///     Get all Admins
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<IAdmin> GetAdmins();
}
