# Native Hooks

本教程将演示如何使用 Hookmanager 来进行 VMT hook 以及 Detour hook。

> [!CAUTION]
> Hook为高危操作，在使用前请确保函数的参数**数量**以及**类型**与游戏内一致，一旦出现任意形式的问题都可能会导致服务器崩溃！

> [!NOTE]
> 为了性能最大化，我们建议在 ``.csproj`` 文件中添加 ``<DisableRuntimeMarshalling>true</DisableRuntimeMarshalling>`` 禁止 RuntimeMarshalling。如需了解更多请访问 https://learn.microsoft.com/zh-cn/dotnet/standard/native-interop/disabled-marshalling
>
> 禁用后，使用 DllImport 与 unmanaged 代码交互时，像 ``string`` 这样的 managed 类型将不再被自动转换，需要手动将其处理为 ``byte*`` 等 unmanaged 类型。
>
> 如果选择不禁用此功能，必须确保 Hook 方法的签名（包括返回值和参数类型）与目标函数的定义完全匹配，并使用 ``[MarshalAs(UnmanagedType.Type)]`` 来指定对应的 unmanaged 类型，如下方代码所示：
```cs
[return: MarshalAs(UnmanagedType.I1)]
private static unsafe bool Method([MarshalAs(UnmanagedType.I1)] bool a1, int a2)
{

}
```

## 完整例子

[!code-csharp[NativeHookExample.cs](../../codes/native-hook.cs)]
