# 游戏事件监听器

请参照 [Hello World 的项目设置](hello-world.md)。

先调 `InstallEventListener` 安装监听器，再为每个想收的事件名调用 `HookEvent`（默认不会挂钩任何事件）。实现 `IEventListener.FireGameEvent` 就能只读监听；`HookFireEvent` 可选，默认返回 `true`。只要有任意一个监听器返回 `false` 就会拦掉事件，后面的监听器返回 `true` 也改不回来。把 `serverOnly` 设为 `true` 可以屏蔽发给客户端的广播，同时保留服务端处理。

这些事件归引擎所有，只在回调期间有效，别缓存、重发或释放。模块 `Shutdown` 时记得移除监听器。如需按事件名注册回调或克隆事件，参见 [Game Event Manager](game-event-manager.md)。

[!code-csharp[event-listener.cs](../../codes/event-listener.cs)]
