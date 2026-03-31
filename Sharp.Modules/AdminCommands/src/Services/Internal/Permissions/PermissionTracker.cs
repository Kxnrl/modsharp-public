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
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Services.Internal.Permissions;

/// <summary>
///     Tracks permissions registered by this module so we can emit a manifest.
/// </summary>
internal sealed class PermissionTracker
{
    private readonly HashSet<string> _permissions = new (StringComparer.OrdinalIgnoreCase);

    public void Track(IEnumerable<string> permissions)
    {
        foreach (var perm in permissions)
        {
            if (!string.IsNullOrWhiteSpace(perm))
            {
                _permissions.Add(perm);
            }
        }
    }

    public void Clear()
    {
        _permissions.Clear();
    }

    public IReadOnlyCollection<string> Permissions => _permissions;
}

internal sealed class TrackingPermissionCommandRegistry : IAdminCommandRegistry
{
    private readonly IAdminCommandRegistry _inner;
    private readonly PermissionTracker     _tracker;

    public TrackingPermissionCommandRegistry(IAdminCommandRegistry inner, PermissionTracker tracker)
    {
        _inner   = inner;
        _tracker = tracker;
    }

    public void RegisterAdminCommand(string command,
        Action<IGameClient?, StringCommand> call,
        ImmutableArray<string>              permissions)
    {
        _tracker.Track(permissions);
        _inner.RegisterAdminCommand(command, call, permissions);
    }

    public void RegisterPermissions(ImmutableArray<string> permissions)
    {
    }
}
