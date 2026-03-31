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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Commands;
using Sharp.Modules.AdminCommands.Common;
using Sharp.Modules.AdminCommands.Extensions;
using Sharp.Modules.AdminCommands.Services;
using Sharp.Modules.AdminCommands.Services.Handlers;
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

    private const  string LocalizeManagerAssemblyName  = "Sharp.Modules.LocalizerManager";
    internal const string AdminManagerAssemblyName     = "Sharp.Modules.AdminManager";
    private const  string TargetingManagerAssemblyName = "Sharp.Modules.TargetingManager";
    private const  string AdminCommandsLocaleName      = "admin_commands";

    string IModSharpModule.DisplayName   => "Sharp.Modules.AdminCommands";
    string IModSharpModule.DisplayAuthor => "Nukoooo";

    private readonly ISharedSystem          _shared;
    private readonly ILogger<AdminCommands> _logger;
    private readonly ServiceProvider        _serviceProvider;
    private readonly PermissionTracker      _permissionTracker;
    private readonly ModuleContext          _moduleContext;
    private readonly AdminOperationStorage  _adminOperationStorage;
    private readonly ICommandCategory[]     _commandCategories;
    private readonly string                 _sharpPath;

    private IModSharpModuleInterface<IAdminManager>?     _adminManager;
    private IModSharpModuleInterface<ILocalizerManager>? _localizerManager;
    private IModSharpModuleInterface<ITargetingManager>? _targetingManager;

    private bool _registered;

    public AdminCommands(ISharedSystem shared,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 configuration,
        bool                           hotReload)
    {
        _shared    = shared;
        _logger    = shared.GetLoggerFactory().CreateLogger<AdminCommands>();
        _sharpPath = sharpPath;

        // Configure DI container
        var services = new ServiceCollection();
        ConfigureServices(services, shared, sharpPath);
        var provider = services.BuildServiceProvider();

        _moduleContext         = provider.GetRequiredService<ModuleContext>();
        _permissionTracker     = provider.GetRequiredService<PermissionTracker>();
        _adminOperationStorage = provider.GetRequiredService<AdminOperationStorage>();
        _commandCategories     = [.. provider.GetServices<ICommandCategory>()];
        _serviceProvider       = provider;
    }

    private static void ConfigureServices(IServiceCollection services, ISharedSystem shared, string sharpPath)
    {
        services.AddSingleton(shared.GetLoggerFactory());
        services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        AddCoreServices(services, shared, sharpPath);
        AddStorageServices(services);
        AddFeatureServices(services);
    }

    private static void AddCoreServices(IServiceCollection services, ISharedSystem shared, string sharpPath)
    {
        services.AddSingleton(new InterfaceBridge(sharpPath, shared));
        services.AddSingleton<PermissionTracker>();
        services.AddSingleton<ModuleContext>();
        services.AddSingleton<CommandContextFactory>();
        services.AddSingleton<AdminOperationService>();
        services.AddSingleton<AdminOperationEngine>();

        services.AddOperationHandler<BanHandler>();
        services.AddOperationHandler<MuteHandler>();
        services.AddOperationHandler<GagHandler>();
    }

    private static void AddStorageServices(IServiceCollection services)
    {
        services.AddSingleton<JsonAdminOperationStorage>();
        services.AddSingleton<AdminOperationStorage>();

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

        RegisterHandlerHooks();

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
            UnregisterCommands();
            _adminManager = null;
            _moduleContext.UpdateAdminManager(null);
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

        var engine = _serviceProvider.GetRequiredService<AdminOperationEngine>();
        engine.UnregisterHandlers(name);
    }

    public void OnAllModulesLoaded()
    {
        RefreshExternalModules(logFailures: true);
    }

    public void Shutdown()
    {
        var engine = _serviceProvider.GetRequiredService<AdminOperationEngine>();
        engine.Shutdown();

        UnregisterCommands();
        UnregisterHandlerHooks();

        _serviceProvider.Dispose();
    }

    private void RegisterCommands(bool logFailture = false)
    {
        if (_adminManager?.Instance is not { } adminManager || _registered)
        {
            return;
        }

        var registeredCategories = new List<ICommandCategory>(_commandCategories.Length);
        _permissionTracker.Clear();

        try
        {
            var inner = adminManager.GetCommandRegistry(AssemblyName);

            var registry = new TrackingPermissionCommandRegistry(inner, _permissionTracker);

            foreach (var category in _commandCategories)
            {
                category.Register(registry);
                registeredCategories.Add(category);
            }

            PermissionCollectionUpdater.Write(adminManager, _sharpPath, "admin", _permissionTracker.Permissions, _logger);

            _registered = true;
        }
        catch (Exception e)
        {
            UnregisterCommands(registeredCategories);

            if (logFailture)
            {
                _logger.LogError(e, "Failed to register commands");
            }
        }
    }

    private void UnregisterCommands()
    {
        if (!_registered)
        {
            return;
        }

        UnregisterCommands(_commandCategories);
        _registered = false;
    }

    private void UnregisterCommands(IEnumerable<ICommandCategory> categories)
    {
        foreach (var category in categories)
        {
            try
            {
                category.Unregister();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister command category '{Category}'.", category.GetType().Name);
            }
        }
    }

    private void TryResolvePermissionManager(bool logFailure = false)
    {
        if (_adminManager?.Instance is not null)
        {
            _moduleContext.UpdateAdminManager(_adminManager.Instance);
            RegisterCommands();

            return;
        }

        _adminManager = _shared.GetSharpModuleManager()
                               .GetOptionalSharpModuleInterface<IAdminManager>(IAdminManager.Identity);

        if (_adminManager?.Instance is not null)
        {
            _moduleContext.UpdateAdminManager(_adminManager.Instance);
            RegisterCommands();
        }
        else if (logFailure)
        {
            _logger.LogWarning(
                "Failed to get AdminManager. Do you have '{AssemblyName}' installed? Admin commands will not work.",
                AdminManagerAssemblyName);
        }
    }

    private void TryResolveLocalizer(bool logFailure = false)
    {
        if (_localizerManager?.Instance is not null)
        {
            return;
        }

        _localizerManager = _shared.GetSharpModuleManager()
                                   .GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);

        _moduleContext.UpdateLocalizer(_localizerManager?.Instance);

        if (_localizerManager?.Instance is null)
        {
            if (logFailure)
            {
                _logger.LogWarning(
                    "Failed to get LocalizerManager. Do you have '{AssemblyName}' installed? Messages will use fallback values.",
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
        if (_targetingManager?.Instance is not null)
        {
            return;
        }

        _targetingManager = _shared.GetSharpModuleManager()
                                   .GetOptionalSharpModuleInterface<ITargetingManager>(ITargetingManager.Identity);

        _moduleContext.UpdateTargeting(_targetingManager?.Instance);

        if (_targetingManager?.Instance is null && logFailure)
        {
            _logger.LogWarning(
                "Failed to get TargetingManager. Do you have '{AssemblyName}' installed? Target selectors will be limited.",
                TargetingManagerAssemblyName);
        }
    }

    private void TryResolveAdminOperationStorage(string? providerName = null)
    {
        try
        {
            var external = _shared.GetSharpModuleManager()
                                  .GetOptionalSharpModuleInterface<IAdminOperationStorageService>(
                                      IAdminOperationStorageService.Identity)
                                  ?.Instance;

            if (external is null)
            {
                return;
            }

            if (!ReferenceEquals(_adminOperationStorage.Current, external))
            {
                _adminOperationStorage.Use(external, providerName);
            }
        }
        catch (Exception)
        {
            _adminOperationStorage.UseFallback();
        }
    }

    private void LoadLocale()
    {
        try
        {
            _localizerManager?.Instance?.LoadLocaleFile(AdminCommandsLocaleName, true);
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

    private void RegisterHandlerHooks()
    {
        foreach (var registrar in _serviceProvider.GetServices<IAdminOperationHandler>()
                                                  .OfType<IAdminOperationHookRegistrar>())
        {
            registrar.RegisterHooks();
        }
    }

    private void UnregisterHandlerHooks()
    {
        foreach (var registrar in _serviceProvider.GetServices<IAdminOperationHandler>()
                                                  .OfType<IAdminOperationHookRegistrar>())
        {
            registrar.UnregisterHooks();
        }
    }
}
