using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Commands;
using Sharp.Modules.AdminCommands.Extensions;
using Sharp.Modules.AdminCommands.Listeners;
using Sharp.Modules.AdminCommands.Services;
using Sharp.Modules.AdminCommands.Services.Internal;
using Sharp.Modules.AdminCommands.Services.Internal.Permissions;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Modules.AdminCommands.Storage;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;

namespace Sharp.Modules.AdminCommands;

public class AdminCommands : IModSharpModule
{
    public static readonly string AssemblyName = typeof(AdminCommands).Assembly.GetName().Name
                                                 ?? "Sharp.Modules.AdminCommands";

    private const string LocalizeManagerAssemblyName  = "Sharp.Modules.LocalizerManager";
    private const string AdminManagerAssemblyName     = "Sharp.Modules.AdminManager";
    private const string TargetingManagerAssemblyName = "Sharp.Modules.TargetingManager";
    private const string AdminCommandsLocaleName      = "admin_commands";

    string IModSharpModule.DisplayName   => "Sharp.Modules.AdminCommands";
    string IModSharpModule.DisplayAuthor => "Nukoooo";

    private readonly ISharedSystem                         _shared;
    private readonly ILogger<AdminCommands>                _logger;
    private readonly ServiceProvider                       _serviceProvider;
    private readonly PermissionTracker                     _permissionTracker;
    private readonly ModuleContext                         _moduleContext;
    private readonly AdminOperationStorage                 _adminOperationStorage;
    private readonly IReadOnlyCollection<ICommandCategory> _commandCategories;
    private readonly string                                _sharpPath;

    private IAdminManager?     _adminManager;
    private ILocalizerManager? _localizerManager;
    private ITargetingManager? _targetingManager;

    private bool _registered;

    public AdminCommands(
        ISharedSystem  shared,
        string         dllPath,
        string         sharpPath,
        Version        version,
        IConfiguration configuration,
        bool           hotReload)
    {
        _shared    = shared;
        _logger    = shared.GetLoggerFactory().CreateLogger<AdminCommands>();
        _sharpPath = sharpPath;

        // Configure DI container
        var services = new ServiceCollection();
        ConfigureServices(services, shared, sharpPath);
        _serviceProvider = services.BuildServiceProvider();

        _moduleContext         = _serviceProvider.GetRequiredService<ModuleContext>();
        _permissionTracker     = _serviceProvider.GetRequiredService<PermissionTracker>();
        _adminOperationStorage = _serviceProvider.GetRequiredService<AdminOperationStorage>();
        _commandCategories     = _serviceProvider.GetServices<ICommandCategory>().ToArray();
    }

    private static void ConfigureServices(IServiceCollection services, ISharedSystem shared, string sharpPath)
    {
        AddCoreServices(services, shared, sharpPath);
        AddStorageServices(services, sharpPath);
        AddFeatureServices(services);
    }

    private static void AddCoreServices(IServiceCollection services, ISharedSystem shared, string sharpPath)
    {
        services.AddSingleton(new InterfaceBridge(sharpPath, shared));
        services.AddSingleton<PermissionTracker>();
        services.AddSingleton<ModuleContext>();
        services.AddSingleton<AdminOperationCache>();
        services.AddSingleton<CommandContextFactory>();
        services.AddSingleton<AdminOperationService>();
        services.AddSingleton<AdminOperationEngine>();
        services.AddSingleton<PunishmentListener>();
    }

    private static void AddStorageServices(IServiceCollection services, string sharpPath)
    {
        services.AddSingleton<JsonAdminOperationStorage>(sp =>
        {
            var bridge = sp.GetRequiredService<InterfaceBridge>();

            return new JsonAdminOperationStorage(sharpPath, bridge.LoggerFactory.CreateLogger<JsonAdminOperationStorage>());
        });

        services.AddSingleton<AdminOperationStorage>(sp =>
        {
            var bridge   = sp.GetRequiredService<InterfaceBridge>();
            var fallback = sp.GetRequiredService<JsonAdminOperationStorage>();

            return new AdminOperationStorage(fallback, bridge.LoggerFactory.CreateLogger<AdminOperationStorage>());
        });

        services.AddSingleton<IAdminOperationStorageService>(sp => sp.GetRequiredService<AdminOperationStorage>());
    }

    private static void AddFeatureServices(IServiceCollection services)
    {
        services.AddCommandService<BanService, IBanService>();
        services.AddCommandService<MuteService, IMuteService>();
        services.AddCommandService<GagService, IGagService>();
        services.AddCommandService<SilenceService, ISilenceService>();

        services.AddSingleton<IAdminService, AdminService>();

        services.AddSingleton<ICommandCategory, KickCommands>();
        services.AddSingleton<ICommandCategory, ChatCommands>();
        services.AddSingleton<ICommandCategory, MovementCommands>();
        services.AddSingleton<ICommandCategory, CombatCommands>();
        services.AddSingleton<ICommandCategory, InventoryCommands>();
        services.AddSingleton<ICommandCategory, IdentityCommands>();
        services.AddSingleton<ICommandCategory, ServerCommands>();
    }

    public bool Init()
        => true;

    public void PostInit()
    {
        // Start with built-in storage, switch to external when it becomes available.
        _adminOperationStorage.UseFallback();
        _logger.LogInformation("Using built-in admin operation storage until an external provider is available.");

        var engine = _serviceProvider.GetRequiredService<AdminOperationEngine>();
        engine.Init();

        var punishmentListener = _serviceProvider.GetRequiredService<PunishmentListener>();
        punishmentListener.Register();

        var adminServices = _serviceProvider.GetRequiredService<IAdminService>();

        _shared.GetSharpModuleManager()
               .RegisterSharpModuleInterface(this, IAdminService.Identity, adminServices);

        RefreshExternalModules();
    }

