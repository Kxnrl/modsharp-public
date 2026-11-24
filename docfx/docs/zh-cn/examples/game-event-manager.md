# Game Event Manager

请参照 [Hello World 的项目设置](hello-world.md)。

安装 `ModSharp.Sharp.Extensions.GameEventManager`，并将其运行时 DLL 随模块一起部署。使用前，先注册共享系统、添加该扩展，再调用 `LoadAllSharpExtensions`；卸载时先调用 `ShutdownAllSharpExtensions`，再释放容器。

本例按事件名称注册回调，阻止了改名事件，并克隆死亡事件以向客户端发送自定义内容，同时让原始事件仅在服务端处理。hook 和监听器收到的事件参数归引擎所有，不能释放，也不能重新发射。克隆出的事件归调用者所有，在调用 `FireToClient` 之后必须释放（异常路径也不例外），示例使用 `finally` 来保证这一点。

[!code-csharp[game-event-manager.cs](../../codes/game-event-manager.cs)]
