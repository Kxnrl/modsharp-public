# 菜单

引用 `ModSharp.Sharp.Modules.MenuManager.Shared`（加 `ExcludeAssets="runtime"`），服务端要同时装好 `Sharp.Modules.MenuManager` 和它依赖的 `Sharp.Modules.LocalizerManager`。项目设置见 [Hello World](hello-world.md)（`MenuManager.Shared` 本身用 C# 14 编译，调用方模块用 C# 13 即可）。

本例在真人玩家出生时弹出菜单，在服务端控制台输入 `menu_type_toggle` 可以在缓存菜单和动态菜单之间切换，演示了子菜单、禁用项、动态标题/条目以及 `Refresh`、`Next`、`BackItem` 和 `ExitItem`。

- 菜单定义可以缓存，但不要缓存玩家 pawn 包装对象，选条目时再重新拿玩家当前的 pawn。
- `OnEnter` 只在首次打开或点进子菜单时触发（`GoBack` 返回时不触发）；`OnExit` 在 `GoBack` 或菜单关闭时触发（`Next` 进子菜单时不触发）。
- 标题和条目文本直接拼进 HTML，别直接塞未经转义的玩家输入。
- 超时回调同时校验提供方包装和会话 ID，避免旧定时器误关新会话。另外客户端当前没开菜单时调 `QuitMenu` 会抛 `InvalidOperationException`，且菜单释放有约 0.01 秒的异步延迟；因此 `Shutdown` 会先查 `IsInMenu`，只关自己开的会话，让 MenuManager 在本模块卸载前释放对菜单委托的引用。

[!code-csharp[Menu.cs](../../codes/menu.cs)]
