# Steam 监听器

请参照 [Hello World 的项目设置](hello-world.md)。

实现 `ISteamListener`，以 `ISteamListener.ApiVersion`（当前为 2）作为版本号，通过 `InstallSteamListener` 注册，并在 `Shutdown` 中通过 `RemoveSteamListener` 移除同一个实例。

本例记录 Steam 服务器连接状态、群组状态响应以及创意工坊下载/安装通知（注意 Steam 服务器连接回调和游戏协调器 GC 回调不是一回事）。`OnGroupStatusResult` 对应 `RequestUserGroupStatus` 请求的结果。

[!code-csharp[steam-listener.cs](../../codes/steam-listener.cs)]
