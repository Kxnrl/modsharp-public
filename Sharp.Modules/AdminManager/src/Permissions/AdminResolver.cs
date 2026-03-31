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

using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.AdminManager.Storage;
using Sharp.Shared;

namespace Sharp.Modules.AdminManager.Permissions;

internal sealed class AdminResolver
{
    private readonly ILogger<AdminResolver> _logger;
    private readonly AdminRepository        _repo;
    private readonly PermissionIndex        _index;

    /// <summary>
    ///     Tracks "module:permission" keys that have already been warned about to avoid log spam.
    ///     Entries are cleared when the missing permission is later registered by another module
    ///     (see <see cref="ClearResolvedWarnings" />), so the warning can fire again if the
    ///     permission disappears and reappears across hot-reload cycles.
    /// </summary>
    private readonly HashSet<string> _warnedUnknownPermissions;

    private readonly HashSet<ulong> _usersWithWildcards;

    public AdminResolver(ISharedSystem shared, AdminRepository repo, PermissionIndex index)
    {
        _logger = shared.GetLoggerFactory().CreateLogger<AdminResolver>();
        _repo   = repo;
        _index  = index;

        _usersWithWildcards       = [];
        _warnedUnknownPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
        AdminTableManifest                manifest,
        HashSet<string>                   newConcretePermissions)
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
            var immunity    = CalculateEffectiveImmunity(moduleIdentity, adminManifest);
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
        // Also refresh users with direct (non-wildcard) references to the new permissions,
        // since those references may have been unresolved when the user was first mounted.
        if (newConcretePermissions.Count > 0)
        {
            CollectUsersAffectedByNewPermissions(newConcretePermissions, usersToRefresh, moduleIdentity);
        }

