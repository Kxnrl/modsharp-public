# Transmit Manager

本例会自动隐藏队友，同时保持敌人以及接收者自己的玩家可见，**不会**注册 `.hide` 指令。项目设置请参照 [Hello World 的项目设置](hello-world.md)。

应当 hook 玩家的 **controller**，而不是 pawn：原生 transmit manager 会拒绝直接 hook pawn。在 `SetEntityState` 中，`true` 表示发送（可见），`false` 表示阻止，通道 `-1` 表示修改所有通道。

重复触发的计时器负责处理已在线的玩家、后加入的玩家以及队伍变化，并在换图后移除已失效的实体包装对象。Transmit hook 是按实体全局共享的，而不是按模块隔离的：示例会跳过已被其他模块 hook 的实体，只跟踪自己创建的 hook，并在 `Shutdown` 中移除它们。如果多个模块需要修改同一个实体的 transmit 状态，必须协调 hook 的所有权。

[!code-csharp[Transmit.cs](../../codes/transmit.cs)]
