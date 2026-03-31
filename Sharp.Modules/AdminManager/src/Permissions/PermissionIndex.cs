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

using System.Runtime.InteropServices;
using Sharp.Modules.AdminManager.Shared;

namespace Sharp.Modules.AdminManager.Permissions;

internal sealed class PermissionIndex
{
    private readonly Dictionary<string, int>          _refCounts = new (StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _buckets   = new (StringComparer.OrdinalIgnoreCase);

    public void Register(IEnumerable<string> permissions)
    {
        foreach (var permission in permissions)
        {
            IncrementReference(permission);
        }
    }

    public void Unregister(IEnumerable<string> permissions)
    {
        foreach (var permission in permissions)
        {
            DecrementReference(permission);
        }
    }

    public bool ContainsPermission(string permission)
        => _refCounts.ContainsKey(permission);

    public IEnumerable<string> GetAllKnownPermissions()
        => _refCounts.Keys;

    public bool TryGetCandidatesForPattern(string pattern, out IEnumerable<string> candidates)
    {
        candidates = [];

        var patternSpan    = pattern.AsSpan();
        var firstSeparator = patternSpan.IndexOf(IAdminManager.SeparatorOperator);

        if (firstSeparator > 0)
        {
            var prefix = patternSpan.Slice(0, firstSeparator);

            if (!prefix.Contains(IAdminManager.WildCardOperator))
            {
                if (_buckets.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(prefix, out var bucket))
                {
                    candidates = bucket;

                    return true;
                }

                return false;
            }

            // Malformed prefix (e.g. "ad*:") - stop processing.
            if (!PermissionMatcher.IsPureWildcardSegment(prefix))
            {
                return false;
            }

            candidates = _refCounts.Keys;

            return true;
        }

        // No separator found (or starts with separator). Scan everything.
        candidates = _refCounts.Keys;

        return true;
    }

    private void DecrementReference(string permission)
    {
        if (!_refCounts.TryGetValue(permission, out var count))
        {
            return;
        }

        if (count <= 1)
        {
            _refCounts.Remove(permission);
            RemoveFromBucket(permission);
        }
        else
        {
            _refCounts[permission] = count - 1;
        }
    }

    private void IncrementReference(string permission)
    {
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_refCounts, permission, out var exists);

        if (!exists)
        {
            AddToBucket(permission);
            count = 0;
        }

        count++;
    }

    private void AddToBucket(string permission)
    {
        var idx  = permission.IndexOf(IAdminManager.SeparatorOperator);
        var root = idx < 0 ? permission : permission.Substring(0, idx);

        if (!_buckets.TryGetValue(root, out var list))
        {
            list           = [];
            _buckets[root] = list;
        }

        list.Add(permission);
    }

    private void RemoveFromBucket(string permission)
    {
        var idx  = permission.IndexOf(IAdminManager.SeparatorOperator);
        var root = idx < 0 ? permission : permission.Substring(0, idx);

        if (_buckets.TryGetValue(root, out var list))
        {
            list.Remove(permission);

            if (list.Count == 0)
            {
                _buckets.Remove(root);
            }
        }
    }
}
