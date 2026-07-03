# SendProxyManager

本教程将会演示如何使用 `ISendProxyManager` 在网络数据发送时**针对每个客户端**覆盖某个网络字段(send-prop)的值,而不改变服务器上的真实值。

使用 `Hook` 为实体上的某个字段注册回调;该回调在主线程上、在该字段写给每个接收者时各触发一次,因此可以让不同客户端看到不同的值(返回 `false` 则发送真实值)。native 会预先把值的 `Kind` 设为该字段编码器所需的类型;设置对应类型的值并返回 `true` 即可覆盖。支持的类型:int、float、bool、qangle 和 string(例如给每个观察者不同的玩家名字)。下面的示例演示了每种类型。

[!code-csharp[SendProxyExample.cs](../../codes/send-proxy.cs)]
