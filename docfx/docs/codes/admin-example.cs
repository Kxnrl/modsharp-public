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

    private IModSharpModuleInterface<IAdminManager>? _adminManager;
    private bool                                     _initialized;

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
        TryResolveAdminManager();
    }

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(AdminManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            TryResolveAdminManager();
        }
    }

    public void OnAllModulesLoaded()
    {
        TryResolveAdminManager(true);
    }

    public void Shutdown()
    {
    }

    public string DisplayName   => "Admin example";
    public string DisplayAuthor => "ModSharp Dev Team";

    private void TryResolveAdminManager(bool logFailure = false)
    {
        if (_adminManager?.Instance is not null)
        {
            return;
        }

        _adminManager = _sharedSystem.GetSharpModuleManager()
                                     .GetOptionalSharpModuleInterface<IAdminManager>(IAdminManager.Identity);

        if (_adminManager?.Instance is null)
        {
            if (logFailure)
            {
                _logger.LogWarning("AdminManager is not installed. Admin commands will not work.");
            }

            return;
        }

        InitializePermissions();
    }

    // Group 1: "admin_offensive" — Slay, Kill
    private const string SlayPermission = "admin_offensive:slay";
    private const string KillPermission = "admin_offensive:kill";

    // Group 2: "admin_medic" — Heal
    // Different prefix means "admin_offensive:*" will NOT match this.
    private const string HealPermission = "admin_medic:heal";

    private void InitializePermissions()
    {
        if (_adminManager?.Instance is not { } adminManager || _initialized)
        {
            return;
        }

        try
        {
            adminManager.MountAdminManifest(ModuleIdentity, BuildAdminManifest);

            var registry = adminManager.GetCommandRegistry(ModuleIdentity);

            // RegisterAdminCommand does NOT auto-register permissions into the global index.
            // Call RegisterPermissions so wildcard rules (e.g. "admin_offensive:*") work.
            // If MountAdminManifest already covers them via PermissionCollection, this is optional.
            registry.RegisterPermissions([SlayPermission, KillPermission, HealPermission]);

            registry.RegisterAdminCommand("slay", OnCommandSlay,   [SlayPermission]);
            registry.RegisterAdminCommand("kill", OnCommandKill,   [KillPermission]);
            registry.RegisterAdminCommand("heal", OnCommandHealth, [HealPermission]);

            _initialized = true;
        }
        catch (InvalidOperationException)
        {
            // CommandCenter isn't loaded yet — will retry when it connects.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize admin permissions.");
        }
    }

    private void OnCommandSlay(IGameClient? issuer, StringCommand cmd)
    {
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
        foreach (var controller in _sharedSystem.GetEntityManager().GetPlayerControllers())
        {
            if (controller.GetPlayerPawn() is { IsAlive: true } pawn)
            {
                pawn.Health    = 1337;
                pawn.MaxHealth = 1337;
            }
        }
    }

    private AdminTableManifest BuildAdminManifest()
    {
        var permissionCollection = new Dictionary<string, HashSet<string>>
        {
            ["admin_group_offensive"] = [SlayPermission, KillPermission],
            ["admin_group_medic"]     = [HealPermission],
        };

        const string exampleRoleName = "GeneralAdmin";
        const byte   exampleImmunity = 1;

        HashSet<string> rolePermissions = [SlayPermission, KillPermission, HealPermission];
        var roles = new List<RoleManifest>
        {
            new (exampleRoleName, exampleImmunity, rolePermissions)
        };

        var admins = new List<AdminManifest>
        {
            // Role inheritance — gets Slay, Kill, and Heal
            new (76561198000000001, exampleImmunity, [$"@{exampleRoleName}"]),

            // Raw permission — Heal only
            new (76561198000000002, exampleImmunity, [HealPermission]),

            // Role + negation — Slay/Kill but NOT Heal
            new (76561198000000003, exampleImmunity, [$"@{exampleRoleName}", $"!{HealPermission}"]),

            // Wildcard — matches admin_offensive:slay and :kill, not admin_medic:heal
            new (76561198000000004, exampleImmunity, ["admin_offensive:*"]),
        };

        return new AdminTableManifest(permissionCollection, roles, admins);
    }
}
