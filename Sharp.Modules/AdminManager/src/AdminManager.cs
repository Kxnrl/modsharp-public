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

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.CommandManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager;

// https://www.doubao.com/thread/wc0f1c5cae120c2bb

// 核心目的是集中管理员注册机制，让所有管理员的注册逻辑都走同一个。
// 由此，复杂是不可避免的：因为这里涉及到二级key。
using PermissionCollectionDictionary = Dictionary<
    string,         // Collection key
    HashSet<string> // Actual permission
>;
using RolesDictionary = Dictionary<
    string,      // Roles key
    RoleManifest // Roles permissions
>;

internal class AdminManager : IAdminManager, IModSharpModule
{
    private ICommandManager _commandManager = null!;

    private readonly ISharedSystem _shared;

    private readonly Dictionary<
        string, // Module Identity
        IAdminCommandRegistry> _commandRegistries = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<
        string, // Module identity
        PermissionCollectionDictionary> _permissionCollections = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<
        string, // module identity
        RolesDictionary> _roles = new(StringComparer.OrdinalIgnoreCase);

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
        HashSet<string> ResolvedDenies
    );

    private readonly ILogger<AdminManager> _logger;

    public AdminManager(
        ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        var moduleIdentity = Path.GetFileName(dllPath);
        _shared = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<AdminManager>();
        var adminConfigPath = Path.Combine(sharpPath, "configs", "admin.jsonc");

        if (!Path.Exists(adminConfigPath))
        {
            _logger.LogWarning("{DefaultConfigPath} does not found, default config will not work!", adminConfigPath);

            return;
        }

        if (JsonSerializer.Deserialize<AdminTableManifest>(File.ReadAllText(adminConfigPath),
                                                           new JsonSerializerOptions
                                                           {
                                                               ReadCommentHandling = JsonCommentHandling.Skip,
                                                               AllowTrailingCommas = true,
                                                           }) is { } manifest)
        {
            MountAdminManifest(moduleIdentity, () => manifest);
        }
        else
        {
            _logger.LogWarning("{DefaultConfigPath} is not a valid json or empty, default config may not work!",
                               adminConfigPath);
        }
    }

    #region IModSharpModule

    public bool Init()
    {
        return true;
    }

    public void PostInit()
    {
        _shared.GetSharpModuleManager().RegisterSharpModuleInterface<IAdminManager>(this, IAdminManager.Identity, this);
    }

    public void OnLibraryConnected(string name)
    {
        if (!name.Equals("Sharp.Modules.CommandManager", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _commandManager = _shared
            .GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<ICommandManager>(ICommandManager.Identity)
            .Instance!;
    }

    public void OnLibraryDisconnect(string moduleIdentity)
    {
        // Remove command registry for this module
        _commandRegistries.Remove(moduleIdentity);

        // Clean up Permissions Collections & Global lookup
        if (_permissionCollections.Remove(moduleIdentity, out var modulePermissionCollections))
        {
            // Flatten all permissions this module introduced
            foreach (var permission in modulePermissionCollections.Values.SelectMany(permissionSet => permissionSet))
            {
                // Shouldn't be possible to have such case, but just to be safe :innocent:
                if (!_permissionReferenceCounts.TryGetValue(permission, out var count))
                {
                    continue;
                }

                if (count <= 1)
                {
                    // This was the last module using this permission, remove
                    _permissionReferenceCounts.Remove(permission);
                }
                else
                {
                    // Other modules still need this permission, just lower the count
                    _permissionReferenceCounts[permission] = count - 1;
                }
            }
        }

        // Remove roles for this module
        _roles.Remove(moduleIdentity);

        // Find all users who had contributions from this module and rebuild their permissions and immunity
        var affectedUsers = new List<ulong>();

        foreach (var kvp in _adminSources)
        {
            if (kvp.Value.Remove(moduleIdentity))
            {
                affectedUsers.Add(kvp.Key);
            }
        }

        foreach (var steamId in affectedUsers)
        {
            RebuildAdmin(steamId);
        }
    }

    public void Shutdown()
    {
    }

    string IModSharpModule.DisplayName => "Sharp.Modules.AdminManager";
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

        // Get a separate CommandRegistry for each module identity
        var commandRegistry = _commandManager.GetRegistry(moduleIdentity);
        var registry = new AdminCommandRegistry(commandRegistry, this, _shared);
        _commandRegistries[moduleIdentity] = registry;
        return registry;
    }


    #endregion

    public void MountAdminManifest(string moduleIdentity, Func<AdminTableManifest> call)
    {
        var manifest = call();

        // Mount permission collections for this module
        if (!_permissionCollections.TryGetValue(moduleIdentity, out var modulePermissionCollection))
        {
            modulePermissionCollection             = new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase);
            _permissionCollections[moduleIdentity] = modulePermissionCollection;
        }

        foreach (var kv in manifest.PermissionCollection)
        {
            modulePermissionCollection[kv.Key] = kv.Value;

            // Add all concrete permissions from this collection to the global set
            foreach (var permission in kv.Value)
            {
                _permissionReferenceCounts.TryAdd(permission, 0);
                _permissionReferenceCounts[permission]++;
            }
        }

        // Mount roles for this module
        if (!_roles.TryGetValue(moduleIdentity, out var moduleRoles))
        {
            moduleRoles            = new RolesDictionary(StringComparer.OrdinalIgnoreCase);
            _roles[moduleIdentity] = moduleRoles;
        }

        foreach (var role in manifest.Roles)
        {
            moduleRoles[role.Name] = role;
        }

        var usersToRebuild = new HashSet<ulong>();

        foreach (var adminManifest in manifest.Admins)
        {
            var (allowed, denied) = ResolvePermissions(moduleIdentity, adminManifest.Permissions);
            var calculatedImmunity = CalculateEffectiveImmunity(moduleIdentity, adminManifest);

            if (!_adminSources.TryGetValue(adminManifest.Identity, out var userSources))
            {
                userSources                           = new Dictionary<string, AdminSource>(StringComparer.OrdinalIgnoreCase);
                _adminSources[adminManifest.Identity] = userSources;
            }

            userSources[moduleIdentity] = new AdminSource(calculatedImmunity, allowed, denied);
            usersToRebuild.Add(adminManifest.Identity);
        }

        foreach (var steamId in usersToRebuild)
        {
            RebuildAdmin(steamId);
        }
    }

    /// <summary>
    ///     Re-calculates the final concrete Admin object based on all module sources.
    ///     Handles merging permissions and finding max immunity.
    /// </summary>
    private void RebuildAdmin(ulong steamId)
    {
        if (!_adminSources.TryGetValue(steamId, out var sources) || sources.Count == 0)
        {
            // No modules claim this user anymore, remove them.
            _globalAdmins.Remove(steamId);
            _adminSources.Remove(steamId);

            return;
        }

        byte maxImmunity = 0;

        var  globalAllows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var  globalDenies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources.Values)
        {
            // we only take the max immunity
            if (source.CalculatedImmunity > maxImmunity)
            {
                maxImmunity = source.CalculatedImmunity;
            }

            // Merge this module's sources into the global pool
            globalAllows.UnionWith(source.ResolvedAllows);
            globalDenies.UnionWith(source.ResolvedDenies);
        }

        // If Module A grants 'kick' and Module B denies 'kick', 'kick' is removed here.
        globalAllows.ExceptWith(globalDenies);

        var mergedAdmin = new Admin(steamId, maxImmunity);

        foreach (var perm in globalAllows)
        {
            mergedAdmin.AddPermission(perm);
        }

        _globalAdmins[steamId] = mergedAdmin;
    }

    /// <summary>
    ///     Calculates immunity by comparing Admin Manifest definition vs referenced Roles.
    /// </summary>
    private byte CalculateEffectiveImmunity(string moduleIdentity, AdminManifest adminManifest)
    {
        var maxImmunity = adminManifest.Immunity;

        // Check immunity in assigned Roles
        if (!_roles.TryGetValue(moduleIdentity, out var rolesDict))
        {
            return maxImmunity;
        }

        foreach (var rule in adminManifest.Permissions)
        {
            if (!rule.StartsWith(IAdminManager.RolesOperator))
            {
                continue;
            }

            var roleName = rule[1..];

            if (!rolesDict.TryGetValue(roleName, out var roleManifest))
            {
                continue;
            }

            if (roleManifest.Immunity > maxImmunity)
            {
                maxImmunity = roleManifest.Immunity;
            }
        }

        return maxImmunity;
    }

    /// <summary>
    ///     Resolves a list of permission rules into separate Allow and Deny sets.
    ///     This does not apply the denial logic yet; it simply categorizes rules so
    ///     that global merging can handle "Deny Wins" across different modules.
    /// </summary>
    /// <param name="moduleIdentity">The module identity to resolve permissions within.</param>
    /// <param name="permissionRules">The raw list of permission rules (e.g. "admin.kick", "!admin.ban", "@SuperAdmin").</param>
    /// <returns>
    ///     A tuple containing:
    ///     <br /><b>Allows:</b> Concrete permissions explicitly granted.
    ///     <br /><b>Denies:</b> Concrete permissions explicitly revoked (prefixed with '!').
    /// </returns>
    private (HashSet<string> Allows, HashSet<string> Denies) ResolvePermissions(
        string          moduleIdentity,
        HashSet<string> permissionRules)
    {
        // a tracker for recursion
        var visitedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allows       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var denies       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ResolvePermissionsRecursive(moduleIdentity, permissionRules, visitedRoles, allows, denies);

        return (allows, denies);
    }

    /// <summary>
    ///     Recursively traverses permission rules and role inheritance trees to populate the Allow/Deny sets.
    /// </summary>
    private void ResolvePermissionsRecursive(
        string          moduleIdentity,
        HashSet<string> permissionRules,
        HashSet<string> visitedRoles,
        HashSet<string> collectedAllows,
        HashSet<string> collectedDenies)
    {
        foreach (var rule in permissionRules)
        {
            if (string.IsNullOrWhiteSpace(rule))
            {
                continue;
            }

            // Handle denial rules (!)
            if (rule.StartsWith(IAdminManager.DenyOperator))
            {
                var deniedRule = rule[1..];

                // Expand wildcards in denied rules
                var matchedPermissions = MatchWildcard(moduleIdentity, deniedRule);
                collectedDenies.UnionWith(matchedPermissions);
            }

            // Handle role inheritance (@)
            else if (rule.StartsWith(IAdminManager.RolesOperator))
            {
                var roleName = rule[1..];

                // if we already visited this role, skip it to prevent infinite loop
                if (!visitedRoles.Add(roleName))
                {
                    continue;
                }

                // Mark as visited
                if (_roles.TryGetValue(moduleIdentity, out var moduleRoles)
                    && moduleRoles.TryGetValue(roleName, out var rolePermissions))
                {
                    ResolvePermissionsRecursive(moduleIdentity,
                                                rolePermissions.Permissions,
                                                visitedRoles,
                                                collectedAllows,
                                                collectedDenies);
                }

                // BACKTRACKING:
                // We are done processing this role for this specific path.
                // We remove it so that other parallel branches can use this role again.
                // (Allows Diamond Inheritance: A->B->D and A->C->D)
                visitedRoles.Remove(roleName);
            }

            // Handle direct permissions and wildcards
            else
            {
                var matchedPermissions = MatchWildcard(moduleIdentity, rule);

                collectedAllows.UnionWith(matchedPermissions);
            }
        }
    }

    /// <summary>
    ///     Matches a permission pattern (with wildcards) against all concrete permissions
    /// </summary>
    /// <param name="moduleIdentity">The module identity to match within, or empty to match globally</param>
    /// <param name="pattern">The permission pattern to match</param>
    private HashSet<string> MatchWildcard(string moduleIdentity, string pattern)
    {
        // Determine which permission collection to search in

        // If a specific module is provided, we could optionally restrict to that module's permissions
        // For now, we'll search globally but this can be modified if needed

        const char wildcard  = IAdminManager.WildCardOperator;
        const char separator = IAdminManager.SeparatorOperator;

        // Concrete permission (no wildcard)
        if (pattern.IndexOf(wildcard) == -1)
        {
            var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_permissionReferenceCounts.ContainsKey(pattern))
            {
                matches.Add(pattern);
            }

            return matches;
        }

        // Global wildcard "*"
        if (pattern is [wildcard])
        {
            return new HashSet<string>(_permissionReferenceCounts.Keys, StringComparer.OrdinalIgnoreCase);
        }

        var patternSegments = pattern.Split(separator);
        var result          = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in _permissionReferenceCounts.Keys)
        {
            if (IsWildcardMatch(permission, patternSegments))
            {
                result.Add(permission);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a concrete permission matches a wildcard pattern
    /// Rule: pattern segments must match permission segments (segment count must be equal)
    /// </summary>
    private static bool IsWildcardMatch(string permission, string[] patternSegments)
    {
        var permSpan     = permission.AsSpan();
        var patternIndex = 0;

        const char separator = IAdminManager.SeparatorOperator;
        const char wildcard  = IAdminManager.WildCardOperator;

        while (true)
        {
            // permission has more segments than pattern, exit immediately
            if (patternIndex >= patternSegments.Length)
            {
                return false;
            }

            var sepIndex       = permSpan.IndexOf(separator);
            var currentPermSeg = sepIndex == -1 ? permSpan : permSpan.Slice(0, sepIndex);
            var currentPatSeg  = patternSegments[patternIndex];

            // if it is NOT a wildcard, we must check for equality
            if (currentPatSeg is not [wildcard])
            {
                if (!currentPermSeg.Equals(currentPatSeg, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            patternIndex++;

            if (sepIndex == -1)
            {
                break;
            }

            permSpan = permSpan.Slice(sepIndex + 1);
        }

        return patternIndex == patternSegments.Length;
    }
}