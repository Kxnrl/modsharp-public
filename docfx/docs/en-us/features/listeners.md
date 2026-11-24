# Listeners

Implement the listener interface for the callbacks you need, then register it with the corresponding manager. There are six public listener interfaces:

| Interface | Notifications | Register / Remove | Example or interface |
| --- | --- | --- | --- |
| `IGameListener` | Server/map lifecycle, resource precache, round restart, and console/script say | `IModSharp.InstallGameListener` / `RemoveGameListener` | [Game listener](../examples/game-listener.md) |
| `IClientListener` | Client connection, chat, and related callbacks | `IClientManager.InstallClientListener` / `RemoveClientListener` | [Client listener](../examples/client-listener.md) |
| `IEventListener` | Legacy game events | `IEventManager.InstallEventListener` / `RemoveEventListener` | [Event listener](../examples/event-listener.md) |
| `ISteamListener` | Steam events and callbacks | `IModSharp.InstallSteamListener` / `RemoveSteamListener` | [Steam listener](../examples/steam-listener.md) |
| `IEntityListener` | Entity lifecycle and hooked inputs/outputs | `IEntityManager.InstallEntityListener` / `RemoveEntityListener` | [Entity listener](../examples/entity-listener.md) |
| `ILoggingListener` | Batched managed runtime logs | `ILoggingManager.InstallLoggingListener` / `RemoveLoggingListener` | [ILoggingListener](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Listeners/LoggingListener.cs) |

## Version, Priority, and Cleanup

Return each interface's own `ApiVersion` from `ListenerVersion` (such as `IGameListener.ApiVersion`) instead of hardcoding a number. For example, `ISteamListener` currently has a different `ApiVersion` (2 vs. 1 for the others), and a version mismatch throws `InvalidOperationException` on registration. When one class implements multiple listener interfaces, implement `ListenerVersion` explicitly for each interface (e.g. `int ISteamListener.ListenerVersion => ISteamListener.ApiVersion;`).

Higher `ListenerPriority` runs first; order among equal priorities is unspecified. Installing a listener does not automatically hook every event or entity input/output, so still call `HookEvent`, `HookEntityInput`, or `HookEntityOutput` as needed (see [Game Events](game-events.md) and [Game Entities](game-entities.md)).

Install each listener once during initialization and remove the **same instance** in `Shutdown`. If one class implements multiple listener interfaces, remove each registration individually (see [Multiple Listeners](../examples/multi-listeners.md)). Core unload cleanup catches leaks as a fallback, though modules should always clean up explicitly.

Callback arguments are often borrowed native wrappers whose lifetime is controlled by the engine; keeping a C# reference does not keep them alive, so check validity according to each interface's rules.

## How `ILoggingListener` Differs

Unlike the other five listeners, `ILoggingListener` does not invoke callbacks on the game thread:

- Still install and remove it on the game main thread via `ILoggingManager`.
- `OnRuntimeLogEntries` runs on the core log dispatch thread; bounce back with `IModSharp.InvokeAction` before touching any game objects.
- `RuntimeMinimumLevel` is read once at install time and cannot recover logs already filtered out by `Logger.LogLevel` in `core.json`.
- The span, `RuntimeLogEntry`, and `LogValue` instances are valid only during the callback. Copy out only the values you need; holding onto scalar objects or exceptions from other modules can prevent them from unloading.
- Keep the callback fast: all logging listeners share one dispatch thread, and new entries are dropped silently if the queue backs up. Logs emitted from that thread are not re-dispatched.
- `RemoveLoggingListener` does not wait for an in-flight batch to finish: up to one more callback may still run (or even start) after removal returns, so write your shutdown path accordingly.

See [ILoggingManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/LoggingManager.cs) and the [Listeners source directory](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Shared/Listeners).
