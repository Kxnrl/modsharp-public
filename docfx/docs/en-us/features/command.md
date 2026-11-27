# Commands

Pick the registration API based on who should be able to run the command. None of these low-level APIs check admin permissions; use the first-party CommandCenter / AdminManager modules when you need permission checks.

## Engine Console Commands

| Register | Callers | Unregister |
| --- | --- | --- |
| `IConVarManager.CreateServerCommand` | Server console and other non-client callers | `ReleaseServerCommandCallback(name, callback)` |
| `IConVarManager.CreateConsoleCommand` | Server and client consoles | `ReleaseConsoleCommandCallback(name, callback)` |

With `CreateConsoleCommand`, `client` is `null` when invoked from the server console. Engine command callbacks return `ECommandAction`, though **the return value does not affect command execution**.

Multiple registrations with the same name share one underlying engine command: flags are OR'd together, help text comes from the first registration, and the engine command is released only after every server and console callback has been removed.

> [!IMPORTANT]
> `ReleaseCommand` is obsolete and is a **no-op**. Always remove the exact callback in `Shutdown` using the matching method above. If you pass a lambda, store the delegate in a field so you can pass the same instance back on unload.

See [IConVarManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ConVarManager.cs).

## Client Virtual Commands

Call `IClientManager.InstallCommandCallback("example", callback)` with the **unprefixed** name to accept both forms from clients:

- Client console: `ms_example ...`
- Chat: `!example ...`, `/example ...`, or another configured [chat prefix](../configurations/core.md#chatcommandtriggerprefixes)

Virtual commands are client-only (not usable from the server console) and are removed via `RemoveCommandCallback("example", callback)`.

From the client console, only returning `Stopped` (or throwing an exception) prevents the engine's `ExecuteStringCommand` from continuing; returning `Handled` or `Skipped` lets it fall through to the engine (typically as an unknown command). From chat, the return value has no effect: chat visibility depends on the public/silent prefix and chat listeners, and the callback is queued to run on the game main thread on the next frame rather than inline inside the chat listener.

## Client Command Listeners

Use `IClientManager.InstallCommandListener(name, callback)` to observe or intercept existing client commands, and `RemoveCommandListener(name, callback)` to remove it. It does not register an `ms_` command or chat trigger, and it cannot see commands handled purely client-side.

Returning `Handled` only blocks commands routed through the game DLL's `ClientCommand` path and may not stop game-DLL ConCommands (`FCVAR_GAMEDLL`); return `Stopped` when you need to guarantee blocking. The callback receives a parsed `StringCommand`, not a mutable command buffer.

See [IClientManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ClientManager.cs) for per-callback return semantics; `ECommandAction.Handled` does not behave identically across every command API.

## Cleanup and Examples

Always pair each registration with removing the same delegate during `Shutdown`. Core unload cleanup catches leaked callbacks as a fallback, but modules should still clean up after themselves.

- [Low-level command example](../examples/command.md)
- [CommandManager extension example](../examples/command-manager.md)
- [AdminManager integration](../examples/admin-example.md)
