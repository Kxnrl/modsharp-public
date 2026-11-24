# 指令

请参照 [Hello World 的项目设置](hello-world.md)。

`CreateServerCommand` 会按传入的名称注册服务端控制台指令（本例为 `ms_echo`）。`InstallCommandCallback("hello", ...)` 会注册客户端控制台指令 `ms_hello`，以及 `.hello`、`/hello`、`!hello` 三种聊天触发形式，实际可用的前缀取决于服务器的触发器配置。

`StringCommand.ArgCount` 只统计参数个数，不含指令名本身；`GetArg(1)` 是第一个参数，按索引读取前请先检查 `ArgCount`。处理完指令后返回 `Stopped`。在 `Shutdown` 中，请用 `ReleaseServerCommandCallback` 释放服务端回调，并移除客户端回调；移除时使用的名称和委托必须与注册时一致。

[!code-csharp[command.cs](../../codes/command.cs)]
