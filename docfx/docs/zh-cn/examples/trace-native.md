# 物理查询（Native）

请参照 [Hello World 的项目设置](hello-world.md)，并启用 `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`。不需要生成器包。请保留程序集上的 `DisableRuntimeMarshalling` 特性：本例会向原生代码传递一个返回值为原生布尔类型的非托管回调。

示例注册了客户端聊天指令 `.trace_line` 和 `.trace_custom`（对应的客户端控制台指令为 `ms_trace_line` 和 `ms_trace_custom`）。为指令添加任意参数即可启用过滤器，该过滤器只允许命中 CT 玩家 pawn 和非玩家几何体。`.trace_line` 演示直线射线，`.trace_custom` 演示使用自定义 `RnQueryShapeAttr` 标志的 hull；两者都会忽略调用者自己的 pawn。

Native 查询会直接暴露引擎的查询属性和命中实体的原始指针。命中几何体时，实体指针可能为空，因此在构造实体包装对象之前必须先检查。标有 `UnmanagedCallersOnly` 的回调必须是静态方法，且不能让托管异常越过原生边界。操作结束后，不要继续保存查询结果中的原生指针。

需要托管过滤器和现成的辅助方法时，请参阅[物理查询（Wrapper）](trace-wrapper.md)。请使用当前 SDK 的 `InteractionLayers` 和内置查询属性，不要照搬旧版 CS2 中的数值掩码。

[!code-csharp[TraceNative.cs](../../codes/trace-native.cs)]
