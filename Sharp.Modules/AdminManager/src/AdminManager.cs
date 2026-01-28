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
        HashSet<string> RawPermissions
    );

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
        // Remove command registry for this module
        _commandRegistries.Remove(moduleIdentity);

        // Clean up Permissions Collections & Global lookup
        if (_permissionCollections.Remove(moduleIdentity, out var modulePermissionCollections))
        {
            // Flatten all permissions this module introduced
            foreach (var permission in modulePermissionCollections.Values.SelectMany(permissionSet => permissionSet))
            {
                // Shouldn't be possible to have such case :innocent:
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

        var usersToRemove = new List<ulong>();

        foreach (var (steamId, sources) in _adminSources)
        {
            if (sources.Remove(moduleIdentity))
            {
                // If the user has no sources left (no modules define them as admin), mark for removal
                if (sources.Count == 0)
                {
                    usersToRemove.Add(steamId);
                }
            }
        }

        foreach (var id in usersToRemove)
        {
            _adminSources.Remove(id);
        }

        // Invalidate Cache.
        // Users need to be rebuilt because:
        // 1. They might have lost permissions granted by this module.
        // 2. They might have wildcards that previously matched permissions from this module.
        _globalAdmins.Clear();
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
    {
        if (_globalAdmins.TryGetValue(identity, out var admin))
        {
            return admin;
        }

        if (!_adminSources.ContainsKey(identity))
        {
            return null;
        }

        RebuildAdmin(identity);

        return _globalAdmins.GetValueOrDefault(identity);
    }

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

        // Cleanup old permissions for this module
        if (_permissionCollections.TryGetValue(moduleIdentity, out var modulePermissionCollection))
        {
            foreach (var permissionSet in modulePermissionCollection.Values)
            {
                foreach (var permission in permissionSet ?? [])
                {
                    if (_permissionReferenceCounts.TryGetValue(permission, out var count))
                    {
                        if (count <= 1)
                        {
                            _permissionReferenceCounts.Remove(permission);
                        }
                        else
                        {
                            _permissionReferenceCounts[permission] = count - 1;
                        }
                    }
                }
            }

            // Clear the collection to prepare for new data (avoids keeping stale keys)
            modulePermissionCollection.Clear();
        }
        else
        {
            // Initialize if doesnt exist
            modulePermissionCollection             = new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase);
            _permissionCollections[moduleIdentity] = modulePermissionCollection;
        }

        if (!_roles.TryGetValue(moduleIdentity, out var moduleRoles))
        {
            moduleRoles            = new RolesDictionary(StringComparer.OrdinalIgnoreCase);
            _roles[moduleIdentity] = moduleRoles;
        }
        else
        {
            // clear old roles
            moduleRoles.Clear();
        }

        // apply new permissions and roles
        foreach (var kv in manifest.PermissionCollection ?? [])
        {
            modulePermissionCollection[kv.Key] = kv.Value;

            // Add all concrete permissions from this collection to the global set
            foreach (var permission in kv.Value)
            {
                _permissionReferenceCounts.TryAdd(permission, 0);
                _permissionReferenceCounts[permission]++;
            }
        }

        foreach (var role in manifest.Roles ?? [])
        {
            moduleRoles[role.Name] = role;
        }

        var processedUsers = new HashSet<ulong>();

        foreach (var adminManifest in manifest.Admins ?? [])
        {
            var calculatedImmunity = CalculateEffectiveImmunity(moduleIdentity, adminManifest);

            var rawPermissions = adminManifest.Permissions != null
                ? new HashSet<string>(adminManifest.Permissions, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!_adminSources.TryGetValue(adminManifest.Identity, out var userSources))
            {
                userSources                           = new Dictionary<string, AdminSource>(StringComparer.OrdinalIgnoreCase);
                _adminSources[adminManifest.Identity] = userSources;
            }

            userSources[moduleIdentity] = new AdminSource(calculatedImmunity, rawPermissions);

            processedUsers.Add(adminManifest.Identity);
        }

        // Find users who have an entry for THIS module in _adminSources, but were not in the manifest we just processed.
        var usersToRemove = new List<ulong>();

        foreach (var (steamId, sources) in _adminSources)
        {
            // If this user has data from this module, but wasn't in the new manifest...
            if (sources.ContainsKey(moduleIdentity) && !processedUsers.Contains(steamId))
            {
                sources.Remove(moduleIdentity);

                // If they have no other sources left, mark for total removal
                if (sources.Count == 0)
                {
                    usersToRemove.Add(steamId);
                }
            }
        }

        // Remove stale users
        foreach (var id in usersToRemove)
        {
            _adminSources.Remove(id);
        }

        // Invalidate the entire cache.
        // Since this module might have added new permissions
        // users with "*" from OTHER modules need to be rebuilt to "see" these new permissions.
        _globalAdmins.Clear();
    }

