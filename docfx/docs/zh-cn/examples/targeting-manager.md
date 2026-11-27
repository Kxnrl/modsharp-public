# 自定义目标解析器

引用 `ModSharp.Sharp.Modules.TargetingManager.Shared`（加 `ExcludeAssets="runtime"`），服务端装好 `Sharp.Modules.TargetingManager`。项目设置见 [Hello World](hello-world.md)。

示例注册了 **`@example_aim`** 来选中准星指着的存活玩家（TargetingManager 已经内置了 `@aim`，同名再注册会失败）。控制台触发或者射线没打中存活玩家时返回空结果。

`RnQueryShapeAttr.Bullets()` 提供原生射线属性，`SetEntityToIgnore(pawn, 0)` 防止打到自己身上。解析器会先确认命中的实体确实是 pawn 再做转换。

Shared 接口是编译期引用，提供方模块在运行时可选：在 `OnAllModulesLoaded` 和 `OnLibraryConnected` 里获取接口包装，断开时清掉引用。注册时用真实的程序集名当 owner，这样模块卸载时 TargetingManager 会自动清掉对应的解析器；提供方热重载后重新获取并再注册一次即可。

[!code-csharp[TargetingManager.cs](../../codes/targeting-manager.cs)]
