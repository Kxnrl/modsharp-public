# Admin Permissions and Commands

Reference `ModSharp.Sharp.Modules.AdminManager.Shared` as a host-provided contract (`ExcludeAssets="runtime"`). Install `Sharp.Modules.AdminManager` and **`Sharp.Modules.CommandCenter`** on the server; referencing the Shared package alone does not provide a command service. Start with the [Hello World project settings](hello-world.md).

The full example exports three permission-protected commands: `example_slay` kills all living players, `example_kill` kills the issuing player, and `example_heal` sets all living players' health and maximum health to 1337. Use `.example_kill` in chat or `ms_example_kill` in a console. Commands issued from the server console have a null issuer and bypass the player permission check. The example prefix avoids collisions with first-party admin commands. Replace the sample SteamID64 values before deployment.

## Ownership and Lifecycle

Use your actual assembly name as a single, stable `moduleIdentity` for both `MountAdminManifest` and `GetCommandRegistry`. AdminManager and CommandCenter use that identity to remove permissions, admin sources and commands when the consumer disconnects.

Resolve the provider wrapper in `OnAllModulesLoaded` and when `Sharp.Modules.AdminManager` or `Sharp.Modules.CommandCenter` connects. The example tolerates a CommandCenter that is not yet available without swallowing genuine manifest or registration errors. `OnAllModulesLoaded` also runs for a reloaded consumer.

> [!WARNING]
> Reloading CommandCenter or AdminManager invalidates existing command registries, because AdminManager caches registries and registered command delegates capture the old AdminManager instance. Reload dependent command modules alongside them, or restart the server; clearing the interface wrapper alone will not rebind already-registered commands.

`MountAdminManifest` and `GetCommandRegistry` must run on the game thread. After asynchronous database/HTTP work, marshal the installation back to the game thread with `IModSharp.InvokeFrameAction` or `InvokeFrameActionAsync`, and resolve the current provider again inside that action.

## Permissions and Commands

Permissions are concrete `group:action` keys, such as `admin_offensive:slay`, `admin_offensive:kill` and `admin_medic:heal`. A command's permission list uses **OR** logic: any listed permission is sufficient. For AND logic, obtain the issuer's `IAdmin` and check every required permission in your handler. Deny rules override grants.

`RegisterAdminCommand` does **not** index its permission strings. Choose one of these approaches:

| Approach | Behavior |
|---|---|
| `MountAdminManifest` → `PermissionCollection` | Publishes concrete permission keys as part of a complete manifest containing roles and admin assignments. Remounting replaces that module's manifest. |
| `registry.RegisterPermissions([...])` | Publishes concrete keys without supplying roles/admins. Calls accumulate in the consumer's scope and are removed when that consumer disconnects. |

The full sample uses `PermissionCollection`, so it does not redundantly call `RegisterPermissions`. A command-only consumer can instead register its concrete keys with `RegisterPermissions` before registering commands. Both direct rules and wildcards resolve against the known concrete permission index; operators may see unresolved-permission diagnostics before the owning consumer publishes its keys.

## Manifest Scope

The manifest contains permission collections, roles and admin assignments. The sample defines role `GeneralAdmin` and demonstrates these rules:

| Assignment | Result |
|---|---|
| `@GeneralAdmin` | Inherits the role's slay, kill and heal permissions. |
| `admin_medic:heal` | Heal only. |
| `@GeneralAdmin`, `!admin_medic:heal` | Slay/kill, but heal is denied. |
| `admin_offensive:*` | Slay/kill, not heal. |

**Role names are module-scoped**, not global. An `@RoleName` in your manifest must refer to a role defined in that **same manifest owner scope**. A role with the same name in `admins.jsonc` or another module cannot satisfy it. Permission keys, by contrast, are globally indexed. Admin sources for the same SteamID merge across modules: grants are unioned, immunity takes the maximum, and denies win globally.

## External Data

Prepare an `AdminTableManifest` from your own database/API data, then mount that completed snapshot on the game thread. The factory callback passed to `MountAdminManifest` (`BuildAdminManifest` in the sample) runs synchronously; do not block it with database or network requests. To assign only concrete permissions that are already registered elsewhere, pass empty permission and role collections. To assign a role, define that role in your own manifest. Remount with a new full snapshot to replace this module's previous assignments, including removals.

[!code-csharp[AdminExample.cs](../../codes/admin-example.cs)]
