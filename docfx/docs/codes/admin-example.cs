using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace AdminExample;

internal class AdminExample : IModSharpModule
{
    private const string AdminManagerAssemblyName = "Sharp.Modules.AdminManager";

    private readonly ISharedSystem         _sharedSystem;
    private readonly ILogger<AdminExample> _logger;

    private static readonly string ModuleIdentity = typeof(AdminExample).Assembly.GetName().Name ?? "AdminExample";

    private IAdminManager? _adminManager;
    private bool           _initialized;

    public AdminExample(ISharedSystem  sharedSystem,
                        string         dllPath,
                        string         sharpPath,
                        Version        version,
                        IConfiguration configuration,
                        bool           hotReload)
    {
        _sharedSystem = sharedSystem;
        _logger       = sharedSystem.GetLoggerFactory().CreateLogger<AdminExample>();
    }

    public bool Init()
        => true;

    public void PostInit()
    {
        // we call it here just to prevent it fails to find **AdminManager** after our module is reloaded
        // this is because OnAllModulesLoaded is only called once when every module is loaded at start up
        TryResolveAdminManager();
    }

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(AdminManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            TryResolveAdminManager();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(AdminManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _adminManager = null;
            _initialized  = false;
        }
    }

    public void OnAllModulesLoaded()
    {
        // we also call it here and see if the end user(module consumer) has admin manager installed, so we can inform them if they don't.
        TryResolveAdminManager(true);
    }

    public void Shutdown()
    {
    }

    public string DisplayName   => "Admin example";
    public string DisplayAuthor => "ModSharp Dev Team";

    private void TryResolveAdminManager(bool logFailure = false)
    {
        if (_adminManager is not null)
        {
            return;
        }

        _adminManager = GetExternalModule<IAdminManager>(IAdminManager.Identity);

        if (_adminManager is null)
        {
            if (logFailure)
            {
                _logger.LogWarning("Failed to get AdminManager. Do you have '{AssemblyName}' installed? Target selectors will be limited.",
                                   AdminManagerAssemblyName);
            }

            return;
        }

        InitializePermissions();
    }

    // Group 1: "admin_offensive"
    // Commands: Slay, Kill
    private const string SlayPermission = "admin_offensive:slay";
    private const string KillPermission = "admin_offensive:kill";

    // Group 2: "admin_medic"
    // Commands: Heal
    // Because this starts with "admin_medic", a wildcard for "admin_offensive:*" will NOT include this.
    private const string HealPermission = "admin_medic:heal";

    private void InitializePermissions()
    {
        if (_adminManager is null || _initialized)
        {
            return;
        }

        try
        {
            Console.WriteLine("Building manifest...");

            // we build manifest first
            _adminManager.MountAdminManifest(ModuleIdentity, BuildAdminManifest);

            // then we add commands
            var registry = _adminManager.GetCommandRegistry(ModuleIdentity);

            // Register permissions into the global index so that admins with wildcard rules
            // (e.g. "admin_offensive:*") can be correctly granted these permissions.
            // RegisterAdminCommand does NOT do this automatically — if you want your
            // command permissions indexed, maintain your own list and call RegisterPermissions.
            //
            // NOTE: In this example, MountAdminManifest above already registers these
            // permissions via PermissionCollection, so this call is redundant here.
            // It is shown for demonstration — you only need one of the two approaches.
            registry.RegisterPermissions([SlayPermission, KillPermission, HealPermission]);

            registry.RegisterAdminCommand("slay", OnCommandSlay,   [SlayPermission]);
            registry.RegisterAdminCommand("kill", OnCommandKill,   [KillPermission]);
            registry.RegisterAdminCommand("heal", OnCommandHealth, [HealPermission]);

            _initialized = true;
        }
        catch (Exception e)
        {
            // ignored.
            // if we initialize first but CommandManager isn't loaded yet, it will throw exception in AdminManager
        }
    }

