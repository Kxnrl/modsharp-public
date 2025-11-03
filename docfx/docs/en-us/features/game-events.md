# Game Events

*Game Events have been marked as* **Legacy** *in the Source 2 engine.*

## Creation

This is code for creating and broadcasting events, and its logic is very similar to SourceMod.

```c#
if (_eventManager.CreateEvent("weapon_fire", false) is { } e)
{
    e.SetPlayer("userid", pawn);
    e.SetString("weapon", "hh,nm");
    e.Fire(); // Fire event and broadcast to all clients
}

if (_eventManager.CreateEvent<IEventPlayerDeath>(false) is { } death)
{
    e.SetPlayer("userid", pawn);
    e.SetString("weapon", "hh,nm");
    e.FireToClients(); // Only broadcast to all clients (don't fire event)
    e.Dispose(); // Events that are not fired need to be manually disposed
}
```

## Listening

- If you want high customization, please refer to this example: [EventListener](../examples/event-listener.md)
- If you want simple and easy lazy mode, please refer to this example: [GameEventManager](../examples/game-event-manager.md)
