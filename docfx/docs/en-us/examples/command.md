# Commands

Use the [Hello World project settings](hello-world.md).

`CreateServerCommand` registers a server-console command under the exact name passed (`ms_echo` here). `InstallCommandCallback("hello", ...)` registers `ms_hello` in the client console, along with the `.hello`, `/hello`, and `!hello` chat triggers (depending on the server's trigger configuration).

`StringCommand.ArgCount` counts only arguments, excluding the command name itself; `GetArg(1)` is the first argument, so check `ArgCount` before indexing. Return `Stopped` once a command has been handled. In `Shutdown`, release the server callback with `ReleaseServerCommandCallback` and remove the client callback using the exact names and delegates passed during registration.

[!code-csharp[command.cs](../../codes/command.cs)]
