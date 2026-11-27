# 物理查询（Wrapper）

请参照 [Hello World 的项目设置](hello-world.md)。托管包装提供直线和 hull 查询的辅助方法、需要忽略的实体参数，以及托管过滤器委托。如需原始的引擎属性和非托管过滤器，请参阅[物理查询（Native）](trace-native.md)。

本例每 0.1 秒执行一次。将 `ms_show_aim_target_type` 设为 `0`（全部）、`1`（队友）或 `2`（敌人）。过滤器会排除调用者自己，但保留固体几何体，因此不会刻意穿墙检测。屏幕上显示的是**目标**的生命值。示例还演示了忽略所有玩家的瞄准位置查询，以及在不杀死玩家的前提下，报告其 hull 与固体几何体相交。

请使用当前的 `InteractionLayers` 和 `UsefulInteractionLayers` 常量，不要沿用旧版的数值掩码。`PushTimer` 创建的计时器会依据回调所属的程序集加载上下文，在其所属模块卸载时自动移除；因此回调必须属于你自己的模块，而不能是生命周期更长的共享程序集中的方法。

[!code-csharp[TraceWrapper.cs](../../codes/trace-wrapper.cs)]
