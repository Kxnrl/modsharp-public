# Entity Hook Manager

Use the [Hello World project settings](hello-world.md).

Install `ModSharp.Sharp.Extensions.EntityHookManager` and deploy its runtime DLL with your module. Register `ISharedSystem` before `AddEntityHookManager()` or call the overload that accepts the shared system.

Call `LoadAllSharpExtensions` before registering entity listeners and IO hooks. `ShutdownAllSharpExtensions` removes the extension's registrations; then dispose your module-owned `ServiceProvider`. IO callbacks receive `ref EHookAction result`: preserve an earlier stronger action unless you intentionally need to change it.

[!code-csharp[entity-hook-manager.cs](../../codes/entity-hook-manager.cs)]
