# 从 CounterStrikeSharp / Metamod 迁移

ModSharp 有独立的一套加载器、API 和生命周期。CounterStrikeSharp 插件、SourceMod 插件和 Metamod 二进制文件都不能直接拿来跑，需要按 ModSharp 的接口移植代码，并照[编写模块](writing-a-module.md)的方式打包部署。

## 分清模块生命周期和地图生命周期

`IModSharpModule.Init()` / `PostInit()` 管的是模块加载，`Shutdown()` 管的是模块卸载，它们**跟换图无关**。一个模块会跨很多张图一直活着，热重载发生在下一次换图初始化之前。别在模块构造函数、`Init()` 或 `PostInit()` 里假设地图或 `GameRules` 已经就绪，首次启动时地图往往还没加载。

地图生命周期由 **`Sharp.Shared.Listeners.IGameListener`** 提供：实现 `ListenerVersion`（返回 `IGameListener.ApiVersion`）和 `ListenerPriority`，通过 `sharedSystem.GetModSharp().InstallGameListener(listener)` 注册，卸载时调 `RemoveGameListener(listener)` 移除。

## 地图阶段怎么对应

下面这张表方便迁移时对号入座，但细节时机跟其他框架未必完全一致：

| 原来的习惯 | ModSharp 对应阶段 |
| --- | --- |
| 早期地图/游戏初始化（`OnLevelInit`） | `OnGameInit` |
| 初始化后阶段（`OnMapInit`） | `OnGamePostInit` |
| 地图激活（`OnMapStart`） | `OnGameActivate` |
| 服务器激活完成 | `OnServerActivate` |
| 地图结束（`OnMapEnd`） | `OnGameDeactivate` |

特别提一句：`OnServerActivate` 挂在引擎的服务器激活函数之后，**不是“所有 `.cfg` 都执行完毕”的专用事件**，别把以前 `OnConfigsExecuted` 里的逻辑直接照搬过来；有依赖配置的初始化，建议自己安排明确的触发时机。

完整的一轮地图回调顺序：

```text
OnServerInit
OnGameInit
OnGamePostInit
OnResourcePrecache
OnServerSpawn
OnGameActivate
OnServerActivate
... 地图运行 ...
OnGameDeactivate
OnGamePreShutdown
OnGameShutdown
```

- `OnServerInit`：每换一张图都会触发（不是整个进程只跑一次），触发前网络服务器和全局变量包装已经建好。
- `OnGameInit`：触发前 `GameRules` 已经就绪。
- `OnResourcePrecache`：预缓存资源要在这里直接调 `PrecacheResource`，回调一结束预缓存上下文就会清掉，放到定时器里再调就晚了。
- `OnServerSpawn`：从这里开始可以执行服务端 `.cfg`，但不代表所有配置都跑完了。
- `OnGameDeactivate` / `OnGamePreShutdown`：地图收尾前清理本地图相关的状态和实体引用。
- `OnGameShutdown`：**进回调之前 `GameRules` 就已经失效了**，回调跑完后服务器和全局变量包装也会被清空。不要把地图相关对象留到下一张图，到新图初始化阶段重新获取。

下一张图会重新从 `OnServerInit` 开始。中途才注册监听器不会补发之前错过的回调。完整代码见 [Game Listener 示例](../examples/game-listener.md)。

## 实体

ModSharp 的实体包装、存活检查和 Schema 字段访问都跟 CounterStrikeSharp / SourceMod 不一样。实体的 C# 引用不能跨删除或换图直接复用，原有读写属性的写法也需要对照改一下，具体看[游戏实体](../features/game-entities.md)。

## 游戏事件

事件名和字段读写看着眼熟，但 ModSharp 用的是显式监听器和显式 `HookEvent`，迁移时需要改写注册方式、回调签名和释放逻辑，详见[游戏事件](../features/game-events.md)。

## 共享 API 与依赖

共享接口放 `sharp/shared`，实现模块放 `sharp/modules`，版本跟框架对齐即可。导出接口放在 `PostInit()`，拿别人的接口放在 `OnAllModulesLoaded()`，提供方热重载时处理好断开与重连。详见[安装共享 API](integrating-api.md)和[重新加载模块](reload-module.md)。

## Stripper

要改地图实体 lump 的话，另外安装 [StripperSharp 模块](https://github.com/Kxnrl/StripperSharp)，按它自己的文档配置即可。

## 创意工坊附加内容

想在当前地图之外额外挂载一个创意工坊 addon，在服务器**启动参数**里加上（不是控制台命令）：

```text
-dual_addon 1234567890
```

把 `1234567890` 换成对应的创意工坊 ID。这个参数一次只能填一个附加内容 ID（不支持逗号列表或合集 ID），也不能代替创意工坊地图本身。改启动参数需要重启生效；启用后，模块还可以在运行时调 `IModSharp.GetDualAddonId()` 和 `IModSharp.SetDualAddonId(ulong)` 读取或切换附加内容 ID。
