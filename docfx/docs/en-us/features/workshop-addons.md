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
| `RefreshClient(steamId)` | Resend the addons to an in-game client that failed to receive them (it reconnects). Wire it to your own command. |
| `InstallAddonListener(listener)` | Deliver extra addons to specific clients through `IAddonListener.OnClientQueryAddons`. |

There are no ConVars or console commands. Anything beyond this belongs in a module.

See the example: [IAddonManager](../examples/addon-manager.md)
