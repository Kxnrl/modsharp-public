# Migrating from CSSharp/Metamod

If you are a CS#/Metamod user and want to migrate to ModSharp, there are some things you need to know in advance.

## OnMapStart/OnMapEnd

In a nutshell:

- `OnLevelInit` → ``OnGameInit``
- `OnMapInit` → ``OnGamePostInit``
- `OnMapStart` → ``OnGameActivate``
- `OnConfigsExecuted` → ``OnServerActivate``
- `OnMapEnd` → ``OnGameDeactivate``

Execution order:

- ``OnServerInit``: You can safely access ``sv``/``globals``
- ``OnGameInit``: You can safely access ``GameRules``
- ``OnGamePostInit``
- ``OnResourcePrecache``: You can only ``PrecacheResource`` here
- ``OnSpawnServer``: From here you can load *.cfg*
- ``OnGameActivate``
- ``OnServerActivate``
- ...
- ``OnGameDeactivate``
- ``OnGamePreShutdown``
- ``OnGameShutdown``: ``sv``/``globals``/``GameRules`` are no longer available from this point on

All of the above are included in `IGameListner`.

> [!TIP]
>
> ``OnGameShutdown`` serves as the end event for the current map  
> ``OnServerInit`` serves as the first event when the map starts  
> This cycles repeatedly  
>
> If you want to learn how to use it, please check the [Game Listener Example](../examples/game-listener.md) for a complete implementation.

---

## Entity

Entity storage, usage, and variable access are very different from CS# and SourceMod.

Please see [Game Entities](../features/game-entities.md)

---

## Events

Event creation and access are similar to CS# and SourceMod,
but game event listening differs from the CS# and SourceMod listening methods.  
In **ModSharp**, the game event listening approach is the same as SourceEngine.

Please see [Game Events](../features/game-events.md)

---

## Stripper

Use this module: [Jump Link](https://github.com/Kxnrl/StripperSharp)

---

## Workshop Addon

Add launch option `-dual_addon {Your workshop subscription ID}`.

> [!NOTE]
> 1. Assume your subscription ID is 1234567890，then here it is`-dual_addon 1234567890`
> 2. Currently we don't support multiple workshop subscription IDs. Optimize your resources.