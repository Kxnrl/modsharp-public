# 编写 ModSharp 扩展包

> [!CAUTION]
> 此项为高危操作，你需要对你所编写的内容足够了解才能写，否则很容易搞炸你的服务器！

和 [模块API](./module-api.md) 所提问的问题一致：

1. 你的东西需不需要跨项目使用？
   - 需要：Extension/Shared
   - 不需要：直接不管

2. 你的东西是否有共享内存的需求（例如：A插件注册了某个内容，导致了B不能注册）？
   - 有：Module API
   - 没有：Extension

如果非常确定不会出现问题，那么你就可以写扩展包。

本教程将会教你怎么写一个扩展包。

首先，定义API：
[!code-csharp[SharpExtensionApi.cs](../../codes/sharp-extension-interface.cs)]

然后你只需要这么编写：
[!code-csharp[SharpExtension.cs](../../codes/sharp-extension-example.cs)]

最后，你需要编写DI：
[!code-csharp[SharpExtensionDi.cs](../../codes/sharp-extension-di.cs)]
