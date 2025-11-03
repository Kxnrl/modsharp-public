# Game Events

*游戏事件在起源2引擎中已被标记为***Legacy**

## 创建

这是一段创建事件并广播的代码，它的逻辑与SourceMod很接近。

```c#
if (_eventManager.CreateEvent("weapon_fire", false) is { } e)
{
    e.SetPlayer("userid", pawn);
    e.SetString("weapon", "hh,nm");
    e.Fire(); // 发射事件并广播给所有客户端
}

if (_eventManager.CreateEvent<IEventPlayerDeath>(false) is { } death)
{
    e.SetPlayer("userid", pawn);
    e.SetString("weapon", "hh,nm");
    e.FireToClients(); // 仅广播给所有客户端 (不发射事件)
    e.Dispose(); // 不发射的事件需要手动销毁
}
```

---

## 监听

- 如果你想高度自定义，请参阅该示例：[EventListener](../examples/event-listener.md)
- 如果你想简单粗暴当懒人，请参阅该示例：[GameEventManager](../examples/game-event-manager.md)
