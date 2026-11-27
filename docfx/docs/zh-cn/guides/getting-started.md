# 入门

本文介绍怎么在已有的 CS2 专用服务器上部署 ModSharp。下文里的 `{CS2}` 指服务端安装根目录（也就是包含 `game` 的那一层）。

## 下载服务端包

去 [Releases](https://github.com/Kxnrl/modsharp-public/releases) 下载对应系统的服务端包：

- Windows x64：`ModSharp-git<build>-windows.zip`
- Linux x64：`ModSharp-git<build>-linux.zip`

`*-extensions.zip` 是开发扩展包，不是完整的服务端安装文件。部署时请确保原生引擎、托管核心、模块、共享程序集、配置和 gamedata 都来自同一个版本。

## 安装运行时

ModSharp 的托管核心和模块基于 **.NET 10（`net10.0`）x64**，架构**必须是 x64**（x86 和 Arm64 无法使用）。

服务端运行只需要安装 **.NET 10 Runtime（x64）**，不必为了开服单独去装 SDK 或 ASP.NET Core Runtime；如果机器上已经装过 x64 版的 **.NET 10 SDK**（自带对应的 .NET Runtime），也可以直接使用。平时在开发机[编写模块](writing-a-module.md)时，则需要安装 **.NET 10 SDK**。

# [Windows](#tab/windows)

先安装 [Visual C++ x64 Redistributable](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)，然后任选以下两种方式之一安装 .NET 10：

- **系统安装**：安装 [.NET 10 x64 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)（宿主会在 `C:\Program Files\dotnet\host\fxr` 里查找）。
- **独立运行时**：下载 Windows x64 版 .NET 10 Runtime 压缩包，直接解压到 `{CS2}/game/sharp/runtime`。

# [Linux](#tab/linux)

请确保服务端跑在兼容 CS2 的 **Steam Runtime 3（sniper）** 环境里（包括 Docker 容器）。.NET 运行时和它的原生依赖必须能在启动 CS2 的实际环境里访问到，光装在宿主机上可能进不了容器。

最省事的做法是从 [.NET 下载页](https://dotnet.microsoft.com/download/dotnet/10.0)下 **Linux x64 .NET 10 Runtime** 压缩包，直接解压到 `{CS2}/game/sharp/runtime`（把下面的路径和文件名换成你的实际值）：

```bash
CS2=/srv/cs2
mkdir -p "$CS2/game/sharp/runtime"
tar -xzf ./dotnet-runtime-10.0.<patch>-linux-x64.tar.gz -C "$CS2/game/sharp/runtime"
```

宿主也会搜 `/usr/share/dotnet/host/fxr` 和 `/usr/lib/dotnet/host/fxr`，前提是服务端运行环境或容器里能看到这些目录和依赖；如果 Steam RT3 看不到宿主机的 .NET，直接用上面的独立运行时最稳。

用面板开服的话可以参考 [1zc/CS2-Pterodactyl](https://github.com/1zc/CS2-Pterodactyl)。

---

独立运行时的目录结构必须长这样：

```text
{CS2}/game/sharp/runtime/
├── dotnet                         # Windows 下为 dotnet.exe
├── host/fxr/10.0.<patch>/
│   └── libhostfxr.so               # Windows 下为 hostfxr.dll
└── shared/Microsoft.NETCore.App/10.0.<patch>/
    └── ...
```

> [!IMPORTANT]
> 别在 `runtime` 和 `host` 之间多套一层 `dotnet` 目录。宿主不会读 `PATH` 或 `DOTNET_ROOT` 来找 `hostfxr`，而是在独立目录和系统目录里挑主版本 >= 10 的最高版本。另外 `sharp/core/Sharp.Core.runtimeconfig.json` 明确要求 .NET 10：如果系统目录里可见更高主版本（如 .NET 11），宿主会选中系统目录的 `hostfxr` 并只在该系统目录里找 .NET 10，此时光在 `sharp/runtime` 放 .NET 10 无法覆盖，需要也在同一系统目录安装 .NET 10。

## 解压 ModSharp

先停服，再把服务端包解压到 `{CS2}/game`。压缩包顶层自带 `sharp` 目录，别解压成 `game/sharp/sharp`：

```text
{CS2}/game/
├── csgo/
│   └── gameinfo.gi
└── sharp/
    ├── bin/
    │   ├── modsharp.dll                   # Windows 原生引擎
    │   ├── win64/server.dll               # Windows 加载器
    │   ├── libmodsharp.so                 # Linux 原生引擎
    │   └── linuxsteamrt64/libserver.so     # Linux 加载器
    ├── core/
    │   ├── Sharp.Core.dll
    │   ├── Sharp.Core.deps.json
    │   ├── Sharp.Core.runtimeconfig.json
    │   ├── Sharp.Shared.dll
    │   └── ...                            # 保留所有配套依赖
    ├── configs/
    │   └── core.json
    ├── gamedata/
    ├── locales/
    ├── modules/
    ├── shared/
    └── runtime/                           # 自行解压的独立运行时（如有）
```

每个平台的安装包只带对应系统的原生文件，也不含 `runtime`，包里其他目录和依赖都原样保留即可。`sharp/bin/<平台>` 下的加载器会自动找 `sharp/bin` 里的原生引擎，别往 `csgo/bin` 里挪。

## 修改 `gameinfo.gi`

打开 `{CS2}/game/csgo/gameinfo.gi`，在 `FileSystem` -> `SearchPaths` 里把 `Game sharp` 加到 `Game csgo` **前面**：

```diff
 SearchPaths
 {
     Game_LowViolence    csgo_lv
+    Game                sharp
     Game                csgo
```

其他行保持不动。原理和 Metamod 一样，都是靠搜索路径抢先加载。CS2 更新有时会覆盖 `gameinfo.gi`，更新完记得回来看一眼。

## 启动与检查

正常启动服务端并加载一张地图。日志里看到 `Sharp.Core initialization completed` 说明核心跑起来了，不过也顺手翻一下它前面有没有加载器、运行时、gamedata 或模块报错。

在**服务端控制台**里敲下面几条命令检查状态：

```text
ms
ms version
ms modules
```

`modsharp` 是 `ms` 的别名。`ms version` 会打印框架构建号、当前使用的 hostfxr 版本（`DotNet Version`）和提交哈希；`ms modules` 会列出各模块的名称、版本、作者和状态（正常运行的模块显示为 `Running`）。

> [!NOTE]
> 启动时必须至少有一个模块成功初始化。首次安装先留着自带模块，如果把所有模块都删了、禁用了或者全加载失败，托管核心会直接启动失败。发行包里 `AdminCommands.SQLStorage` 默认是禁用的，用到数据库存储时再配好启用。

接下来可以看：[编写模块](writing-a-module.md)、[安装共享 API](integrating-api.md) 或 [重新加载模块](reload-module.md)。
