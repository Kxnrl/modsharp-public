# NetMessage Hook

Use the [Hello World project settings](hello-world.md).

Register a `PostEventAbstract` pre-hook and call `HookNetMessage` for each message ID you want to inspect (this hook point only supports pre-hooks). Messages handled earlier in the engine (`UM_TextMsg`, which uses `IHookManager.TextMsg`, plus `GE_FireBulletsId`, `TE_MuzzleFlashId`, `TE_WorldDecalId`, `TE_BloodStreamId`, and `TE_ArmorRicochetId`) and messages sent via `IModSharp.SendNetMessage` bypass `PostEventAbstract`. Remove the pre-hook in `Shutdown`; do not call the obsolete `UnhookNetMessage`.

The example modifies Fade colors via deserialization and Shake amplitude via native protobuf reflection. Reflection uses the case-sensitive `.proto` field name `amplitude`, not the generated C# property `Amplitude`. Network messages are only valid during the callback.

If `previousResult.Action` is already `SkipCallReturnOverride`, return `default` (`Ignored`) without mutating the message. When filtering recipients, intersect with the incoming receiver mask rather than broadcasting to players excluded by the sender. Note a current core limitation: if an earlier (higher-priority) hook already returned `ChangeParamReturnDefault`, the core keeps that earlier hook's return value when another hook also returns `ChangeParamReturnDefault`, so a lower-priority hook cannot further narrow recipients in that case. Use `GetPlayerControllers` rather than iterating slots yourself and treating `MaxClients` as the last valid slot index.

[!code-csharp[net-message-hook.cs](../../codes/net-message-hook.cs)]
