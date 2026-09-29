# Workshop Addons

ModSharp ships two mechanisms for delivering Steam Workshop addons to clients:
**DualAddon** for the single-extra-addon case, and **ExtraAddon** (a port of
[MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager)) for everything else.

The two are mutually exclusive — pick one based on your use case.

## When to use which

| Your use case | Pick |
|---|---|
| Workshop map + exactly **1** extra addon | **DualAddon** |
| Workshop map + **multiple** extra addons | **ExtraAddon** |
| Per-client addon delivery (different addons for different players) | **ExtraAddon** |
| Runtime add/remove of addons (without restart) | **ExtraAddon** |
| Delivering an addon on a Valve (non-workshop) map | **ExtraAddon** |

For most servers running a single workshop content pack alongside a workshop map,
DualAddon is the simpler and more battle-tested choice.

> [!WARNING]
> `-dual_addon` and `-extra_addons` cannot be used together. Specifying both
> will cause a fatal error at startup.

---

## DualAddon

Delivers exactly one extra workshop addon alongside an active workshop map.

### Usage

Add `-dual_addon <workshopId>` to your launch options:

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -dual_addon 123123123123
```

That's it. No ConVars, no runtime API.

### Notes

- The addon is delivered when a client connects. They will see two reconnect
  cycles: first to download the workshop map, then to download the dual addon.
- DualAddon only activates while the server is running a workshop map. On a
  Valve map (e.g. `de_dust2`), the addon will not be sent. Use ExtraAddon if
  you need addon delivery on Valve maps.
- The addon is mounted server-side automatically.

---

## ExtraAddon

Delivers an arbitrary number of workshop addons, with optional per-client targeting
and a runtime API for adding/removing addons without a restart.

### Usage

Add `-extra_addons <id1>,<id2>,...` to your launch options:

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -extra_addons "123123123123,123123123456,123123123789"
```

The same list is also exposed via the `ms_extra_addons` ConVar and can be
modified at runtime through commands or the C# API.

### Reconnect cycles

Clients have to reconnect once per addon they need to download. With the cache
ConVars enabled (recommended for production), this only happens on first visit
— subsequent connections are direct.

### ConVars

| ConVar | Default | Description |
|---|---|---|
| `ms_extra_addons` | (CLI value) | Workshop IDs of server-side extra addons, comma-separated. Changing it triggers a remount. |
| `ms_client_extra_addons` | `""` | Workshop IDs delivered to all clients (download-only, not mounted server-side). |
| `ms_extra_addons_timeout` | `10` | Seconds to allow between client reconnects when downloading addons. Range: 1 – 600. |
| `ms_extra_addons_debug` | `false` | Verbose debug logging for the addon delivery flow. |
| `ms_cache_clients_with_addons` | `false` | Cache which addons each client has already downloaded so reconnects skip the redownload flow. |
| `ms_cache_clients_duration` | `0` | How long (seconds) to keep a client's cache. `0` = forever. |
| `ms_block_disconnect_messages` | `false` | Suppress "loop shutdown" disconnect messages emitted while clients reconnect to receive addons. |
| `ms_addon_mount_download` | `false` | Always re-download an addon when mounting, even if it's already installed. |

### Console commands

| Command | Description |
|---|---|
| `ms_add_addon <id>` | Add a workshop ID to the server-side extra addons list. |
| `ms_remove_addon <id>` | Remove a workshop ID from the server-side extra addons list. |
| `ms_add_client_addon <id>` | Add a workshop ID to the global client addons list. |
| `ms_remove_client_addon <id>` | Remove a workshop ID from the global client addons list. |
| `ms_download_addon <id>` | Manually queue a workshop addon download. |
| `ms_print_searchpaths` | Print the current filesystem search paths (server console). |
| `ms_print_searchpaths_client` | Print the current filesystem search paths (client/listenserver). |

### Runtime API

The `IAddonManager` interface (obtained via `ISharedSystem.GetAddonManager()`) exposes the full ExtraAddon surface. Highlights:

| Method | Purpose |
|---|---|
| `ExtraAddonAddAddon(id, refresh)` | Add a server-side addon at runtime. |
| `ExtraAddonRemoveAddon(id, refresh)` | Remove a server-side addon at runtime. |
| `ExtraAddonAddClientAddon(id, steamId, refresh)` | Add an addon to a specific client (or all clients with `steamId=0`). |
| `ExtraAddonRemoveClientAddon(id, steamId)` | Remove a client-side addon. |
| `ExtraAddonRefreshAddons(reloadMap)` | Re-mount all server-side addons. |
| `ExtraAddonReloadMap()` | Reload the current map. |
| `ExtraAddonGetMountedAddons()` | Inspect what's currently mounted. |
| `ExtraAddonHasUGCConnection()` | Check whether SteamUGC is available. |

See the example for typical usage: [Extra Addon Example](../examples/extra-addon.md)
