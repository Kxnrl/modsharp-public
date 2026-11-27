# Game Event Listener

Use the [Hello World project settings](hello-world.md).

Install the listener with `InstallEventListener` and call `HookEvent` for every event name you want to receive (no events are hooked by default). Implement `IEventListener.FireGameEvent` for read-only observation. `HookFireEvent` is optional and defaults to `true`; if any listener returns `false`, the event is blocked and later listeners cannot unblock it. Setting `serverOnly = true` stops client broadcast while keeping server-side handling.

These events are owned by the engine and remain valid only during the callback; do not cache, re-fire, or dispose them. Remove the listener in `Shutdown`. For named-event registration and cloning, see [Game Event Manager](game-event-manager.md).

[!code-csharp[event-listener.cs](../../codes/event-listener.cs)]
