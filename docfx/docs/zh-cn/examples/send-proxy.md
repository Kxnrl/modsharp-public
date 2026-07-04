# SendProxyManager

本教程将会演示如何使用 `ISendProxyManager` 在网络数据发送时**针对每个客户端**覆盖某个网络字段(send-prop)的值,而不改变服务器上的真实值。

使用 `Hook` 为实体上的某个字段注册回调;该回调在主线程上、**每 tick 为该字段触发一次**(而不是每个客户端一次),通过 `batch.SetFor(client, value)` 填入每个客户端应看到的值 —— 未设置的客户端将收到真实值。设置的值需匹配 `batch.Kind`。支持的类型:int、float、bool、qangle 和 string(例如给每个观察者不同的玩家名字)。下面的示例演示了每种类型。

[!code-csharp[SendProxyExample.cs](../../codes/send-proxy.cs)]
