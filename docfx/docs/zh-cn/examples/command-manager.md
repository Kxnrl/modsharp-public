# Command Manager

除 [Hello World 的 SDK 引用](hello-world.md)外，还需要安装 `ModSharp.Sharp.Extensions.CommandManager`。扩展是模块私有的类库依赖，应保留其运行时资产，并随模块一起部署。

| | 扩展（`Sharp.Extensions.CommandManager`） | 模块（`Sharp.Modules.CommandCenter`） |
|---|---|---|
| 接入方式 | `services.AddCommandManager(sharedSystem)` | 通过模块管理器解析 `ICommandCenter` |
| 作用域 | 单个模块自己的 DI 容器 | 共享的提供方，registry 按模块标识（identity）分别持有 |
| 生命周期 | 先调用 `LoadAllSharpExtensions`，之后调用 `ShutdownAllSharpExtensions` 并释放容器 | 遵循 ModSharp 的模块生命周期 |
| 受限指令 | 使用 [AdminManager](admin-example.md) | 使用 `IAdminManager.GetCommandRegistry` |

扩展中旧的 `RegisterAdminCommand` 已弃用，计划在 2.2 中移除，因此下方示例刻意不使用它。需要权限控制时，请使用 AdminManager，而不是旧的 SDK 管理员标志系统。

本例注册了客户端指令 `.kill` / `ms_kill` 和服务端指令 `ms_echo`。这些回调由 `ShutdownAllSharpExtensions` 注销，只释放容器并不会注销它们。

[!code-csharp[CommandManager.cs](../../codes/command-manager.cs)]
