# Native Hooks

This tutorial will demonstrate how to use the HookManager to create a VMTHook and DetourHook.

> [!CAUTION]
> Hooking is a high-risk operation. Before you begin, ensure that the parameter **count** and **type** of your function perfectly match the original one in the game. Any mismatch can, and likely will, cause the server to crash!

## Complete Example

[!code-csharp[NativeHookExample.cs](../../codes/native-hook.cs)]
