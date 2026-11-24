# 发送网络消息

请参照 [Hello World 的项目设置](hello-world.md)。

SDK 已包含生成好的 protobuf 消息类型，不需要额外的生成器。本例注册了 `.sendnetmsg` / `ms_sendnetmsg`，并通过 `RecipientFilter` 将 `CUserMessageSayText2` 分别发送给单个客户端、CT 玩家以及所有客户端。

这三次发送是相互独立的演示：同一个客户端如果同时属于多个过滤范围，就会收到多次示例消息。客户端指令回调会在 `Shutdown` 中移除。

[!code-csharp[net-message-send.cs](../../codes/net-message-send.cs)]
