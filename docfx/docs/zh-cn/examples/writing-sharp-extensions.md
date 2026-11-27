# 编写 ModSharp 扩展

扩展是在每个调用方自己的 DI 容器里实例化的类库，不是全局模块。如果多个模块要共享同一个运行时实例，请用[模块共享 API](module-api.md)。

把下面**三个文件一起**编译成一个 `net10.0`、C# 13 类库，参照 [Hello World](hello-world.md) 引入与服务器版本一致的 `ModSharp.Sharp.Shared`，并同时加上 `PrivateAssets="all" ExcludeAssets="runtime"`（避免把宿主 SDK 复制到输出目录或向下游调用方传递；Microsoft DI API 由它提供）。普通的 `this IServiceCollection` 扩展方法不需要 C# 14 特性。

## 接口

[!code-csharp[SharpExtensionApi.cs](../../codes/sharp-extension-interface.cs)]

## 实现

`Load` 和 `Shutdown` 定义扩展的生命周期。实际的扩展应在 `Load` 中安装监听器和 hook，并在 `Shutdown` 中释放这些确切的注册项；这个最小示例则只记录其生命周期和 API 调用。

[!code-csharp[SharpExtension.cs](../../codes/sharp-extension-example.cs)]

## DI 注册

`ISharpExtension` 和公开 API 必须解析为**同一个**单例。如果分别注册不同的实现，就会出现加载的是一个对象、调用的却是另一个对象的问题。一个重载接收 `ISharedSystem`；无参重载则要求此前已经注册过它。

[!code-csharp[SharpExtensionDi.cs](../../codes/sharp-extension-di.cs)]

## 使用

沿用[使用 Sharp 扩展](sharp-extensions.md)中的模块写法：引用你自己的扩展类库，把示例中的注册代码替换为 `services.AddExampleSharpExtension(sharedSystem)`，调用 `LoadAllSharpExtensions`，然后通过 `_provider.GetRequiredService<IExampleSharpExtension>().CallMe()` 使用它。卸载时，先调用 `ShutdownAllSharpExtensions`，再调用 `_provider.Dispose()`。添加 `using ExampleSharpExtension;` 即可引入其契约和 DI 方法。请将扩展 DLL 及其私有依赖部署到使用方旁边，不要把它当作另一个模块入口。
