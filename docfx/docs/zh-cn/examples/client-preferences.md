# Client Preferences

引用 `ModSharp.Sharp.Modules.ClientPreferences.Shared`（加 `ExcludeAssets="runtime"`），服务端装好并配好 `Sharp.Modules.ClientPreferences`。项目设置见 [Hello World](hello-world.md)。

示例保存玩家出生时的自定义问候语：聊天输 `.greeting "Welcome back!"` 或客户端控制台输 `ms_greeting "Welcome back!"`（加引号让整句话当成一个参数），以后每次出生都会打印出来。

只有在 `IsLoaded(client.SteamId)` 返回 `true` 之后才能读写 cookie。`ListenOnLoad` 返回 `IDisposable`，只留一份订阅并在提供方断开或模块卸载时 `Dispose`，提供方重连后再重新订阅。另外调用方模块热重载后不会补发已经在线玩家的加载事件，所以需要主动扫一遍已加载完的玩家。生命周期回调里用的名字是提供方的完整程序集名 `Sharp.Modules.ClientPreferences`。

[!code-csharp[ClientPreferences.cs](../../codes/client-preferences.cs)]
