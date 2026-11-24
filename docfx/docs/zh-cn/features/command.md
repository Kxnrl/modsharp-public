# 指令

按你希望谁能触发这条指令来选对应的注册 API。下面这些底层接口都不做管理员鉴权，需要权限校验请用第一方的 CommandCenter / AdminManager。

## 引擎控制台指令

| 注册方法 | 可触发来源 | 移除方法 |
| --- | --- | --- |
| `IConVarManager.CreateServerCommand` | 服务端控制台及其他非客户端来源 | `ReleaseServerCommandCallback(name, callback)` |
| `IConVarManager.CreateConsoleCommand` | 服务端控制台和客户端控制台 | `ReleaseConsoleCommandCallback(name, callback)` |

用 `CreateConsoleCommand` 时，如果是从服务端控制台触发的，回调里的 `client` 为 `null`。这两个回调虽然声明了 `ECommandAction` 返回值，但**返回值本身不影响指令执行**。

同名注册项会共享同一个引擎指令：flags 取并集，帮助文本用最先注册的那份，等所有服务端和控制台回调都移除后引擎指令才会被释放。

> [!IMPORTANT]
> `ReleaseCommand` 已经废弃并且是**空实现**。模块 `Shutdown` 时必须用上表里对应的方法按回调委托精确移除，如果注册了 lambda，要自己把委托存成字段以便卸载时传回去。

详情见 [IConVarManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ConVarManager.cs)。

## 客户端虚拟指令

调用 `IClientManager.InstallCommandCallback("example", callback)` 时传**不带前缀**的名字，客户端可以通过两种方式触发：

- 客户端控制台：`ms_example ...`
- 聊天框：`!example ...`、`/example ...`，或其他配置好的[聊天前缀](../configurations/core.md#chatcommandtriggerprefixes)

这种指令只给客户端用（服务端控制台不可用），卸载时调 `RemoveCommandCallback("example", callback)`。

从控制台触发时，只有返回 `Stopped`（或回调抛异常）才会拦住引擎继续执行 `ExecuteStringCommand`；返回 `Handled` 或 `Skipped` 会放行给引擎按原流程处理（通常会报未知指令）。从聊天框触发时，返回值不起作用，消息公不公开取决于公开/静默前缀以及聊天监听器；聊天指令的回调会排进队列，延迟到下一帧在游戏主线程执行，不会在聊天监听器里直接内联调用。

## 客户端指令监听器

调用 `IClientManager.InstallCommandListener(name, callback)` 可以监听或拦截客户端发给服务端的已有指令，卸载时用 `RemoveCommandListener(name, callback)`。它不会注册虚拟的 `ms_` 指令或聊天触发器，也看不到只在客户端本地消耗掉的指令。

返回 `Handled` 只能拦住走游戏 DLL `ClientCommand` 路径的指令，对游戏 DLL 注册的 ConCommand（`FCVAR_GAMEDLL`）不一定生效，真要拦住请返回 `Stopped`。回调里拿到的是解析好的 `StringCommand`，不能原地改命令字符串。

各回调的返回值细节见 [IClientManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ClientManager.cs)，注意 `ECommandAction.Handled` 在不同指令 API 里的行为略有差别。

## 清理与示例

模块 `Shutdown` 时要把自己注册过的回调一一移除干净。虽然核心在卸载模块时会兜底清理漏掉的回调，但不要靠兜底代替正常清理。

- [底层指令示例](../examples/command.md)
- [CommandManager 扩展示例](../examples/command-manager.md)
- [AdminManager 集成](../examples/admin-example.md)
