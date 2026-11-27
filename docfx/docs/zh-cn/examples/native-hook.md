# Native Hooks

本例使用内置 GameData 键 `CBaseEntity::CollisionRulesChanged` 为 `CCSPlayerPawn` 的虚表安装 VMT hook，并使用内置键 `CreateEntityByName` 安装 detour，记录每次调用并保留原函数和 hook 调用链。

请参照 [Hello World 的项目设置](hello-world.md)，并启用 `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`。原始 hook 不需要源生成器包。示例引入 `System.Runtime.CompilerServices` 以使用 `[assembly: DisableRuntimeMarshalling]`，引入 `System.Runtime.InteropServices` 以使用 `[UnmanagedCallersOnly]`。

> [!CAUTION]
> 原生调用的参数数量、类型、返回值和调用约定必须和目标平台 ABI 完全一致，否则会直接崩服。关闭运行时封送不会修正错误签名，托管异常也绝对不能穿出非托管回调。参见 [.NET 禁用运行时封送](https://learn.microsoft.com/dotnet/standard/native-interop/disabled-marshalling)。

`Prepare` 选定目标，`Install` 启用 hook，`Trampoline` 则提供调用链中的下一个函数。使用 `UnmanagedCallersOnly` 时，回调必须是静态方法。除非有意同时阻止原生行为**以及所有后续 hook**，否则每条执行路径上都应调用 trampoline。

请按安装顺序的逆序卸载 hook。detour 还需要额外调用 `Dispose`，而 `IVirtualHook` 没有实现 `IDisposable`。`Init` 失败后，ModSharp 同样会调用 `Shutdown`，因此清理逻辑必须能处理只安装了一部分 hook 的情况。多个模块 hook 同一个目标时，需要协调各自的卸载顺序。

[!code-csharp[NativeHook.cs](../../codes/native-hook.cs)]

地址配置见 [GameData](../features/gamedata.md)；`ILibraryModule` 的查找方式和内存所有权规则见[原生库访问](../features/library-modules.md)。
