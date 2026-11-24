# Panorama Custom HUD

`ISharedSystem.GetPanoramaManager()` controls CS2's `custom_hud_layout` entities. Your server DLL cannot ship client UI files on its own; getting a HUD on screen takes three parts:

1. Author and **compile** the Panorama XML layout and CSS stylesheet with CS2 Workshop Tools.
2. Package, distribute, and mount those compiled assets on both the server and every client (typically bundled inside a Workshop map).
3. Spawn the server entity from your module and update its networked state as needed.

A successful `CreateLayout` only means the server entity spawned. It does not check client files, download addons, mount VPKs, or report whether the client rendered anything.

## Supported features

Per Valve's [custom-HUD release notes](https://store.steampowered.com/news/app/730/view/717913919982667304), custom HUDs support `Panel`, `Label`, `Image`, `Button`, and CSS styling, with **no ordinary Panorama XML events and no client-side scripting**. Skip `onactivate`, JavaScript includes, and Dota-style custom UI manifests. Server logic here is handled in C# (server-side `cs_script` is a separate system).

| ModSharp API | Purpose |
| --- | --- |
| `CreateLayout(resource, targetName)` | Spawns and tracks a `custom_hud_layout` (`targetName` defaults to `"ms_custom_hud_layout"`). Throws on blank resource paths; returns `null` if spawning fails. |
| `GetManagedLayouts()` | Enumerates all valid manager-tracked `custom_hud_layout` entities across all modules. |
| `Layout` / `Observable` (`ICustomHudLayout`) | Reads the layout resource path (`Layout`) and gets/sets spectator visibility (`Observable`). |
| `SetClassOverride` / `SetClassOverrideForPlayer` | Toggles a **predefined CSS class** on a panel globally or for one player. Does not upload CSS or set arbitrary inline styles. |
| `SetDialogVariableString` / `SetDialogVariableStringForPlayer` | Sets a panel's string dialog variable globally or per player, referenced in Labels via `{s:variable_name}`. |
| `ClearDialogVariableStringForPlayer` | Clears a player's string override so they inherit the global value again. |
| `SetInputCaptureEnabled` / `IsInputCaptureEnabled` | Sets or reads the server's input-capture flag for one `(layout, player)` pair. |
| `InstallClickCallback` / `RemoveClickCallback` | Attaches or removes a click callback for one manager-created layout. |
| `InstallClickListener` / `RemoveClickListener` | Attaches or removes a global click listener covering all manager-tracked layouts. |

`HudPanelClassStatus.ForceEnable` adds the class, `ForceDisable` removes it, and `Undefined` clears the override (using CSS rules already compiled into your stylesheet). Per-player overloads accept a `PlayerSlot`, `IGameClient`, or `IPlayerController`.

`Observable` only controls whether spectators see the HUD. For per-player visibility, put a hidden class on a child panel by default and remove it only for the target player. Keep in mind this is purely visual hiding: the entity may still replicate to other clients, so never put secret data in a shared layout.

Input capture lets the client click buttons on this HUD (exact interaction with other layouts, movement, and cursor restoration is up to the client). Always disable capture when closing the UI: disabling capture does not hide the panel, and hiding the panel does not release capture.

## Resource names and source files

This walkthrough uses an addon named `modsharp_docs`. Keep these paths and casing exact:

| Purpose | Path |
| --- | --- |
| Layout source | `content/csgo_addons/modsharp_docs/panorama/layout/custom_game/modsharp_docs/panorama-layout.xml` |
| Style source | `content/csgo_addons/modsharp_docs/panorama/styles/custom_game/modsharp_docs/panorama-style.css` |
| Compiled layout | `game/csgo_addons/modsharp_docs/panorama/layout/custom_game/modsharp_docs/panorama-layout.vxml_c` |
| Compiled style | `game/csgo_addons/modsharp_docs/panorama/styles/custom_game/modsharp_docs/panorama-style.vcss_c` |
| Logical path for `CreateLayout` | `panorama/layout/custom_game/modsharp_docs/panorama-layout.vxml` |

