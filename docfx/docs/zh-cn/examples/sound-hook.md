# 音效 Hook

请参照 [Hello World 的项目设置](hello-world.md)。

`EmitSound` 和 `SoundEvent` 要分别挂 pre/post 回调：不是所有音效都走 `EmitSound`（通过 `EmitSound` 发出的音效不会再进 `SoundEvent`，而地图音乐会分流到 `EmitMusic`）。卸载时把注册的回调全移除掉。

示例演示了拦掉受害者爆头音效、替换旁观者音效并改接收者掩码，以及在 post 回调里修改参数并记日志。如果前面已有 hook 返回 `SkipCallReturnOverride`，应直接返回 `default` 且不要修改参数。对于 `EmitSound` 和 `SoundEvent`，只要对应钩子的 pre 回调返回 `SkipCallReturnOverride`，原调用和该钩子的 post 回调都会跳过（示例里 post 回调中的判断仅为防御性检查）。改了参数返回 `ChangeParamReturnDefault`，不改动返回 `Ignored`。

[!code-csharp[sound-hook.cs](../../codes/sound-hook.cs)]
