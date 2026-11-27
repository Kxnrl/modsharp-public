# Client Listener

Use the [Hello World project settings](hello-world.md).

Implement `IClientListener` and register the same instance with `InstallClientListener` in `Init`; remove it with `RemoveClientListener` in `Shutdown`. Return `IClientListener.ApiVersion` from `ListenerVersion`, and set `ListenerPriority` to `0` unless you need a specific ordering.

The example logs connection, admin-check, disconnect, and chat callbacks. A few details to keep in mind:

- `OnClientPutInServer` is actually bound to the engine's `CSource2GameClients::Active` post-hook (`OnClientActive`), not `PutInServer`.
- `OnClientConnected` fires after `CheckConnect` allows a human player, and after `Connected` for bots.
- Returning `false` from `OnClientPreAdminCheck` lets the check proceed, while `true` permanently blocks `OnClientPostAdminCheck` for that connection. Bots skip `OnClientPreAdminCheck` and go straight to `OnClientPostAdminCheck` at the end of a frame.
- In `OnClientSayCommand`, `commandName` is `"say"` or `"say_team"`. Both `Handled` and `Stopped` block the chat broadcast (without cancelling queued chat command callbacks), and messages of length 2..255 starting with a silent prefix are always hidden regardless of return value.

[!code-csharp[client-listener.cs](../../codes/client-listener.cs)]
