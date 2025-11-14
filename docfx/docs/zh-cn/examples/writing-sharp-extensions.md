# 编写 ModSharp 扩展包

> [!WARNING]
> 此项为高危操作，你需要对你所编写的内容足够了解！

和 [模块API](./module-api.md) 所提问的问题一致：

1. 你的东西是否有共享代码的需求？
   - ✔️：Extension / Module Shared API
   - ❌：直接不管

2. 你的东西是否有数据交互？
   - ✔️：Module Shared API
   - ❌：Extension

如果非常确定不会出现问题，那么你就可以写扩展包。

本教程将会教你怎么写一个扩展包。

首先，定义API：
[!code-csharp[SharpExtensionApi.cs](../../codes/sharp-extension-interface.cs)]

然后你只需要这么编写：
[!code-csharp[SharpExtension.cs](../../codes/sharp-extension-example.cs)]

最后，你需要编写DI：
[!code-csharp[SharpExtensionDi.cs](../../codes/sharp-extension-di.cs)]
