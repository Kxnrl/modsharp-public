# Physics Query (Wrapper)

This tutorial will demonstrate how to use `PhysicsQueryManager` to perform Trace operations.

> [!TIP]
>
> To simplify the development process and reduce developer cognitive load, we provide a **Wrapper** version.  
> **Wrapper** has slightly lower performance than the **Native** version, and the encapsulation limits flexibility.  
> If you need high customization or ultimate performance, please use the **Native** version: [Documentation](./trace-native.md).
> [!Warning]
>
> The **Wrapper** version is based on reverse engineering results from November 2023, and some `Layers` may be outdated.  

[!code-csharp[TraceWrapperExample.cs](../../codes/trace-wrapper.cs)]
