# 从CSSharp/Metamod迁移

如果你是CS#/Metamod用户，想迁移到ModSharp，有一些事情你需要提前了解。

## OnMapStart/OnMapEnd

一句话概括:

- `OnLevelInit` → ``OnGameInit``
- `OnMapInit` → ``OnGamePostInit``
- `OnMapStart` → ``OnGameActivate``
- `OnConfigsExecuted` → ``OnServerActivate``
- `OnMapEnd` → ``OnGameDeactivate``

执行顺序:

- ``OnServerInit``: safe to get sv/globals
- ``OnGameInit``: safe to get GameRules
- ``OnGamePostInit``
- ``OnResourcePrecache``: safe to precache game resources
- ``OnSpawnServer``: safe to execute .cfg
- ``OnGameActivate``
- ``OnServerActivate``
- ...
- ``OnGameDeactivate``
- ``OnGamePreShutdown``
- ``OnGameShutdown``: sv/globals/GameRules is null here

以上均包含在`IGameListner`中。

> [!TIP]
> 如果想了解如何使用，请查看 [Game Listener 示例](../examples/game-listener.md) 了解完整的实现方式。
