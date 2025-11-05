# Native Hooks

This tutorial will demonstrate how to use the HookManager to create a VMTHook and DetourHook.

> [!CAUTION]
> Hooking is a high-risk operation. Before you begin, ensure that the parameter **count** and **type** of your function perfectly match the original one in the game. Any mismatch can, and likely will, cause the server to crash!

> [!NOTE]
> To maximize performance, we recommend disabling RuntimeMarshalling by adding `<DisableRuntimeMarshalling>true</DisableRuntimeMarshalling>` to the `.csproj` file. To learn more, please visit: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/disabled-marshalling
>
> After disabling it, when using DllImport to interact with unmanaged code, managed types like `string` will no longer be automatically converted and need to be manually handled as unmanaged types like `byte*`.
>
> If you choose not to disable this feature, you must ensure that the hook method's signature (including return value and parameter types) perfectly matches the target function's definition, and use `[MarshalAs(UnmanagedType.Type)]` to specify the corresponding unmanaged type, as shown in the code below:
```cs
[return: MarshalAs(UnmanagedType.I1)]
private static unsafe bool Method([MarshalAs(UnmanagedType.I1)] bool a1, int a2)
{

}
```

## Complete Example

[!code-csharp[NativeHookExample.cs](../../codes/native-hook.cs)]
