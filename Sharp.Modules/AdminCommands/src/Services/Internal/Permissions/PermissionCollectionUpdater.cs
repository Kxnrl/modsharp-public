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

    public static void Write(IAdminManager               adminManager,
                             string                      sharpPath,
                             string                      collectionName,
                             IReadOnlyCollection<string> permissions,
                             ILogger                     logger)
    {
        var configPath = Path.Combine(sharpPath, "configs", "admins.jsonc");

        try
        {
            AdminTableManifest manifest;

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);

                manifest = JsonSerializer.Deserialize<AdminTableManifest>(json, Options)
                           ?? new AdminTableManifest(new (StringComparer.OrdinalIgnoreCase), [], []);
            }
            else
            {
                manifest = new AdminTableManifest(new (StringComparer.OrdinalIgnoreCase), [], []);
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
