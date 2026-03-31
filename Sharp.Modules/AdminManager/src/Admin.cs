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

using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager;

internal sealed class Admin : IAdmin
{
    public SteamID Identity { get; }

    public byte Immunity { get; private set; }

    public IReadOnlySet<string> Permissions { get; private set; }

    public Admin(SteamID identity, byte immunity)
    {
        Identity    = identity;
        Immunity    = immunity;
        Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public bool HasPermission(string permission)
        => Permissions.Contains(permission);

    internal void Update(byte immunity, HashSet<string> permissions)
    {
        Immunity    = immunity;
        Permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }
}
