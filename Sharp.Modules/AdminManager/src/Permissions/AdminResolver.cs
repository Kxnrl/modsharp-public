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

using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.AdminManager.Storage;

namespace Sharp.Modules.AdminManager.Permissions;

internal sealed class AdminResolver
{
    private readonly AdminRepository _repo;
    private readonly PermissionIndex _index;
    private readonly ILogger<AdminManager> _logger;
    private readonly HashSet<ulong> _usersWithWildcards = [];

    public AdminResolver(AdminRepository repo, PermissionIndex index, ILogger<AdminManager> logger)
    {
        _repo   = repo;
        _index  = index;
        _logger = logger;
    }

    public HashSet<ulong> RemoveModuleFromAdminSources(string moduleIdentity)
    {
        var affectedUsers = _repo.RemoveModuleFromAdminSources(moduleIdentity);

        foreach (var steamId in affectedUsers)
        {
            UpdateUserWildcardStatus(steamId);
        }

        return affectedUsers;
    }

    public void RefreshModuleScope(string moduleIdentity,
                                   AdminTableManifest manifest,
                                   HashSet<string> newConcretePermissions)
    {
        var usersToRefresh = new HashSet<ulong>();
        var newManifestIds = (manifest.Admins ?? []).Select(x => x.Identity).ToHashSet();

        // Identify users who lost the module or rules changed
        var usersToPurge = new List<ulong>();

        foreach (var (steamId, userSources) in _repo.EnumerateAdminSources())
        {
            if (!userSources.ContainsKey(moduleIdentity))
            {
                continue;
            }

            if (!newManifestIds.Contains(steamId))
            {
                // User removed from this specific module
                userSources.Remove(moduleIdentity);
                usersToRefresh.Add(steamId);

                if (userSources.Count == 0)
                {
                    usersToPurge.Add(steamId);
                }
            }
            else
            {
                // User still exists, rules might have changed
                usersToRefresh.Add(steamId);
            }
        }

        foreach (var id in usersToPurge)
        {
            _repo.RemoveAdmin(id);
            usersToRefresh.Remove(id);
        }

        // Handle New & Updated Admins from Manifest
        foreach (var adminManifest in manifest.Admins ?? [])
        {
            var immunity = CalculateEffectiveImmunity(moduleIdentity, adminManifest);
            var userSources = _repo.GetOrAddUserSources(adminManifest.Identity);

            userSources[moduleIdentity] = new AdminSource(immunity,
                                                          [],
                                                          [],
                                                          adminManifest.Permissions ?? []);

            UpdateUserWildcardStatus(adminManifest.Identity);
            usersToRefresh.Add(adminManifest.Identity);
        }

        // If the module introduced new permissions (e.g., "admin:god"),
        // users from OTHER modules who have "*" or "admin:*" need to be refreshed.
        if (newConcretePermissions.Count > 0)
        {
            IdentifyAffectedWildcardUsers();
        }

        foreach (var uid in usersToRefresh)
        {
            RefreshSingleAdmin(uid);
        }

        return;

        void IdentifyAffectedWildcardUsers()
        {
            foreach (var steamId in _usersWithWildcards)
            {
                if (usersToRefresh.Contains(steamId))
                {
                    continue;
                }

                if (!_repo.TryGetUserSources(steamId, out var sourceMap))
                {
                    continue;
                }

                foreach (var (modId, adminSource) in sourceMap)
                {
                    if (modId.Equals(moduleIdentity, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (UserHasMatchingRule(adminSource.RawRules))
                    {
                        usersToRefresh.Add(steamId);

                        break;
                    }
                }
            }
        }

        bool UserHasMatchingRule(HashSet<string> userRawRules)
        {
            foreach (var rule in userRawRules)
            {
                if (!rule.Contains(IAdminManager.WildCardOperator))
                {
                    continue;
                }

                if (CheckRuleMatch(rule.AsSpan()))
                {
                    return true;
                }
            }

            return false;
        }

        bool CheckRuleMatch(ReadOnlySpan<char> ruleRaw)
        {
            if (ruleRaw.IsWhiteSpace())
            {
                return false;
            }

            if (ruleRaw.StartsWith([IAdminManager.DenyOperator]) || ruleRaw.StartsWith([IAdminManager.RolesOperator]))
            {
                ruleRaw = ruleRaw.Slice(1);
            }

            if (ruleRaw.IsWhiteSpace())
            {
                return false;
            }

            var isPureWildcard = true;

            foreach (var c in ruleRaw)
            {
                if (c != IAdminManager.WildCardOperator)
                {
                    isPureWildcard = false;

                    break;
                }
            }

            if (isPureWildcard)
            {
                return true;
            }

            foreach (var newPerm in newConcretePermissions)
            {
                if (string.IsNullOrEmpty(newPerm))
                {
                    continue;
                }

                if (PermissionMatcher.IsWildcardMatch(newPerm.AsSpan(), ruleRaw))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void RefreshSingleAdmin(ulong steamId)
    {
        if (!_repo.TryGetUserSources(steamId, out var userSources))
        {
            return;
        }

        foreach (var modId in userSources.Keys.ToList())
        {
            var source = userSources[modId];
            var (newAllows, newDenies) = ResolvePermissions(modId, source.RawRules);

            source.ResolvedAllows.Clear();
            source.ResolvedAllows.UnionWith(newAllows);

            source.ResolvedDenies.Clear();
            source.ResolvedDenies.UnionWith(newDenies);
        }

        RebuildAdmin(steamId);
    }

    public void RefreshAllAdmins()
    {
        foreach (var id in _repo.GetAllSteamIds().ToList())
        {
            RefreshSingleAdmin(id);
        }
    }

    private void UpdateUserWildcardStatus(ulong steamId)
    {
        if (!_repo.TryGetUserSources(steamId, out var userSources) || userSources.Count == 0)
        {
            _usersWithWildcards.Remove(steamId);

            return;
        }

        var hasWildcard = false;

        foreach (var source in userSources.Values)
        {
            foreach (var rule in source.RawRules)
            {
                if (PermissionMatcher.HasWildcard(rule))
                {
                    hasWildcard = true;

                    break;
                }
            }

            if (hasWildcard)
            {
                break;
            }
        }

        if (hasWildcard)
        {
            _usersWithWildcards.Add(steamId);
        }
        else
        {
            _usersWithWildcards.Remove(steamId);
        }
    }

    private void RebuildAdmin(ulong steamId)
    {
        if (!_repo.TryGetUserSources(steamId, out var sources) || sources.Count == 0)
        {
            _repo.RemoveAdmin(steamId);

            return;
        }

        byte maxImmunity  = 0;
        var  globalAllows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var  globalDenies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources.Values)
        {
            if (source.CalculatedImmunity > maxImmunity)
            {
                maxImmunity = source.CalculatedImmunity;
            }

            globalAllows.UnionWith(source.ResolvedAllows);
            globalDenies.UnionWith(source.ResolvedDenies);
        }

        globalAllows.ExceptWith(globalDenies);

        _repo.PersistCalculatedAdmin(steamId, maxImmunity, globalAllows);
    }

    private (HashSet<string> Allows, HashSet<string> Denies) ResolvePermissions(
        string          moduleIdentity,
        HashSet<string> permissionRules)
    {
        var visitedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allows       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var denies       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _repo.TryGetModuleRoles(moduleIdentity, out var moduleRoles);

        Recurse(permissionRules);

        return (allows, denies);

        void Recurse(HashSet<string> currentRules)
        {
            foreach (var rule in currentRules)
            {
                if (string.IsNullOrWhiteSpace(rule))
                {
                    continue;
                }

                if (rule.StartsWith(IAdminManager.DenyOperator))
                {
                    MatchWildcard(rule[1..], denies);
                }
                else if (rule.StartsWith(IAdminManager.RolesOperator))
                {
                    var roleName = rule[1..];

                    if (visitedRoles.Add(roleName))
                    {
                        if (moduleRoles != null && moduleRoles.TryGetValue(roleName, out var rolePermissions))
                        {
                            Recurse(rolePermissions.Permissions);
                        }
                        else
                        {
                            _logger.LogWarning("Module '{Module}' refers to undefined Role '@{Role}'",
                                               moduleIdentity,
                                               roleName);
                        }
                    }
                }
                else
                {
                    MatchWildcard(rule, allows);
                }
            }
        }
    }

    private byte CalculateEffectiveImmunity(string moduleIdentity, AdminManifest adminManifest)
    {
        if (!_repo.TryGetModuleRoles(moduleIdentity, out var rolesDict))
        {
            return adminManifest.Immunity;
        }

        var visitedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return Recurse(adminManifest.Immunity, adminManifest.Permissions);

        byte Recurse(byte currentMax, HashSet<string> currentPermissions)
        {
            foreach (var rule in currentPermissions)
            {
                if (!rule.StartsWith(IAdminManager.RolesOperator))
                {
                    continue;
                }

                var roleName = rule[1..];

                if (visitedRoles.Add(roleName) && rolesDict.TryGetValue(roleName, out var roleManifest))
                {
                    if (roleManifest.Immunity > currentMax)
                    {
                        currentMax = roleManifest.Immunity;
                    }

                    currentMax = Recurse(currentMax, roleManifest.Permissions);
                }
            }

            return currentMax;
        }
    }

    /// <summary>
    ///     Matches a permission pattern against existing permissions.
    ///     Includes optimizations to avoid scanning the entire permission list.
    /// </summary>
    private void MatchWildcard(string pattern, HashSet<string> collected)
    {
        const char wildcard = IAdminManager.WildCardOperator;

        if (!pattern.Contains(wildcard))
        {
            if (_index.ContainsPermission(pattern))
            {
                collected.Add(pattern);
            }

            return;
        }

        // If the pattern is just wildcards, it matches absolutely everything.
        if (pattern.AsSpan().Trim(wildcard).IsEmpty)
        {
            collected.UnionWith(_index.GetAllKnownPermissions());

            return;
        }

        if (!_index.TryGetCandidatesForPattern(pattern, out var candidates))
        {
            return;
        }

        var patternSpan = pattern.AsSpan();

        foreach (var permission in candidates)
        {
            if (PermissionMatcher.IsWildcardMatch(permission.AsSpan(), patternSpan))
            {
                collected.Add(permission);
            }
        }
    }
}
