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

using System.Text;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Storage;
using Sharp.Modules.CommandCenter.Shared;
using Sharp.Shared;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminManager.Commands;

internal sealed class ServerCommands
{
    private readonly ILogger<ServerCommands> _logger;
    private readonly AdminManager            _adminManager;
    private readonly AdminRepository         _repository;

    private bool _registered;

    public ServerCommands(ISharedSystem shared, AdminManager adminManager, AdminRepository repository)
    {
        _logger       = shared.GetLoggerFactory().CreateLogger<ServerCommands>();
        _adminManager = adminManager;
        _repository   = repository;
    }

    public void TryRegister(ICommandCenter? commandCenter, string moduleIdentity)
    {
        if (_registered || commandCenter is null)
        {
            return;
        }

        var registry = commandCenter.GetRegistry(moduleIdentity);

        registry.RegisterServerCommand("perms",  OnPermissionsCommand, "Lists all registered permissions grouped by module.");
        registry.RegisterServerCommand("admins", OnAdminsCommand,      "Lists all loaded admins. Usage: ms_admins [steamid64]");

        registry.RegisterServerCommand("reload_admins",
                                       OnReloadAdminsCommand,
                                       "Reloads admin configuration from admins.jsonc and admins_simple.jsonc.");

        _registered = true;
    }

    private void OnReloadAdminsCommand()
    {
        _adminManager.ReloadAdmins();
    }

    private void OnPermissionsCommand()
    {
        var collections = _repository.GetAllPermissionCollections();

        if (collections.Count == 0)
        {
            _logger.LogInformation("No permissions registered.");

            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== Registered Permissions ===");

        foreach (var (moduleIdentity, permissionSets) in collections.OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (permissionSets.Count == 0)
            {
                continue;
            }

            sb.Append("  Module: ").AppendLine(moduleIdentity);

            foreach (var (collectionKey, permissions) in permissionSets)
            {
                sb.Append("    Collection '").Append(collectionKey).AppendLine("':");

                foreach (var perm in permissions.Order(StringComparer.OrdinalIgnoreCase))
                {
                    sb.Append("      - ").AppendLine(perm);
                }
            }
        }

        sb.Append("=== End ===");
        _logger.LogInformation("{Permissions}", sb.ToString());
    }

    private void OnAdminsCommand(StringCommand cmd)
    {
        var admins = _repository.EnumerateAdmins().ToList();

        if (admins.Count == 0)
        {
            _logger.LogInformation("No admins loaded.");

            return;
        }

        if (cmd.ArgCount > 0)
        {
            ShowAdminDetail(admins, cmd.GetArg(1));

            return;
        }

        var sb = new StringBuilder();
        sb.Append("=== Loaded Admins (").Append(admins.Count).AppendLine(") ===");
        sb.AppendLine("  Usage: ms_admins <steamid64> for detailed view.");

        foreach (var (steamId, cachedAdmin, sources) in admins.OrderBy(a => a.SteamId))
        {
            if (cachedAdmin is null)
            {
                sb.Append("  ").Append(steamId).AppendLine("  (not resolved)");

                continue;
            }

            sb.Append("  ")
              .Append(steamId)
              .Append("  |  Immunity: ")
              .Append(cachedAdmin.Immunity)
              .Append("  |  Permissions: ")
              .Append(cachedAdmin.Permissions.Count);

            if (sources.Count > 1)
            {
                sb.Append("  [")
                  .Append(string.Join(", ", sources.Keys.Order(StringComparer.OrdinalIgnoreCase)))
                  .Append(']');
            }

            sb.AppendLine();
        }

        sb.Append("=== End ===");
        _logger.LogInformation("{Admins}", sb.ToString());
    }

    private void ShowAdminDetail(List<(ulong SteamId, Admin? CachedAdmin, Dictionary<string, AdminSource> Sources)> admins,
        string                                                                                                      filter)
    {
        if (!ulong.TryParse(filter, out var targetId))
        {
            _logger.LogInformation("Invalid SteamID64: '{Filter}'.", filter);

            return;
        }

        var match = admins.FirstOrDefault(a => a.SteamId == targetId);

        if (match.CachedAdmin is null && match.Sources is null)
        {
            _logger.LogInformation("No admin found with SteamID64: {SteamId}.", targetId);

            return;
        }

        var admin = match.CachedAdmin;

        if (admin is null)
        {
            _logger.LogInformation("SteamID64: {SteamId} (not resolved)", targetId);

            return;
        }

        var sb = new StringBuilder();
        sb.Append("=== Admin Detail: ").Append(targetId).AppendLine(" ===");
        sb.Append("  Immunity: ").AppendLine(admin.Immunity.ToString());
        sb.Append("  Effective permissions (").Append(admin.Permissions.Count).AppendLine("):");

        foreach (var perm in admin.Permissions.Order(StringComparer.OrdinalIgnoreCase))
        {
            sb.Append("    - ").AppendLine(perm);
        }

        if (match.Sources.Count > 0)
        {
            sb.AppendLine("  Sources:");

            foreach (var (moduleId, source) in match.Sources.OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append("    Module '")
                  .Append(moduleId)
                  .Append("': ")
                  .Append("immunity=")
                  .Append(source.CalculatedImmunity)
                  .Append(", allows=")
                  .Append(source.ResolvedAllows.Count)
                  .Append(", denies=")
                  .AppendLine(source.ResolvedDenies.Count.ToString());
            }
        }

        sb.Append("=== End ===");
        _logger.LogInformation("{AdminDetail}", sb.ToString());
    }
}
