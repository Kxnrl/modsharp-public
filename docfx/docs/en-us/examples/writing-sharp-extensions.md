# Writing ModSharp Extensions

An extension is a library instantiated inside each consumer's DI container, not a globally loaded provider module. Use a [Module Shared API](module-api.md) when consumers must talk to one shared runtime instance.

Compile the following **three files together** in one `net10.0`, C# 13 class library. Reference `ModSharp.Sharp.Shared` pinned to your server's SDK version with `PrivateAssets="all" ExcludeAssets="runtime"` (as in [Hello World](hello-world.md)) so the host-provided SDK is neither copied into the output nor flowed transitively to consumers; the SDK supplies the Microsoft DI APIs. The ordinary `this IServiceCollection` methods below do not require C# 14 extension blocks.

## Contract

[!code-csharp[SharpExtensionApi.cs](../../codes/sharp-extension-interface.cs)]

## Implementation

`Load` and `Shutdown` define the extension lifecycle. Real extensions install listeners/hooks in `Load` and release those exact registrations in `Shutdown`; this minimal extension logs its lifecycle and its API call.

[!code-csharp[SharpExtension.cs](../../codes/sharp-extension-example.cs)]

## DI Registration

Both `ISharpExtension` and the public API must resolve the **same** singleton instance. Registering separate implementations would load one object and call another. One overload accepts `ISharedSystem`; the parameterless overload requires it to have already been registered.

[!code-csharp[SharpExtensionDi.cs](../../codes/sharp-extension-di.cs)]

## Consumption

Use the module pattern in [Using Sharp Extensions](sharp-extensions.md): reference your extension library, replace the example registrations with `services.AddExampleSharpExtension(sharedSystem)`, call `LoadAllSharpExtensions`, and then use `_provider.GetRequiredService<IExampleSharpExtension>().CallMe()`. On shutdown call `ShutdownAllSharpExtensions` before `_provider.Dispose()`. Add `using ExampleSharpExtension;` to import its contract and DI methods. Deploy the extension DLL and its private dependencies beside the consumer, not as another module entry point.
