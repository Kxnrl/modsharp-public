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

// ReSharper disable UnusedParameter.Local

using System.Buffers;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.CommandManager.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Units;

// 核心目的是集中管理员注册机制，让所有管理员的注册逻辑都走同一个。
// 由此，复杂是不可避免的：因为这里涉及到二级key。
using PermissionCollectionDictionary = System.Collections.Generic.Dictionary<
    string,                                    // Collection key
    System.Collections.Generic.HashSet<string> // Permissions
>;
using RolesDictionary = System.Collections.Generic.Dictionary<
    string,                                        // Roles key
    Sharp.Modules.AdminManager.Shared.RoleManifest // Roles permissions + immunity
>;

namespace Sharp.Modules.AdminManager;

internal class AdminManager : IAdminManager, IModSharpModule
{
    private const string CommandManagerAssemblyName  = "Sharp.Modules.CommandManager";
    private const string LocalizeManagerAssemblyName = "Sharp.Modules.LocalizerManager";

    private ICommandManager?   _commandManager;
    private ILocalizerManager? _localizerManager;

    private readonly ISharedSystem _shared;

    private readonly Dictionary<
        string, // Module Identity
        IAdminCommandRegistry> _commandRegistries = new (StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<
        string, // Module identity
        PermissionCollectionDictionary> _permissionCollections = new (StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<
        string, // module identity
        RolesDictionary> _roles = new (StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, int> _permissionReferenceCounts = new (StringComparer.OrdinalIgnoreCase);

    // No duplicate SteamIDs allowed here.
    private readonly Dictionary<ulong, Admin> _globalAdmins = new ();

    // Input: Raw permission/immunity data from each module, keyed by SteamID -> ModuleIdentity
    // This is merged into _globalAdmins (the output) via RebuildAdmin()
    private readonly Dictionary<ulong, Dictionary<string, AdminSource>> _adminSources = new ();

    // Represents what a specific module contributed to a user's admin state
    private record AdminSource(
        byte            CalculatedImmunity,
        HashSet<string> ResolvedAllows,
        HashSet<string> ResolvedDenies,
        HashSet<string> RawRules
    );

    private readonly Dictionary<string, List<string>> _permissionBuckets  = new (StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<ulong>                   _usersWithWildcards = [];

    private static readonly SearchValues<char> WildcardChars = SearchValues.Create(IAdminManager.WildCardOperator);

    private readonly ILogger<AdminManager> _logger;

    public AdminManager(
        ISharedSystem  sharedSystem,
        string         dllPath,
        string         sharpPath,
        Version        version,
        IConfiguration coreConfiguration,
        bool           hotReload)
    {
        var moduleIdentity = Path.GetFileName(dllPath);
        _shared = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<AdminManager>();

        var jsonOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true, PropertyNameCaseInsensitive = true,
        };

        var manifest = LoadMergedManifest(sharpPath, jsonOptions);

        // 2. Mount (if valid)
        if (manifest.Admins.Count > 0 || manifest.Roles.Count > 0)
        {
            MountAdminManifest(moduleIdentity, () => manifest);
        }
    }

    private AdminTableManifest LoadMergedManifest(string sharpPath, JsonSerializerOptions jsonOptions)
    {
        var simpleConfigPath   = Path.Combine(sharpPath, "configs", "admins_simple.jsonc");
        var advancedConfigPath = Path.Combine(sharpPath, "configs", "admin.jsonc");

        AdminTableManifest? manifest = null;

        if (File.Exists(advancedConfigPath))
        {
            try
            {
                var json = File.ReadAllText(advancedConfigPath);
                manifest = JsonSerializer.Deserialize<AdminTableManifest>(json, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admin.jsonc");
            }
        }

        if (manifest is null)
        {
            manifest = new AdminTableManifest(new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase),
                                              [],
                                              []);
        }
        else
        {
            if (manifest.Admins is { Count: > 1 })
            {
                var mergedAdmins = manifest.Admins
                                           .GroupBy(x => x.Identity)
                                           .Select(g => new AdminManifest(g.Key,
                                                                          g.Max(x => x.Immunity),
                                                                          g.SelectMany(x => x.Permissions ?? [])
                                                                           .ToHashSet()))
                                           .ToList();

                manifest = manifest with { Admins = mergedAdmins };
            }

            manifest = new AdminTableManifest(manifest.PermissionCollection
                                              ?? new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase),
                                              manifest.Roles  ?? [],
                                              manifest.Admins ?? []);
        }

        if (File.Exists(simpleConfigPath))
        {
            try
            {
                var simpleJson = File.ReadAllText(simpleConfigPath);
                var simpleDict = JsonSerializer.Deserialize<Dictionary<string, string>>(simpleJson, jsonOptions);

                if (simpleDict != null)
                {
                    var existingIds = manifest.Admins.Select(x => x.Identity).ToHashSet();

                    foreach (var (steamIdStr, roleName) in simpleDict)
                    {
                        if (!ulong.TryParse(steamIdStr, out var steamId))
                        {
                            continue;
                        }

                        if (existingIds.Contains(steamId))
                        {
                            continue;
                        }

                        manifest.Admins.Add(new AdminManifest(steamId,
                                                              0,
                                                              [$"{IAdminManager.RolesOperator}{roleName}"]));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admins_simple.jsonc");
            }
        }

        return manifest;
    }

#region IModSharpModule

    public bool Init()
        => true;

    public void PostInit()
    {
        _shared.GetSharpModuleManager().RegisterSharpModuleInterface<IAdminManager>(this, IAdminManager.Identity, this);

        RefreshModuleManagers(force: true);
    }

    public void OnLibraryConnected(string name)
    {
        RefreshModuleManagers(name, true);
    }

    public void OnLibraryDisconnect(string moduleIdentity)
    {
        _commandRegistries.Remove(moduleIdentity);

        // 1. Unregister Permissions & Ref Counts
        UnregisterModulePermissions(moduleIdentity);

        // 2. Unregister Roles
        _roles.Remove(moduleIdentity);

        // 3. Remove admin sources for this module
        var usersToRefresh = RemoveModuleFromAdminSources(moduleIdentity);

        // 4. Refresh users who lost permissions (or remove them if they have no sources left)
        foreach (var steamId in usersToRefresh)
        {
            if (_adminSources.ContainsKey(steamId))
            {
                RefreshSingleAdmin(steamId);
            }
            else
            {
                _globalAdmins.Remove(steamId);
            }
        }

        // Note: We might need a full refresh here if users relied on wildcards matching removed permissions
        RefreshAllAdmins();
    }

    public void OnAllModulesLoaded()
    {
        RefreshModuleManagers();

        if (_localizerManager is null)
        {
            _logger.LogWarning("Failed to get LocalizerManager, Do you have '{assemblyName}' installed? If you don't, messages will use the fallback value.",
                               LocalizeManagerAssemblyName);
        }

        if (_commandManager is null)
        {
            _logger.LogWarning("Failed to get CommandManager, Do you have '{assemblyName}' installed? If you don't, admin commands will not work.",
                               CommandManagerAssemblyName);
        }
    }

    public void Shutdown()
    {
    }

    string IModSharpModule.DisplayName   => "Sharp.Modules.AdminManager";
    string IModSharpModule.DisplayAuthor => "laper32";

#endregion

#region IAdminManager

    public IAdmin? GetAdmin(SteamID identity)
        => _globalAdmins.GetValueOrDefault(identity);

    public IAdminCommandRegistry GetCommandRegistry(string moduleIdentity)
    {
        if (_commandRegistries.TryGetValue(moduleIdentity, out var value))
        {
            return value;
        }

        if (_commandManager is null)
        {
            throw new NullReferenceException($"CommandManager is null! Did you have '{CommandManagerAssemblyName}' installed?");
        }

        var commandRegistry = _commandManager.GetRegistry(moduleIdentity);
        var registry        = new AdminCommandRegistry(commandRegistry, this, _shared);
        _commandRegistries[moduleIdentity] = registry;

        return registry;
    }

    public void MountAdminManifest(string moduleIdentity, Func<AdminTableManifest> call)
    {
        var manifest = call();

        if (manifest is null)
        {
            _logger.LogWarning("Module '{Identity}' attempted to mount a null manifest.", moduleIdentity);

            return;
        }

        UnregisterModulePermissions(moduleIdentity);
        var newConcretePermissions = RegisterModuleData(moduleIdentity, manifest);
        RefreshAffectedAdmins(moduleIdentity, manifest, newConcretePermissions);
    }

#endregion

#region Module Definition Management (Register/Unregister)

    private void UnregisterModulePermissions(string moduleIdentity)
    {
        if (!_permissionCollections.Remove(moduleIdentity, out var modulePermissionCollection))
        {
            return;
        }

        foreach (var permissionSet in modulePermissionCollection.Values)
        {
            foreach (var permission in permissionSet ?? [])
            {
                DecrementPermissionReference(permission);
            }
        }
    }

    private HashSet<string> RegisterModuleData(string moduleIdentity, AdminTableManifest manifest)
    {
        var modulePermissionCollection = new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase);
        _permissionCollections[moduleIdentity] = modulePermissionCollection;

        var newConcretePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (manifest.PermissionCollection != null)
        {
            foreach (var kv in manifest.PermissionCollection)
            {
                modulePermissionCollection[kv.Key] = kv.Value;

                if (kv.Value == null)
                {
                    continue;
                }

                newConcretePermissions.UnionWith(kv.Value);

                foreach (var permission in kv.Value)
                {
                    IncrementPermissionReference(permission);
                }
            }
        }

        var moduleRoles = new RolesDictionary(StringComparer.OrdinalIgnoreCase);
        _roles[moduleIdentity] = moduleRoles;

        foreach (var role in manifest.Roles ?? [])
        {
            moduleRoles[role.Name] = role;
        }

        return newConcretePermissions;
    }

    private void DecrementPermissionReference(string permission)
    {
        if (!_permissionReferenceCounts.TryGetValue(permission, out var count))
        {
            return;
        }

        if (count <= 1)
        {
            _permissionReferenceCounts.Remove(permission);
            RemoveFromPermissionBucket();
        }
        else
        {
            _permissionReferenceCounts[permission] = count - 1;
        }

        return;

        void RemoveFromPermissionBucket()
        {
            var idx  = permission.IndexOf(IAdminManager.SeparatorOperator);
            var root = idx < 0 ? permission : permission.Substring(0, idx);

            if (_permissionBuckets.TryGetValue(root, out var list))
            {
                list.Remove(permission);

                if (list.Count == 0)
                {
                    _permissionBuckets.Remove(root);
                }
            }
        }
    }

    private void IncrementPermissionReference(string permission)
    {
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_permissionReferenceCounts, permission, out var exists);

        if (!exists)
        {
            AddToPermissionBucket();
            count = 0;
        }

        count++;

        return;

        void AddToPermissionBucket()
        {
            var idx  = permission.IndexOf(IAdminManager.SeparatorOperator);
            var root = idx < 0 ? permission : permission.Substring(0, idx);

            if (!_permissionBuckets.TryGetValue(root, out var list))
            {
                list                     = [];
                _permissionBuckets[root] = list;
            }

            list.Add(permission);
        }
    }

    /// <summary>
    ///     Removes a module from all users in _adminSources. Returns a list of SteamIDs that were affected.
    /// </summary>
    private HashSet<ulong> RemoveModuleFromAdminSources(string moduleIdentity)
    {
        var affectedUsers = new HashSet<ulong>();
        var usersToRemove = new List<ulong>();

        foreach (var (steamId, userSources) in _adminSources)
        {
            if (userSources.Remove(moduleIdentity))
            {
                affectedUsers.Add(steamId);

                UpdateUserWildcardStatus(steamId);

                if (userSources.Count == 0)
                {
                    usersToRemove.Add(steamId);
                }
            }
        }

        foreach (var id in usersToRemove)
        {
            _adminSources.Remove(id);
            _globalAdmins.Remove(id);
            affectedUsers.Remove(id); // handled by removal
        }

        return affectedUsers;
    }

#endregion

#region Admin Refresh Logic

    private void RefreshAffectedAdmins(string             moduleIdentity,
                                       AdminTableManifest manifest,
                                       HashSet<string>    newConcretePermissions)
    {
        var usersToRefresh = new HashSet<ulong>();
        var newManifestIds = (manifest.Admins ?? []).Select(x => x.Identity).ToHashSet();

        // Identify users who lost the module or rules changed
        var usersToPurge = new List<ulong>();

        foreach (var (steamId, userSources) in _adminSources)
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
            _adminSources.Remove(id);
            _globalAdmins.Remove(id);
            usersToRefresh.Remove(id);
        }

        // 2. Handle New & Updated Admins from Manifest
        foreach (var adminManifest in manifest.Admins ?? [])
        {
            var immunity = CalculateEffectiveImmunity(moduleIdentity, adminManifest);

            if (!_adminSources.TryGetValue(adminManifest.Identity, out var userSources))
            {
                userSources                           = new Dictionary<string, AdminSource>(StringComparer.OrdinalIgnoreCase);
                _adminSources[adminManifest.Identity] = userSources;
            }

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

                if (!_adminSources.TryGetValue(steamId, out var sourceMap))
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

                if (IsWildcardMatch(newPerm.AsSpan(), ruleRaw))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void UpdateUserWildcardStatus(ulong steamId)
    {
        if (!_adminSources.TryGetValue(steamId, out var userSources) || userSources.Count == 0)
        {
            _usersWithWildcards.Remove(steamId);

            return;
        }

        var hasWildcard = false;

        foreach (var source in userSources.Values)
        {
            foreach (var rule in source.RawRules)
            {
                // Fast check for wildcard
                if (rule.AsSpan().ContainsAny(WildcardChars))
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

    private void RefreshSingleAdmin(ulong steamId)
    {
        if (!_adminSources.TryGetValue(steamId, out var userSources))
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

    private void RefreshAllAdmins()
    {
        foreach (var id in _adminSources.Keys.ToList())
        {
            RefreshSingleAdmin(id);
        }
    }

    private void RebuildAdmin(ulong steamId)
    {
        if (!_adminSources.TryGetValue(steamId, out var sources) || sources.Count == 0)
        {
            _globalAdmins.Remove(steamId);
            _adminSources.Remove(steamId);

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

        if (!_globalAdmins.TryGetValue(steamId, out var admin))
        {
            admin                  = new Admin(steamId, maxImmunity);
            _globalAdmins[steamId] = admin;
        }

        admin.Update(maxImmunity, globalAllows);
    }

#endregion

#region Permission Resolver & Wildcard Logic

    private (HashSet<string> Allows, HashSet<string> Denies) ResolvePermissions(
        string          moduleIdentity,
        HashSet<string> permissionRules)
    {
        var visitedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allows       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var denies       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _roles.TryGetValue(moduleIdentity, out var moduleRoles);

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
        if (!_roles.TryGetValue(moduleIdentity, out var rolesDict))
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
            if (_permissionReferenceCounts.ContainsKey(pattern))
            {
                collected.Add(pattern);
            }

            return;
        }

        // If the pattern is just wildcards, it matches absolutely everything.
        if (pattern.AsSpan().Trim(wildcard).IsEmpty)
        {
            collected.UnionWith(_permissionReferenceCounts.Keys);

            return;
        }

        IEnumerable<string> candidates;

        var patternSpan = pattern.AsSpan();

        var firstSeparator = patternSpan.IndexOf(IAdminManager.SeparatorOperator);

        if (firstSeparator > 0)
        {
            // Extract the prefix (e.g., "admin" from "admin:money")
            var prefix = patternSpan.Slice(0, firstSeparator);

            if (!prefix.Contains(wildcard))
            {
                if (_permissionBuckets.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(prefix, out var bucket))
                {
                    // Case A: The bucket exists. We scan ONLY these permissions.
                    candidates = bucket;
                }
                else
                {
                    // Case B: The bucket does not exist.
                    // This means no permissions start with this prefix.
                    return;
                }
            }
            else
            {
                // Prefix involves a wildcard (e.g. "ad*:money"). We must scan everything.
                candidates = _permissionReferenceCounts.Keys;
            }
        }
        else
        {
            // No separator found (e.g. "kick_command"). We must scan everything.
            candidates = _permissionReferenceCounts.Keys;
        }

        foreach (var permission in candidates)
        {
            if (IsWildcardMatch(permission.AsSpan(), patternSpan))
            {
                collected.Add(permission);
            }
        }
    }

    /// <summary>
    ///     Validates if a concrete permission matches a pattern using Zero-Allocation Spans.
    /// </summary>
    /// <param name="permission">The concrete permission (e.g., "admin:money:give")</param>
    /// <param name="pattern">The pattern (e.g., "admin:*:give")</param>
    private static bool IsWildcardMatch(ReadOnlySpan<char> permission, ReadOnlySpan<char> pattern)
    {
        const char separator = IAdminManager.SeparatorOperator;
        const char wildcard  = IAdminManager.WildCardOperator;

        // Optimization: identical strings always match
        if (permission.SequenceEqual(pattern))
        {
            return true;
        }

        while (true)
        {
            // If the pattern is exhausted, the permission must also be exhausted.
            if (pattern.IsEmpty)
            {
                return permission.IsEmpty;
            }

            var patSepIdx  = pattern.IndexOf(separator);
            var permSepIdx = permission.IndexOf(separator);

            // If no separator is found (-1), take the rest of the string.
            var currPatSeg = patSepIdx == -1 ? pattern : pattern.Slice(0, patSepIdx);

            // Check if the current pattern segment is a pure wildcard (e.g. "*" or "**")
            var isWildcardSegment = !currPatSeg.IsEmpty && currPatSeg.Trim(wildcard).IsEmpty;

            if (isWildcardSegment)
            {
                // LOGIC: Trailing Wildcard (e.g. "admin:*")
                // If this is the LAST segment in the pattern, it matches EVERYTHING remaining.
                if (patSepIdx == -1)
                {
                    return true;
                }

                // LOGIC: Mid-Wildcard (e.g. "admin:*:give")
                // The wildcard requires "something" to be here. 
                // If the permission string runs out early, it's a fail.
                if (permission.IsEmpty)
                {
                    return false;
                }
            }
            else
            {
                // If permission runs out but pattern expects more -> Fail
                if (permission.IsEmpty)
                {
                    return false;
                }

                // Get the permission segment to compare
                var currPermSeg = permSepIdx == -1 ? permission : permission.Slice(0, permSepIdx);

                // Compare ignoring case
                if (!currPermSeg.Equals(currPatSeg, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Move the span forward past the separator.
            pattern    = patSepIdx  == -1 ? ReadOnlySpan<char>.Empty : pattern.Slice(patSepIdx     + 1);
            permission = permSepIdx == -1 ? ReadOnlySpan<char>.Empty : permission.Slice(permSepIdx + 1);
        }
    }

#endregion

#region Module management

    public ILocalizerManager? GetLocalizerManager()
        => _localizerManager;

    private void RefreshModuleManagers(string? changedModuleName = null, bool force = false)
    {
        var checkAll = changedModuleName is null;

        var updateCommand
            = checkAll || changedModuleName!.Equals(CommandManagerAssemblyName, StringComparison.OrdinalIgnoreCase);

        var updateLocalizer
            = checkAll || changedModuleName!.Equals(LocalizeManagerAssemblyName, StringComparison.OrdinalIgnoreCase);

        var moduleManager = _shared.GetSharpModuleManager();

        if (updateCommand && (force || _commandManager is null))
        {
            _commandManager = moduleManager
                              .GetOptionalSharpModuleInterface<ICommandManager>(ICommandManager.Identity)
                              ?.Instance;
        }

        if (updateLocalizer && (force || _localizerManager is null))
        {
            _localizerManager = moduleManager
                                .GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity)
                                ?.Instance;
        }
    }

#endregion
}
