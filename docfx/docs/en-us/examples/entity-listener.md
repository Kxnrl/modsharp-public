# Entity Listener

Use the [Hello World project settings](hello-world.md).

Register `IEntityListener` in `Init` and remove the same instance in `Shutdown`. `HookEntityInput` and `HookEntityOutput` select the class/input/output combinations for which ModSharp should dispatch entity IO callbacks; installing a listener alone does not select those combinations.

The example observes creation, spawn, deletion, following, and `func_door` IO. Return `Ignored` to leave an IO call alone, or `SkipCallReturnOverride` to block it. Do not retain entity wrappers or callback arguments after an entity is deleted.

[!code-csharp[entity-listener.cs](../../codes/entity-listener.cs)]
