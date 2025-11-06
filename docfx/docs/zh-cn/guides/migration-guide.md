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

- ``OnServerInit``: 可以安全的访问 ``sv``/``globals``
- ``OnGameInit``: 可以安全的访问 ``GameRules``
- ``OnGamePostInit``
- ``OnResourcePrecache``: 你只能在这里 ``PrecacheResource``
- ``OnSpawnServer``: 从这里开始你可以加载*.cfg*
- ``OnGameActivate``
- ``OnServerActivate``
- ...
- ``OnGameDeactivate``
- ``OnGamePreShutdown``
- ``OnGameShutdown``: ``sv``/``globals``/``GameRules`` 从此以后不可用

以上均包含在`IGameListner`中。

> [!TIP]
>
> ``OnGameShutdown`` 作为当前地图的结束事件  
> ``OnServerInit`` 作为地图启动时最开始的事件  
> 往复循环  
>
> 如果想了解如何使用，请查看 [Game Listener 示例](../examples/game-listener.md) 了解完整的实现方式。

---

## Entity

实体的保存和使用以及访问变量与CS#和SourceMod有很大的不同。

请查看 [游戏实体](../features/game-entities.md)

---

## Events

事件的创建和访问与CS#和SourceMod类似，
但游戏事件监听与CS#和SourceMod的监听方式不同，  
**ModSharp**中的游戏事件监听思路与SourceEngine相同。

请查看 [游戏事件](../features/game-events.md)

---

## Stripper

使用该模块即可：[跳转连接](https://github.com/Kxnrl/StripperSharp)

---

## 创意工坊订阅

添加启动项`-dual_addon {你的创意工坊订阅ID}`即可。

> [!NOTE]
>
> 1. 假设你的订阅ID是1234567890，那么在这里就是`-dual_addon 1234567890`  
> 2. 目前我们不支持多个创意工坊订阅ID，建议优化你的资源包。
