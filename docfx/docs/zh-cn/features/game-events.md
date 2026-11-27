# 游戏事件

ModSharp 通过 `IEventManager` 和 `IGameEvent` 提供旧式游戏事件接口。常见事件有强类型包装（比如 `IEventWeaponFired`），其他事件直接按字段名读写即可。

## 创建与发送

`CreateEvent(name, force)` 和 `CreateEvent<T>(force)` 都可能返回 `null`。你自己创建的事件以及 `CloneEvent` 拷贝出来的副本归模块管理，直到你交给引擎发射或者主动 `Dispose`。

| 操作 | 效果 | 操作之后 |
| --- | --- | --- |
| `Fire(serverOnly: false)` | 走引擎流程并广播给客户端 | 归引擎释放；当前包装失效 |
| `Fire(serverOnly: true)` | 只在服务端走引擎流程，不发给客户端 | 归引擎释放；当前包装失效 |
| `FireToClient(...)` / `FireToClients()` | 直接发给指定客户端，不走引擎发射流程 | 仍归模块所有，可以继续改写或复用 |
| `Dispose()` | 不发射，直接释放事件 | 当前包装失效 |

`Fire` 必须显式传 `serverOnly`，没有无参重载。调完 `Fire` 后就别再 `Dispose`、重复发射或继续读写字段了。用 `FireToClient` / `FireToClients` 直接发出去的事件不会自动释放，用完记得手动 `Dispose`（创建了但临时决定不发的事件也一样）。

下面这段代码演示了两种用法，请在游戏主线程传入有效的 pawn 调用：

```csharp
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Managers;

public static class WeaponEventExample
{
    public static void Broadcast(IEventManager events, IPlayerPawn pawn)
    {
        if (!pawn.IsValid() || events.CreateEvent<IEventWeaponFired>(force: true) is not { } weaponFire)
        {
            return;
        }

        try
        {
            weaponFire.Pawn = pawn;
            weaponFire.Weapon = "glock";
            weaponFire.Fire(serverOnly: false);
        }
        finally
        {
            if (!weaponFire.IsDisposed)
            {
                weaponFire.Dispose();
            }
        }
    }

    public static void SendOnly(IEventManager events, IPlayerPawn pawn)
    {
        if (!pawn.IsValid() || events.CreateEvent("weapon_fire", force: true) is not { } weaponFire)
        {
            return;
        }

        try
        {
            weaponFire.SetPlayer("userid", pawn);
            weaponFire.SetString("weapon", "glock");
            weaponFire.FireToClients();
        }
        finally
        {
            weaponFire.Dispose();
        }
    }
}
```

第一个 `finally` 只在事件还没交给引擎时兜底释放（比如设字段中途抛异常）；第二个则是每次发完直接释放。注意 `IGameEvent` 虽然有 `Dispose()` 方法，但没实现 `IDisposable`，不能写 `using`。

## 监听与拦截

使用底层监听 API 的步骤：

1. 对每个要监听的事件调用 `IEventManager.HookEvent("event_name")`。
2. 用 `InstallEventListener` 注册 `IEventListener`。
3. 关闭时用 `RemoveEventListener` 移除同一个实例（`HookEvent` 本身没有按模块取消的接口）。

注意 `HookEvent` 是全局生效的，所有已安装的 `IEventListener` 都会收到任意模块挂钩过的事件，在回调里务必按事件名或强类型接口自行过滤。

`HookFireEvent` 在引擎真正发射前触发，可以改字段，也可以改 `serverOnly`。返回 `false` 会拦掉这次事件，后面的监听器就算返回 `true` 也恢复不了；被拦掉的事件由引擎负责释放，监听器不用管。

`FireGameEvent` 是只读通知。**这两个回调拿到的事件都归引擎所有，且只在回调期间有效**，不要存下来、不要发射，也不要 `Dispose`（就算 `HookFireEvent` 里 `Editable == true` 也不归你管）。想留到后面用，就把需要的字段拷出来，或者在回调里调 `CloneEvent` 拿一份自己的副本，用完再自行发射或释放。

- [底层事件监听示例](../examples/event-listener.md)
- [GameEventManager 扩展示例](../examples/game-event-manager.md)
- 相关接口：[IGameEvent](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Objects/IGameEvent.cs)、[IEventListener](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Listeners/EventListener.cs)
