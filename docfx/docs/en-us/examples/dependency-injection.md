# Dependency Injection

Start with the [Hello World project settings](hello-world.md). This example uses `Microsoft.Extensions.DependencyInjection`, available through the SDK's package dependencies.

ModSharp creates the module entry point with its fixed six-argument constructor. Create a module-owned `ServiceCollection` inside it to compose your other services; this does not change the entry-point constructor.

Register host-owned objects with `AddSingleton(instance)` as shown. The module disposes its own `ServiceProvider` in `Shutdown`, but must not dispose the shared system, logger factory, or configuration supplied by ModSharp. Registering an existing instance does not transfer disposal ownership to the container.

[!code-csharp[DependencyInjection.cs](../../codes/dependency-injection.cs)]

For Sharp extensions, explicitly call `LoadAllSharpExtensions` before use and `ShutdownAllSharpExtensions` before disposing `ServiceProvider`; container disposal alone does not invoke `ISharpExtension.Shutdown`. See [Using Sharp Extensions](sharp-extensions.md).
