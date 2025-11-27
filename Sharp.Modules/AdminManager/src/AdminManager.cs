// ReSharper disable UnusedParameter.Local

using Microsoft.Extensions.Configuration;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Units;
using System.Text.Json;
using Sharp.Modules.CommandManager.Shared;

namespace Sharp.Modules.AdminManager;

public class AdminManager : IAdminManager, IModSharpModule
{
    private ICommandManager _commandManager = null!;

    private readonly ISharedSystem _shared;
    private readonly List<Admin> _admins = [];
    private readonly Dictionary<string, IAdminCommandRegistry> _registries = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, HashSet<string>> _permissionSets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _roles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allConcretePermissions = new(StringComparer.OrdinalIgnoreCase);
    public AdminManager(
        ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        _shared = sharedSystem;

        var manifest = JsonSerializer.Deserialize<AdminTableManifest>(Path.Combine(sharpPath, "configs", "admin.jsonc"),
            new JsonSerializerOptions
            {
                WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? throw new InvalidOperationException("Unable to load admin config!");

        foreach (var kv in manifest.PermissionSets)
        {
            _permissionSets.Add(kv.Key, kv.Value);
        }

        foreach (var kv in manifest.Roles)
        {
            _roles.Add(kv.Name, kv.Permissions);
        }

        // Build the set of all concrete permissions from PermissionSets
        foreach (var permission in _permissionSets.Values.SelectMany(permissionSet => permissionSet))
        {
            _allConcretePermissions.Add(permission);
        }

        foreach (var kv in manifest.Admins)
        {
            var admin = new Admin(kv.Name, kv.Identity, kv.Immunity);

            var resolvedPermissions = ResolvePermissions(kv.Permissions);
            foreach (var permission in resolvedPermissions)
            {
                admin.AddPermission(permission);
            }

            _admins.Add(admin);
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

    public void OnAllModulesLoaded()
    {
        _commandManager = _shared.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<ICommandManager>(ICommandManager.Identity)
            .Instance!;
    }


    public void OnLibraryDisconnect(string name)
    {
        // Remove the module's registry - CommandManager will clean up the commands
        _registries.Remove(name);
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
        return _admins.FirstOrDefault(x => x.Identity == identity);
    }

    public IAdminCommandRegistry GetCommandRegistry(string moduleIdentity)
    {
        if (_registries.TryGetValue(moduleIdentity, out var value))
        {
            return value;
        }

        // Get a separate CommandRegistry for each module identity
        var commandRegistry = _commandManager.GetRegistry(moduleIdentity);
        var registry = new AdminCommandRegistry(commandRegistry, this, _shared);
        _registries[moduleIdentity] = registry;
        
        return registry;
    }

    #endregion

    /// <summary>
    /// Resolves a list of permission rules into concrete permissions
    /// </summary>
    private HashSet<string> ResolvePermissions(HashSet<string> permissionRules)
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
                var matchedPermissions = MatchWildcard(deniedRule);
                foreach (var permission in matchedPermissions)
                {
                    deniedPermissions.Add(permission);
                }
            }
            // Handle role inheritance (@)
            else if (rule.StartsWith(IAdminManager.RolesOperator))
            {
                var roleName = rule[1..];
                if (!_roles.TryGetValue(roleName, out var rolePermissions))
                {
                    continue;
                }

                var roleResolved = ResolvePermissions(rolePermissions);
                foreach (var permission in roleResolved)
                {
                    allowedPermissions.Add(permission);
                }
            }
            // Handle direct permissions and wildcards
            else
            {
                var matchedPermissions = MatchWildcard(rule);
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
    private HashSet<string> MatchWildcard(string pattern)
    {
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // If it's a concrete permission (no wildcard), check if it exists in PermissionSets
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
        var permissionSegments = permission.Split(':');

        // Pattern cannot have MORE segments than permission (prefix matching)
        // Example: Pattern "System:*:*:*" (4) cannot match "System:Role" (2)
        if (patternSegments.Length > permissionSegments.Length)
            return false;

        // Check each segment in the pattern
        for (int i = 0; i < patternSegments.Length; i++)
        {
            // Wildcard matches any value at this position
            if (patternSegments[i] == "*")
                continue;

            // Non-wildcard must match exactly (case-insensitive)
            if (!string.Equals(patternSegments[i], permissionSegments[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // All pattern segments matched - this is a valid prefix match
        return true;
    }
}