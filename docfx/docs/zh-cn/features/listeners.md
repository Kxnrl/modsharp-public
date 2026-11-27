# 监听器

需要接收哪类事件，就实现对应的监听器接口，再通过所属的管理器注册上去。目前公开的监听器接口一共有六种：

| 接口 | 监听内容 | 注册 / 移除 | 示例或接口定义 |
| --- | --- | --- | --- |
| `IGameListener` | 服务端/地图生命周期、资源预缓存、回合重置及控制台/脚本聊天 | `IModSharp.InstallGameListener` / `RemoveGameListener` | [游戏监听器](../examples/game-listener.md) |
| `IClientListener` | 客户端连接、聊天及相关回调 | `IClientManager.InstallClientListener` / `RemoveClientListener` | [客户端监听器](../examples/client-listener.md) |
| `IEventListener` | 旧式游戏事件 | `IEventManager.InstallEventListener` / `RemoveEventListener` | [事件监听器](../examples/event-listener.md) |
| `ISteamListener` | Steam 事件与回调 | `IModSharp.InstallSteamListener` / `RemoveSteamListener` | [Steam 监听器](../examples/steam-listener.md) |
| `IEntityListener` | 实体生命周期及已挂钩的输入/输出 | `IEntityManager.InstallEntityListener` / `RemoveEntityListener` | [实体监听器](../examples/entity-listener.md) |
| `ILoggingListener` | 批量派发的托管运行时日志 | `ILoggingManager.InstallLoggingListener` / `RemoveLoggingListener` | [ILoggingListener](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Listeners/LoggingListener.cs) |

## 版本、优先级与清理

`ListenerVersion` 直接返回**对应接口自己的** `ApiVersion`（例如 `IGameListener.ApiVersion`），别手写固定数字。比如 `ISteamListener` 当前的 `ApiVersion`（2）就和其他五个接口（1）不一样，版本号不匹配时注册会直接抛 `InvalidOperationException`。一个类同时实现多个监听器接口时，请为每个接口显式实现 `ListenerVersion`（例如 `int ISteamListener.ListenerVersion => ISteamListener.ApiVersion;`）。

`ListenerPriority` 越大越先执行，同优先级的先后顺序不保证。另外，装上监听器并不会自动拦截所有事件名或实体输入/输出，该调 `HookEvent`、`HookEntityInput`、`HookEntityOutput` 的还是要显式调用，见[游戏事件](game-events.md)和[游戏实体](game-entities.md)。

监听器在初始化时注册一次，`Shutdown` 时把**同一个实例**移除掉。如果一个类同时实现了多个监听器接口，每一项都要分别移除（见[多监听器示例](../examples/multi-listeners.md)）。核心在卸载模块时会尽量兜底清理漏掉的监听器，但模块自己仍应主动清理干净。

回调里收到的参数往往是原生对象的临时包装，存下 C# 引用并不会延长原生对象的生命周期，使用前按各接口的要求做好有效性检查。

## `ILoggingListener` 的特别之处

和其他五个监听器不同，`ILoggingListener` 的回调不在游戏主线程跑：

- 安装和移除依然要在游戏主线程通过 `ILoggingManager` 调用。
- `OnRuntimeLogEntries` 跑在核心独立的日志派发线程上，里面要碰游戏对象的话必须用 `IModSharp.InvokeAction` 切回主线程。
- `RuntimeMinimumLevel` 只在安装时读一次，已经被 `core.json` 里 `Logger.LogLevel` 过滤掉的日志收不到。
- 回调里的 span、`RuntimeLogEntry` 和 `LogValue` 只在本次回调期间有效。不要直接保存来自其他模块的对象引用或异常（那样会卡住对应模块的热卸载），把需要的文本或数据拷到自己的结构里。
- 回调要尽快返回：所有日志监听器共用这一条派发线程，队列堆满后新日志会被静默丢弃。在这条线程上再写日志不会重新派发给监听器。
- `RemoveLoggingListener` 不会阻塞等派发线程跑完：移除返回后，最多还可能再进一次回调（甚至可能在移除返回后才刚进来），写关闭逻辑时留个心眼。

完整定义见 [ILoggingManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/LoggingManager.cs) 和 [Listeners 源码目录](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Shared/Listeners)。
