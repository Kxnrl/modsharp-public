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

using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager.Shared;

public interface IAdmin
{
    /// <summary>
    ///     管理员名字
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     SteamID
    /// </summary>
    SteamID Identity { get; }

    /// <summary>
    ///     权限级别
    /// </summary>
    byte Immunity { get; }

    /// <summary>
    ///     权限
    /// </summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    ///     是否拥有权限
    /// </summary>
    /// <param name="permission">权限字段</param>
    /// <returns></returns>
    bool HasPermission(string permission);

    /// <summary>
    ///     添加权限
    /// </summary>
    /// <param name="permission">权限字段</param>
    /// <returns></returns>
    bool AddPermission(string permission);

    /// <summary>
    ///     删除权限
    /// </summary>
    /// <param name="permission">权限字段</param>
    /// <returns></returns>
    bool RemovePermission(string permission);
}