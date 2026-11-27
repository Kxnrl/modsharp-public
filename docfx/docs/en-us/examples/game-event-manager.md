# Game Event Manager

Use the [Hello World project settings](hello-world.md).

Install `ModSharp.Sharp.Extensions.GameEventManager` and deploy its runtime DLL with your module. Register the shared system, add the extension, and call `LoadAllSharpExtensions` before using it. On shutdown call `ShutdownAllSharpExtensions` before disposing `ServiceProvider`.

The example hooks named events, blocks a name-change event, and clones a death event for custom client delivery while keeping the original server-only. Hook and listener arguments are engine-owned: do not dispose or re-fire them. A cloned event is owned by the caller and must be disposed after `FireToClient` calls (including on exception paths, which this sample handles with `finally`).

[!code-csharp[game-event-manager.cs](../../codes/game-event-manager.cs)]
