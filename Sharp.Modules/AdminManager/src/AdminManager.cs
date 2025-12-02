// ReSharper disable UnusedParameter.Local

using Microsoft.Extensions.Configuration;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Units;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sharp.Modules.CommandManager.Shared;

namespace Sharp.Modules.AdminManager;

// https://www.doubao.com/thread/wc0f1c5cae120c2bb

// 核心目的是集中管理员注册机制，让所有管理员的注册逻辑都走同一个。
// 由此，复杂是不可避免的：因为这里涉及到二级key。

using PermissionCollectionDictionary = Dictionary<
    string, // Collection key
    HashSet<string> // Actual permission
>;
using RolesDictionary = Dictionary<
    string, // Roles key
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

    // Centralized admin storage - all admins from all modules
    private readonly Dictionary<string, List<Admin>> _admins = new(StringComparer.OrdinalIgnoreCase);

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
        var logger = sharedSystem.GetLoggerFactory().CreateLogger<AdminManager>();
        var adminConfigPath = Path.Combine(sharpPath, "configs", "admin.jsonc");

        if (!Path.Exists(adminConfigPath))
        {
            logger.LogWarning("{DefaultConfigPath} does not found, default config will not work!", adminConfigPath);
            return;
        }

        if (JsonSerializer.Deserialize<AdminTableManifest>(File.ReadAllText(adminConfigPath),
                new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                }) is { } manifest)
        {
            MountAdminManifest(moduleIdentity, () => manifest);
        }
        else
        {
            logger.LogWarning("{DefaultConfigPath} is not a valid json or empty, default config may not work!", adminConfigPath);
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
        if (_permissionCollections.TryGetValue(name, out var modulePermissionCollections))
        {
            foreach (var permission in modulePermissionCollections.Values.SelectMany(permissionSet => permissionSet))
            {
                _allConcretePermissions.Remove(permission);
            }
        }

        // Remove permission collections for this module
        _permissionCollections.Remove(name);

        // Remove roles for this module
        _roles.Remove(name);

        // Remove admins from this module
        _admins.Remove(name);
    }

    public void Shutdown()
    {
    }

    string IModSharpModule.DisplayName => "Sharp.Modules.AdminManager";
    string IModSharpModule.DisplayAuthor => "laper32";

    #endregion

    #region IAdminManager

    public IAdmin? GetAdmin(SteamID identity)
    {
        // Search across all modules for the admin
        foreach (var moduleAdmins in _admins.Values)
        {
            var admin = moduleAdmins.FirstOrDefault(x => x.Identity == identity);
            if (admin != null)
            {
                return admin;
            }
        }
        return null;
    }

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
        if (!_permissionCollections.ContainsKey(moduleIdentity))
        {
            _permissionCollections[moduleIdentity] = new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase);
        }

        var modulePermissionCollection = _permissionCollections[moduleIdentity];
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
        if (!_roles.ContainsKey(moduleIdentity))
        {
            _roles[moduleIdentity] = new RolesDictionary(StringComparer.OrdinalIgnoreCase);
        }

        var moduleRoles = _roles[moduleIdentity];
        foreach (var role in manifest.Roles)
        {
            moduleRoles[role.Name] = role.Permissions;
        }

        // Process admins from this module
        ProcessAdmins(moduleIdentity, manifest.Admins);
    }

    /// <summary>
    /// Processes and adds admins from a module manifest
    /// </summary>
    private void ProcessAdmins(string moduleIdentity, List<AdminManifest> adminManifests)
    {
        // Ensure module has an admin list
        if (!_admins.ContainsKey(moduleIdentity))
        {
            _admins[moduleIdentity] = [];
        }

        var moduleAdmins = _admins[moduleIdentity];

        foreach (var adminManifest in adminManifests)
        {
            // Resolve permissions for this admin from this module
            var resolvedPermissions = ResolvePermissions(moduleIdentity, adminManifest.Permissions);

            // Check if admin already exists in this module (by SteamID)
            var existingAdmin = moduleAdmins.FirstOrDefault(x => x.Identity == adminManifest.Identity);
            
            if (existingAdmin != null)
            {
                // Update existing admin with permissions from this module
                foreach (var permission in resolvedPermissions)
                {
                    existingAdmin.AddPermission(permission);
                }
                
                // Update immunity if this manifest specifies a higher level
                if (adminManifest.Immunity > existingAdmin.Immunity)
                {
                    // Note: Admin class doesn't expose Immunity setter
                    // This would require refactoring Admin class or recreating the admin
                    // For now, we'll keep the first immunity value
                }
            }
            else
            {
                // Create new admin for this module
                var admin = new Admin(adminManifest.Name, adminManifest.Identity, adminManifest.Immunity);
                
                foreach (var permission in resolvedPermissions)
                {
                    admin.AddPermission(permission);
                }
                
                moduleAdmins.Add(admin);
            }
        }
    }

    /// <summary>
    /// Resolves a list of permission rules into concrete permissions
    /// </summary>
    /// <param name="moduleIdentity">The module identity to resolve permissions within</param>
    /// <param name="permissionRules">Permission rules to resolve</param>
    private HashSet<string> ResolvePermissions(string moduleIdentity, HashSet<string> permissionRules)
    {
        var allowedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deniedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in permissionRules.Where(rule => !string.IsNullOrWhiteSpace(rule)))
        {
            // Handle denial rules (!)
            if (rule.StartsWith(IAdminManager.DenyOperator))
            {
                var deniedRule = rule[1..];

                // Expand wildcards in denied rules
                var matchedPermissions = MatchWildcard(moduleIdentity, deniedRule);
                foreach (var permission in matchedPermissions)
                {
                    deniedPermissions.Add(permission);
                }
            }
            // Handle role inheritance (@)
            else if (rule.StartsWith(IAdminManager.RolesOperator))
            {
                var roleName = rule[1..];
                
                // Try to find the role in the module's roles
                if (_roles.TryGetValue(moduleIdentity, out var moduleRoles) && 
                    moduleRoles.TryGetValue(roleName, out var rolePermissions))
                {
                    var roleResolved = ResolvePermissions(moduleIdentity, rolePermissions);
                    foreach (var permission in roleResolved)
                    {
                        allowedPermissions.Add(permission);
                    }
                }
            }
            // Handle direct permissions and wildcards
            else
            {
                var matchedPermissions = MatchWildcard(moduleIdentity, rule);
                foreach (var permission in matchedPermissions)
                {
                    allowedPermissions.Add(permission);
                }
            }
        }

        // Remove denied permissions (denial has the highest priority)
        allowedPermissions.ExceptWith(deniedPermissions);

        return allowedPermissions;
    }

    /// <summary>
    /// Matches a permission pattern (with wildcards) against all concrete permissions
    /// </summary>
    /// <param name="moduleIdentity">The module identity to match within, or empty to match globally</param>
    /// <param name="pattern">The permission pattern to match</param>
    private HashSet<string> MatchWildcard(string moduleIdentity, string pattern)
    {
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Determine which permission collection to search in

        // If a specific module is provided, we could optionally restrict to that module's permissions
        // For now, we'll search globally but this can be modified if needed
        
        // If it's a concrete permission (no wildcard), check if it exists
        if (!pattern.Contains(IAdminManager.WildCardOperator))
        {
            if (_allConcretePermissions.Contains(pattern))
            {
                matches.Add(pattern);
            }
            return matches;
        }

        // Handle wildcard matching
        var patternSegments = pattern.Split(IAdminManager.SeparatorOperator);

        // Global wildcard: match all permissions
        if (pattern == IAdminManager.WildCardOperator.ToString())
        {
            foreach (var permission in _allConcretePermissions)
            {
                matches.Add(permission);
            }
            return matches;
        }

        // Match against all concrete permissions
        foreach (var permission in _allConcretePermissions.Where(permission => IsWildcardMatch(permission, patternSegments)))
        {
            matches.Add(permission);
        }

        return matches;
    }

    /// <summary>
    /// Checks if a concrete permission matches a wildcard pattern
    /// Rule: pattern segments must match permission segments (segment count must be equal)
    /// </summary>
    private static bool IsWildcardMatch(string permission, string[] patternSegments)
    {
        var permissionSegments = permission.Split(IAdminManager.SeparatorOperator);

        // Segment count must match
        if (patternSegments.Length != permissionSegments.Length)
        {
            return false;
        }

        // Check each segment
        return !patternSegments
            .Where((t, i) => t != IAdminManager.WildCardOperator.ToString() && !string.Equals(t, permissionSegments[i], StringComparison.OrdinalIgnoreCase))
            .Any();
    }
}