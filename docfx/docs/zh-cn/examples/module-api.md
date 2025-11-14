# Module Shared API

本教程将会教你怎么编写可以导出供其他模块使用的API。

> [!NOTE]
> 本项目示例名为`SharedInterface`。

首先，你需要至少2个项目来处理这个事情，本教程为演示，分别叫做`SharedInterface.Shared`、`SharedInterface`。

在编写API之前，先思考几个问题：

1. 你的东西是否有共享代码的需求？
   - ✔️：Extension / Module Shared API
   - ❌：直接不管

2. 你的东西是否有数据交互？
   - ✔️：Module Shared API
   - ❌：Extension

本文仅教学如何编写Module API。

在`SharedInterface.Shared`中，做如下定义

[!code-csharp[SharedInterface.Shared.cs](../../codes/module-api.cs)]

然后，你在`SharedInterface`中编写如下实现
[!code-csharp[SharedInterface.cs](../../codes/module-impl.cs)]

最后，在其他插件调用你所写好的API
[!code-csharp[UseSharedModule.cs](../../codes/module-use-api.cs)]