Pass a logical path inside the mounted resource namespace to `CreateLayout`, with no `content/`, `game/csgo_addons/modsharp_docs/`, `file://`, or compiled `_c` suffix.

Inside XML, `file://{resources}/styles/...` maps to `panorama/styles/...` and references the CSS source resource; the compiler resolves the compiled dependency automatically. Never rename raw XML/CSS files to `.vxml_c` or `.vcss_c`.

Download the [layout source](../../codes/panorama-layout.xml) and [style source](../../codes/panorama-style.css). The root panel carries only a class (no `id`), while the dialog and button underneath have IDs the server addresses:

[!code-xml[panorama-layout.xml](../../codes/panorama-layout.xml)]

[!code-css[panorama-style.css](../../codes/panorama-style.css)]

The button only needs an `id`: CS2's custom-HUD click message forwards that ID straight to the server callback with no client script. Following Valve's welcome-HUD sample, the dialog starts with the `Dismissed` class (zero opacity). Opening uses `ForceDisable` to remove that class for the requesting player and turns on input capture; clicking Acknowledge clears that player's override with `Undefined` (restoring the XML default `Dismissed` class) and releases capture.

If you add an `Image` later, bundle the local image asset in the mounted addon as well (`panorama/images/custom_game` is included in current packaging). Neither `CreateLayout` nor `SetClassOverride` transfers images.

## Compile, package, and ship

You will need CS2 with the **Counter-Strike 2 Workshop Tools** DLC, a Steam account that can publish Workshop items, and a playable map addon. Bundling the HUD directly into the map addon avoids needing a separate resource-mounting plugin. See Valve's [CS2 Workshop Tools](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools), [official tools list](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Official_Tools), and [map compilation workflow](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Level_Design/Compiling).

1. Create or open `modsharp_docs` in the Workshop Tools addon launcher and keep your playable map there. If you integrate into an existing addon, swap its name in the filesystem paths below; the logical `panorama/.../modsharp_docs/...` namespace can stay as is.
2. Save the two source files at the source paths in the table above (not in `game/sharp/modules`).
3. Compile both files with CS2's `resourcecompiler.exe`. In PowerShell, adjust `$CS2` to your install path and run:

   ```powershell
   $CS2 = 'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive'
   $Content = "$CS2/content/csgo_addons/modsharp_docs"
   $Compiler = "$CS2/game/bin/win64/resourcecompiler.exe"

   & $Compiler -game "$CS2/game/csgo" -nop4 -i "$Content/panorama/styles/custom_game/modsharp_docs/panorama-style.css"
   if ($LASTEXITCODE -ne 0) { throw 'Panorama style compilation failed.' }
   & $Compiler -game "$CS2/game/csgo" -nop4 -i "$Content/panorama/layout/custom_game/modsharp_docs/panorama-layout.xml"
   if ($LASTEXITCODE -ne 0) { throw 'Panorama layout compilation failed.' }
   ```

4. Check the compiler output and confirm both `_c` files exist (`game/bin/assettypes_common.txt` maps CSS/XML to `CompilePanorama` `vcss`/`vxml` outputs).
5. Compile the map in Hammer (`F9`, full compile), then package and publish/update the addon with CS2 Workshop Manager so the VPK includes the compiled layout and style. Current `game/csgo/gameinfo.gi` lists `panorama/layout/custom_game`, `panorama/styles/custom_game`, and `panorama/images/custom_game` under `AddonConfig/VpkDirectories`, though it is still worth inspecting the VPK directly rather than assuming the map scanner picked up strings referenced only in C#.
6. Run the published Workshop map on your server and verify clients actually mount the addon. Test with a second client that does not have your loose local files, which can hide missing VPK entries. Using a standalone resource addon with unrelated maps requires your own distribution/mounting setup.
7. Deploy the C# module separately and open the HUD once the client assets are mounted.

