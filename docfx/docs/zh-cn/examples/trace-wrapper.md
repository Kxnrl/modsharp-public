# Physics Query (Wrapper)

本教程将会演示如何使用 `PhysicsQueryManager` 进行 Trace 操作。

> [!TIP]
>
> 出于简化开发流程以及降低开发者心智负担, 我们提供了**Wrapper**版  
> **Wrapper**性能略微逊色于**Native**版本, 并且封装之后自由度受限.  
> 如果你需要高度自定义或极致性能, 请使用**Native**版本的API: [文档](./trace-native.md).
> [!Warning]
>
> **Wrapper**版本基于2023年11月逆向结果, 某些`Layers`可能已过时.  

[!code-csharp[TraceWrapperExample.cs](../../codes/trace-wrapper.cs)]
