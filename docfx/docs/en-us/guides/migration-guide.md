# Migrating from CSSharp/Metamod

If you are a CS#/Metamod user and want to migrate to ModSharp, there are some things you need to know in advance.

## OnMapStart/OnMapEnd

In a nutshell:

- `OnLevelInit` → `OnGameInit`
- `OnMapInit` → `OnGamePostInit`
- `OnMapStart` → `OnGameActivate`
- `OnConfigsExecuted` → `OnServerActivate`
- `OnMapEnd` → `OnGameDeactivate`

Execution order:

- `OnServerInit`: safe to get sv/globals
- `OnGameInit`: safe to get GameRules
- `OnGamePostInit`
- `OnResourcePrecache`: safe to precache game resources
- `OnSpawnServer`: safe to execute .cfg
- `OnGameActivate`
- `OnServerActivate`
- ...
- `OnGameDeactivate`
- `OnGamePreShutdown`
- `OnGameShutdown`: sv/globals/GameRules is null here

All of the above are included in `IGameListener`.

> [!TIP]
> If you want to learn how to use it, please check the [Game Listener Example](../examples/game-listener.md) for a complete implementation.

## Entity

Entity storage, usage, and variable access are very different from CS# and SourceMod.

Please see [Game Entities](../features/game-entities.md)

## Events

Event creation and access are similar to CS# and SourceMod,
but game event listening is different from the CS# and SourceMod approaches.  
In **ModSharp**, the game event listening approach is the same as SourceEngine.

Please see [Game Events](../features/game-events.md)

## Stripper

Use this module: [Jump Link](https://github.com/Kxnrl/StripperSharp)
