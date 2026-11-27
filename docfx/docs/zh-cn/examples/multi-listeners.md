# 在一个模块中实现多个监听器

请参照 [Hello World 的项目设置](hello-world.md)。

一个模块可以同时实现多个监听器接口。请分别显式实现每个接口的 `ListenerVersion` 和 `ListenerPriority`，这样每个监听器都可以拥有各自独立的版本和优先级。

将模块分别注册到对应的管理器，并在 `Shutdown` 中逐一移除所有注册项。`IEventListener.FireGameEvent` 必须实现，本例用它记录事件名称；其他用不到的监听器回调可以沿用接口的默认实现。

[!code-csharp[multi-listeners.cs](../../codes/multi-listeners.cs)]
