# Sending NetMessages

Use the [Hello World project settings](hello-world.md).

The SDK includes generated protobuf message types; a separate generator is not needed. This example registers `.sendnetmsg` / `ms_sendnetmsg` and sends `CUserMessageSayText2` to one client, CT players, and all clients using `RecipientFilter`.

These three sends are independent demonstrations: a client included in several filters receives the example message several times. The client command callback is removed in `Shutdown`.

[!code-csharp[net-message-send.cs](../../codes/net-message-send.cs)]
