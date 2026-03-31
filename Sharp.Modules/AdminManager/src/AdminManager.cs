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

// ReSharper disable UnusedParameter.Local

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Commands;
using Sharp.Modules.AdminManager.IO;
using Sharp.Modules.AdminManager.Permissions;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.AdminManager.Storage;
using Sharp.Modules.CommandCenter.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager;

internal class AdminManager : IAdminManager, IModSharpModule
{
    private const string CommandCenterAssemblyName   = "Sharp.Modules.CommandCenter";
    private const string LocalizeManagerAssemblyName = "Sharp.Modules.LocalizerManager";
    private const string AdminManagerLocaleName      = "admin_manager";

    private static readonly string ModuleIdentity
        = typeof(AdminManager).Assembly.GetName().Name ?? "Sharp.Modules.AdminManager";

    private IModSharpModuleInterface<ICommandCenter>?    _commandCenter;
    private IModSharpModuleInterface<ILocalizerManager>? _localizerManager;

    private readonly ISharedSystem _shared;
    private readonly string        _sharpPath;

    private readonly ILogger<AdminManager> _logger;
    private readonly PermissionIndex       _permissionIndex;
    private readonly AdminRepository       _repository;
    private readonly AdminResolver         _resolver;
    private readonly ServerCommands        _serverCommands;

    private readonly Dictionary<string /* Module Identity*/, IAdminCommandRegistry> _commandRegistries;

    public AdminManager(ISharedSystem sharedSystem,
        string                        dllPath,
        string                        sharpPath,
        Version                       version,
        IConfiguration                coreConfiguration,
        bool                          hotReload)
    {
        _shared    = sharedSystem;
        _sharpPath = sharpPath;
        _logger    = sharedSystem.GetLoggerFactory().CreateLogger<AdminManager>();

        _permissionIndex = new PermissionIndex();
        _repository      = new AdminRepository();
        _resolver        = new AdminResolver(sharedSystem, _repository, _permissionIndex);
        _serverCommands  = new ServerCommands(sharedSystem, this, _repository);

        _commandRegistries = new Dictionary<string, IAdminCommandRegistry>(StringComparer.OrdinalIgnoreCase);

        LoadConfigManifest();
    }

#region IModSharpModule

    public bool Init()
        => true;

    public void PostInit()
    {
        _shared.GetSharpModuleManager().RegisterSharpModuleInterface<IAdminManager>(this, IAdminManager.Identity, this);

        RefreshModuleManagers(force: true);
        _serverCommands.TryRegister(_commandCenter?.Instance, ModuleIdentity);
    }

