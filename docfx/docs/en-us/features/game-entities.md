# Entity

## Storage and Usage

- In SourceMod, entities are usually stored using Ref or Index
- In CS#, entities are usually stored using Native Pointer

In ModSharp, entities are stored as managed instances.  
As long as you ensure `IBaseEntity.IsValid()` returns **true** before calling,
you can use it freely,  
without worrying about crashes from invalid pointers in CS#,  
or accessing the wrong entity due to Index reassignment in SourceMod.

```c#
if (entity.IsValid())
{
    entity.AcceptInput("Blabla");

    _modSharp.PushTimer(() => 
    {
        if (entity.IsValid())
        {
            entity.Kill();
        }
    }, 2.33);
}
```

> [!TIP]
> Sometimes you can also store `CEntityHandle<T>` instead of `IBaseEntity`  
> But when using it, you need to use `IEntityManager.FindEntityByHandle` to retrieve the entity

**ModSharp** also supports accessing entities directly from pointers, but the validity of entity pointers is guaranteed by you

```c#
// ... get pointer
if (_entityManager.MakeEntityFromPointer<IPlayerPawn>(pointer) is { } pawn)
{
    pawn.Slay(true);
}
```

> [!CAUTION]
>
> If the passed pointer is null, you will also get null  
> If your pointer is illegal or not an entity pointer, the server will crash immediately

Whether it's `CEntityHandle<T>` or `IBaseEntity`,  
they can both be safely used as keys in containers like `Dictionary` or `HashSet`.

```c#
var map = new Dictionary<IBaseEntity, int>();
map.Add(entity, 1);
var handle = entity.Handle;
if (_entityManager.FindEntityByHandle(handle) is { } find)
{
    if (map.TryGetValue(find, out var value))
    {
        find.Health = value;
    }
}
```

## Creation

In SourceMod/CS#, entities are created using `CreateEntityByName` and spawned with `DispatchSpawn`,  
**ModSharp** also provides this method, but we recommend using `SpawnEntitySync<T>`,  
which has better performance and more intuitive code.

```c#
var kv = new Dictionary<string, KeyValuesVariantValueItem>
{
    {"origin", pawn.GetAbsOrigin().ToString()},
    {"angles", "0 90 0"},
    {"model", "models/foo/bar.vmdl"},
    {"spawnflags", 3},
    {"disabled", true}
};

if (_entityManager.SpawnEntitySync<IBaseAnimGraph>("prop_dynamic", kv) is { } entity)
{
    entity.AcceptInput("Blabla");
}

```

## NetProps/Schemas

Unlike CS# which exports all Schema members, **ModSharp** only provides partial member exports,  
and provides methods similar to SourceMod's `GetEntProp`/`SetEntityProp`.

```c#
pawn.Health = 233; // Unlike CS#, this automatically calls StateChanged
pawn.SetNetVar("m_bIsScoped", false); // Automatic StateChanged, no need to call manually
var bulletForce1 = pawn.GetNetVar<Vector>("m_vecTotalBulletForce"); // Get using Schema field name
var bulletForce2 = _schemas.GetNetVar<Vector>(pawn, "CCSPlayerPawn", "m_vecTotalBulletForce"); // Explicit class + field name
var bulletForce3 = _schemas.GetNetVar<Vector>(pawn, pawn.GetSchemaClassname(), "m_vecTotalBulletForce"); // Auto class + explicit field name
pawn.SetNetVar("m_vecTotalBulletForce", new Vector()); // Set to specified value
```

> [!Caution]
>
> When the field you pass doesn't exist, an exception will be thrown immediately  
> When the field you pass doesn't match the class, an exception will be thrown immediately

## Listening

Entity listening is different from CS# and SourceMod listening methods,  
**ModSharp**'s entity listening approach is the same as SourceEngine.

- If you want high customization, please refer to this example: [EntityListener](../examples/entity-listener.md)
- If you want simple and easy lazy mode, please refer to this example: [EntityHookManager](../examples/entity-hook-manager.md)
