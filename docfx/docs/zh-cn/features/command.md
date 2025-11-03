# 指令

## 服务器指令

注册与释放:  

- `IConVarManager.CreateServerCommand`: 创建仅服务器控制台可执行的指令
- `IConVarManager.ReleaseCommand`: 释放

> [!IMPORTANT]
> 当你的模块卸载时需要进行释放，否则可能会出现意料之外的错误。

---

## 服务器/客户端指令

注册与释放:  

- `IConVarManager.CreateConsoleCommand`: 创建服务器控制台/客户端控制台均可执行的指令
- `IConVarManager.ReleaseCommand`: 释放

> [!IMPORTANT]
> 当你的模块卸载时需要进行释放，否则可能会出现意料之外的错误。

---

## 客户端虚拟指令

虚拟指令同时支持:

- 客户端控制台
- 客户端聊天栏

注册与释放:  

- `IClientManager.InstallCommandCallback`: 创建自动包含``ms_``前缀的虚拟指令
- `IClientManager.RemoveCommandCallback`: 释放

> [!IMPORTANT]
> 当你的模块卸载时需要进行释放，否则可能会出现意料之外的错误。

---

## 监听客户端命令

与SourceMod类似，我们提供了客户端命令监听功能，
以便拦截和修改特定的客户端命令。

- `IClientManager.InstallCommandListener`: 安装命令监听器
- `IClientManager.RemoveCommandListener`: 移除命令监听器

> [!IMPORTANT]
> 当你的模块卸载时需要进行释放，否则可能会出现意料之外的错误。

---

## 示例

请通过该示例了解如何创建指令：[跳转链接](../examples/command.md)
