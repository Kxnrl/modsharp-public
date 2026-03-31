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

using System.Collections.Immutable;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminManager.Shared;

public interface IAdminCommandRegistry
{
    /// <summary>
    ///     Registers an admin-protected command and its required permissions.
    /// </summary>
    /// <param name="command">The command name to register.</param>
    /// <param name="call">
    ///     Callback executed when authorization succeeds.
    ///     <see cref="IGameClient" /> can be <see langword="null" /> for server-console execution.
    /// </param>
    /// <param name="permissions">
    ///     <para>
    ///         Permission rules required to execute this command.
    ///     </para>
    ///     <para>
    ///         <b>IMPORTANT — OR logic:</b> the player needs <b>any one</b> of the listed
    ///         permissions to pass the check, not all of them. For example,
    ///         <c>["admin:mute", "admin:silence"]</c> means a player with <em>either</em>
    ///         <c>admin:mute</c> or <c>admin:silence</c> can execute the command.
    ///     </para>
    ///     <para>
    ///         If you need AND logic (require <em>all</em> permissions), perform additional
    ///         checks inside your <paramref name="call" /> handler via
    ///         <see cref="IAdmin.HasPermission" />.
    ///     </para>
    ///     <para>
    ///         Any deny rule (e.g. <c>!admin:ban</c>) still overrides grants at runtime.
    ///     </para>
    /// </param>
    public void RegisterAdminCommand(string command,
        Action<IGameClient?, StringCommand> call,
        ImmutableArray<string>              permissions);

    /// <summary>
    ///     Registers concrete permissions into the global permission index under this module's scope.
    ///     Registered permissions become visible to wildcard expansion, diagnostics, and validation.
    /// </summary>
    /// <param name="permissions">
    ///     Concrete permission strings to register (e.g. <c>"admin:kick"</c>, <c>"admin:ban"</c>).
    ///     Duplicates within the same module are ignored.
    /// </param>
    /// <remarks>
    ///     This is independent of <see cref="RegisterAdminCommand" />: calling
    ///     <see cref="RegisterAdminCommand" /> does <b>not</b> automatically register its permissions.
    ///     Registered permissions are automatically unregistered when the owning module disconnects.
    /// </remarks>
    public void RegisterPermissions(ImmutableArray<string> permissions);
}