    private void OnCommandSlay(IGameClient? issuer, StringCommand cmd)
    {
        // for the sake of simplicity, we will just slay every alive player
        foreach (var controller in _sharedSystem.GetEntityManager().GetPlayerControllers())
        {
            if (controller.GetPlayerPawn() is { IsAlive: true } pawn)
            {
                pawn.Slay();
            }
        }
    }

    private void OnCommandKill(IGameClient? issuer, StringCommand cmd)
    {
        // is issued by console, ignoring
        if (issuer?.GetPlayerController()?.GetPlayerPawn() is not { } pawn)
        {
            Console.WriteLine("You can't kill console");

            return;
        }

        if (!pawn.IsAlive)
        {
            pawn.Print(HudPrintChannel.Chat, "You are dead, you can't kill yourself.");

            return;
        }

        pawn.Slay();
    }

    private void OnCommandHealth(IGameClient? issuer, StringCommand cmd)
    {
        // for the sake of simplicity, we will just give health to every alive player
        foreach (var controller in _sharedSystem.GetEntityManager().GetPlayerControllers())
        {
            if (controller.GetPlayerPawn() is { IsAlive: true } pawn)
            {
                pawn.Health    = 1337;
                pawn.MaxHealth = 1337;
            }
        }
    }

    // Change this function to your own need
    // To know more about AdminTableManifest, please refer to admins.jsonc.example, which explains in detail
    private AdminTableManifest BuildAdminManifest()
    {
        // if you have other sources for permissions, you can do something like
        // var permissionCollection = _requestManager.GetPermissionCollections();
        // you can leave it empty if you want to use existing permission sets

        // Any permission used in 'Admins' or 'Roles' MUST be defined here first!
        var permissionCollection = new Dictionary<string, HashSet<string>>
        {
            // "Offensive" group has legacy slay/kill
            ["admin_group_offensive"] = [SlayPermission, KillPermission],

            // "Medic" group has the new heal
            ["admin_group_medic"] = [HealPermission],
        };

        List<RoleManifest>  roles;
        List<AdminManifest> admins;

        const string exampleRoleName = "GeneralAdmin";
        const byte   exampleImmunity = 1;

        // if you have other sources for permissions, you can do something like
        // var roles = _requestManager.GetPermissionRoles();
        // you can leave it empty if you want to use existing permission roles.
        // HOWEVER, when you assign roles to admins, you have to ensure the roles
        // you want to give them exist

        // assigning roles
        {
            // This role gets EVERYTHING (Both offensive and medic)
            HashSet<string> rolePermissions = [SlayPermission, KillPermission, HealPermission];
            roles = [new (exampleRoleName, exampleImmunity, rolePermissions)];
        }

        // if you have other sources for admin, you can do something like
        // var adminIdentities = _requestManager.GetAdmins();

        {
            admins =
            [
                // Example 1: Use the Role (Gets Slay, Kill, AND Heal)
                new (76561198000000001, exampleImmunity, [$"@{exampleRoleName}"]),

                // Example 2: Grants ONLY Heal (Raw permission)
                new (76561198000000002, exampleImmunity, [HealPermission]),

                // Example 3: Use Role, but REMOVE Heal (Negation)
                // They can Slay/Kill, but cannot Heal.
                new (76561198000000003, exampleImmunity, [$"@{exampleRoleName}", $"!{HealPermission}"]),

                // Example 4: Wildcard "group_offensive"
                // Matches "admin_offensive:slay" and "admin_offensive:kill"
                // Does NOT match "admin_medic:heal"
                new (76561198000000004, exampleImmunity, ["admin_offensive:*"]),
            ];
        }

        return new AdminTableManifest(permissionCollection, roles, admins);
    }

    private T? GetExternalModule<T>(string identity) where T : class
        => _sharedSystem.GetSharpModuleManager()
                        .GetOptionalSharpModuleInterface<T>(identity)
                        ?.Instance;
}