Updating the DLL does not update UI files, and updating a Workshop item does not force already-connected clients to remount it. If you hit stale cached assets, reconnect, reload the map, or restart the client.

## Complete C# module

Create a separate `PanoramaExample` class library per [Writing a Module](../guides/writing-a-module.md): `net10.0`, C# 13, x64, nullable enabled, implicit usings disabled, and `ModSharp.Sharp.Shared` matching your server (`2.1.227` here, with `PrivateAssets="all"` and `ExcludeAssets="runtime"`). Keep only this single module entry point in the project.

[!code-csharp[panorama.cs](../../codes/panorama.cs)]

Deploy the module, join the server, and run `ms_panorama` in the client console or `.panorama` / `/panorama` / `!panorama` in chat. The first call lazily creates one shared server entity while tracking each player's open/closed state independently. Clicking **Acknowledge** closes only your HUD and prints a chat confirmation; running the command again also closes the HUD and releases input capture even if missing client assets prevented the button from showing.

Spawning is intentionally deferred out of the constructor and `Init`, because a map may not be active yet and lazy creation also works seamlessly across module hot reloads. After a map change or reload, the next request spawns a fresh entity. Player join/disconnect resets that slot's presentation and capture flags so a reused slot never inherits the previous player's state.

## Click handling and cleanup

All Panorama manager and entity calls must run on the **game main thread**. The native click hook (`ProcessClientSvcUserMessage` post-hook) synchronously forwards `CS_UM_CustomHudClicked` after the engine's handler, so callbacks cannot block engine or `cs_script` click handling. Keep callbacks short and never carry player or layout objects across async work or map changes.

Even though the bridge validates message shape, connection state, and handles (and the manager only dispatches clicks for tracked layouts), never treat a client click or `buttonId` as a permission check. Before running sensitive logic, verify the player, your owned layout, the allowed button IDs, and current server state. This sample verifies the full entity handle, exact button ID, and open-player set before printing its acknowledgement.

Lifecycle notes from [PanoramaManager.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Managers/PanoramaManager.cs):

- `GetManagedLayouts` returns tracked layouts from every module; never blindly kill everything it returns.
- `InstallClickCallback` only accepts entities created through this manager (pre-placed map entities are not adopted), and global listeners only receive clicks from manager-tracked layouts.
- Global listeners run before layout-specific callbacks. Dispatch runs over a snapshot and logs exceptions; removing a handler mid-dispatch will not cancel callbacks already in that snapshot, so recheck entity validity and state if an earlier handler might have killed the layout.
- Deleting an entity clears its layout-specific callbacks. Map shutdown clears tracked layouts and layout-specific callbacks, but **global listeners stay installed**, so always remove global listeners in `Shutdown`.
- Core unload cleanup only strips leaked click listeners and callbacks as a fallback; it does **not** kill your spawned layout entities or disable input capture. Likewise, removing a callback does not kill the entity, and killing an entity does not remove a global listener, so clean all of them up explicitly in `Shutdown`.

The sample keeps only a map-local full handle, resolves the entity on each use, detaches its callback before killing the entity on map deactivation or shutdown, and clears local state if the entity is deleted externally. `OnGameShutdown` clears managed state only and never touches stale native objects.

## Testing and references

Before shipping, test asset compilation, VPK contents, rendering, button clicks, multi-player isolation, spectators, cursor/movement recovery, slot reuse, map changes, and module reloads on a real CS2 server with a clean client.

For Valve's latest syntax, check `content/csgo_addons/cs_script_demo/panorama/layout/custom_game/welcome.xml`, `styles/custom_game/welcome.css`, and `maps/scripts/setup.js` in your CS2 installation. This sample follows that official CS2 pattern (class-based dismissal and ID-only buttons). Source references: [IPanoramaManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/PanoramaManager.cs), [ICustomHudLayout](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/GameEntities/ICustomHudLayout.cs), and [native click bridge](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/client.cpp).
