# Entitiy

## 保存和使用

- 在SourceMod中, 实体通常使用Ref或者Index
- 在CS#中, 实体通常使用Native Pointer进行保存

在ModSharp中，实体保存为托管的实例。  
只要你在调用之前确保`IBaseEntity.IsValid()`返回**true**，
那么你可以尽情使用它，  
而不必担心CS#中的无效指针导致的崩溃，  
又或者SourceMod中的Index被重新分配而访问错误的实体。  

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
> 有些时候你也可以保存``CEntityHandle<T>``代替``IBaseEntity``  
> 但是在使用的时候需要``IEntityManager.FindEntityByHandle``取回实体

---

**ModSharp**当然也支持直接从指针访问实体，但实体指针的有效性由你自己保证

```c#
// ... get pointer
if (_entityManager.MakeEntityFromPointer<IPlayerPawn>(pointer) is { } pawn)
{
    pawn.Slay(true);
}
```

> [!CAUTION]
>
> 如果传入的pointer是空指针，则你得到的也是一个null  
> 如果你的pointer是非法指针，亦或者非实体的指针，服务器会立即崩溃

无论是``CEntityHandle<T>``还是``IBaseEntity``，  
都可以安全的当做`Dictionary`或`HashSet`等容器的Key。  

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

---

## 创建

在SourceMod/CS#中，实体使用`CreateEntityByName`创建，并且``DispatchSpawn``生成，  
在**ModSharp**中也提供了该方法，但我们推荐使用``SpawnEntitySync<T>``，  
它具有更好的性能和更直观的代码。  

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

---

## NetProps/Schemas

与CS#导出所有的Schema成员不同，**ModSharp**只提供了部分成员导出，  
并提供了类似SourceMod中``GetEntProp``/``SetEntiyProp``的方法。

```c#
pawn.Health = 233; // 与CS#不同, 这里会自动StateChanged
pawn.SetNetVar("m_bIsScoped", false); // 自动StateChanged, 无需手动调用
var bulletForce1 = pawn.GetNetVar<Vector>("m_vecTotalBulletForce"); // 使用Schema字段名称获取
var bulletForce2 = _schemas.GetNetVar<Vector>(pawn, "CCSPlayerPawn", "m_vecTotalBulletForce"); // 显式类 + 字段名
var bulletForce3 = _schemas.GetNetVar<Vector>(pawn, pawn.GetSchemaClassname(), "m_vecTotalBulletForce"); // 自动类 + 显式字段名
pawn.SetNetVar("m_vecTotalBulletForce", new Vector()); // 设置为指定的值
```

---

> [!Caution]
>
> 当你传入的字段不存在时, 会立即抛出异常  
> 当你传入的字段与class不匹配时, 会立即抛出异常  

## 监听

实体监听与CS#和SourceMod的监听方式不同，  
**ModSharp**中的实体监听监听思路与SourceEngine相同。

- 如果你想高度自定义，请参阅该示例：[EntityListener](../examples/entity-listener.md)
- 如果你想简单粗暴当懒人，请参阅该示例：[EntityHookManager](../examples/entity-hook-manager.md)
