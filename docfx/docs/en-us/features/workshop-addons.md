# Workshop Addons

ModSharp can deliver Steam Workshop addons to clients alongside the map.

## Usage

Install the `Sharp.Modules.AddonManager` module and list your addons in `core.json`:

```json
{
  "AddonManager": {
    "Addons": [123123123123, 123123123456],
    "ClientAddons": []
  }
}
```

`core.json` is read before the first map loads, so no launch parameter is needed.
The addons can also be changed at runtime through the ConVars / commands below or the `IAddonManager` module interface.

> [!NOTE]
> The `-dual_addon` launch parameter is deprecated but still works, its addons are added to the list above.
>
> ```text
> ./cs2 -dedicated ... +host_workshop_map 300123123123 -dual_addon "123123123123,123123123456"
> ```

## Delivery flow

The module always uses the **MultiAddon** flow, based on [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager):
clients reconnect once per addon they still need, on workshop and Valve maps alike.
Missing addons are downloaded on the server automatically and the map is reloaded once they finish.

Clients that already have the addons skip the reconnects on map changes and rejoins (`ms_cache_clients_with_addons`, on by default).

> [!NOTE]
> Without the module, the deprecated `-dual_addon` keeps its previous behavior:
> 1 addon uses the legacy **DualAddon** flow (workshop maps only), 2+ addons use the MultiAddon flow.

## Server-only files (`sharp/assets`)

When the `sharp/assets` folder exists it is added to the server's `GAME` search path (always added with the deprecated `-dual_addon`).
Files there are only read by the server, nothing is sent to clients.

To serve an addon's content from loose files (e.g. extracted by a plugin) instead of mounting its VPK on the server,
put the files in `sharp/assets` and list the addon in `ClientAddons`: clients still download it from the workshop,
while the server neither mounts nor downloads it.

## ConVars

| ConVar | Default | Description |
|---|---|---|
| `ms_extra_addons` | `core.json` value | Server addons, comma-separated. Applied on the next map change. |
| `ms_client_extra_addons` | `core.json` value | Addons delivered to every client (download-only). |
| `ms_block_disconnect_messages` | `false` | Hide "loop shutdown" disconnect messages while clients reconnect for addons. |
| `ms_addon_mount_download` | `false` | Re-download (update) server addons on every map start. |
| `ms_extra_addons_timeout` | `10` | Seconds allowed between reconnects for the next addon. |
| `ms_addon_connection_timeout` | `30` | Seconds allowed to accept the first addon before being kicked, 0 disables. |
| `ms_cache_clients_with_addons` | `true` | Remember downloaded addons so map changes / rejoins skip the reconnects. |
| `ms_cache_clients_duration` | `0` | How long to remember them in seconds, 0 forever. |
| `ms_addon_debug` | `false` | Print verbose information about the download flow. |

## Commands

| Command | Description |
|---|---|
| `ms_add_addon <id>` / `ms_remove_addon <id>` | Edit the server addons. |
| `ms_add_client_addon <id>` / `ms_remove_client_addon <id>` | Edit the global client addons. |
| `ms_download_addon <id>` | Download an addon on the server. |
| `ms_reload_map` | Reload the current map to apply changes. |
| `ms_addon_refresh` (client) | Re-fetch the addons when they failed to arrive. |

## Runtime API

Other modules use the `IAddonManager` module interface (`Sharp.Modules.AddonManager.Shared`):

| Method | Purpose |
|---|---|
| `GetAddons()` / `AddAddon` / `RemoveAddon` / `ClearAddons` | Server addons, applied on the next map change or right away with `reloadMap: true`. |
| `GetClientAddons` / `AddClientAddon` / `RemoveClientAddon` / `ClearClientAddons` | Download-only addons for every client (`default` SteamID) or a single one. |
| `RefreshClient(steamId)` | Resend every addon to an in-game client that failed to receive them (it reconnects). |
| `DownloadAddon(id, reloadMap)` | Download (update) an addon on the server. |
| `ReloadMap()` | Reload the current map. Workshop maps use `ds_workshop_changelevel` when the server already has them, otherwise `host_workshop_map`. |

> [!NOTE]
> `ISharedSystem.GetAddonManager()` (`Sharp.Shared.Managers.IAddonManager`) is the low-level API the module is built on.
> Use it only to write your own addon manager. If you import both namespaces, alias one of them
> (e.g. `using IAddonManager = Sharp.Modules.AddonManager.Shared.IAddonManager;`).

See the example: [IAddonManager](../examples/addon-manager.md)
