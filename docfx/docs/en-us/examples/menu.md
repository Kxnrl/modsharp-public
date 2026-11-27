# Menu

Reference `ModSharp.Sharp.Modules.MenuManager.Shared` as a host-provided contract (`ExcludeAssets="runtime"`) and install `Sharp.Modules.MenuManager` on the server along with its required runtime dependency `Sharp.Modules.LocalizerManager`. A reference to the Shared package alone does not load the menu provider. Use the [Hello World project settings](hello-world.md). The current MenuManager.Shared project itself uses C# 14; this consumer example uses C# 13.

This example opens a menu for human players on spawn. Run `menu_type_toggle` in the server console to switch between a cached definition and a dynamically built menu. It demonstrates submenus, disabled items, title factories, item generators, `Refresh`, `Next`, `BackItem` and `ExitItem`.

- Cache menu definitions, not native pawn wrappers. Resolve the player's current pawn again when an item is selected.
- `OnEnter` runs on initial entry or forward navigation, not when returning from a child menu via `GoBack`. `OnExit` runs on `GoBack` and when the session closes, not when navigating forward to a submenu via `Next`.
- Titles and item text are embedded in HTML. Do not insert unescaped player-controlled markup.
- The timeout callback checks both the provider wrapper and the session ID, so an old timer cannot close a replacement session. `QuitMenu` throws `InvalidOperationException` if the client has no active menu, and it schedules menu disposal asynchronously (~0.01s later). In `Shutdown`, check `IsInMenu` and close only the sessions owned by this module so MenuManager releases references to this module's menu delegates before unload.

[!code-csharp[Menu.cs](../../codes/menu.cs)]