    public void OnLibraryConnected(string name)
    {
        RefreshModuleManagers(name, true);
        _serverCommands.TryRegister(_commandCenter?.Instance, ModuleIdentity);

        if (name.Equals(LocalizeManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            LoadLocale();
        }
    }

    public void OnLibraryDisconnect(string moduleIdentity)
    {
        _commandRegistries.Remove(moduleIdentity);

        var removedPermissions = _repository.UnregisterModulePermissions(moduleIdentity);
        _permissionIndex.Unregister(removedPermissions);

        _repository.RemoveModuleRoles(moduleIdentity);

        var usersToRefresh = _resolver.RemoveModuleFromAdminSources(moduleIdentity);

        foreach (var steamId in usersToRefresh)
        {
            if (_repository.ContainsUser(steamId))
            {
                _resolver.RefreshSingleAdmin(steamId);
            }
        }

        _resolver.RefreshWildcardAdmins();
    }

    public void OnAllModulesLoaded()
    {
        RefreshModuleManagers();

        if (_localizerManager?.Instance is null)
        {
            _logger.LogWarning(
                "Failed to get LocalizerManager, Do you have '{assemblyName}' installed? If you don't, messages will use the fallback value.",
                LocalizeManagerAssemblyName);
        }
        else
        {
            LoadLocale();
        }

        if (_commandCenter?.Instance is null)
        {
            _logger.LogWarning(
                "Failed to get CommandCenter, Do you have '{assemblyName}' installed? If you don't, admin commands will not work.",
                CommandCenterAssemblyName);
        }

        _serverCommands.TryRegister(_commandCenter?.Instance, ModuleIdentity);

        _resolver.ValidateAllPermissions();

        _logger.LogInformation(
            "AdminManager: {AdminCount} admin(s), {RoleCount} role(s), {PermCount} permission collection(s) loaded.",
            _repository.AdminCount,
            _repository.RoleCount,
            _repository.GetAllPermissionCollections().Count);
    }

    public void Shutdown()
    {
    }

    string IModSharpModule.DisplayName   => "Sharp.Modules.AdminManager";
    string IModSharpModule.DisplayAuthor => "laper32";

#endregion

#region IAdminManager

    public IAdmin? GetAdmin(SteamID identity)
        => _repository.GetAdmin(identity);

    public IAdminCommandRegistry GetCommandRegistry(string moduleIdentity)
    {
        if (_commandRegistries.TryGetValue(moduleIdentity, out var value))
        {
            return value;
        }

        if (_commandCenter?.Instance is null)
        {
            throw new InvalidOperationException(
                $"CommandCenter is not available. Ensure '{CommandCenterAssemblyName}' is installed and loaded before calling {nameof(IAdminManager.GetCommandRegistry)}.");
        }

        var commandRegistry = _commandCenter.Instance.GetRegistry(moduleIdentity);
        var registry        = new AdminCommandRegistry(commandRegistry, this, moduleIdentity);
        _commandRegistries[moduleIdentity] = registry;

        return registry;
    }

    public void MountAdminManifest(string moduleIdentity, Func<AdminTableManifest> call)
    {
        var manifest             = call.Invoke();
        var permissionCollection = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (manifest.PermissionCollection is { Count: > 0 })
        {
            foreach (var (key, perms) in manifest.PermissionCollection)
            {
                permissionCollection[key] = new HashSet<string>(perms, StringComparer.OrdinalIgnoreCase);
            }
        }

        var roles = manifest.Roles is { Count: > 0 }
            ? manifest.Roles.Select(r => r with
                      {
                          Permissions = new HashSet<string>(r.Permissions ?? [],
                                                            StringComparer.OrdinalIgnoreCase),
                      })
                      .ToList()
            : [];

        var admins = new List<AdminManifest>();

        if (manifest.Admins is { Count: > 0 })
        {
            foreach (var a in manifest.Admins)
            {
                SteamID steamId = a.Identity;

                if (steamId.IsValidUserId())
                {
                    admins.Add(a with
                    {
                        Permissions = new HashSet<string>(a.Permissions ?? [], StringComparer.OrdinalIgnoreCase),
                    });
                }
                else
                {
                    _logger.LogWarning("Module '{Module}': Ignoring admin with invalid SteamID64 '{SteamId}'.",
                                       moduleIdentity,
                                       a.Identity);
                }
            }

            if (admins.Count < manifest.Admins.Count)
            {
                _logger.LogWarning("Module '{Module}': {Removed} admin(s) removed due to invalid SteamID64.",
                                   moduleIdentity,
                                   manifest.Admins.Count - admins.Count);
            }
        }

        var sanitized = new AdminTableManifest(permissionCollection, roles, admins);

        var removedPermissions = _repository.UnregisterManifestPermissions(moduleIdentity);
        _permissionIndex.Unregister(removedPermissions);

        var permissionsToRegister = _repository.RegisterModuleData(moduleIdentity, sanitized, out var newConcretePermissions);
        _permissionIndex.Register(permissionsToRegister);

        _resolver.RefreshModuleScope(moduleIdentity, sanitized, newConcretePermissions);
    }

#endregion

#region Module management

    internal void RegisterModulePermissions(string moduleIdentity, IEnumerable<string> permissions)
    {
        var newPermissions = _repository.RegisterStandalonePermissions(moduleIdentity, permissions);

        if (newPermissions.Count == 0)
        {
            return;
        }

        _permissionIndex.Register(newPermissions);
        _resolver.OnNewPermissionsRegistered(newPermissions);
    }

    public ILocalizerManager? GetLocalizerManager()
        => _localizerManager?.Instance;

    private void LoadLocale()
    {
        try
        {
            _localizerManager?.Instance?.LoadLocaleFile(AdminManagerLocaleName, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load admin manager locale file '{LocaleFile}'.", AdminManagerLocaleName);
        }
    }

    internal void ReloadAdmins()
    {
        LoadConfigManifest();

        _resolver.ValidateAllPermissions();

        _logger.LogInformation(
            "Admin config reloaded: {AdminCount} admin(s), {RoleCount} role(s), {PermCount} permission collection(s).",
            _repository.AdminCount,
            _repository.RoleCount,
            _repository.GetAllPermissionCollections().Count);
    }

    private void LoadConfigManifest()
    {
        var configLoader = new AdminConfigLoader(_shared);
        var manifest     = configLoader.LoadMergedManifest(_sharpPath);

        if (manifest.PermissionCollection is { Count: > 0 }
            || manifest.Admins is { Count           : > 0 }
            || manifest.Roles is { Count            : > 0 })
        {
            MountAdminManifest(ModuleIdentity, () => manifest);
        }
    }

    private void RefreshModuleManagers(string? changedModuleName = null, bool force = false)
    {
        var checkAll = changedModuleName is null;

        var updateCommand
            = checkAll || CommandCenterAssemblyName.Equals(changedModuleName, StringComparison.OrdinalIgnoreCase);

        var updateLocalizer
            = checkAll || LocalizeManagerAssemblyName.Equals(changedModuleName, StringComparison.OrdinalIgnoreCase);

        var moduleManager = _shared.GetSharpModuleManager();

        if (updateCommand && (force || _commandCenter?.Instance is null))
        {
            _commandCenter = moduleManager
                .GetOptionalSharpModuleInterface<ICommandCenter>(ICommandCenter.Identity);
        }

        if (updateLocalizer && (force || _localizerManager?.Instance is null))
        {
            _localizerManager = moduleManager
                .GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);
        }
    }

#endregion
}
