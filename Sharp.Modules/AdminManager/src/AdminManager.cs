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
    string,         // Roles key
    HashSet<string> // Roles permissions
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

    // 这个无视所有插件的，这个是统一的，这个只是用来方便内部调用的，跟外部无关。
    private readonly HashSet<string> _allConcretePermissions = new(StringComparer.OrdinalIgnoreCase);

    // No duplicate SteamIDs allowed here.
    private readonly Dictionary<ulong, Admin> _globalAdmins = new ();

    // Ownership tracker: Which module owns which SteamIDs?
    private readonly Dictionary<string, HashSet<ulong>> _moduleOwnership = new (StringComparer.OrdinalIgnoreCase);

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

    public void OnLibraryDisconnect(string name)
    {
        // Remove command registry for this module
        _commandRegistries.Remove(name);

        // Remove permissions from the disconnecting module before removing its collections
        if (_permissionCollections.Remove(name, out var modulePermissionCollections))
        {
            foreach (var permission in modulePermissionCollections.Values.SelectMany(permissionSet => permissionSet))
            {
                _allConcretePermissions.Remove(permission);
            }
        }

        // Remove roles for this module
        _roles.Remove(name);

        // Remove admins from this module
        if (_moduleOwnership.Remove(name, out var ownedIds))
        {
            foreach (var id in ownedIds)
            {
                _globalAdmins.Remove(id);
            }
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

        foreach (var adminDef in manifest.Admins)
        {
            if (_globalAdmins.ContainsKey(adminDef.Identity))
            {
                var existingOwner = FindOwner(adminDef.Identity);

                _logger.LogError("Module '{NewModule}' failed to mount admins! Conflict detected: SteamID {Id} is already owned by '{OldModule}'.",
                                 moduleIdentity,
                                 adminDef.Identity,
                                 existingOwner);

                return; // STOP EXECUTION. Do not load partially.
            }
        }

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
                _allConcretePermissions.Add(permission);
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
            moduleRoles[role.Name] = role.Permissions;
        }

        if (!_moduleOwnership.TryGetValue(moduleIdentity, out var ownedIds))
        {
            ownedIds                         = [];
            _moduleOwnership[moduleIdentity] = ownedIds;
        }

        foreach (var adminManifest in manifest.Admins)
        {
            // Resolve recursive/wildcard permissions
            var resolvedPermissions = ResolvePermissions(moduleIdentity, adminManifest.Permissions);

            // Create the concrete Admin object
            // Note: Since we verified conflicts earlier, we know this ID is unique in _globalAdmins.
            var admin = new Admin(adminManifest.Name, adminManifest.Identity, adminManifest.Immunity);

            foreach (var permission in resolvedPermissions)
            {
                admin.AddPermission(permission);
            }

            _globalAdmins.Add(admin.Identity, admin);
            ownedIds.Add(admin.Identity);
        }
    }

    /// <summary>
    ///     Resolves a list of permission rules into concrete permissions
    /// </summary>
    /// <param name="moduleIdentity">The module identity to resolve permissions within</param>
    /// <param name="permissionRules">Permission rules to resolve</param>
    private HashSet<string> ResolvePermissions(string moduleIdentity, HashSet<string> permissionRules)
    {
        // a tracker for recursion
        var visitedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return ResolvePermissionsInternal(moduleIdentity, permissionRules, visitedRoles);
    }

    private HashSet<string> ResolvePermissionsInternal(string          moduleIdentity,
                                                       HashSet<string> permissionRules,
                                                       HashSet<string> visitedRoles)
    {
        var allowedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deniedPermissions  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                deniedPermissions.UnionWith(matchedPermissions);
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
                    // Pass the visited set down
                    var roleResolved = ResolvePermissionsInternal(moduleIdentity, rolePermissions, visitedRoles);

                    allowedPermissions.UnionWith(roleResolved);
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

                allowedPermissions.UnionWith(matchedPermissions);
            }
        }

        allowedPermissions.ExceptWith(deniedPermissions);

        return allowedPermissions;
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

            if (_allConcretePermissions.Contains(pattern))
            {
                matches.Add(pattern);
            }

            return matches;
        }

        // Global wildcard "*"
        if (pattern is [wildcard])
        {
            return new HashSet<string>(_allConcretePermissions, StringComparer.OrdinalIgnoreCase);
        }

        var patternSegments = pattern.Split(separator);
        var result          = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in _allConcretePermissions)
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

    private string FindOwner(ulong identity)
    {
        foreach (var kv in _moduleOwnership)
        {
            if (kv.Value.Contains(identity))
            {
                return kv.Key;
            }
        }

        return "Unknown";
    }
}