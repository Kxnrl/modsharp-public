# Hello World

该示例演示了如何编写最简单的模块。

首先创建如下项目

[!code-csharp[HelloWorld.xml](../../codes/hello-world.xml)]

> [!WARNING]
>
> 1. `ModSharp.Sharp.Shared`必须加上``PrivateAssets="all"``标签，因为我们需要防止你在Publish时自动复制`Sharp.Shared.dll`和其与之相关的所有dll。  
> 2. `AssemblyName`为必需字段，否则会出现依赖加载不上的情况。该项的字段就和项目同名即可，我们的项目名是**Example**，所以该项填入的是`Example`，你实际开发的时候自行调整即可。

编写模块

[!code-csharp[HelloWorld.cs](../../codes/hello-world.cs)]
