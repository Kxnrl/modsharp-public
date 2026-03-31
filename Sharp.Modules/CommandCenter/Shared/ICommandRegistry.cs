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

using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using DelegateClientCommand = Sharp.Shared.Managers.IClientManager.DelegateClientCommand;

namespace Sharp.Modules.CommandCenter.Shared;

public interface ICommandRegistry
{
    /// <summary>
    ///     Registers a client command.<br />
    ///     The command can be invoked in the following ways:<br />
    ///     1. Chat: Type .{<paramref name="command" />} (e.g., <b>.ztele</b>).<br />
    ///     (Note: You can replace "." with "!" or "/"; they function identically.)<br />
    ///     2. Client Console: Requires the `ms_` prefix (e.g., <b>ms_ztele</b>).<br />
    ///     Commands registered via this method CANNOT be used in the server console.
    /// </summary>
    /// <param name="command">
    ///     The command name. The prefix 'ms_' is automatically added during registration.
    ///     Do not include 'ms_' manually, or the command will result in a double prefix (e.g., "ms_ms_command").
    /// </param>
    /// <param name="call">The callback function to execute.</param>
    void RegisterClientCommand(string command, Action<IGameClient, StringCommand> call);

    /// <summary>
    ///     Registers a server command.<br />
    ///     Commands registered with this method only work in the server console.
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <param name="call">The callback function to execute.</param>
    /// <param name="description">The command description.</param>
    /// <param name="addPrefix">Whether to add the 'ms_' prefix to the command. Defaults to true.</param>
    void RegisterServerCommand(string command, Action<StringCommand> call, string description = "", bool addPrefix = true);

    /// <summary>
    ///     Registers a server command.<br />
    ///     Commands registered with this method only work in the server console.
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <param name="call">The callback function to execute.</param>
    /// <param name="description">The command description.</param>
    /// <param name="addPrefix">Whether to add the 'ms_' prefix to the command. Defaults to true.</param>
    void RegisterServerCommand(string command, Action call, string description = "", bool addPrefix = true);

    /// <summary>
    ///     Registers a "generic" command usable in in-game chat, client console, and server console.
    ///     <br />
    ///     Note: This will strictly enforce the 'ms_' prefix.
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <param name="call">The callback function to execute.</param>
    /// <param name="description">The command description.</param>
    void RegisterGenericCommand(string command, Action<IGameClient?, StringCommand> call, string description = "");

    /// <summary>
    ///     Registers a console command. <br />
    ///     Commands registered with this method work in both the client console and server console (but not chat).
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <param name="callback">The callback function to execute.</param>
    /// <param name="addPrefix">Whether to add the 'ms_' prefix to the command. Defaults to true.</param>
    void RegisterConsoleCommand(string command, Action<IGameClient?, StringCommand> callback, bool addPrefix = true);

    /// <summary>
    ///     Adds a listener for a specific command.<br />
    ///     This is primarily used to listen for commands entered in the client console (e.g., player_ping).<br />
    /// </summary>
    /// <param name="commandName">The name of the command to listen for.</param>
    /// <param name="callback">The callback delegate.</param>
    void AddCommandListener(string commandName, DelegateClientCommand callback);
}
