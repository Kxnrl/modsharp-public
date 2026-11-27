# Game Listener

Use the [Hello World project settings](hello-world.md).

Register `IGameListener` with `IModSharp.InstallGameListener` and remove it in `Shutdown`. Use `OnServerInit` for globals, `OnGameInit` for GameRules, and `OnServerSpawn` when executing server configuration. `GetGameRules` is not usable in `OnGameShutdown`.

Precache resources only inside `OnResourcePrecache` (the sample precaches the game's built-in defuser model). If a module loads after a map's precache phase has passed, wait for the next precache cycle. `ConsoleSay` intercepts both server-console `say` and script chat broadcasts (`ScriptPrintMessageChatAll`); only `Stopped` blocks the original flow.

[!code-csharp[game-listener.cs](../../codes/game-listener.cs)]
