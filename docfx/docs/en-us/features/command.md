# Command

## Server Commands

Registration and release:

- `IConVarManager.CreateServerCommand`: Create a command executable only from the server console
- `IConVarManager.ReleaseCommand`: Release

> [!IMPORTANT]
> You must release when your module is unloaded, otherwise unexpected errors may occur.

## Server/Client Commands

Registration and release:

- `IConVarManager.CreateConsoleCommand`: Create a command executable from both server console and client console
- `IConVarManager.ReleaseCommand`: Release

> [!IMPORTANT]
> You must release when your module is unloaded, otherwise unexpected errors may occur.

## Client Virtual Commands

Virtual commands support both:

- Client console
- Client chat box

Registration and release:

- `IClientManager.InstallCommandCallback`: Create a virtual command that automatically includes the `ms_` prefix
- `IClientManager.RemoveCommandCallback`: Release

> [!IMPORTANT]
> You must release when your module is unloaded, otherwise unexpected errors may occur.

## Listen to Client Commands

Similar to SourceMod, we provide client command listening functionality,
to intercept and modify specific client commands.

- `IClientManager.InstallCommandListener`: Install command listener
- `IClientManager.RemoveCommandListener`: Remove command listener

> [!IMPORTANT]
> You must release when your module is unloaded, otherwise unexpected errors may occur.

## Examples

Please refer to this example to learn how to create commands: [Jump Link](../examples/command.md)
