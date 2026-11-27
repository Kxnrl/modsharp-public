# Entity Hook Manager

请参照 [Hello World 的项目设置](hello-world.md)。

安装 `ModSharp.Sharp.Extensions.EntityHookManager`，并将其运行时 DLL 随模块一起部署。请先注册 `ISharedSystem`，再调用 `AddEntityHookManager()`；也可以直接使用接收共享系统参数的重载。

先调用 `LoadAllSharpExtensions`，再注册实体监听器和 IO hook。`ShutdownAllSharpExtensions` 会移除该扩展的全部注册项，之后再释放模块自有的容器。IO 回调通过 `ref EHookAction result` 传递结果：除非确实需要改变，否则应保留此前已设定的、作用更强的动作。

[!code-csharp[entity-hook-manager.cs](../../codes/entity-hook-manager.cs)]
