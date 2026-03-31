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
using PermissionCollectionDictionary = System.Collections.Generic.Dictionary<
    string,                                    // Collection key
    System.Collections.Generic.HashSet<string> // Permissions
>;
using RolesDictionary = System.Collections.Generic.Dictionary<
    string,                                        // Roles key
    Sharp.Modules.AdminManager.Shared.RoleManifest // Roles permissions + immunity
>;

namespace Sharp.Modules.AdminManager.Storage;

internal sealed class AdminRepository
{
    private sealed class AdminUserEntry
    {
        public Dictionary<string, AdminSource> Sources     { get; } = new (StringComparer.OrdinalIgnoreCase);
        public Admin?                          CachedAdmin { get; set; }
        public bool                            IsEmpty     => Sources.Count == 0;
    }

    private readonly Dictionary<string, PermissionCollectionDictionary> _permissionCollections;
    private readonly Dictionary<string, HashSet<string>>                _standalonePermissions;
    private readonly Dictionary<string, RolesDictionary>                _roles;
    private readonly Dictionary<ulong, AdminUserEntry>                  _adminEntries;

    public AdminRepository()
    {
        _permissionCollections = new Dictionary<string, PermissionCollectionDictionary>(StringComparer.OrdinalIgnoreCase);
        _standalonePermissions = new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase);
        _roles                 = new Dictionary<string, RolesDictionary>(StringComparer.OrdinalIgnoreCase);
        _adminEntries          = [];
    }

    public IAdmin? GetAdmin(SteamID identity)
        => _adminEntries.TryGetValue(identity, out var entry) ? entry.CachedAdmin : null;

    public bool TryGetUserSources(ulong steamId, out Dictionary<string, AdminSource> sources)
    {
        if (_adminEntries.TryGetValue(steamId, out var entry))
        {
            sources = entry.Sources;

            return true;
        }

        sources = null!;

        return false;
    }

    public Dictionary<string, AdminSource> GetOrAddUserSources(ulong steamId)
    {
        if (_adminEntries.TryGetValue(steamId, out var entry))
        {
            return entry.Sources;
        }

        entry                  = new AdminUserEntry();
        _adminEntries[steamId] = entry;

        return entry.Sources;
    }

    public bool ContainsUser(ulong steamId)
        => _adminEntries.ContainsKey(steamId);

    public IEnumerable<KeyValuePair<ulong, Dictionary<string, AdminSource>>> EnumerateAdminSources()
    {
        foreach (var entry in _adminEntries)
        {
            yield return new KeyValuePair<ulong, Dictionary<string, AdminSource>>(entry.Key, entry.Value.Sources);
        }
    }

    public IEnumerable<ulong> GetAllSteamIds()
        => _adminEntries.Keys;

    public bool TryGetModuleRoles(string moduleIdentity, out RolesDictionary roles)
        => _roles.TryGetValue(moduleIdentity, out roles!);

    public void RemoveModuleRoles(string moduleIdentity)
        => _roles.Remove(moduleIdentity);

    /// <summary>
    ///     Unregisters only manifest-sourced permissions for a module.
    ///     Standalone permissions (from <see cref="RegisterStandalonePermissions" />) are preserved.
    ///     Used during <c>MountAdminManifest</c> remount.
    /// </summary>
    public List<string> UnregisterManifestPermissions(string moduleIdentity)
    {
        var removed = new List<string>();

        if (_permissionCollections.Remove(moduleIdentity, out var modulePermissionCollection))
        {
            foreach (var permissionSet in modulePermissionCollection.Values)
            {
                foreach (var permission in permissionSet ?? [])
                {
                    removed.Add(permission);
                }
            }
        }

        return removed;
    }

    /// <summary>
    ///     Unregisters all permissions for a module (manifest + standalone).
    ///     Used during module disconnect.
    /// </summary>
    public List<string> UnregisterModulePermissions(string moduleIdentity)
    {
        var removed = UnregisterManifestPermissions(moduleIdentity);

        if (_standalonePermissions.Remove(moduleIdentity, out var standalone))
        {
            removed.AddRange(standalone);
        }

        return removed;
    }

    /// <summary>
    ///     Registers standalone permissions for a module (outside of manifest flow).
    ///     Returns only the permissions that were truly new (not already tracked).
    /// </summary>
    public HashSet<string> RegisterStandalonePermissions(string moduleIdentity, IEnumerable<string> permissions)
    {
        if (!_standalonePermissions.TryGetValue(moduleIdentity, out var existing))
        {
            existing                               = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _standalonePermissions[moduleIdentity] = existing;
        }

        var newPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissions)
        {
            if (existing.Add(permission))
            {
                newPermissions.Add(permission);
            }
        }

        return newPermissions;
    }

    public List<string> RegisterModuleData(string moduleIdentity,
        AdminTableManifest                        manifest,
        out HashSet<string>                       newConcretePermissions)
    {
        var modulePermissionCollection = new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase);
        _permissionCollections[moduleIdentity] = modulePermissionCollection;

        var permissionsToRegister = new List<string>();
        newConcretePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (manifest.PermissionCollection != null)
        {
            foreach (var kv in manifest.PermissionCollection)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                modulePermissionCollection[kv.Key] = kv.Value;

                newConcretePermissions.UnionWith(kv.Value);

                foreach (var permission in kv.Value)
                {
                    permissionsToRegister.Add(permission);
                }
            }
        }

        var moduleRoles = new RolesDictionary(StringComparer.OrdinalIgnoreCase);
        _roles[moduleIdentity] = moduleRoles;

        foreach (var role in manifest.Roles ?? [])
        {
            moduleRoles[role.Name] = role;
        }

        return permissionsToRegister;
    }

    /// <summary>
    ///     Removes a module from all users in _adminSources. Returns a list of SteamIDs that were affected.
    /// </summary>
    public HashSet<ulong> RemoveModuleFromAdminSources(string moduleIdentity)
    {
        var affectedUsers = new HashSet<ulong>();
        var usersToRemove = new List<ulong>();

        foreach (var (steamId, entry) in _adminEntries)
        {
            if (entry.Sources.Remove(moduleIdentity))
            {
                affectedUsers.Add(steamId);

                if (entry.IsEmpty)
                {
                    usersToRemove.Add(steamId);
                }
            }
        }

        foreach (var id in usersToRemove)
        {
            _adminEntries.Remove(id);
            affectedUsers.Remove(id);
        }

        return affectedUsers;
    }

    public void PersistCalculatedAdmin(ulong steamId, byte immunity, HashSet<string> resolvedPerms)
    {
        if (!_adminEntries.TryGetValue(steamId, out var entry))
        {
            entry                  = new AdminUserEntry();
            _adminEntries[steamId] = entry;
        }

        entry.CachedAdmin ??= new Admin(steamId, immunity);
        entry.CachedAdmin.Update(immunity, resolvedPerms);
    }

    public void RemoveAdmin(ulong steamId)
    {
        _adminEntries.Remove(steamId);
    }

    public IReadOnlyDictionary<string, PermissionCollectionDictionary> GetAllPermissionCollections()
        => _permissionCollections;

    public int AdminCount => _adminEntries.Count;
    public int RoleCount  => _roles.Values.Sum(r => r.Count);

    public IEnumerable<(ulong SteamId, Admin? CachedAdmin, Dictionary<string, AdminSource> Sources)> EnumerateAdmins()
    {
        foreach (var (steamId, entry) in _adminEntries)
        {
            yield return (steamId, entry.CachedAdmin, entry.Sources);
        }
    }
}
