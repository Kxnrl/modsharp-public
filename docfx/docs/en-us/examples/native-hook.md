# Native Hooks

This tutorial will demonstrate how to use the HookManager to create a VMTHook and DetourHook.

> [!CAUTION]
> Hook is a high-risk operation. You **MUST** ensure the **number** and **type** of the function's parameters are consistent with those in the game. Any form of issue may cause your server crash!
>
> [!WARNING]
> For better performance and code consistency, we recommend to disabe `RuntimeMarshalling`. For more information, please visit the [documentation](https://learn.microsoft.com/dotnet/standard/native-interop/disabled-marshalling)
>
> - After disabling this feature, when using `DllImport` to interact with **unmanaged** code, **managed** types like `string` will no longer be automatically converted and need to be manually handled as **unmanaged** types like `byte*`.
>
> - If you do not disable this feature, you must ensure that the Hook method's signature (including return value and parameter types) perfectly matches the target function's definition, and use `[MarshalAs(UnmanagedType.Type)]` to specify the corresponding **unmanaged** type, as shown in the code below:
>
> ```cs
> [return: MarshalAs(UnmanagedType.I1)]
> private static unsafe bool Method([MarshalAs(UnmanagedType.I1)] bool argBool, byte* argPointer, int argInt32, long argInt64)
> {
> 
> }
> ```

## Complete Example

[!code-csharp[NativeHookExample.cs](../../codes/native-hook.cs)]
