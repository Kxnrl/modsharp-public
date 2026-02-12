# Admin FlatFile

ModSharp separates administrator configuration into two files to allow for both quick assignments and complex permission management.

1.  **Simple Assignment:** `{CS2}/sharp/configs/admins_simple.jsonc`
2.  **Advanced Configuration:** `{CS2}/sharp/configs/admins.jsonc`

## Programmatic Integration Note

If your module also uses `IAdminManager.MountAdminManifest(...)` and `GetCommandRegistry(...)`, use one stable `moduleIdentity` value for both calls and keep it unchanged across calls.

Prefer your module `AssemblyName`, for example:

```csharp
private static readonly string ModuleIdentity = typeof(MyModule).Assembly.GetName().Name ?? "MyModule";
```

## Simple Configuration (`admins_simple.jsonc`)

Use this file to quickly assign existing **Roles** to users using a simple Key-Value pair format.

*   **Key:** The player's SteamID64 as a **string** (JSON keys must be strings).
*   **Value:** The Role Name (string), or an array of Role Names.

**Notes:**
1. The Role name (e.g., "root", "admin") **must** be defined in the main `admins.jsonc` file. If a role name doesn't exist there, the admin will load with no permissions from that role — only a warning in the server log.
2. If a user is defined in both this file and the main `admins.jsonc`, the entry here will be **skipped** (`admins.jsonc` takes precedence). A log message will be emitted to help you notice this.

**Tip:** Run `ms_perms` in the server console to see all available permissions, and `ms_admins` to inspect loaded admins.

**Example:**
```json
{
    // Owner / Root
    "76561198000000001": "root",

    // General Admins
    "76561198000000002": "admin",
    "76561198000000003": "admin",

    // Multiple roles: admin + vip
    "76561198000000004": ["admin", "vip"]
}
```

---

## Advanced Configuration (`admins.jsonc`)

This file contains the definitions for Permissions, Roles, and detailed Admin configurations. It is divided into three sections:

1.  **PermissionCollection:** A registry of permission strings, grouped by a key name (e.g., `"admin"`). Each string (e.g., `"admin:kick"`) represents a grantable/deniable action. Registered permissions enable wildcard expansion (`admin:*`), load-time validation, and console listing via `ms_perms`. Server operators usually don't need to modify this section — the defaults cover all built-in admin commands.
2.  **Roles:** Defines groups of permissions and immunity levels that can be assigned to admins.
3.  **Admins:** Assigns roles and specific permissions to individual SteamIDs.

### 1. Structure

#### Roles Object
*   `Name` (required): The name of the role (referenceable via `@Name`).
*   `Immunity` (optional): Immunity level (0-255).
*   `Permissions` (required): A list of permission strings or inherited roles.

#### Admins Object
*   `Identity` (required): The player's SteamID64 (e.g., `76561198000000001`). Accepts both integer and `"string"` format in JSON.
*   `Immunity` (optional): Overrides role immunity. The highest value found across all entries is used. An admin with higher immunity cannot be targeted by admins with lower immunity (e.g., kick, ban, slay).
*   `Permissions` (required): A list of roles to inherit or specific permissions to grant/deny.

### 2. Permission Syntax rules

You can use the following syntax within the `Permissions` array for both Roles and Admins:

*   **`@RoleName`**: Inherit all permissions and immunity from a Role (Recursive).
*   **`!permission`**: Deny a permission. This is global and overrides any grants.
*   **`*`**: Grants every permission.
*   **`module:*`**: Grants all permissions within a specific module (Recursive).

#### Wildcard Matching Details

The `:` character is the segment separator. Wildcard behavior depends on position:

*   **Trailing wildcard** (at the end of a pattern): matches all remaining segments.
    *   `admin:*` matches `admin:ban`, `admin:ban:extended`, etc.
    *   `*` alone matches every registered permission.
*   **Mid-segment wildcard**: matches exactly one segment.
    *   `admin:*:give` matches `admin:items:give` but NOT `admin:items:sub:give`.

### 3. General Rules
*   **Case Insensitivity:** `Admin:Kick` is treated the same as `admin:kick`.
*   **Identity Format:** Must be SteamID64. Accepts both integer (`76561198000000001`) and string (`"76561198000000001"`) format. Do not use legacy formats (STEAM_0:...) or SteamID3 ([U:1:...]).

### 4. Multi-Module Merge

If the same SteamID appears in multiple module configs:
1.  **Merging:** The user gets the combined permissions of ALL entries.
2.  **Immunity:** The user gets the HIGHEST immunity value found across all entries.
3.  **Denials:** A `!permission` overrides grants from ANY module.
4.  **Role Scope:** `@RoleName` is resolved against the Roles defined by the same module that owns the admin entry. It does not look up roles from other modules.

**Example:** Module A defines role `admin` and assigns user `76561198000000001` with `@admin`. Module B defines role `vip` and assigns the same user with `@vip`. The result:
*   `@admin` resolves using Module A's roles → grants Module A's admin permissions.
*   `@vip` resolves using Module B's roles → grants Module B's vip permissions.
*   The user gets the union of both.
*   If Module B's admin entry references `@admin`, it will **not** find Module A's role — it only searches Module B's own roles.

### Complete Example (`admins.jsonc`)

```json
{
    "PermissionCollection": {
        "admin": [
            "admin:kick", "admin:ban", "admin:mute", "admin:slay",
            "admin:freeze", "admin:tp", "admin:map", "admin:noclip",
            "admin:rcon", "admin:cvar"
        ]
    },

    "Roles": [
        {
            "Name": "root",
            "Immunity": 255,
            "Permissions": ["*"]
        },
        {
            "Name": "senior_admin",
            "Immunity": 80,
            "Permissions": ["@admin", "admin:rcon", "admin:cvar"]
        },
        {
            "Name": "admin",
            "Immunity": 60,
            "Permissions": ["@moderator", "admin:map", "admin:slay", "admin:noclip"]
        },
        {
            "Name": "moderator",
            "Immunity": 40,
            "Permissions": ["@helper", "admin:ban", "admin:freeze", "admin:tp"]
        },
        {
            "Name": "helper",
            "Immunity": 20,
            "Permissions": ["admin:kick", "admin:mute"]
        }
    ],

    "Admins": [
        {
            "Identity": 76561198000000001,
            "Permissions": ["@root"]
        },
        {
            // Admin who cannot ban
            "Identity": 76561198000000002,
            "Permissions": ["@admin", "!admin:ban"]
        }
    ]
}
```