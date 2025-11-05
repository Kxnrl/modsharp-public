# Hook manager

本教程将演示如何使用 Hookmanager 来进行 VMT hook 或者 Detour hook。

> [!CAUTION]
> Hook为高危操作，在使用前请确保函数的参数**数量**以及**类型**与游戏内一致，一旦出现任意形式的问题都可能会导致服务器崩溃！

# 完整例子

[!code-csharp[NativeHookExample.cs](../../codes/native-hook.cs)]
