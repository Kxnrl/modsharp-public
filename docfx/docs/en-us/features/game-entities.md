# Game Entities

ModSharp exposes engine entities through managed interfaces such as `IBaseEntity` and `IPlayerPawn`. They are live proxies over engine-owned objects, not managed copies.

## Retaining and Resolving Entities

Holding a C# reference to an entity wrapper does not keep the native entity alive. After any timer, frame boundary, async continuation, or call that could delete or replace the entity, recheck `IsValid()` on the game thread before reading fields or calling methods (unless the current callback explicitly guarantees validity). Never touch game entities from a background thread.

- `IsValid()` checks whether the wrapper is still alive and its handle resolves to a native entity. It is not the same as `IsAlive`, and it does not tell you whether the entity is already queued for deletion; check `IsMarkedForDeletion()` when that matters.
- `CEntityHandle<T>` stores both the index and the **serial number**. Save the full handle rather than a bare index, and look it up again with `IEntityManager.FindEntityByHandle`.
- `CEntityHandle<T>.IsValid()` only checks that the handle is not the invalid sentinel value; it does **not** check whether the entity still exists. Always resolve the handle and check for `null`.
- Wrappers and handles do not survive map transitions. Clear cached entity collections on map change, drop stale entries on deletion, and pass `GameTimerFlags.StopOnMapEnd` to map-scoped timers.
- Full handles work well as map-local dictionary/set keys, though your own collections will not auto-evict deleted entities.

The helper below saves a full handle and re-resolves it when the timer fires instead of keeping a stale reference around:

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

Remove entities with engine methods like `Kill()`; entities remain engine-owned and expose no public `Dispose()` method (`IsDisposed` is maintained internally by the entity pool). To see how wrapper invalidation works under the hood, look at [EntityPool.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Pools/EntityPool.cs) and [BaseEntity.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/GameEntities/BaseEntity.cs).

## Raw Pointers and Type Conversions

`IEntityManager.MakeEntityFromPointer<T>(pointer)` is an unsafe escape hatch: passing a null pointer throws `InvalidOperationException` (instead of returning `null`), passing an invalid pointer can crash the server, and `T` is not checked against the actual native class.

Similarly, `As<T>()`, `CEntityHandle<T>.As<TOut>()`, and generic creation methods do not verify the underlying native type. When you are not certain of the type, use checked helpers like `AsPlayerPawn()` and `AsPlayerController()`, and do not pass `safeCheck: false` unless you have already verified the native type.

## Creating and Spawning Entities

`SpawnEntitySync` runs the full **create + precache + DispatchSpawn** sequence in one call. It works for weapons and needs no extra `DispatchSpawn` call (see `SpawnMarker` above).

When you know an entity's assets are already loaded and want to skip precaching, you can call `CreateEntityByName` followed by `DispatchSpawn`. Do not use that shortcut for weapons (it can crash the server) or entities with unloaded assets; when in doubt, stick to `SpawnEntitySync`. Generic `<T>` overloads do not check the native class.

See [IEntityManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/EntityManager.cs) for creation and lookup APIs. Wildcard lookups by classname or targetname scan the full entity list, so track entities yourself instead of scanning every frame.

## Schema Fields and Network State

Use typed properties like `pawn.Health` whenever available. For fields that are not exposed directly, use `GetNetVar<T>(fieldName)` and `SetNetVar(fieldName, value)` on the entity, or explicit class/field access through `ISchemaManager`.

Standard entity `SetNetVar` writes trigger network state change notifications automatically. Writes with `isStruct: true`, raw pointer writes, and edits through returned `ref` values do not; call `NetworkStateChanged` as required for those paths.

Missing field names may throw, but schema APIs cannot catch every layout or type mismatch. A wrong class, value type, offset, or pointer will silently read or corrupt the wrong memory. Match the C# value type accurately and keep the entity valid throughout access. See [ISchemaObject](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/CStrike/ISchemaObject.cs).

## Entity Notifications

Install an `IEntityListener` via `IEntityManager.InstallEntityListener` and remove the same instance in `Shutdown`. To receive specific inputs or outputs, also call `HookEntityInput` or `HookEntityOutput`. These hooks are global and permanent, dispatching to all installed `IEntityListener` instances (including inputs/outputs hooked by other modules), so filter by entity and input/output name inside your callbacks.

- [EntityListener example](../examples/entity-listener.md)
- [EntityHookManager extension example](../examples/entity-hook-manager.md)