    public void OnLibraryConnected(string name)
    {
        RefreshExternalModules(name);
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(AdminManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _adminManager = null;
            _moduleContext.UpdateAdminManager(null);
            _registered        = false;
        }
        else if (name.Equals(LocalizeManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _localizerManager = null;
            _moduleContext.UpdateLocalizer(null);
        }
        else if (name.Equals(TargetingManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _targetingManager = null;
            _moduleContext.UpdateTargeting(null);
        }
        else
        {
            TryResolveAdminOperationStorage();
        }
    }

    public void OnAllModulesLoaded()
    {
        RefreshExternalModules(logFailures: true);
    }

    public void Shutdown()
    {
        var engine = _serviceProvider.GetRequiredService<AdminOperationEngine>();
        engine.Shutdown();

        var punishmentListener = _serviceProvider.GetRequiredService<PunishmentListener>();
        punishmentListener.Unregister();

        _serviceProvider.Dispose();
    }

    private void RegisterCommands(bool logFailture = false)
    {
        if (_adminManager is null || _registered)
        {
            return;
        }

        try
        {
            var inner = _adminManager.GetCommandRegistry(AssemblyName);

            var registry = new TrackingPermissionCommandRegistry(inner, _permissionTracker);

            foreach (var category in _commandCategories)
            {
                category.Register(registry);
            }

            PermissionCollectionUpdater.Write(_adminManager, _sharpPath, "admin", _permissionTracker.Permissions, _logger);

            _registered = true;
        }
        catch (Exception e)
        {
            if (logFailture)
            {
                _logger.LogError(e, "Failed to register commands");
            }
        }
    }

    private void TryResolvePermissionManager(bool logFailure = false)
    {
        if (_adminManager is not null)
        {
            _moduleContext.UpdateAdminManager(_adminManager);
            RegisterCommands();

            return;
        }

        _adminManager = GetExternalModule<IAdminManager>(IAdminManager.Identity);

        if (_adminManager is not null)
        {
            _moduleContext.UpdateAdminManager(_adminManager);
            RegisterCommands();
        }
        else if (logFailure)
        {
            _logger.LogWarning("Failed to get AdminManager. Do you have '{AssemblyName}' installed? Admin commands will not work.",
                               AdminManagerAssemblyName);
        }
    }

    private void TryResolveLocalizer(bool logFailure = false)
    {
        if (_localizerManager is not null)
        {
            return;
        }

        _localizerManager = GetExternalModule<ILocalizerManager>(ILocalizerManager.Identity);
        _moduleContext.UpdateLocalizer(_localizerManager);

        if (_localizerManager is null)
        {
            if (logFailure)
            {
                _logger.LogWarning("Failed to get LocalizerManager. Do you have '{AssemblyName}' installed? Messages will use fallback values.",
                                   LocalizeManagerAssemblyName);
            }
        }
        else
        {
            LoadLocale();
        }
    }

    private void TryResolveTargetingManager(bool logFailure = false)
    {
        if (_targetingManager is not null)
        {
            return;
        }

        _targetingManager = GetExternalModule<ITargetingManager>(ITargetingManager.Identity);
        _moduleContext.UpdateTargeting(_targetingManager);

        if (_targetingManager is null && logFailure)
        {
            _logger.LogWarning("Failed to get TargetingManager. Do you have '{AssemblyName}' installed? Target selectors will be limited.",
                               TargetingManagerAssemblyName);
        }
    }

    private void TryResolveAdminOperationStorage(string? providerName = null)
    {
        var external = GetExternalModule<IAdminOperationStorageService>(IAdminOperationStorageService.Identity);

        if (external is not null)
        {
            if (!ReferenceEquals(_adminOperationStorage.Current, external))
            {
                _adminOperationStorage.Use(external, providerName);
            }

            return;
        }

        _adminOperationStorage.UseFallback();
    }

    private void LoadLocale()
    {
        try
        {
            _localizerManager?.LoadLocaleFile(AdminCommandsLocaleName, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load admin commands locale file '{LocaleFile}'.", AdminCommandsLocaleName);
        }
    }

    private void RefreshExternalModules(string? changedModuleName = null, bool logFailures = false)
    {
        var checkAll = changedModuleName is null;

        var resolvePermission
            = checkAll || changedModuleName!.Equals(AdminManagerAssemblyName, StringComparison.OrdinalIgnoreCase);

        var resolveLocalizer
            = checkAll || changedModuleName!.Equals(LocalizeManagerAssemblyName, StringComparison.OrdinalIgnoreCase);

        var resolveTargeting
            = checkAll || changedModuleName!.Equals(TargetingManagerAssemblyName, StringComparison.OrdinalIgnoreCase);

        var resolveStorage = checkAll || (!resolvePermission && !resolveLocalizer && !resolveTargeting);

        if (resolvePermission)
        {
            TryResolvePermissionManager(logFailures);
        }

        if (resolveLocalizer)
        {
            TryResolveLocalizer(logFailures);
        }

        if (resolveTargeting)
        {
            TryResolveTargetingManager(logFailures);
        }

        if (resolveStorage)
        {
            TryResolveAdminOperationStorage(changedModuleName);
        }
    }

    private T? GetExternalModule<T>(string identity) where T : class
        => _shared.GetSharpModuleManager()
                  .GetOptionalSharpModuleInterface<T>(identity)
                  ?.Instance;
}
