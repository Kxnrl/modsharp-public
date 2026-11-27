# 实体监听器

请参照 [Hello World 的项目设置](hello-world.md)。

在 `Init` 中注册 `IEntityListener`，并在 `Shutdown` 中移除同一个实例。`HookEntityInput` 和 `HookEntityOutput` 用来指定哪些类名及输入/输出的组合需要由 ModSharp 派发实体 IO 回调；仅安装监听器并不会选定这些组合。

本例监听了实体的创建、生成、删除、跟随，以及 `func_door` 的 IO。返回 `Ignored` 表示不干预这次 IO 调用，返回 `SkipCallReturnOverride` 则会阻止该调用。实体被删除后，不要再使用此前保存的实体包装对象和回调参数。

[!code-csharp[entity-listener.cs](../../codes/entity-listener.cs)]
