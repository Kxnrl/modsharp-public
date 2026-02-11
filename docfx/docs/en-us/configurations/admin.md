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

*   **Key:** The player's Steam64 ID.
*   **Value:** The Role Name (string).

**Notes:** 
1. The Role name (e.g., "root", "admin") must be defined in the main `admins.jsonc` file.
2. If a user is defined in both this file and the main `admins.jsonc`, the settings in the main `admins.jsonc` take precedence.

**Example:**
```json
{
    // Owner / Root
    "76561198000000001": "root",

    // General Admins
    "76561198000000002": "admin",
    "76561198000000003": "admin"
}
```

---

## Advanced Configuration (`admins.jsonc`)

This file contains the definitions for Permissions, Roles, and detailed Admin configurations. It is divided into three sections:

1.  **PermissionCollection:** Defines specific permission strings.
2.  **Roles:** Defines groups of permissions and immunity levels that can be assigned to admins.
3.  **Admins:** Assigns roles and specific permissions to individual SteamIDs.

### 1. Structure

#### Roles Object
*   `Name` (required): The name of the role (referenceable via `@Name`).
*   `Immunity` (optional): Immunity level (0-255).
*   `Permissions` (required): A list of permission strings or inherited roles.

#### Admins Object
*   `Identity` (required): The player's Steam64 ID (Integer format preferred).
*   `Immunity` (optional): Overrides role immunity. The highest value found across all entries is used.
*   `Permissions` (required): A list of roles to inherit or specific permissions to grant/deny.

### 2. Permission Syntax rules

You can use the following syntax within the `Permissions` array for both Roles and Admins:

*   **`@RoleName`**: Inherit all permissions and immunity from a Role (Recursive).
*   **`!permission`**: Deny a permission. This is global and overrides any grants.
*   **`*`**: Grants every permission.
*   **`module:*`**: Grants all permissions within a specific module (Recursive).

### 3. General Rules
*   **Case Insensitivity:** `Admin:Kick` is treated the same as `admin:kick`.
*   **Identity Format:** Must be strictly SteamID64 (Integer). Do not use legacy formats (STEAM_0:...) or SteamID3 ([U:1:...]).

### Complete Example (`admins.jsonc`)

```json
{
    "PermissionCollection": {
        "admin": ["admin:kick", "admin:ban", "admin:slay"],
        "vip": ["vip:skins", "vip:reserved_slot"]
    },

    "Roles": [
        {
            "Name": "root",
            "Immunity": 255,
            "Permissions": ["*"]
        },
        {
            "Name": "admin",
            "Immunity": 50,
            "Permissions": ["admin:*"]
        },
        {
            // Inherits 'admin' but removes the ban permission
            "Name": "junior_admin",
            "Immunity": 25,
            "Permissions": ["@admin", "!admin:ban"]
        }
    ],

    "Admins": [
        {
            // Server owner with root access
            "Identity": 76561198000000001,
            "Permissions": ["@root"]
        },
        {
            // Specific user with a specific setup
            "Identity": 76561198000000002,
            "Immunity": 20, 
            "Permissions": ["@junior_admin", "vip:*"]
        }
    ]
}
