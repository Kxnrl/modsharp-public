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

using System.Collections.Generic;
using Sharp.Shared.Units;

namespace Sharp.Shared.Objects;

public interface IAdmin
{
    /// <summary>
    ///     Administrator name
    /// </summary>
    string               Name        { get; }

    /// <summary>
    ///     Steam ID
    /// </summary>
    SteamID              Identity    { get; }

    /// <summary>
    ///     Immunity level
    /// </summary>
    byte                 Immunity    { get; }

    /// <summary>
    ///     Set of permissions
    /// </summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    ///     Check if admin has specific permission
    /// </summary>
    /// <param name="permission">Permission name</param>
    /// <returns></returns>
    bool HasPermission(string permission);

    /// <summary>
    ///     Add permission to admin
    /// </summary>
    /// <param name="permission">Permission name</param>
    /// <returns></returns>
    bool AddPermission(string permission);

    /// <summary>
    ///     Remove permission from admin
    /// </summary>
    /// <param name="permission">Permission name</param>
    /// <returns></returns>
    bool RemovePermission(string permission);
}
