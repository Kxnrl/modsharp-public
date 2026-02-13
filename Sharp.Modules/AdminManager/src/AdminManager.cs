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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Commands;
using Sharp.Modules.AdminManager.IO;
using Sharp.Modules.AdminManager.Permissions;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.AdminManager.Storage;
using Sharp.Modules.CommandManager.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Units;

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

    private readonly ILogger<AdminManager> _logger;
    private readonly PermissionIndex       _permissionIndex;
    private readonly AdminRepository       _repository;
    private readonly AdminResolver         _resolver;
    private readonly ServerCommands        _serverCommands;

    private static readonly string ModuleIdentity = typeof(AdminManager).Assembly.GetName().Name ?? "Sharp.Modules.AdminManager";

    public AdminManager(
        ISharedSystem  sharedSystem,
        string         dllPath,
        string         sharpPath,
        Version        version,
        IConfiguration coreConfiguration,
        bool           hotReload)
    {
        _shared = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<AdminManager>();

        _permissionIndex = new PermissionIndex();
        _repository      = new AdminRepository();
        _resolver        = new AdminResolver(_repository, _permissionIndex, _logger);
        _serverCommands  = new ServerCommands(_repository, _logger);

        var configLoader = new AdminConfigLoader(_logger);

        var manifest = configLoader.LoadMergedManifest(sharpPath);

        // Mount config manifest (PermissionCollection + Roles + Admins) under AdminManager's identity.
        // PermissionCollectionUpdater will remount under this same identity when updating at runtime.
        if (manifest.PermissionCollection is { Count: > 0 } || manifest.Admins is { Count: > 0 } || manifest.Roles is { Count: > 0 })
        {
            MountAdminManifest(ModuleIdentity, () => manifest);
        }
    }

#region IModSharpModule

    public bool Init()
        => true;

    public void PostInit()
    {
        _shared.GetSharpModuleManager().RegisterSharpModuleInterface<IAdminManager>(this, IAdminManager.Identity, this);

        RefreshModuleManagers(force: true);
        _serverCommands.TryRegister(_commandManager, ModuleIdentity);
    }

    public void OnLibraryConnected(string name)
    {
        RefreshModuleManagers(name, true);
        _serverCommands.TryRegister(_commandManager, ModuleIdentity);
    }

    public void OnLibraryDisconnect(string moduleIdentity)
    {
        _commandRegistries.Remove(moduleIdentity);

        // 1. Unregister Permissions & Ref Counts
        var removedPermissions = _repository.UnregisterModulePermissions(moduleIdentity);
        _permissionIndex.Unregister(removedPermissions);

        // 2. Unregister Roles
        _repository.RemoveModuleRoles(moduleIdentity);

        // 3. Remove admin sources for this module
        var usersToRefresh = _resolver.RemoveModuleFromAdminSources(moduleIdentity);

        // 4. Refresh users who lost permissions (or remove them if they have no sources left)
        foreach (var steamId in usersToRefresh)
        {
            if (_repository.ContainsUser(steamId))
            {
                _resolver.RefreshSingleAdmin(steamId);
            }
        }

        // Note: We might need a full refresh here if users relied on wildcards matching removed permissions
        _resolver.RefreshAllAdmins();
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

        _serverCommands.TryRegister(_commandManager, ModuleIdentity);

        _resolver.ValidateAllPermissions();

        _logger.LogInformation("AdminManager: {AdminCount} admin(s), {RoleCount} role(s), {PermCount} permission collection(s) loaded.",
            _repository.AdminCount, _repository.RoleCount, _repository.GetAllPermissionCollections().Count);
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

        if (_commandManager is null)
        {
            throw new InvalidOperationException(
                $"CommandManager is not available. Ensure '{CommandManagerAssemblyName}' is installed and loaded before calling {nameof(IAdminManager.GetCommandRegistry)}.");
        }

        var commandRegistry = _commandManager.GetRegistry(moduleIdentity);
        var registry        = new AdminCommandRegistry(commandRegistry, this, _shared, moduleIdentity);
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

        if (manifest.Admins is { Count: > 0 })
        {
            var validCount = manifest.Admins.Count;

            manifest.Admins.RemoveAll(a =>
            {
                SteamID steamId = a.Identity;

                if (steamId.IsValidUserId())
                    return false;

                _logger.LogWarning(
                    "Module '{Module}': Ignoring admin with invalid SteamID64 '{SteamId}'.",
                    moduleIdentity, a.Identity);

                return true;
            });

            if (manifest.Admins.Count < validCount)
            {
                _logger.LogWarning(
                    "Module '{Module}': {Removed} admin(s) removed due to invalid SteamID64.",
                    moduleIdentity, validCount - manifest.Admins.Count);
            }
        }

        var removedPermissions = _repository.UnregisterManifestPermissions(moduleIdentity);
        _permissionIndex.Unregister(removedPermissions);

        var permissionsToRegister = _repository.RegisterModuleData(moduleIdentity, manifest, out var newConcretePermissions);
        _permissionIndex.Register(permissionsToRegister);

        _resolver.RefreshModuleScope(moduleIdentity, manifest, newConcretePermissions);
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

#endregion
}
