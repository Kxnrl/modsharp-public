# Listeners

The current version has the following Listeners:

- `IGameListener`: Game flow [Example](../examples/game-listener.md)
- `IClientListener`: Client events [Example](../examples/client-listener.md)
- `IEventListener`: Game events [Example](../examples/event-listener.md)
- `ISteamListener`: Steam events/callbacks [Example](../examples/steam-listener.md)
- `IEntityListener`: Entity events [Example](../examples/entity-listener.md)

> [!Caution]
> After installing listeners, you must remove them when your module is unloaded, otherwise unexpected errors may occur.

```c#
public void PostInit()
    => _modSharp.InstallGameListener(this);

public void Shutdown()
    => _modSharp.RemoveGameListener(this);
```
