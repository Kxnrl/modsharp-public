# 依赖注入

请参照 [Hello World 的项目设置](hello-world.md)。本例使用的 `Microsoft.Extensions.DependencyInjection` 由 SDK 的包依赖提供。

ModSharp 使用固定的六参数构造函数创建模块入口。你可以在构造函数中建立模块自有的 `ServiceCollection`，用来组织其他服务，但这不会改变入口构造函数的签名。

宿主传入的对象按示例用 `AddSingleton(instance)` 放进容器就行。`Shutdown` 时释放你自己建的 `ServiceProvider`，不要去释放 ModSharp 传入的 `ISharedSystem`、`ILoggerFactory` 和 `IConfiguration`（直接传已有实例给 `AddSingleton` 不会把释放责任交给容器）。

[!code-csharp[DependencyInjection.cs](../../codes/dependency-injection.cs)]

使用 Sharp 扩展时，必须在使用前显式调用 `LoadAllSharpExtensions`，并在释放 `ServiceProvider` 之前调用 `ShutdownAllSharpExtensions`；仅释放容器不会调用 `ISharpExtension.Shutdown`。详见[使用 Sharp 扩展](sharp-extensions.md)。
