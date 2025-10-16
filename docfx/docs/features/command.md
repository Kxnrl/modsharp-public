# 指令

我们通过如下API创建函数：
- `IConVarManager.CreateServerCommand`
- `IClientManager.InstallCommandCallback`

请注意，在注册指令后需要手动调用函数清理，否则在你重新加载模块的时候会出非预期的问题。

- `IConVarManager.CreateServerCommand` （注册） → `IConVarManager.ReleaseCommand` （释放
- `IClientManager.InstallCommandCallback` （注册） → `IClientManager.RemoveCommandCallback`（释放）

请通过该示例了解如何创建指令：[跳转链接](../examples/command.md)
