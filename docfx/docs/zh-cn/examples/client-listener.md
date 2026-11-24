# Client Listener

请参照 [Hello World 的项目设置](hello-world.md)。

实现 `IClientListener`，在 `Init` 中用 `InstallClientListener` 注册同一个实例，并在 `Shutdown` 中用 `RemoveClientListener` 移除。版本号填写接口当前的 `ApiVersion`；没有排序需求时，优先级设为 0 即可。

示例演示了连接、管理员检查、断开和聊天等回调，有几个容易踩坑的细节：

- `OnClientPutInServer` 实际绑定的是引擎的 `CSource2GameClients::Active` 后置钩子（即 `OnClientActive`），不是 `PutInServer`。
- 真人玩家的 `OnClientConnected` 在 `CheckConnect` 放行后触发，机器人则在 `Connected` 后触发。
- `OnClientPreAdminCheck` 返回 `false` 放行，返回 `true` 会永久阻止本次连接的 `OnClientPostAdminCheck`；机器人直接跳过 `OnClientPreAdminCheck`，在帧末进入 `OnClientPostAdminCheck`。
- `OnClientSayCommand` 的 `commandName` 是 `"say"` 或 `"say_team"`，返回 `Handled` 或 `Stopped` 都会拦掉聊天广播（不会取消已入队的聊天指令回调）；长度在 2..255 且带静默前缀的消息无论返回什么都会被隐藏。

[!code-csharp[client-listener.cs](../../codes/client-listener.cs)]
