# Steam Listener

Use the [Hello World project settings](hello-world.md).

Implement `ISteamListener` using `ISteamListener.ApiVersion` (currently 2), register with `InstallSteamListener`, and remove the instance with `RemoveSteamListener` in `Shutdown`.

This example logs Steam server connectivity, group-status responses, and Workshop download/install notifications. Steam server connectivity callbacks are not game-coordinator (GC) callbacks. `OnGroupStatusResult` responds to `RequestUserGroupStatus`.

[!code-csharp[steam-listener.cs](../../codes/steam-listener.cs)]
