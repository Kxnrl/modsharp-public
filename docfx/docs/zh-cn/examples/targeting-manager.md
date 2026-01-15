# 自定义目标解析器

本示例展示了如何为 Targeting Manager 实现一个自定义的目标解析器（具体演示了 `@aim` 功能）。

该示例涵盖了两个高级概念：
1. **射线检测 (Raycasting):** 如何从玩家的视角位置投射射线以查找目标实体。
2. **软依赖 (Soft Dependencies):** 如何仅在 Targeting Manager 已安装的情况下与之交互，并在其缺失时避免插件崩溃。

## 核心概念

### 1. 射线检测逻辑
为了判断玩家当前正在瞄准谁，我们需要执行一次RayCast。

> [!NOTE]
> **关键点：** 必须设置 `attr.SetEntityToIgnore(pawn, 0)`，以确保射线检测忽略发起者自身，否则射线会直接打在自己身上。

### 2. 模块交互
我们通过 `ITargetingManager.Identity` 来获取其接口实例，同时利用 `OnLibraryConnected` 事件来处理 Targeting Manager 在本模块之后才加载的情况（例如重载）。

## 完整源代码

[!code-csharp[TargetingManager.cs](../../codes/targeting-manager.cs)]