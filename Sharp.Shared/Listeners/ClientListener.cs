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

using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Shared.Listeners;

public interface IClientListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     Listener version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority (higher values run first)
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     Called before <see cref="OnClientPostAdminCheck" /> to determine whether admin post-check should be blocked.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Runs deferred at the end of a game frame (<c>OnGameFramePost</c> with <c>last == true</c>) after
    ///         <see cref="OnClientPutInServer" /> once the client is still valid and <see cref="IGameClient.IsAuthenticated" /> is <see langword="true" />.
    ///     </para>
    ///     <para>
    ///         Not called for fake clients (bots), which proceed directly to <see cref="OnClientPostAdminCheck" />.
    ///         All installed listeners are invoked without short-circuiting; if any listener returns <see langword="true" />,
    ///         <see cref="OnClientPostAdminCheck" /> is skipped for this connection.
    ///     </para>
    /// </remarks>
    /// <param name="client">The authenticated client.</param>
    /// <returns><see langword="true" /> to block <see cref="OnClientPostAdminCheck" />; <see langword="false" /> to allow it.</returns>
    bool OnClientPreAdminCheck(IGameClient client)
        => false;

    /// <summary>
    ///     Called when a client finishes connecting to the server.
    /// </summary>
    /// <remarks>
    ///     For human players, this is invoked after <c>CSource2GameClients::CheckConnect</c> allows the connection (before Steam authentication completes).
    ///     For fake clients (bots), <c>CheckConnect</c> is bypassed and this is invoked after <c>CSource2GameClients::Connected</c>.
    /// </remarks>
    /// <param name="client">The connected client.</param>
    void OnClientConnected(IGameClient client)
    {
    }

    /// <summary>
    ///     Called after the client becomes active in the game (<c>CSource2GameClients::Active</c> post-hook).
    /// </summary>
    /// <remarks>
    ///     Despite its name, this callback is bound to the engine's <c>OnClientActive</c> forward
    ///     (<c>CSource2GameClients::Active</c> post-hook), not <c>CSource2GameClients::PutInServer</c>.
    ///     After notifying listeners, the client is queued for the deferred admin check (<see cref="OnClientPreAdminCheck" /> / <see cref="OnClientPostAdminCheck" />).
    /// </remarks>
    /// <param name="client">The client that became active in the game.</param>
    void OnClientPutInServer(IGameClient client)
    {
    }

    /// <summary>
    ///     Called after the client passes the deferred admin check following <see cref="OnClientPutInServer" />.
    /// </summary>
    /// <remarks>
    ///     Runs at the end of a game frame (<c>OnGameFramePost</c> with <c>last == true</c>).
    ///     For human players, this waits until <see cref="IGameClient.IsAuthenticated" /> is <see langword="true" /> and runs only if no
    ///     <see cref="OnClientPreAdminCheck" /> listener returned <see langword="true" />.
    ///     For fake clients (bots), authentication and <see cref="OnClientPreAdminCheck" /> are skipped.
    ///     Queued checks are cleared if the client becomes invalid or the map shuts down.
    /// </remarks>
    /// <param name="client">The client that completed the admin check.</param>
    void OnClientPostAdminCheck(IGameClient client)
    {
    }

    /// <summary>
    ///     Called before a client disconnects from the server (<c>CSource2GameClients::Disconnect</c> pre-hook).
    /// </summary>
    /// <remarks>
    ///     On dedicated servers (<c>-dedicated</c>), if a client disconnects while the server is still loading
    ///     (before the server becomes active and while the client has a controller), both <see cref="OnClientDisconnecting" />
    ///     and <see cref="OnClientDisconnected" /> are invoked back-to-back before <c>CNetworkGameServer::DisconnectClient</c>.
    /// </remarks>
    /// <param name="client">The disconnecting client.</param>
    /// <param name="reason">The network disconnection reason.</param>
    void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
    }

    /// <summary>
    ///     Called after a client has disconnected from the server (<c>CSource2GameClients::Disconnect</c> post-hook).
    /// </summary>
    /// <remarks>
    ///     On dedicated servers (<c>-dedicated</c>), if a client disconnects while the server is still loading,
    ///     this may be invoked immediately after <see cref="OnClientDisconnecting" /> before <c>CNetworkGameServer::DisconnectClient</c>.
    /// </remarks>
    /// <param name="client">The disconnected client.</param>
    /// <param name="reason">The network disconnection reason.</param>
    void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
    }

    /// <summary>
    ///     Called after a client changes a ConVar in <see cref="IGameClient.GetConVars" /> that has the
    ///     <see cref="ConVarFlags.UserInfo" /> flag set (<c>CSource2GameClients::SettingsChanged</c> post-hook).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is commonly triggered when a player changes their name or other user info settings.
    ///     </para>
    ///     <para>
    ///         You can use this to validate or revert changes, such as enforcing name restrictions.
    ///     </para>
    /// </remarks>
    /// <param name="client">The client who changed the setting.</param>
    void OnClientSettingChanged(IGameClient client)
    {
    }

    /// <summary>
    ///     Called when an in-game client sends a chat message (<c>HostSay</c> pre-hook).
    /// </summary>
    /// <param name="client">The client who sent the message.</param>
    /// <param name="teamOnly"><see langword="true" /> for team chat; <see langword="false" /> for all chat.</param>
    /// <param name="isCommand">
    ///     <see langword="true" /> if <paramref name="message" /> matched a configured chat trigger prefix (length 2..255)
    ///     and a command registered via <see cref="Managers.IClientManager.InstallCommandCallback" />.
    /// </param>
    /// <param name="commandName">The chat command token (<c>"say"</c> or <c>"say_team"</c>), not the parsed chat trigger command.</param>
    /// <param name="message">The chat message text with surrounding quotes stripped.</param>
    /// <returns>
    ///     Both <see cref="ECommandAction.Handled" /> and <see cref="ECommandAction.Stopped" /> block this chat message from being broadcast;
    ///     with multiple listeners, all listeners run and the highest <see cref="ECommandAction" /> value is used.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Empty messages, messages starting with an ASCII control character, whitespace-only messages (when <c>ms_chat_block_whitespace</c> is enabled),
    ///         and chat commands from clients not yet in game are dropped in native code before this callback runs.
    ///     </para>
    ///     <para>
    ///         When <paramref name="isCommand" /> is <see langword="true" />, the registered command callback is already queued for the start of the next frame;
    ///         returning <see cref="ECommandAction.Handled" /> or <see cref="ECommandAction.Stopped" /> only suppresses the chat message, not the queued command.
    ///     </para>
    ///     <para>
    ///         Any message of length 2..255 starting with a silent prefix (<c>ChatCommand.TriggerPrefixes.Silent</c>) is blocked regardless of the returned value
    ///         or whether <paramref name="isCommand" /> is <see langword="true" />.
    ///     </para>
    /// </remarks>
    ECommandAction OnClientSayCommand(IGameClient client, bool teamOnly, bool isCommand, string commandName, string message)
        => ECommandAction.Skipped;

    /// <summary>
    ///     Called at the end of a game frame after the obsolete <see cref="Managers.IClientManager.ReloadAdmins" /> is invoked.
    /// </summary>
    void OnAdminCacheReload()
    {
    }
}
