# Command Manager

Install `ModSharp.Sharp.Extensions.CommandManager` alongside the [Hello World SDK reference](hello-world.md). Extensions are private library dependencies: keep their runtime assets and deploy them with your module.

| | Extension (`Sharp.Extensions.CommandManager`) | Module (`Sharp.Modules.CommandCenter`) |
|---|---|---|
| Integration | `services.AddCommandManager(sharedSystem)` | Resolve `ICommandCenter` through the module manager |
| Scope | One module's own DI container | A shared provider with registries owned by module identity |
| Lifecycle | `LoadAllSharpExtensions`, then `ShutdownAllSharpExtensions` and container disposal | ModSharp's module lifecycle |
| Restricted commands | Use [AdminManager](admin-example.md) | Use `IAdminManager.GetCommandRegistry` |

The extension's legacy `RegisterAdminCommand` is obsolete and scheduled for removal in 2.2. The example below deliberately avoids it. For permissions, use AdminManager rather than the old SDK admin flag system.

The example registers client command `.kill` / `ms_kill` and server command `ms_echo`. `ShutdownAllSharpExtensions` unregisters these callbacks; disposing `ServiceProvider` alone does not.

[!code-csharp[CommandManager.cs](../../codes/command-manager.cs)]
