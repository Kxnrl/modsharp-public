# Migrating from CounterStrikeSharp / Metamod

ModSharp has its own loader, module API, and lifecycle. CounterStrikeSharp plugins, SourceMod plugins, and Metamod binaries cannot run directly under ModSharp; port the code to `IModSharpModule` and deploy as shown in [Writing a Module](writing-a-module.md).

## Module lifetime vs. map lifetime

`IModSharpModule.Init()` / `PostInit()` and `Shutdown()` track module load and unload, **not** map start and end. A module stays loaded across many maps, and hot reloads happen right before the next map starts. Do not assume map-scoped objects or `GameRules` are ready in the module constructor, `Init()`, or `PostInit()`, since no map is loaded yet on initial startup.

For map callbacks, implement **`Sharp.Shared.Listeners.IGameListener`** (returning `IGameListener.ApiVersion` from `ListenerVersion` and setting `ListenerPriority`), install it via `sharedSystem.GetModSharp().InstallGameListener(listener)`, and remove it in `Shutdown()` with `RemoveGameListener(listener)`.

## Mapping lifecycle stages

Use this table as a rough guide; exact engine timing may differ slightly from other frameworks:

| Familiar hook | ModSharp equivalent |
| --- | --- |
| Early level init (`OnLevelInit`) | `OnGameInit` |
| Post-init (`OnMapInit`) | `OnGamePostInit` |
| Map active (`OnMapStart`) | `OnGameActivate` |
| Server activated | `OnServerActivate` |
| Map end (`OnMapEnd`) | `OnGameDeactivate` |

One important difference: `OnServerActivate` fires after the engine's server activation function and is **not an `OnConfigsExecuted` event**. If your module needs logic to run after specific `.cfg` files finish, schedule or trigger it explicitly rather than assuming `OnServerActivate` covers it.

Full map cycle order:

```text
OnServerInit
OnGameInit
OnGamePostInit
OnResourcePrecache
OnServerSpawn
OnGameActivate
OnServerActivate
... map runs ...
OnGameDeactivate
OnGamePreShutdown
OnGameShutdown
```

- `OnServerInit`: fires for every map (not once per process), right after network server and global variable wrappers are created.
- `OnGameInit`: `GameRules` is ready before listeners run.
- `OnResourcePrecache`: call `PrecacheResource` directly here; the precache context is cleared as soon as dispatch finishes, so deferring it to a timer is too late.
- `OnServerSpawn`: server `.cfg` execution can start from here onward, though configs may not all have finished yet.
- `OnGameDeactivate` / `OnGamePreShutdown`: clean up map-scoped state and entity references before shutdown finishes.
- `OnGameShutdown`: **`GameRules` is already invalid before listeners run**, and server/globals wrappers are cleared right afterwards. Reacquire map-scoped objects on the next map instead of carrying them across.

The next map starts over at `OnServerInit`. Late-registered listeners do not receive earlier events. See the [Game Listener example](../examples/game-listener.md) for full code.

## Entities

Entity wrappers, validity checks, and schema field access differ from CounterStrikeSharp and SourceMod. Do not keep entity wrappers across deletion or map changes, and check [Game Entities](../features/game-entities.md) when porting property reads and writes.

## Game events

Event names and fields will look familiar, though ModSharp uses explicit listeners and `HookEvent` calls instead of attribute-based hooks. See [Game Events](../features/game-events.md) for registration, callbacks, and disposal rules.

## Shared APIs and dependencies

Shared interface assemblies go in `sharp/shared` and provider modules go in `sharp/modules`, with versions aligned to the host. Export interfaces in `PostInit()`, resolve dependencies in `OnAllModulesLoaded()`, and handle disconnect/reconnect when providers reload. See [Installing a Shared API](integrating-api.md) and [Reloading a Module](reload-module.md).

## Stripper

If you need to modify map entity lumps, install the separate [StripperSharp module](https://github.com/Kxnrl/StripperSharp) and follow its setup guide.

## Workshop addon

To mount one extra workshop addon alongside the current map, pass this **launch option** (not a console command):

```text
-dual_addon 1234567890
```

Replace `1234567890` with the addon's Workshop ID. It accepts a single item ID (not a comma-separated list or collection ID) and does not replace selecting the map itself. Changing launch options requires a server restart; once enabled, modules can also query or switch the active addon ID at runtime via `IModSharp.GetDualAddonId()` and `IModSharp.SetDualAddonId(ulong)`.
