# 模块共享 API

共享 API 需要三个项目，而不是在同一个程序集中放三个模块入口：

| 项目 / AssemblyName | 源文件 | 引用 |
|---|---|---|
| `SharedInterface.Shared` | `module-api.cs` | 无 |
| `SharedInterface` | `module-impl.cs` | `SharedInterface.Shared`、ModSharp SDK |
| `UseSharedModule` | `module-use-api.cs` | `SharedInterface.Shared`、ModSharp SDK |

提供方和调用方都按 [Hello World 项目设置](hello-world.md)配置；共享接口项目则是普通的 `net10.0` 类库。把**一份** `SharedInterface.Shared.dll` 放到 `{CS2}/game/sharp/shared/`，不要在模块目录里再拷副本。项目引用时加上 `Private="false" ExcludeAssets="runtime"`，NuGet 包引用时加上 `ExcludeAssets="runtime"`。

提供方的 `AssemblyName` 保持为 `SharedInterface`：生命周期回调传的是不带 `.dll` 的程序集名，不是类名 `MySharedModule`。

## 共享接口

接口标识是全局字符串键，建议带命名空间前缀防重名。

[!code-csharp[SharedInterface.Shared](../../codes/module-api.cs)]

## 提供方

在 `PostInit` 里注册接口（此时 ModSharp 已经把入口实例和所属模块绑好了）。模块卸载时框架会自动注销。

[!code-csharp[SharedInterface](../../codes/module-impl.cs)]

## 调用方

在 `OnAllModulesLoaded` 里获取已加载的提供方（调用方自身热重载时也会走这里），在 `OnLibraryConnected` 里接住后加载的提供方，在 `OnLibraryDisconnect` 里清掉引用。缓存 `IModSharpModuleInterface<T>` 包装对象本身，不要直接缓存 `.Instance`：提供方卸载后 `.Instance` 会变 `null`，每次调用时再读 `.Instance`。

[!code-csharp[UseSharedModule](../../codes/module-use-api.cs)]

只放接口库不会加载提供方，提供方模块需要单独部署到 `sharp/modules`。如果只是想在模块内部复用代码，而不是跨模块共享运行时服务，直接写成[扩展](writing-sharp-extensions.md)就行。
