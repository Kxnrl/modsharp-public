# 安装共享 API

共享 API 程序集放的是多个模块共用的接口和类型定义，具体实现写在单独的提供方模块里。部署时，提供方模块放 `sharp/modules`，共享接口程序集放 `sharp/shared`。编译调用方时引用的 NuGet 包不会自动安装到服务器上，服务端两边都要部署好。

## 服务端目录与加载规则

以 `SharedInterface.Shared` 为例，服务端结构长这样：

```text
{CS2}/game/sharp/
├── shared/
│   └── SharedInterface.Shared/
│       ├── SharedInterface.Shared.dll
│       └── ...                         # 接口库自己的依赖（如有）
└── modules/
    └── SharedInterface/
        ├── SharedInterface.dll
        ├── SharedInterface.deps.json
        └── ...
```

共享程序集会在首次扫描模块**之前**加载，加载器只看 `sharp/shared` 下的直接子目录：

- 正常情况下，目录 `Name` 里必须直接放着 `Name.dll`，散放在 `sharp/shared` 根目录下的 DLL 不会被扫描。
- 如果目录名本身就以 `.dll` 结尾，加载器会直接用这个名字，不再补后缀（日常还是建议按上面这种普通目录名放）。
- 目录名以 `.disabled` 结尾，或者里面有 `.disabled` 文件，启动时会跳过它；注意禁用接口库不会自动禁用依赖它的模块。
- 跟模块不同，共享程序集不依赖 `*.deps.json` 找入口；不过如果接口库本身有第三方依赖，还是要发布整套输出，别只拷一个主 DLL。
- 共享程序集加载在不可卸载的上下文中，且不支持热重载。共享类型按程序集名解析，不要装多个冲突副本。

> [!IMPORTANT]
> 共享接口在服务器整个进程周期里只加载一次。新增、修改或禁用后必须**重启服务器**，模块热重载不会重读 `sharp/shared`。缺了或不匹配会导致依赖它的模块加载失败。框架自带的 `Sharp.Shared.dll` 已经在 `sharp/core` 里，千万别往这里放。

## 编译并部署一个最小接口库

装好 .NET 10 SDK 后执行：

```shell
dotnet new classlib --name SharedInterface.Shared --framework net10.0
```

把生成的 `Class1.cs` 替换为：

```csharp
namespace SharedInterface.Shared;

public interface IMySharedModule
{
    string GetMessage();
}
```

这个接口不涉及框架类型，所以不用引 ModSharp 包。把 `.csproj` 改成：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
    <AssemblyName>SharedInterface.Shared</AssemblyName>
  </PropertyGroup>
</Project>
```

按服务端系统发布（Windows 服务端把 `--runtime` 换成 `win-x64`）：

```shell
dotnet publish SharedInterface.Shared/SharedInterface.Shared.csproj --configuration Release --framework net10.0 --runtime linux-x64 --no-self-contained --output ./publish/SharedInterface.Shared
```

停服后复制过去：

# [Windows](#tab/windows)

```powershell
$CS2 = 'C:\CS2'
New-Item -ItemType Directory -Force "$CS2/game/sharp/shared/SharedInterface.Shared" | Out-Null
Copy-Item ./publish/SharedInterface.Shared/* "$CS2/game/sharp/shared/SharedInterface.Shared/" -Recurse -Force
```

# [Linux](#tab/linux)

```bash
CS2=/srv/cs2
mkdir -p "$CS2/game/sharp/shared/SharedInterface.Shared"
cp -a ./publish/SharedInterface.Shared/. "$CS2/game/sharp/shared/SharedInterface.Shared/"
```

---

如果接口定义里用到了 ModSharp 的类型，按[编写模块](writing-a-module.md)的方式引用对应版本的 `ModSharp.Sharp.Shared`，记得加上 `ExcludeAssets="runtime"`，别把框架文件一起打进接口目录。

## 在模块里引用它

如果接口项目和模块项目在同一层目录，在提供方和调用方的 `.csproj` 里都加上：

```xml
<ItemGroup>
  <ProjectReference Include="../SharedInterface.Shared/SharedInterface.Shared.csproj"
                    Private="false" ExcludeAssets="runtime" />
</ItemGroup>
```

`Private="false"` 防止复制接口 DLL，`ExcludeAssets="runtime"` 把它带出来的运行时资产一并排掉。如果是用 NuGet 包引用接口库，同样给 `PackageReference` 加上 `ExcludeAssets="runtime"` 并锁死到服务端部署的版本（只写 `PrivateAssets="all"` 挡不住发布时复制 DLL）。

代码层面：提供方在 `PostInit()` 里调 `ISharedSystem.GetSharpModuleManager().RegisterSharpModuleInterface(...)` 注册实现；调用方在 `OnAllModulesLoaded()` 里用 `GetRequiredSharpModuleInterface<T>` 或 `GetOptionalSharpModuleInterface<T>` 拿到 `IModSharpModuleInterface<T>` 包装对象。平时保留这个包装对象、用前检查 `Instance`，不要缓存底层的原始实现实例（不然提供方重载后会挂着旧引用）。提供方卸载时，旧包装对象的 `Instance` 会被永久置空，不会自动重绑，因此要在 `OnLibraryDisconnect` 里清掉旧引用，并在 `OnLibraryConnected` 里重新调用 `Get...SharpModuleInterface<T>` 获取新的包装对象。注册接口名尽量带命名空间，别用容易撞车的短名字。

接口程序集本身不需要 `IModSharpModule`，实现它的提供方模块才需要。完整代码见[模块共享 API 示例](../examples/module-api.md)。
