# Administrator Configuration (AdminManager)

Administrator permissions are managed by **AdminManager**, which loads two files:

- `sharp/configs/admins.jsonc`: permission collections, roles, and full admin entries.
- `sharp/configs/admins_simple.jsonc`: optional shorthand mapping SteamID64s to existing roles.

Both files support comments, trailing commas, and case-insensitive property names. See the [bundled `admins.jsonc`](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/admins.jsonc), [annotated example](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/admins.jsonc.example), and [simple example](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/admins_simple.jsonc.example).

> [!WARNING]
> Legacy **AdminFlatFile** reads `admins.json`, depends on the deprecated core admin API, and is slated for removal in 2.2. It ignores these `.jsonc` files; migrate your config to the AdminManager format instead of renaming the old file.

## Adding a Root Admin

1. Keep the bundled `admins.jsonc` (or copy `admins.jsonc.example` if missing). Make sure `Roles` defines a `root` role with `"*"`, and clear or replace the sample SteamIDs in `Admins`.
2. Copy `admins_simple.jsonc.example` to `admins_simple.jsonc` and put your SteamID64 in it:

   ```json
   {
     "76561198000000001": "root"
   }
   ```

   Use your 64-bit SteamID, not a custom profile URL, `STEAM_0:...`, or `[U:1:...]`.
3. Run `ms_reload_admins` in the server console (or restart), then verify with `ms_admins <steamid64>`.

With **CommandCenter** loaded alongside AdminManager, these **server console** commands are available:

| Command | Purpose |
| --- | --- |
| `ms_perms` | List registered permissions grouped by module and collection |
| `ms_admins [steamid64]` | List all admins, or inspect one admin and its grant sources |
| `ms_reload_admins` | Reload `admins.jsonc` and `admins_simple.jsonc` |

Admin files are not watched automatically, so run `ms_reload_admins` after edits and check the logs for syntax errors or unknown roles/permissions. The previously loaded manifest stays active in memory only when the combined result of both `admins.jsonc` and `admins_simple.jsonc` is completely empty (no permission collections, roles, or admins). If `admins.jsonc` fails to parse while `admins_simple.jsonc` still loads valid entries, the reload still mounts that partial manifest without any role definitions, leaving those admins with no permissions.

## Simple Assignments (`admins_simple.jsonc`)

Keys are SteamID64 strings; values are a single role name or an array of role names:

```json
{
  "76561198000000001": "root",
  "76561198000000002": ["moderator", "helper"]
}
```

Every role must exist in `admins.jsonc` (unknown roles log a warning and grant nothing). If a SteamID also appears in `admins.jsonc`, its entry in `admins_simple.jsonc` is skipped rather than merged.

## Advanced Manifest (`admins.jsonc`)

Three top-level sections:

- `PermissionCollection`: maps collection names to permission strings. Permissions can be registered here or by modules; wildcards expand against this registered permission index.
- `Roles`: entries with `Name`, `Immunity` (0–255, default 0), and `Permissions`.
- `Admins`: entries with `Identity` (SteamID64 as a JSON number or string), `Immunity` (0–255, default 0), and `Permissions`.

An admin's effective immunity is the **maximum** of its own value and all inherited roles (an explicit lower number on the admin never lowers a higher role immunity). Multiple entries for the same SteamID in `admins.jsonc` merge permissions and take the highest explicit immunity. Immunity checks are enforced by the commands/modules targeting players.

Minimal example illustrating the layout (keep the bundled file for the full built-in permissions):

```json
{
  "PermissionCollection": {
    "admin": ["admin:kick", "admin:ban"]
  },
  "Roles": [
    { "Name": "root", "Immunity": 255, "Permissions": ["*"] },
    { "Name": "moderator", "Immunity": 40, "Permissions": ["admin:kick", "admin:ban"] }
  ],
  "Admins": [
    { "Identity": "76561198000000001", "Permissions": ["@root"] },
    { "Identity": "76561198000000002", "Permissions": ["@moderator", "!admin:ban"] }
  ]
}
```

### Permission Rules

Role and permission matching is case-insensitive:

| Syntax | Meaning |
| --- | --- |
| `@RoleName` | Recursively inherit the role's permissions and immunity |
| `!admin:ban` | Deny a permission, overriding grants from any source |
| `*` | Grant all registered permissions |
| `admin:*` | Grant all registered permissions under `admin:`, including deeper levels |
| `admin:*:give` | Match exactly one middle segment (`admin:items:give`, not `admin:items:sub:give`) |

`*` matches whole colon-separated segments (not substrings), and `!` denials support the same wildcards. Roles resolve only within the manifest of the module that owns the assignment.

When multiple modules assign the same SteamID, all grants are unioned, all denials are subtracted, and the highest immunity wins. Direct wildcards on an admin update automatically as new permissions are registered; wildcards inherited only through `@RoleName` recalculate on remount or `ms_reload_admins`. Check `ms_perms` and `ms_admins` to confirm live results.

## Module Integration

Use `IAdminManager` from `Sharp.Modules.AdminManager.Shared` instead of the deprecated `IClientManager` admin methods. Pass the same stable module identity (typically the assembly name) to `MountAdminManifest(moduleIdentity, ...)` and `GetCommandRegistry(moduleIdentity)`. Call `MountAdminManifest` first if your module supplies its own permission collections, roles, or admins; command-only modules can instead call `GetCommandRegistry` directly and publish their permissions via `RegisterPermissions` before registering commands. Command registration requires CommandCenter.

Both methods must be called on the game thread; after async DB or background work, dispatch back via `IModSharp.InvokeFrameAction` or `InvokeFrameActionAsync`. See the [Admin example](../examples/admin-example.md) and [IAdminManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/AdminManager/Shared/IAdminManager.cs).
