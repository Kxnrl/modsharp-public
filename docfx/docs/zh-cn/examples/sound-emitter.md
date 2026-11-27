# 音效播放

请参照 [Hello World 的项目设置](hello-world.md)。

在聊天中输入 `.hello Player.DeathHeadShot`，或在客户端控制台输入 `ms_hello Player.DeathHeadShot` 即可运行音效演示；不传参数时，同样使用这个内置音效事件。播放自定义事件名称之前，请先调用 `IsSoundEventValid` 检查；对应的音效资源必须存在，并且服务端和客户端均可使用。

本例展示了获取音效时长、仅向指定客户端播放、以实体/队伍/全局为范围播放、修改音效参数，以及通过返回的 `SoundOpEventGuid` 停止音效。这些发送方式本是可互相替代的做法，这里放在一起演示，因此会刻意播放多份音效。延迟执行的参数修改和停止操作只捕获音效 GUID 和管理器，不捕获玩家实体。模块创建的计时器会在所属模块卸载时自动移除，指令回调则需要在 `Shutdown` 中显式移除。

[!code-csharp[sound-emitter.cs](../../codes/sound-emitter.cs)]
