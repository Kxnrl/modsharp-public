# 从CSSharp/Metamod迁移

如果你是CS#/Metamod用户，想迁移到ModSharp，有一些事情你需要提前了解。

## OnMapStart/OnMapEnd

一句话概括:

- `OnLevelInit` → ``OnGameInit``
- `OnMapInit` → ``OnGamePostInit``
- `OnMapStart` → ``OnGameActivate``
- `OnConfigsExecuted` → ``OnServerActivate``
- `OnMapEnd` → ``OnGameDeactivate``

执行顺序:

- ``OnServerInit``: safe to get sv/globals
- ``OnGameInit``: safe to get GameRules
- ``OnGamePostInit``
- ``OnResourcePrecache``: safe to precache game resources
- ``OnSpawnServer``: safe to execute .cfg
- ``OnGameActivate``
- ``OnServerActivate``
- ...
- ``OnGameDeactivate``
- ``OnGamePreShutdown``
- ``OnGameShutdown``: sv/globals/GameRules is null here

以上均包含在`IGameListner`中。

> [!TIP]
> 如果想了解如何使用，请查看 [Game Listener 示例](../examples/game-listener.md) 了解完整的实现方式。

## Entity

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