#endregion

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

            return;
        }

        byte maxImmunity = 0;

        var globalAllows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var globalDenies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (moduleIdentity, source) in sources)
        {
            // we only take the max immunity
            if (source.CalculatedImmunity > maxImmunity)
            {
                maxImmunity = source.CalculatedImmunity;
            }

            // Lazy Evaluation:
            // We pass 'moduleIdentity' so that roles (@RoleName) are looked up 
            // in the correct module's manifest.
            // MatchWildcard will now see ALL permissions currently registered in the system.
            var (modAllows, modDenies) = ResolvePermissions(moduleIdentity, source.RawPermissions);

            globalAllows.UnionWith(modAllows);
            globalDenies.UnionWith(modDenies);
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
        var baseImmunity = adminManifest.Immunity;

        if (!_roles.TryGetValue(moduleIdentity, out var rolesDict))
        {
            return baseImmunity;
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roleMax = GetMaxRoleImmunity(adminManifest.Permissions, rolesDict, visited);

        return Math.Max(roleMax, baseImmunity);
    }

    private static byte GetMaxRoleImmunity(IEnumerable<string>? rules, RolesDictionary rolesDict, HashSet<string> visited)
    {
        if (rules is null)
        {
            return 0;
        }

        byte max = 0;

        foreach (var rule in rules)
        {
            if (!rule.StartsWith(IAdminManager.RolesOperator))
            {
                continue;
            }

            var roleName = rule[1..];
            var roleMax  = GetRoleImmunityRecursive(roleName, rolesDict, visited);

            if (roleMax > max)
            {
                max = roleMax;
            }
        }

        return max;
    }

    private static byte GetRoleImmunityRecursive(string roleName, RolesDictionary rolesDict, HashSet<string> visited)
    {
        if (!visited.Add(roleName))
        {
            return 0;
        }

        if (!rolesDict.TryGetValue(roleName, out var roleManifest))
        {
            visited.Remove(roleName);

            return 0;
        }

        var max       = roleManifest.Immunity;
        var nestedMax = GetMaxRoleImmunity(roleManifest.Permissions, rolesDict, visited);

        if (nestedMax > max)
        {
            max = nestedMax;
        }

        visited.Remove(roleName);

        return max;
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

                // if false it means we already visited this role, skip it to prevent infinite loop
                if (!visitedRoles.Add(roleName))
                {
                    continue;
                }

                if (_roles.TryGetValue(moduleIdentity, out var moduleRoles))
                {
                    if (moduleRoles.TryGetValue(roleName, out var rolePermissions))
                    {
                        ResolvePermissionsRecursive(moduleIdentity,
                                                    rolePermissions.Permissions ?? [],
                                                    visitedRoles,
                                                    collectedAllows,
                                                    collectedDenies);
                    }
                    else
                    {
                        _logger.LogWarning("Module '{Module}' refers to Role '@{Role}', but it is not defined in the manifest!",
                                           moduleIdentity,
                                           roleName);
                    }
                }
                else
                {
                    _logger.LogWarning("Module '{Module}' refers to Role '@{Role}', but this module has no Roles defined!",
                                       moduleIdentity,
                                       roleName);
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
        if (!pattern.Contains(wildcard))
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
    ///     Checks if a concrete permission matches a wildcard pattern
    ///     Rule: pattern segments must match permission segments (segment count must be equal)
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
