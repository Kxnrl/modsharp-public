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
using Sharp.Shared;
using Sharp.Shared.Units;
using PermissionCollectionDictionary = System.Collections.Generic.Dictionary<
    string,                                    // Collection key
    System.Collections.Generic.HashSet<string> // Permissions
>;

namespace Sharp.Modules.AdminManager.IO;

internal sealed class AdminConfigLoader
{
    private readonly ILogger<AdminConfigLoader> _logger;

    public AdminConfigLoader(ISharedSystem shared)
        => _logger = shared.GetLoggerFactory().CreateLogger<AdminConfigLoader>();

    public AdminTableManifest LoadMergedManifest(string sharpPath)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            ReadCommentHandling         = JsonCommentHandling.Skip,
            AllowTrailingCommas         = true,
            PropertyNameCaseInsensitive = true,
            Converters                  = { new SteamIdConverter() },
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
                _logger.LogError(ex,
                                 "Failed to parse admins.jsonc — starting with NO admins, NO roles, and NO permissions. "
                                 + "Fix the JSON syntax in admins.jsonc and reload.");
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
                var simpleDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(simpleJson, jsonOptions);

                if (simpleDict != null)
                {
                    var existingIds = manifest.Admins.Select(x => x.Identity).ToHashSet();
                    var knownRoles  = manifest.Roles.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var (steamIdStr, value) in simpleDict)
                    {
                        if (!SteamID.TryParse(steamIdStr, out var steamId) || !steamId.IsValidUserId())
                        {
                            _logger.LogWarning("Ignoring invalid SteamID64 '{SteamId}' in admins_simple.jsonc.", steamIdStr);

                            continue;
                        }

                        if (existingIds.Contains(steamId))
                        {
                            _logger.LogWarning(
                                "SteamID64 '{SteamId}' in admins_simple.jsonc is already defined in admins.jsonc and will be skipped.",
                                steamIdStr);

                            continue;
                        }

                        var roleNames = ParseSimpleRoleValue(steamIdStr, value);

                        if (roleNames.Count == 0)
                        {
                            continue;
                        }

                        var permissions = new HashSet<string>(roleNames.Count);

                        foreach (var role in roleNames)
                        {
                            if (!knownRoles.Contains(role))
                            {
                                _logger.LogWarning(
                                    "Role '{Role}' referenced by SteamID64 '{SteamId}' in admins_simple.jsonc is not defined in admins.jsonc. "
                                    + "This admin will have no permissions from this role — check for typos.",
                                    role,
                                    steamIdStr);
                            }

                            permissions.Add($"{IAdminManager.RolesOperator}{role}");
                        }

                        manifest.Admins.Add(new AdminManifest(steamId, 0, permissions));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                 "Failed to parse admins_simple.jsonc — entries from this file will be skipped. Fix the JSON syntax and reload.");
            }
        }

        return manifest;
    }

    private List<string> ParseSimpleRoleValue(string steamIdStr, JsonElement value)
    {
        var roles = new List<string>();

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
            {
                var roleName = value.GetString();

                if (string.IsNullOrWhiteSpace(roleName))
                {
                    _logger.LogWarning("Ignoring empty role assignment for SteamID64 '{SteamId}' in admins_simple.jsonc.",
                                       steamIdStr);
                }
                else
                {
                    roles.Add(roleName);
                }

                break;
            }

            case JsonValueKind.Array:
            {
                foreach (var element in value.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String)
                    {
                        _logger.LogWarning("Ignoring non-string role value for SteamID64 '{SteamId}' in admins_simple.jsonc.",
                                           steamIdStr);

                        continue;
                    }

                    var roleName = element.GetString();

                    if (!string.IsNullOrWhiteSpace(roleName))
                    {
                        roles.Add(roleName);
                    }
                }

                if (roles.Count == 0)
                {
                    _logger.LogWarning("Ignoring empty role array for SteamID64 '{SteamId}' in admins_simple.jsonc.",
                                       steamIdStr);
                }

                break;
            }

            default:
                _logger.LogWarning(
                    "Ignoring invalid role value for SteamID64 '{SteamId}' in admins_simple.jsonc. Expected string or array.",
                    steamIdStr);

                break;
        }

        return roles;
    }

    /// <summary>
    ///     Accepts both JSON number (<c>76561198000000001</c>) and
    ///     JSON string (<c>"76561198000000001"</c>) for SteamID64 fields,
    ///     so admins.jsonc and admins_simple.jsonc can use the same format.
    /// </summary>
    private sealed class SteamIdConverter : JsonConverter<ulong>
    {
        public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetUInt64(),
                JsonTokenType.String when SteamID.TryParse(reader.GetString(), out var steamId)
                                          && steamId.IsValidUserId() => steamId.AsPrimitive(),
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to a valid SteamID64."),
            };
        }

        public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }
}
