# 编写模块

ModSharp 模块是一个由服务器加载的 .NET 类库。下面用命令行从零建一个最小可运行模块，习惯用 Rider 或 Visual Studio 的话，只要装了 .NET 10 SDK 操作也完全一样。

## 创建项目并锁定 SDK 版本

在开发机装好 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 后运行：

```shell
dotnet new classlib --name Example --framework net10.0
```

把 `Example/Example.csproj` 改成下面这样：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
    <AssemblyName>Example</AssemblyName>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="[2.1.227]"
                      PrivateAssets="all" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

`2.1.227` 对应当前文档基线的已发布 SDK 版本。**请务必让 `ModSharp.Sharp.Shared` 版本与服务器上的 ModSharp 版本保持一致**，方括号用来把 NuGet 锁死在精确版本上（别写 `Version="*"`，拉到更新的 API 只会导致跟服上的核心对不上；源码里的默认 `2.1.1` 在发布流水线里会被构建号替换，也不是实际发布的包版本）。

几个关键配置项的作用：

- `net10.0`、C# 13、x64 与框架默认目标保持一致。
- `AssemblyName` 决定输出的 DLL 和 `.deps.json` 名字，显式写出来方便部署时对得上号。
- `EnableDynamicLoading` 让类库按插件方式发布依赖（部署时要把整个发布目录带过去，别只拷主 DLL）。
- `ExcludeAssets="runtime"` 避免把 `Sharp.Shared.dll` 及其依赖打进模块目录，这些程序集由宿主统一提供。
- `PrivateAssets="all"` 只控制依赖传递，**单独写它挡不住 `dotnet publish` 复制 DLL**，必须配合 `ExcludeAssets="runtime"` 一起用。详见 [NuGet 资产元数据](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#controlling-dependency-assets)和 [.NET 插件教程](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support#reference-a-plugin-interface-from-a-nuget-package)。

## 写模块入口

把生成的 `Class1.cs` 改名为 `Example.cs`，填上入口代码：

```csharp
using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;

namespace Example;

public sealed class Example : IModSharpModule
{
    public Example(ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration configuration,
        bool hotReload)
    {
    }

    public string DisplayName => "Example";
    public string DisplayAuthor => "YourName";

    public bool Init()
    {
        Console.WriteLine("Hello, World!");
        return true;
    }

    public void Shutdown()
    {
        Console.WriteLine("Byebye, World!");
    }
}
```

一个入口程序集里只放一个非抽象的 `IModSharpModule` 实现，且要有**公开**的六参数构造函数（类本身建议写 `public`，虽然 `internal` 也能扫到）。加载器只认找到的第一个实现，想做多个模块就拆到不同的模块目录里。

构造函数的六个参数按固定顺序传入：

| 参数 | 说明 |
| --- | --- |
| `ISharedSystem sharedSystem` | 框架总入口，用来拿 `GetModSharp()`、`GetSharpModuleManager()` 等服务。 |
| `string dllPath` | 当前模块所在的**目录路径**（不是 DLL 文件路径）。 |
| `string sharpPath` | 服务器的 `game/sharp` 根目录。 |
| `Version version` | 当前模块程序集的版本号。 |
| `IConfiguration configuration` | 从 `sharp/configs/core.json` 读进来的核心配置。 |
| `bool hotReload` | 首次启动为 `false`，换图热重载替换模块时为 `true`。 |

核心会监听 `configs/core.json` 的改动，但不会自动替各个模块重新加载配置；模块自己的配置文件请直接基于 `dllPath` 或 `sharpPath` 手动读写。

## 模块生命周期

首次加载时，模块按这个顺序启动：

1. 执行构造函数（如果构造函数抛异常，实例都没建出来，不会进 `Shutdown()`，但核心会清理掉已经注册的资源）。
2. 执行 `Init()`，返回 `true` 才算成功；返回 `false` 或抛异常时会判定加载失败，并立刻调该实例的 `Shutdown()` 清理。
3. 模块切到 `Running` 状态，接着执行 `PostInit()`。对外导出的共享接口和动态 native 要放在 `PostInit()` 里注册，别放在构造函数或 `Init()` 里。
4. 其他已经在跑的模块收到 `OnLibraryConnected(assemblyName)`。
5. 所有模块扫描加载完后，运行中的模块收到 `OnAllModulesLoaded()`。需要拿别的模块的接口时放在这里，别赌目录或加载顺序。

`PostInit()`、`OnLibraryConnected()`、`OnLibraryDisconnect()` 和 `OnAllModulesLoaded()` 都有默认空实现，按需覆写；`Init()`、`Shutdown()`、`DisplayName`、`DisplayAuthor` 是必填项。

`OnLibraryConnected` 不会补发之前已经加载过的模块，它的参数是被加载模块的程序集名（不含 `.dll`）。如果依赖的模块支持热重载，调用方还要处理 `OnLibraryDisconnect` 和随后的重新连接，参见[模块共享 API 示例](../examples/module-api.md)。

模块卸载时，核心会先把该模块的加载上下文标成已卸载（清理它的计时器和 `cs_script` 委托，并拦截 `Shutdown()` 里新注册的监听器、钩子或计时器），再发 `OnLibraryDisconnect` 通知其他模块、移除它导出的接口和 native，最后调用它的 `Shutdown()`。你在 `Shutdown()` 里把自己的监听器、钩子、计时器和外部资源清干净就行。另外注意地图生命周期和模块生命周期是分开的，模块 `Init()` 跑完时地图和 `GameRules` 未必就绪，详见[迁移指南](migration-guide.md)。

## 发布与部署

在 `Example` 项目的上一级目录按**服务器操作系统**运行对应命令：

```shell
# Windows x64 服务端
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime win-x64 --no-self-contained --output ./publish/Example
```

```shell
# Linux x64 服务端
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime linux-x64 --no-self-contained --output ./publish/Example
```

必须用框架依赖发布（`--no-self-contained`），别用自包含、单文件、裁剪或 Native AOT。发布目录里自带的模块专属依赖和资源子目录都要一起拷上去。

首次部署先停服，把发布目录整份拷到 `game/sharp/modules/Example/`：

# [Windows](#tab/windows)

```powershell
$CS2 = 'C:\CS2'
New-Item -ItemType Directory -Force "$CS2/game/sharp/modules/Example" | Out-Null
Copy-Item ./publish/Example/* "$CS2/game/sharp/modules/Example/" -Recurse -Force
```

# [Linux](#tab/linux)

```bash
CS2=/srv/cs2
mkdir -p "$CS2/game/sharp/modules/Example"
cp -a ./publish/Example/. "$CS2/game/sharp/modules/Example/"
```

---

放好后的目录结构至少长这样：

```text
{CS2}/game/sharp/modules/Example/
├── Example.dll
├── Example.deps.json
└── ...                    # 模块自己的依赖或资源（如有）
```

加载器会扫描 `sharp/modules` 下的直接子目录，找每个目录顶层的第一个 `*.deps.json`，然后加载同名 DLL。所以每个目录只放一个模块的发布产物，别把多个模块混在一个目录里。目录名不强制和 `AssemblyName` 相同（有几个内置模块就不一样），不过保持同名看着最清楚；Linux 下注意文件名大小写原样保留。

启动时如果目录名以 `.disabled` 结尾，或者里面放了个 `.disabled` 文件，加载器就会跳过它（运行中加 `.disabled` 不会立刻卸载模块）。

启动服务端，先在启动日志里确认打印出了 `Hello, World!`，再在服务端控制台运行 `ms modules`，确认模块状态为 `Running`。以后更新模块请走[模块重载流程](reload-module.md)，不要直接覆盖正在跑的文件。
