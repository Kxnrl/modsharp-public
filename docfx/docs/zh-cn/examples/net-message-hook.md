# 网络消息 Hook

请参照 [Hello World 的项目设置](hello-world.md)。

注册一个 `PostEventAbstract` pre-hook（该钩子点只支持 pre-hook），再对每个要拦截的消息 ID 调用 `HookNetMessage`。注意原生层更早处理掉的消息（`UM_TextMsg` 走 `IHookManager.TextMsg`，以及 `GE_FireBulletsId`、`TE_MuzzleFlashId`、`TE_WorldDecalId`、`TE_BloodStreamId`、`TE_ArmorRicochetId`）和通过 `IModSharp.SendNetMessage` 发出的消息都不会经过 `PostEventAbstract`。`Shutdown` 时移除该 pre-hook 即可，别调已废弃的 `UnhookNetMessage`。

本例通过反序列化修改 Fade 的颜色，并通过原生 protobuf 反射修改 Shake 的振幅。反射使用区分大小写的 `.proto` 字段名 `amplitude`，而不是生成的 C# 属性名 `Amplitude`。网络消息仅在回调期间有效。

如果 `previousResult.Action` 已经是 `SkipCallReturnOverride`，应直接返回 `default`（`Ignored`）且不要修改消息内容。修改接收者时，请与传入的接收者掩码取交集，避免向发送方已经排除的玩家广播。注意当前核心的一条限制：如果更早（更高优先级）的 hook 已经返回 `ChangeParamReturnDefault`，后续 hook 同样返回 `ChangeParamReturnDefault` 时，核心会保留更早 hook 的返回值，因此低优先级 hook 此时无法再进一步缩减接收者。请使用 `GetPlayerControllers`，不要自己遍历 slot 并把 `MaxClients` 当作最后一个有效的 slot 索引。

[!code-csharp[net-message-hook.cs](../../codes/net-message-hook.cs)]
