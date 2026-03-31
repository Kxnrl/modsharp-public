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

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;

namespace Sharp.Modules.AdminCommands.Services.Internal.Permissions;

internal static class PermissionCollectionUpdater
{
    private static readonly JsonSerializerOptions Options =
        new ()
        {
            ReadCommentHandling         = JsonCommentHandling.Skip,
            AllowTrailingCommas         = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented               = true,
            NumberHandling              = JsonNumberHandling.AllowReadingFromString,
        };

    public static void Write(IAdminManager adminManager,
        string                             sharpPath,
        string                             collectionName,
        IReadOnlyCollection<string>        permissions,
        ILogger                            logger)
    {
        var configPath = Path.Combine(sharpPath, "configs", "admins.jsonc");

        try
        {
            AdminTableManifest manifest;

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);

                manifest = JsonSerializer.Deserialize<AdminTableManifest>(json, Options)
                           ?? new AdminTableManifest(new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase),
                                                     [],
                                                     []);
            }
            else
            {
                manifest = new AdminTableManifest(new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase),
                                                  [],
                                                  []);
            }

            var permissionCollection = manifest.PermissionCollection ?? [];
            var roles                = manifest.Roles                ?? [];
            var users                = manifest.Admins               ?? [];

            permissionCollection[collectionName] = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var normalized = new AdminTableManifest(permissionCollection, roles, users);

            var serialized = JsonSerializer.Serialize(normalized, Options);
            File.WriteAllText(configPath, serialized);

            // Remount full config manifest under AdminManager's identity (replace semantics).
            // This keeps runtime state in sync with the file on disk.
            adminManager.MountAdminManifest(AdminCommands.AdminManagerAssemblyName, () => normalized);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update permission collection '{Collection}' in admins.jsonc.", collectionName);
        }
    }
}
