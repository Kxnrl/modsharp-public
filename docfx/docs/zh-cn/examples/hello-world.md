# Hello World

创建一个名为 `HelloWorld` 的 .NET 10 类库，并将以下内容保存为 `HelloWorld.csproj`。SDK 必须支持 C# 13；ModSharp 模块是 x64 类库，而不是独立的可执行程序。

每个教程模块都应建立独立的项目，不要把所有入口类合并到同一个 DLL，也不要同时加载全部演示。需要组合使用多个文件的情形（共享 API 和扩展编写页面），文中会明确说明。

[!code-xml[HelloWorld.csproj](../../codes/hello-world.xml)]

本例将包版本固定为 `2.1.227`，即与本文档源码基线一致的发布版本。请使其与服务器所用的版本匹配，不要使用浮动版本 `*`。

- `AssemblyName` 应保持稳定。本例生成 `HelloWorld.dll` 和 `HelloWorld.deps.json`。
- `EnableDynamicLoading` 会把模块自身的依赖复制到输出目录。
- `ExcludeAssets="runtime"` 用于排除由宿主提供的 SDK 运行时资产，避免将其复制到输出目录。`PrivateAssets="all"` 只能阻止包引用向下游传递，**不能**单独阻止复制。参见 [.NET 插件依赖说明](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support#simple-plugin-with-no-dependencies)。
- 模块需要自行部署的普通第三方库，不要设置 `ExcludeAssets="runtime"`。

将以下模块保存为 `HelloWorld.cs`。请保持其公开构造函数的六参数签名不变：这六个参数由 ModSharp 传入，入口实例由 ModSharp 创建，而不是由模块自己的 DI 容器创建。

[!code-csharp[HelloWorld.cs](../../codes/hello-world.cs)]

使用 `dotnet publish HelloWorld.csproj -c Release --self-contained false -o publish` 发布，然后将 `publish` 目录的**内容**复制到 `{CS2}/game/sharp/modules/HelloWorld/`，其中包括 `HelloWorld.deps.json` 和模块自身的依赖。不要复制 `Sharp.Shared.dll` 等由宿主提供的共享程序集。模块目录中只能保留入口模块自己的 `.deps.json`，加载器会根据它来确定入口 DLL。

`Init` 负责注册回调并返回是否加载成功；`PostInit` 是最早可以安全导出共享接口的时机。`OnAllModulesLoaded` 会在首次加载完成后调用，**重载的模块也会收到该回调**，因此使用方可以在这里解析已存在的提供方；`OnLibraryConnected` 则用来处理之后才加载的提供方。请在 `Shutdown` 中释放监听器、hook、指令、订阅以及模块自有的 DI 容器；`Init` 部分失败后也可能调用 `Shutdown`。
