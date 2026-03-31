# Command Manager

This tutorial demonstrates how to use the CommandManager extension.

## Extension vs Module

ModSharp provides two ways to register commands, and it is important to understand the difference:

| | Extension (`Sharp.Extensions.CommandManager`) | Module (`Sharp.Modules.CommandCenter`) |
|---|---|---|
| | Extension (`Sharp.Extensions.CommandManager`) | Module (`Sharp.Modules.CommandCenter`) |
|---|---|---|
| **Use case** | Monorepo — all features in a single module | Multi-module — multiple modules share the command system |
| **Installation** | Via NuGet package | Loaded by the ModSharp module system |
| **Integration** | DI-based (`IServiceCollection.AddCommandManager()`) | Obtained via `ISharedSystem.GetSharpModuleManager()` |
| **Admin commands** | Built-in `RegisterAdminCommand` (deprecated, use AdminManager instead) | Use `AdminManager.GetCommandRegistry()` for permission-checked commands |
| **Lifecycle** | Managed by the plugin's own `IServiceProvider` | Managed by ModSharp's module lifecycle |

> [!TIP]
> If your project is a **monorepo** (single module handles everything), use the **Extension** (`Sharp.Extensions.CommandManager`).
> If your project consists of **multiple modules** that need to share the command system, use the **Module** (`Sharp.Modules.CommandCenter`).

## Example

[!code-csharp[CommandManagerExample.cs](../../codes/command-manager.cs)]
