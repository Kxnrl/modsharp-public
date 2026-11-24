# Native Hooks

This example installs a VMT hook on `CCSPlayerPawn`'s vtable using the built-in `CBaseEntity::CollisionRulesChanged` GameData key, and a detour using the built-in `CreateEntityByName` key, logging calls while preserving the original function and hook chain.

Use the [Hello World project settings](hello-world.md) and enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`. No source generator package is required for raw hooks. The sample imports `System.Runtime.CompilerServices` for `[assembly: DisableRuntimeMarshalling]` and `System.Runtime.InteropServices` for `[UnmanagedCallersOnly]`.

> [!CAUTION]
> Native calls must match the target platform's ABI (parameter count and types, return type, and calling convention) or the server will crash. Disabling runtime marshalling will not mask a wrong signature, and managed exceptions must never escape an unmanaged callback. See [.NET disabled runtime marshalling](https://learn.microsoft.com/dotnet/standard/native-interop/disabled-marshalling).

`Prepare` selects a target, `Install` activates the hook, and `Trampoline` supplies the next function in the chain. Keep callbacks static when using `UnmanagedCallersOnly`. Call the trampoline on every path unless intentionally blocking the native behavior **and** all downstream hooks.

Uninstall hooks in reverse installation order. Detours additionally require `Dispose`; `IVirtualHook` does not implement `IDisposable`. ModSharp calls `Shutdown` after an unsuccessful `Init`, so cleanup must handle partial installation. Multiple modules hooking the same target must coordinate their unload order.

[!code-csharp[NativeHook.cs](../../codes/native-hook.cs)]

See [GameData](../features/gamedata.md) for address configuration and [native libraries](../features/library-modules.md) for `ILibraryModule` lookup and memory-ownership rules.
