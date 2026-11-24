# 游戏生命周期监听器

请参照 [Hello World 的项目设置](hello-world.md)。

通过 `IModSharp.InstallGameListener` 注册 `IGameListener`，并在 `Shutdown` 中移除。`OnServerInit` 中可以使用 globals，`OnGameInit` 中可以使用 GameRules，`OnServerSpawn` 适合执行服务器配置。`OnGameShutdown` 中不能调用 `GetGameRules`。

预缓存资源只能在 `OnResourcePrecache` 里做（本例用的是游戏自带的拆弹器模型）。如果模块加载时当前地图的预缓存阶段已经过了，就要等到下次换图再预缓存。`ConsoleSay` 会同时拦截服务端控制台 `say` 和脚本广播 `ScriptPrintMessageChatAll`，只有返回 `Stopped` 才会拦住原流程。

[!code-csharp[game-listener.cs](../../codes/game-listener.cs)]
