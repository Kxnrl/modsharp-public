# Workshop Addons

ModSharp can deliver Steam Workshop addons to clients alongside the map.

## Usage

Add `-dual_addon` to your launch options:

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -dual_addon 123123123123
```

Multiple addons are comma-separated:

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -dual_addon "123123123123,123123123456"
```

`-dual_addon` must be present to use addon support at all, including the runtime API.

## Delivery flow

The flow is picked automatically on every map change:

| Addons | Flow |
|---|---|
| 1 | **DualAddon**: the battle-tested single addon flow. Only works on workshop maps. |
| 2+, or an `IAddonListener` is installed | **MultiAddon**: based on [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager). Clients reconnect once per addon they still need. Works on Valve maps too. |

Most servers only need one addon, so DualAddon is the default.

Clients that already downloaded the addons are cached for 10 minutes so reconnects skip the download flow.
Missing addons are downloaded on the server automatically and the map is reloaded once they finish.

## Runtime API

`IAddonManager` (from `ISharedSystem.GetAddonManager()`) keeps things minimal:

| Method | Purpose |
|---|---|
| `GetAddons()` | Server addons applied on the next map change. |
| `SetAddons(ids)` | Replace the server addons. Change the map yourself to apply them (a restart is best). |
| `ResetClientCache(steamId)` | Forget what a client (or everyone with `default`) already downloaded. |
| `RefreshClient(steamId, resetCache)` | Resend the addons to an in-game client (it reconnects). `resetCache: true` resends everything when it failed to receive them, `false` only sends what it does not have yet. |
| `UpdateAddon(id)` | Force a workshop update, remounting the addon around it (Windows locks mounted files). |
| `ReloadMap()` | Reload the current map. Workshop maps use `ds_workshop_changelevel` when the server already has them, otherwise `host_workshop_map`. |
| `SetOptions(options)` | Timeouts, client cache and debug logging of the MultiAddon flow. |
| `InstallAddonListener(listener)` | Deliver extra addons to specific clients through `IAddonListener.OnClientQueryAddons`. |

There are no ConVars or console commands. Anything beyond this belongs in a module.

## Extra Addon Manager module

`Sharp.Modules.ExtraAddonManager` provides the [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager) feature set on top of `IAddonManager`.
Installing it always enables the MultiAddon flow. Other modules can use it through `IExtraAddonManager`.

| ConVar | Default | Description |
|---|---|---|
| `ms_extra_addons` | `-dual_addon` value | Server addons, comma-separated. Applied on the next map change. |
| `ms_client_extra_addons` | `""` | Addons delivered to every client (download-only). |
| `ms_block_disconnect_messages` | `false` | Hide "loop shutdown" disconnect messages while clients reconnect for addons. |
| `ms_addon_mount_download` | `false` | Re-download (update) server addons on every map start. |
| `ms_extra_addons_timeout` | `10` | Seconds allowed between reconnects for the next addon. |
| `ms_addon_connection_timeout` | `30` | Seconds allowed to accept the first addon before being kicked, 0 disables. |
| `ms_cache_clients_with_addons` | `false` | Remember downloaded addons so map changes / rejoins skip the reconnects. |
| `ms_cache_clients_duration` | `0` | How long to remember them in seconds, 0 forever. |
| `ms_addon_debug` | `false` | Print verbose information about the download flow. |

| Command | Description |
|---|---|
| `ms_add_addon <id>` / `ms_remove_addon <id>` | Edit the server addons. |
| `ms_add_client_addon <id>` / `ms_remove_client_addon <id>` | Edit the global client addons. |
| `ms_download_addon <id>` | Download an addon on the server. |
| `ms_reload_map` | Reload the current map to apply changes. |
| `ms_addon_refresh` (client) | Re-fetch the addons when they failed to arrive. |

Search path printing is not available.

See the example: [IAddonManager](../examples/addon-manager.md)
