# 使用 Sharp 扩展

请参照 [Hello World 的项目设置](hello-world.md)。

本例需要安装 `ModSharp.Sharp.Extensions.EntityHookManager` 和 `ModSharp.Sharp.Extensions.GameEventManager`。它们是模块私有的类库依赖，而不是单独加载的模块入口。请保留它们的运行时资产，并随你的插件一起部署；只有宿主提供的 SDK 和 Shared 契约才应排除运行时资产。

扩展的注册方法要求已注册 `ISharedSystem`。请建立一个模块自有的 `ServiceProvider`，在使用各项服务之前调用 `LoadAllSharpExtensions`，并在 `Shutdown` 中先调用 `ShutdownAllSharpExtensions`，再释放容器。仅释放容器不会触发扩展的生命周期。如需编写自己的扩展，请参阅[编写 Sharp 扩展](writing-sharp-extensions.md)。

[!code-csharp[sharp-extensions.cs](../../codes/sharp-extensions.cs)]
