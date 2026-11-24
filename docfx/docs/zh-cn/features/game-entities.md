# 游戏实体

ModSharp 用 `IBaseEntity`、`IPlayerPawn` 等托管接口来包装引擎里的实体对象。它们只是原生实体的代理，不是拷贝出来的托管副本。

## 保存与重新获取实体

你可以把实体包装对象存起来，但持有 C# 引用不会阻止引擎删掉实体。每次跨帧、跨定时器、跨异步，或经历任何可能删除/替换实体的操作后，访问字段或调方法前都要在游戏主线程重新检查 `IsValid()`（除非当前回调本身明确保证实体有效）。任何时候都不要在后台线程碰游戏实体。

- `IsValid()` 检查包装对象有没有被释放、对应的实体句柄是否还能解析到原生实体。它和 `IsAlive` 不是一回事，也不代表实体没被标为待删除；需要时再补一个 `IsMarkedForDeletion()`。
- `CEntityHandle<T>` 里同时包含索引和**序列号**。想稍后找回同一个实体，请存完整句柄而不是裸索引，再用 `IEntityManager.FindEntityByHandle` 重新解析。
- `CEntityHandle<T>.IsValid()` 只判断句柄值本身不是无效哨兵，**不会去查实体还在不在**。实际使用时必须解析句柄并处理 `null`。
- 实体包装和句柄都不跨图有效。换图时清掉自己缓存的实体集合，实体被删时也顺手清掉旧条目；跟地图绑定的延迟任务加上 `GameTimerFlags.StopOnMapEnd`。
- 完整句柄很适合做当前地图内字典或集合的键，但别指望实体被删时集合会自动替你清理。

下面这个例子把完整句柄存进定时器，到期后再重新解析并删除实体，避免直接拿着过期的对象引用操作：

```csharp
using System.Collections.Generic;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

public static class EntityReferenceExample
{
    public static void RemoveLater(IModSharp modSharp, IEntityManager entities, IBaseEntity entity)
    {
        if (!entity.IsValid())
        {
            return;
        }

        var handle = entity.Handle;
        modSharp.PushTimer(() =>
        {
            if (entities.FindEntityByHandle(handle) is { } current
                && current.IsValid()
                && !current.IsMarkedForDeletion())
            {
                current.Kill();
            }
        }, 2.33, GameTimerFlags.StopOnMapEnd);
    }

    public static IBaseEntity? SpawnMarker(IEntityManager entities)
        => entities.SpawnEntitySync("info_target", new Dictionary<string, KeyValuesVariantValueItem>
        {
            ["targetname"] = "docs_marker",
            ["origin"] = "0 0 64"
        });
}
```

移除实体请用 `Kill()` 这类引擎方法；实体始终归引擎所有，没有公开的 `Dispose()` 方法（`IsDisposed` 由实体池内部维护）。想了解底层怎么让包装对象失效，可以看[实体池](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Pools/EntityPool.cs)和 [BaseEntity 实现](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/GameEntities/BaseEntity.cs)。

## 原生指针与类型转换

`IEntityManager.MakeEntityFromPointer<T>(pointer)` 是底层不安全入口：传空指针会直接抛 `InvalidOperationException`（不是返回 `null`），传非法指针可能直接把服打崩，而且泛型参数 `T` 不会跟原生实际类型做校验。

同样，`As<T>()`、`CEntityHandle<T>.As<TOut>()` 以及泛型创建方法都不会替你验证原生类型。不确定实体类型时，直接用 `AsPlayerPawn()`、`AsPlayerController()` 这类自带校验的转换方法，没确认原生类型前不要传 `safeCheck: false`。

## 创建与生成实体

`SpawnEntitySync` 一步走完 **创建 + 预缓存 + DispatchSpawn**，支持武器，也不用再手动调 `DispatchSpawn`（上面的 `SpawnMarker` 就是示例）。

如果明确知道目标实体所需资源已经加载好，想省掉预缓存开销，可以先 `CreateEntityByName` 再 `DispatchSpawn`。但千万别拿这条快捷路径去生成武器（容易直接崩服），其他实体缺资源也会失败；拿不准就老老实实用 `SpawnEntitySync`。注意泛型 `<T>` 重载同样不校验原生类名。

更多创建和查找方法见 [IEntityManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/EntityManager.cs)。按类名或名字通配查找会遍历整张实体表，能自己跟踪就别每帧全表扫。

## Schema 字段与网络同步

常见字段优先用封装好的强类型属性（比如 `pawn.Health`）。没导出的字段可以通过 `GetNetVar<T>(fieldName)` 和 `SetNetVar(fieldName, value)` 按 Schema 类读写，`ISchemaManager` 也支持显式传类名和字段名访问。

普通实体的 `SetNetVar` 会自动触发网络状态变更通知，一般不用再手动通知。但带 `isStruct: true`、直接写指针，或者通过返回的 `ref` 修改字段时不会自动通知，要按各自文档的要求补调 `NetworkStateChanged`。

字段名拼错可能会抛异常，但 Schema 访问不做全面的内存布局校验：类、字段类型、偏移或指针一旦对不上，往往是直接读写错内存而不是友好报错。请确认 C# 值类型完全匹配，且访问期间实体有效。参见 [ISchemaObject](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/CStrike/ISchemaObject.cs)。

## 实体通知

通过 `IEntityManager.InstallEntityListener` 安装 `IEntityListener`，模块关闭时移除同一个实例。如果要监听特定实体输入或输出，除了安装监听器，还要调用 `HookEntityInput` / `HookEntityOutput`；这些挂钩是全局且永久生效的，所有已安装的 `IEntityListener` 都会收到其他模块挂钩的输入/输出，回调里需自行按实体和输入/输出名过滤。

- [EntityListener 示例](../examples/entity-listener.md)
- [EntityHookManager 扩展示例](../examples/entity-hook-manager.md)
