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

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using PermissionCollectionDictionary = System.Collections.Generic.Dictionary<
    string,                                    // Collection key
    System.Collections.Generic.HashSet<string> // Permissions
>;

namespace Sharp.Modules.AdminManager.IO;

internal sealed class AdminConfigLoader
{
    private readonly ILogger<AdminManager> _logger;

    public AdminConfigLoader(ILogger<AdminManager> logger)
    {
        _logger = logger;
    }

    public AdminTableManifest LoadMergedManifest(string sharpPath)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            ReadCommentHandling      = JsonCommentHandling.Skip,
            AllowTrailingCommas      = true,
            PropertyNameCaseInsensitive = true,
        };

        var simpleConfigPath   = Path.Combine(sharpPath, "configs", "admins_simple.jsonc");
        var advancedConfigPath = Path.Combine(sharpPath, "configs", "admins.jsonc");

        AdminTableManifest? manifest = null;

        if (File.Exists(advancedConfigPath))
        {
            try
            {
                var json = File.ReadAllText(advancedConfigPath);
                manifest = JsonSerializer.Deserialize<AdminTableManifest>(json, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admin.jsonc");
            }
        }

        if (manifest is null)
        {
            manifest = new AdminTableManifest(new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase),
                                              [],
                                              []);
        }
        else
        {
            if (manifest.Admins is { Count: > 1 })
            {
                var mergedAdmins = manifest.Admins
                                           .GroupBy(x => x.Identity)
                                           .Select(g => new AdminManifest(g.Key,
                                                                          g.Max(x => x.Immunity),
                                                                          g.SelectMany(x => x.Permissions ?? [])
                                                                           .ToHashSet()))
                                           .ToList();

                manifest = manifest with { Admins = mergedAdmins };
            }

            manifest = new AdminTableManifest(manifest.PermissionCollection
                                              ?? new PermissionCollectionDictionary(StringComparer.OrdinalIgnoreCase),
                                              manifest.Roles  ?? [],
                                              manifest.Admins ?? []);
        }

        if (File.Exists(simpleConfigPath))
        {
            try
            {
                var simpleJson = File.ReadAllText(simpleConfigPath);
                var simpleDict = JsonSerializer.Deserialize<Dictionary<string, string>>(simpleJson, jsonOptions);

                if (simpleDict != null)
                {
                    var existingIds = manifest.Admins.Select(x => x.Identity).ToHashSet();

                    foreach (var (steamIdStr, roleName) in simpleDict)
                    {
                        if (!ulong.TryParse(steamIdStr, out var steamId))
                        {
                            continue;
                        }

                        if (existingIds.Contains(steamId))
                        {
                            continue;
                        }

                        manifest.Admins.Add(new AdminManifest(steamId,
                                                              0,
                                                              [$"{IAdminManager.RolesOperator}{roleName}"]));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admins_simple.jsonc");
            }
        }

        return manifest;
    }
}