        foreach (var uid in usersToRefresh)
        {
            RefreshSingleAdmin(uid);
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

            source.ResolvedAllows = newAllows;
            source.ResolvedDenies = newDenies;
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

    /// <summary>
    ///     Refreshes only admins whose rules contain wildcards.
    ///     Use this instead of <see cref="RefreshAllAdmins" /> when a module disconnects,
    ///     since only wildcard users can be affected by the removal of permissions from the index.
    /// </summary>
    public void RefreshWildcardAdmins()
    {
        foreach (var steamId in _usersWithWildcards.ToList())
        {
            RefreshSingleAdmin(steamId);
        }
    }

    /// <summary>
    ///     Refreshes admins whose wildcard or direct rules now match newly registered permissions.
    /// </summary>
    public void OnNewPermissionsRegistered(HashSet<string> newPermissions)
    {
        if (newPermissions.Count == 0)
        {
            return;
        }

        var usersToRefresh = new HashSet<ulong>();
        CollectUsersAffectedByNewPermissions(newPermissions, usersToRefresh);

        foreach (var uid in usersToRefresh)
        {
            RefreshSingleAdmin(uid);
        }
    }

    public void ValidateAllPermissions()
    {
        var unresolvedPerms = new Dictionary<ulong, HashSet<string>>();
        var emptyWildcards  = new Dictionary<ulong, HashSet<string>>();
        var unresolvedRoles = new Dictionary<ulong, HashSet<string>>();

        foreach (var (steamId, userSources) in _repo.EnumerateAdminSources())
        {
            foreach (var (moduleId, adminSource) in userSources)
            {
                _repo.TryGetModuleRoles(moduleId, out var moduleRoles);
                var visitedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                CheckRules(adminSource.RawRules);

                void CheckRules(HashSet<string> rules)
                {
                    foreach (var rule in rules)
                    {
                        if (string.IsNullOrWhiteSpace(rule))
                        {
                            continue;
                        }

                        var effective = rule;

                        if (effective.StartsWith(IAdminManager.DenyOperator))
                        {
                            effective = effective[1..];
                        }

                        if (effective.StartsWith(IAdminManager.RolesOperator))
                        {
                            var roleName = effective[1..];

                            if (!visitedRoles.Add(roleName))
                            {
                                continue;
                            }

                            if (moduleRoles != null && moduleRoles.TryGetValue(roleName, out var roleManifest))
                            {
                                CheckRules(roleManifest.Permissions);
                            }
                            else
                            {
                                AddToSet(unresolvedRoles, steamId, roleName);
                            }

                            continue;
                        }

                        if (effective.Contains(IAdminManager.WildCardOperator))
                        {
                            // Pure wildcards ("*") always match everything — skip.
                            if (effective.AsSpan().Trim(IAdminManager.WildCardOperator).IsEmpty)
                            {
                                continue;
                            }

                            if (!WildcardHasAnyMatch(effective))
                            {
                                AddToSet(emptyWildcards, steamId, effective);
                            }

                            continue;
                        }

                        if (!_index.ContainsPermission(effective))
                        {
                            AddToSet(unresolvedPerms, steamId, effective);
                        }
                    }
                }
            }
        }

        if (unresolvedPerms.Count == 0 && emptyWildcards.Count == 0 && unresolvedRoles.Count == 0)
        {
            return;
        }

        var issueCount = unresolvedPerms.Values.Sum(s => s.Count)
                         + emptyWildcards.Values.Sum(s => s.Count)
                         + unresolvedRoles.Values.Sum(s => s.Count);

        var affectedUsers = unresolvedPerms.Keys
                                           .Union(emptyWildcards.Keys)
                                           .Union(unresolvedRoles.Keys)
                                           .Count();

        _logger.LogWarning(
            "=== AdminManager: Post-load permission validation — {IssueCount} issue(s) across {UserCount} admin(s) ===",
            issueCount,
            affectedUsers);

        foreach (var (steamId, perms) in unresolvedPerms)
        {
            foreach (var perm in perms)
            {
                var suggestion = FindClosestPermission(perm);

                if (suggestion != null)
                {
                    _logger.LogWarning(
                        "  User {SteamId}: Permission '{Permission}' is not defined by any loaded module. Did you mean '{Suggestion}'?",
                        steamId,
                        perm,
                        suggestion);
                }
                else
                {
                    _logger.LogWarning("  User {SteamId}: Permission '{Permission}' is not defined by any loaded module. "
                                       + "Check that the module providing it is installed and loaded.",
                                       steamId,
                                       perm);
                }
            }
        }

        foreach (var (steamId, patterns) in emptyWildcards)
        {
            foreach (var pattern in patterns)
            {
                _logger.LogWarning(
                    "  User {SteamId}: Wildcard '{Pattern}' matched 0 registered permissions. Check for typos in the prefix.",
                    steamId,
                    pattern);
            }
        }

        foreach (var (steamId, roles) in unresolvedRoles)
        {
            foreach (var role in roles)
            {
                _logger.LogWarning("  User {SteamId}: Role '@{Role}' is not defined.",
                                   steamId,
                                   role);
            }
        }

        _logger.LogWarning("=== End of permission validation ===");

        return;

        static void AddToSet(Dictionary<ulong, HashSet<string>> dict, ulong key, string value)
        {
            if (!dict.TryGetValue(key, out var set))
            {
                set       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                dict[key] = set;
            }

            set.Add(value);
        }
    }

    private bool WildcardHasAnyMatch(string pattern)
    {
        if (!_index.TryGetCandidatesForPattern(pattern, out var candidates))
        {
            return false;
        }

        var patternSpan = pattern.AsSpan();

        foreach (var candidate in candidates)
        {
            if (PermissionMatcher.IsWildcardMatch(candidate.AsSpan(), patternSpan))
            {
                return true;
            }
        }

        return false;
    }

    private string? FindClosestPermission(string unresolved)
    {
        var sepIdx = unresolved.IndexOf(IAdminManager.SeparatorOperator);

        if (sepIdx <= 0)
        {
            return null;
        }

        var prefix = unresolved[..sepIdx];

        if (!_index.TryGetCandidatesForPattern($"{prefix}:*", out var candidates))
        {
            return null;
        }

        return candidates.FirstOrDefault();
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
                    MatchWildcard(moduleIdentity, rule[1..], denies);
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
                    MatchWildcard(moduleIdentity, rule, allows);
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
    private void MatchWildcard(string moduleIdentity, string pattern, HashSet<string> collected)
    {
        const char wildcard = IAdminManager.WildCardOperator;

        if (!pattern.Contains(wildcard))
        {
            if (_index.ContainsPermission(pattern))
            {
                collected.Add(pattern);
            }
            else
            {
                WarnUnknownPermission(moduleIdentity, pattern);
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

    private void WarnUnknownPermission(string moduleIdentity, string permission)
    {
        var key = $"{moduleIdentity}:{permission}";

        if (_warnedUnknownPermissions.Add(key))
        {
            _logger.LogWarning("Module '{Module}' refers to undefined permission '{Permission}'",
                               moduleIdentity,
                               permission);
        }
    }

    private void CollectUsersAffectedByNewPermissions(HashSet<string> newConcretePermissions,
        HashSet<ulong>                                                usersToRefresh,
        string?                                                       excludeModuleIdentity = null)
    {
        // Wildcard users
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
                if (modId == excludeModuleIdentity)
                {
                    continue;
                }

                if (HasMatchingWildcardRule(adminSource.RawRules, newConcretePermissions))
                {
                    usersToRefresh.Add(steamId);

                    break;
                }
            }
        }

        // Direct-reference users
        foreach (var (steamId, userSources) in _repo.EnumerateAdminSources())
        {
            if (usersToRefresh.Contains(steamId))
            {
                continue;
            }

            foreach (var (modId, adminSource) in userSources)
            {
                if (modId == excludeModuleIdentity)
                {
                    continue;
                }

                if (HasDirectMatchForPermissions(modId, adminSource.RawRules, newConcretePermissions))
                {
                    usersToRefresh.Add(steamId);

                    break;
                }
            }
        }

        ClearResolvedWarnings(newConcretePermissions);
    }

    private void ClearResolvedWarnings(HashSet<string> newConcretePermissions)
    {
        _warnedUnknownPermissions.RemoveWhere(key =>
        {
            var sepIdx = key.IndexOf(IAdminManager.SeparatorOperator);

            if (sepIdx < 0)
            {
                return false;
            }

            var permPart = key[(sepIdx + 1)..];

            return newConcretePermissions.Contains(permPart);
        });
    }

    private static bool HasMatchingWildcardRule(HashSet<string> rawRules, HashSet<string> newConcretePermissions)
    {
        foreach (var rule in rawRules)
        {
            if (!rule.Contains(IAdminManager.WildCardOperator))
            {
                continue;
            }

            var ruleRaw = rule.AsSpan();

            if (ruleRaw.IsWhiteSpace())
            {
                continue;
            }

            if (ruleRaw.StartsWith([IAdminManager.DenyOperator]) || ruleRaw.StartsWith([IAdminManager.RolesOperator]))
            {
                ruleRaw = ruleRaw[1..];
            }

            if (ruleRaw.IsWhiteSpace())
            {
                continue;
            }

            if (PermissionMatcher.IsPureWildcardSegment(ruleRaw))
            {
                return true;
            }

            foreach (var newPerm in newConcretePermissions)
            {
                if (!string.IsNullOrEmpty(newPerm) && PermissionMatcher.IsWildcardMatch(newPerm.AsSpan(), ruleRaw))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasDirectMatchForPermissions(string sourceModuleId,
        HashSet<string>                              rawRules,
        HashSet<string>                              newConcretePermissions)
    {
        var visitedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return CheckRulesForDirectMatch(sourceModuleId, rawRules, visitedRoles, newConcretePermissions);
    }

    private bool CheckRulesForDirectMatch(string sourceModuleId,
        HashSet<string>                          rules,
        HashSet<string>                          visitedRoles,
        HashSet<string>                          newConcretePermissions)
    {
        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule))
            {
                continue;
            }

            var effective = rule.AsSpan();

            if (effective.StartsWith([IAdminManager.DenyOperator]))
            {
                effective = effective[1..];
            }

            if (effective.StartsWith([IAdminManager.RolesOperator]))
            {
                var roleName = effective[1..].ToString();

                if (visitedRoles.Add(roleName)
                    && _repo.TryGetModuleRoles(sourceModuleId, out var roles)
                    && roles.TryGetValue(roleName, out var roleManifest))
                {
                    if (CheckRulesForDirectMatch(sourceModuleId,
                                                 roleManifest.Permissions,
                                                 visitedRoles,
                                                 newConcretePermissions))
                    {
                        return true;
                    }
                }

                continue;
            }

            if (effective.Contains(IAdminManager.WildCardOperator))
            {
                continue;
            }

            if (newConcretePermissions.Contains(effective.ToString()))
            {
                return true;
            }
        }

        return false;
    }
}
