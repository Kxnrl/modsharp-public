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

using System.Collections.Immutable;
using Sharp.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager.Shared;

public interface IAdminManager
{
    public const char RolesOperator = '@';
    public const char DenyOperator = '!';
    public const char WildCardOperator = '*';
    public const char SeparatorOperator = ':';

    public const string Identity = nameof(IAdminManager);

    /// <summary>
    ///     Gets the resolved admin snapshot for a Steam identity.
    /// </summary>
    /// <param name="identity">Target SteamID to query.</param>
    /// <returns>
    ///     The merged <see cref="IAdmin" /> view (all mounted module sources applied),
    ///     or <see langword="null" /> when the identity has no admin source.
    /// </returns>
    /// <remarks>
    ///     Returned data is the latest cached result after manifest refresh logic,
    ///     including cross-module merges and global deny precedence.
    /// </remarks>
    public IAdmin? GetAdmin(SteamID identity);

    /// <summary>
    ///     Mounts (or remounts) one module's admin manifest snapshot into the global admin graph.
    /// </summary>
    /// <param name="moduleIdentity">
    ///     Stable module identity used as the ownership scope.
    ///     Prefer using your module <c>AssemblyName</c> (for example:
    ///     <c>typeof(MyModule).Assembly.GetName().Name</c>) and keep it unchanged across calls.
    /// </param>
    /// <param name="call">
    ///     Factory callback that returns the full <see cref="AdminTableManifest" /> for this module at call time.
    ///     If the callback returns <see langword="null" />, the call is silently ignored and a warning is logged.
    /// </param>
    /// <remarks>
    ///     Behavior summary:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Replace semantics per module: calling this method again with the same
    ///                 <paramref name="moduleIdentity" /> replaces that module's previous
    ///                 PermissionCollection, Roles, and Admin entries.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Cross-module merge semantics by SteamID: when the same user appears in multiple modules,
    ///                 allows are unioned, immunity becomes the highest value, and denies (for example,
    ///                 <c>!admin:ban</c>) override grants globally.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Role resolution is module-scoped: <c>@RoleName</c> is resolved against roles owned by
    ///                 the same <paramref name="moduleIdentity" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Wildcard/direct rules resolve only against known concrete permissions registered in
    ///                 PermissionCollection (for example, <c>module:*</c> or <c>*</c> expands from this index).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The refresh is immediate: users removed from this module are detached, users still present
    ///                 are recalculated, and users with wildcard rules in other modules are also refreshed if new
    ///                 concrete permissions are introduced.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     Recommended startup order (see <c>docfx/docs/codes/admin-example.cs</c>):
    ///     call <see cref="MountAdminManifest" /> first, then register admin commands via
    ///     <see cref="GetCommandRegistry" />.
    ///     <para>
    ///         <b>Threading:</b> Must be called on the game thread. From an async or background
    ///         context, use <see cref="IModSharp.InvokeFrameAction" /> or
    ///         <see cref="IModSharp.InvokeFrameActionAsync{T}" /> to dispatch back first.
    ///     </para>
    /// </remarks>
    void MountAdminManifest(string moduleIdentity, Func<AdminTableManifest> call);

    /// <summary>
    ///     Gets the admin command registry for a module scope.
    /// </summary>
    /// <param name="moduleIdentity">
    ///     The module identity that owns command registrations.
    ///     Prefer using the same <c>AssemblyName</c> value used in <see cref="MountAdminManifest" />.
    /// </param>
    /// <returns>A module-scoped <see cref="IAdminCommandRegistry" /> instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <c>Sharp.Modules.CommandCenter</c> is not available.
    /// </exception>
    /// <remarks>
    ///     Use the same <paramref name="moduleIdentity" /> you pass to <see cref="MountAdminManifest" />
    ///     so permission ownership, wildcard expansion, and lifecycle behavior stay aligned.
    ///     <para>
    ///         <b>Threading:</b> Must be called on the game thread. From an async or background
    ///         context, use <see cref="IModSharp.InvokeFrameAction"/> or
    ///         <see cref="IModSharp.InvokeFrameActionAsync{T}"/> to dispatch back first.
    ///     </para>
    /// </remarks>
    public IAdminCommandRegistry GetCommandRegistry(string moduleIdentity);
}


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
    ///         checks inside your <paramref name="call"/> handler via
    ///         <see cref="IAdmin.HasPermission"/>.
    ///     </para>
    ///     <para>
    ///         Any deny rule (e.g. <c>!admin:ban</c>) still overrides grants at runtime.
    ///     </para>
    /// </param>
    public void RegisterAdminCommand(string command, Action<IGameClient?, StringCommand> call,
        ImmutableArray<string> permissions);

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
