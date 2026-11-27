# Game Events

ModSharp exposes legacy game events through `IEventManager` and `IGameEvent`. Common events have strongly typed interfaces such as `IEventWeaponFired`, while others can be read and written by field name.

## Creating and Sending

`CreateEvent(name, force)` and `CreateEvent<T>(force)` return nullable results. Events you create (and copies from `CloneEvent`) belong to your module until you hand them to the engine with `Fire` or call `Dispose`.

| Operation | Effect | Afterwards |
| --- | --- | --- |
| `Fire(serverOnly: false)` | Runs through the engine and broadcasts to clients | Engine owns it; wrapper is disposed |
| `Fire(serverOnly: true)` | Runs through the engine on the server only | Engine owns it; wrapper is disposed |
| `FireToClient(...)` / `FireToClients()` | Sends directly to the chosen client(s), bypassing engine firing | Still owned by your module; can be modified or reused |
| `Dispose()` | Frees the event without firing | Wrapper is disposed |

`Fire` always requires the `serverOnly` argument (there is no parameterless overload). Once you call `Fire`, do not call `Dispose`, fire it again, or keep reading its fields (ownership transfers to the engine immediately, even if a `HookFireEvent` listener blocks it). Direct sends (`FireToClient` / `FireToClients`) do not free the event, so dispose it when done, including any cached event at module shutdown or an event you created and decided not to send.

Here is both paths side by side (call on the game thread with a valid pawn):

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

The first `finally` cleans up only if `Fire` was never reached (e.g. a field setter threw); the second always disposes the directly sent event. Note that `IGameEvent` defines `Dispose()` without implementing `IDisposable`, so `using` cannot be used with it.

## Listening and Hooking

Using the low-level listener API:

1. Call `IEventManager.HookEvent("event_name")` for each event you want to receive.
2. Register an `IEventListener` with `InstallEventListener`.
3. Remove the same instance with `RemoveEventListener` during shutdown (`HookEvent` itself has no public per-module unhook).

Note that `HookEvent` is global: every installed `IEventListener` receives events hooked by any module, so always filter by event name or typed interface inside your callbacks.

`HookFireEvent` runs before the engine fires the event; fields and `serverOnly` can be modified. Returning `false` blocks the event, and later listeners returning `true` cannot unblock it. Blocked events are freed by the engine, not by your listener.

`FireGameEvent` is a read-only notification. **Events passed to both callbacks belong to the engine and are valid only while the callback runs**, so never save, fire, or dispose them (even when `Editable == true` in `HookFireEvent`). Copy out the values you need, or call `CloneEvent` inside the callback to get an independent copy you can fire or dispose later.

- [Low-level event listener example](../examples/event-listener.md)
- [GameEventManager extension example](../examples/game-event-manager.md)
- Source interfaces: [IGameEvent](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Objects/IGameEvent.cs) and [IEventListener](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Listeners/EventListener.cs)
