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

using System.Collections.Generic;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.Shared;

public interface ITargetingManager
{
    const string Identity = nameof(ITargetingManager);

    /// <summary>
    ///     Resolves a target string into a list of game clients.
    /// </summary>
    /// <param name="activator">The client who initiated the targeting (can be null).</param>
    /// <param name="target">
    ///     The target string. Resolution order:
    ///     <list type="bullet">
    ///         <item>
    ///             <term><c>#name</c></term>
    ///             <description>Forces a literal name match (escapes special characters like <c>@</c>).</description>
    ///         </item>
    ///         <item>
    ///             <term><c>@resolver_name</c></term>
    ///             <description>Registered resolvers (e.g., <c>@me</c>, <c>@t</c>, <c>@ct</c>).</description>
    ///         </item>
    ///         <item>
    ///             <term><c>@!resolver_name</c></term>
    ///             <description>Inversion (targets all players EXCEPT those matching the target).</description>
    ///         </item>
    ///         <item>
    ///             <term><c>7656...</c> or <c>@7656...</c></term>
    ///             <description>Targeted by SteamID64.</description>
    ///         </item>
    ///         <item>
    ///             <term>Player Name</term>
    ///             <description>
    ///                 If input is not special, matches by Name (Exact matches return all; Partial matches return only if
    ///                 unique).
    ///             </description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <returns>A collection of matching game clients.</returns>
    public IEnumerable<IGameClient> GetByTarget(IGameClient? activator, string target);

    /// <summary>
    ///     Register a custom target resolver.
    /// </summary>
    /// <param name="ownerIdentity">
    ///     The exact module assembly name (case-sensitive) so resolvers are unregistered automatically on <c>OnLibraryDisconnect</c>.
    ///     Recommended: <c>typeof(YourModule).Assembly.GetName().Name</c>.
    /// </param>
    /// <param name="resolver">The resolver logic. <see cref="ITargetResolver.GetTarget" /> must return a non-empty string starting with <c>'@'</c>.</param>
    /// <returns><see langword="true" /> if registered successfully; <see langword="false" /> if the target is null/whitespace, does not start with <c>'@'</c>, or is already registered.</returns>
    public bool RegisterResolver(string ownerIdentity, ITargetResolver resolver);
}
