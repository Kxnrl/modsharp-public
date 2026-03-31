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

using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager.Shared;

public interface IAdmin
{
    /// <summary>
    ///     The admin's Steam identity.
    /// </summary>
    SteamID Identity { get; }

    /// <summary>
    ///     Immunity level (0–255). Higher value = more protection against other admins.
    /// </summary>
    byte Immunity { get; }

    /// <summary>
    ///     The resolved set of granted permissions (after merge and deny processing).
    /// </summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    ///     Checks whether this admin has the specified permission.
    /// </summary>
    /// <param name="permission">The permission string to check (e.g. <c>"admin:kick"</c>).</param>
    /// <returns><see langword="true" /> if the permission is granted; otherwise <see langword="false" />.</returns>
    bool HasPermission(string permission);
}
