# Using Sharp Extensions

Use the [Hello World project settings](hello-world.md).

Install `ModSharp.Sharp.Extensions.EntityHookManager` and `ModSharp.Sharp.Extensions.GameEventManager` for this example. These are module-private library dependencies, not separately loaded module entry points. Keep their runtime assets and ship them alongside your plugin; only host-provided SDK/Shared contracts should exclude runtime assets.

The extension registration methods require `ISharedSystem`. Build one module-owned `ServiceProvider`, call `LoadAllSharpExtensions` before using the services, and call `ShutdownAllSharpExtensions` before disposing the container in `Shutdown`. Container disposal alone does not invoke the extension lifecycle. See [Writing Sharp Extensions](writing-sharp-extensions.md) to register your own extension.

[!code-csharp[sharp-extensions.cs](../../codes/sharp-extensions.cs)]
