# Client Preferences

Reference `ModSharp.Sharp.Modules.ClientPreferences.Shared` as a host-provided contract (`ExcludeAssets="runtime"`) and install/configure `Sharp.Modules.ClientPreferences` on the server. Start with the [Hello World project settings](hello-world.md).

The example stores a player's spawn greeting. Use `.greeting "Welcome back!"` in chat or `ms_greeting "Welcome back!"` in the client console; the quotes keep the greeting as one argument. The stored value is printed on subsequent spawns. Unlike a model-changing example, it needs no custom models or resource precaching.

Read or write cookies only after `IsLoaded(client.SteamId)` returns true. `ListenOnLoad` returns an `IDisposable`: keep one subscription, dispose it on provider disconnect and module shutdown, and subscribe again for a replacement provider. After a consumer hot reload, enumerate the clients whose data is already loaded, because earlier load notifications are not replayed. Lifecycle callback names use the full provider assembly name, `Sharp.Modules.ClientPreferences`.

[!code-csharp[ClientPreferences.cs](../../codes/client-preferences.cs)]
