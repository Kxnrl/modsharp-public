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
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Extensions.CommandManager;

public interface ICommandManager
{
    public delegate void DelegateOnServerCommand(StringCommand command);

    public delegate void DelegateOnClientCommand(IGameClient client, StringCommand command);

    /// <summary>
    ///     Registers a client-only command accessible from both client console and chat.
    /// </summary>
    /// <remarks>
    ///     <paramref name="command" /> is normalized to lowercase and automatically exposed with the <c>ms_</c> prefix in the client console via <see cref="Sharp.Shared.Managers.IClientManager.InstallCommandCallback" />.
    ///     Multiple registrations for the same command compose as multicast delegates.
    /// </remarks>
    /// <param name="command">The command name without prefix.</param>
    /// <param name="callback">The callback to invoke when the command executes.</param>
    void RegisterClientCommand(string command, DelegateOnClientCommand callback);

    /// <summary>
    ///     Registers a client-only admin command accessible from both client console and chat.
    /// </summary>
    /// <remarks>Automatically exposed with the <c>ms_</c> prefix in the client console.</remarks>
    /// <param name="command">The command name without prefix.</param>
    /// <param name="callback">The callback to invoke when the command executes.</param>
    /// <param name="permission">Required permission name.</param>
    [Obsolete("This has been merged into AdminManager. Will be removed at 2.2")]
    void RegisterAdminCommand(string command, DelegateOnClientCommand callback, string permission);

    /// <summary>
    ///     Registers a server-console-only command.
    /// </summary>
    /// <remarks>
    ///     <paramref name="command" /> is normalized to lowercase and registered as-is (no <c>ms_</c> prefix is added automatically).
    ///     Multiple registrations for the same command compose as multicast delegates.
    /// </remarks>
    /// <param name="command">The full server command name.</param>
    /// <param name="description">Command help description.</param>
    /// <param name="callback">The callback to invoke when the command executes.</param>
    void RegisterServerCommand(string command, string description, DelegateOnServerCommand callback);
}
