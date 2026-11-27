# Panorama 自定义 HUD

`ISharedSystem.GetPanoramaManager()` 用来控制 CS2 的 `custom_hud_layout` 实体。服务端 DLL 本身不包含客户端 UI 资源，要让 HUD 真正显示出来，需要走完三步：

1. 用 CS2 Workshop Tools 写好并**编译** Panorama 布局和样式。
2. 把编译产物打包发布并挂载到服务端和每个客户端（常见做法是随创意工坊地图一起打包）。
3. 在模块里生成服务端实体，再按需更新它的同步状态。

`CreateLayout` 成功只表示服务端实体建好了，它不管客户端文件下载、VPK 挂载，也不会反馈客户端是否渲染成功。

## 支持的功能

根据 Valve 的[自定义 HUD 更新说明](https://store.steampowered.com/news/app/730/view/717913919982667304)，目前支持 `Panel`、`Label`、`Image`、`Button` 和 CSS 样式，**不支持普通 Panorama XML 事件和客户端脚本**。别往 XML 里加 `onactivate`、JS 脚本或 Dota 风格的自定义 UI manifest；服务端逻辑这里直接用 C# 写（服务端的 `cs_script` 是另一套机制）。

| ModSharp API | 作用 |
| --- | --- |
| `CreateLayout(resource, targetName)` | 生成并跟踪一个 `custom_hud_layout`（`targetName` 默认为 `"ms_custom_hud_layout"`）。路径为空白时抛异常，生成失败返回 `null`。 |
| `GetManagedLayouts()` | 枚举管理器当前跟踪的所有有效 `custom_hud_layout`（包含其他模块创建的）。 |
| `Layout` / `Observable`（`ICustomHudLayout`） | 读取布局资源路径（`Layout`），或读写观察者可见性（`Observable`）。 |
| `SetClassOverride` / `SetClassOverrideForPlayer` | 针对全体或单个玩家，开关面板上某个**已在 CSS 中定义好**的类。不能动态下发 CSS 或直接改单个样式属性。 |
| `SetDialogVariableString` / `SetDialogVariableStringForPlayer` | 为全体或单个玩家设置面板的字符串对话变量，Label 可用 `{s:variable_name}` 引用。 |
| `ClearDialogVariableStringForPlayer` | 清除单个玩家的字符串覆盖，恢复使用全局值。 |
| `SetInputCaptureEnabled` / `IsInputCaptureEnabled` | 开关或读取服务端为 `(layout, player)` 记录的输入捕获状态。 |
| `InstallClickCallback` / `RemoveClickCallback` | 为某个由管理器创建的布局挂接或移除点击回调。 |
| `InstallClickListener` / `RemoveClickListener` | 挂接或移除全局点击监听器，接收所有受管布局的点击事件。 |

`HudPanelClassStatus.ForceEnable` 会加上该 CSS 类，`ForceDisable` 会移除，`Undefined` 会清除覆盖。按玩家操作的重载支持传入 `PlayerSlot`、`IGameClient` 或 `IPlayerController`。

`Observable` 只控制观察者能不能看到这个 HUD。想按玩家控制显隐，通常是在子面板上默认挂一个隐藏类，再只给目标玩家取消这个类。注意这只是视觉隐藏，不能拿来挡敏感信息，服务端实体本身仍可能同步给其他玩家。

输入捕获决定客户端能不能直接点这个 HUD（它和其他 HUD 的叠加、移动和光标恢复都由客户端决定）。关掉面板时记得同步关闭输入捕获：取消捕获不会自动隐藏面板，隐藏面板也不会自动释放捕获。

## 资源路径与源码

下面的例子用名为 `modsharp_docs` 的 addon，这些路径和大小写需要完全对上：

| 用途 | 路径 |
| --- | --- |
| 布局源码 | `content/csgo_addons/modsharp_docs/panorama/layout/custom_game/modsharp_docs/panorama-layout.xml` |
| 样式源码 | `content/csgo_addons/modsharp_docs/panorama/styles/custom_game/modsharp_docs/panorama-style.css` |
| 编译后的布局 | `game/csgo_addons/modsharp_docs/panorama/layout/custom_game/modsharp_docs/panorama-layout.vxml_c` |
| 编译后的样式 | `game/csgo_addons/modsharp_docs/panorama/styles/custom_game/modsharp_docs/panorama-style.vcss_c` |
| 传给 `CreateLayout` 的逻辑路径 | `panorama/layout/custom_game/modsharp_docs/panorama-layout.vxml` |

传给 `CreateLayout` 的是挂载后的资源逻辑路径，不要带 `content/`、`game/csgo_addons/modsharp_docs/`、`file://`，也不要带编译后的 `_c` 后缀。

XML 里的 `file://{resources}/styles/...` 对应 `panorama/styles/...`，直接引用 CSS 源文件即可，编译器会自动处理依赖。不要手动把 XML/CSS 文件改后缀成 `.vxml_c` 或 `.vcss_c`。

下载[布局源码](../../codes/panorama-layout.xml)和[样式源码](../../codes/panorama-style.css)。顶层面板只带 class、没有 `id`，需要被服务端控制的对话框和按钮都放在它下面：

[!code-xml[panorama-layout.xml](../../codes/panorama-layout.xml)]

[!code-css[panorama-style.css](../../codes/panorama-style.css)]

按钮只要写上 `id`，玩家点击时 CS2 的自定义 HUD 消息就会把这个 ID 传给服务端回调，不需要客户端脚本。对话框默认挂了 `Dismissed` 类（透明度为 0，沿用 Valve 自带欢迎 HUD 的写法）：打开时用 `ForceDisable` 只给当前玩家去掉这个类并开启输入捕获，点确认时用 `Undefined` 清除该玩家的覆盖（恢复 XML 默认的 `Dismissed` 类）并关闭捕获。

以后如果要用 `Image`，对应的本地图片也要放进已挂载的 addon 里（目前打包目录包含 `panorama/images/custom_game`）。`CreateLayout` 和 `SetClassOverride` 都不负责传输图片，具体支持的图片格式和引用方式以当前 CS2 工具为准。

## 编译、打包与发布

开始前需要装好 CS2 客户端和 **Counter-Strike 2 Workshop Tools** DLC，有一个能发创意工坊的 Steam 账号和一个你自己的可游玩地图 addon。这里直接把 HUD 打进地图 addon 里，省去额外装资源挂载插件。相关官方资料见 Valve 的 [CS2 Workshop Tools](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools)、[官方工具列表](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Official_Tools)和[地图编译流程](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Level_Design/Compiling)。

1. 在 Workshop Tools 的 addon 启动器里创建或打开 `modsharp_docs`，放进你的测试地图。如果合进已有 addon，把下面文件系统路径里的 addon 名换成你自己的就行，`panorama/.../modsharp_docs/...` 逻辑路径可以不动。
2. 按上表把 XML 和 CSS 放进对应的源码目录（不要放到 `game/sharp/modules`）。
3. 用 CS2 自带的 `resourcecompiler.exe` 编译它们。在 PowerShell 里把 `$CS2` 改成你的实际安装路径后运行：

   ```powershell
   $CS2 = 'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive'
   $Content = "$CS2/content/csgo_addons/modsharp_docs"
   $Compiler = "$CS2/game/bin/win64/resourcecompiler.exe"

   & $Compiler -game "$CS2/game/csgo" -nop4 -i "$Content/panorama/styles/custom_game/modsharp_docs/panorama-style.css"
   if ($LASTEXITCODE -ne 0) { throw 'Panorama style compilation failed.' }
   & $Compiler -game "$CS2/game/csgo" -nop4 -i "$Content/panorama/layout/custom_game/modsharp_docs/panorama-layout.xml"
   if ($LASTEXITCODE -ne 0) { throw 'Panorama layout compilation failed.' }
   ```

4. 确认编译日志无报错，且上表里的两个 `_c` 文件都已生成（`game/bin/assettypes_common.txt` 会把 CSS/XML 交给 `CompilePanorama` 编译为 `vcss`/`vxml`）。
5. 在 Hammer 里完整编译地图（`F9`），再用 CS2 Workshop Manager 打包发布或更新该 addon，确认 VPK 里打进了编译后的布局和样式。目前 `game/csgo/gameinfo.gi` 的 `AddonConfig/VpkDirectories` 默认包含 `panorama/layout/custom_game`、`panorama/styles/custom_game` 和 `panorama/images/custom_game`，不过仍建议打包后实际检查一下 VPK 内容，别指望地图依赖扫描能自动发现只写在 C# 字符串里的资源。
6. 服务端切到这张发布后的创意工坊地图，让客户端正常挂载。测试时最好找一台没有本地开发散装文件的机器验证，免得本地文件掩盖了 VPK 漏打包的问题。如果要做独立资源包配合其他地图使用，需要自己搭一套服务端/客户端资源挂载方案。
7. 最后部署 C# 模块，等客户端资源就绪后再打开 HUD。

注意改 DLL 不会更新 UI 资源，更新创意工坊条目后已连在服里的客户端也不会立刻切到新包，排查缓存问题时记得重连、换图或重启客户端。

## 完整 C# 模块

按[编写模块](../guides/writing-a-module.md)建一个独立的 `PanoramaExample` 项目：`net10.0`、C# 13、x64、开启 nullable、关闭隐式 using，引用与服务端匹配的 `ModSharp.Sharp.Shared`（本文为 `2.1.227`，带 `PrivateAssets="all"` 和 `ExcludeAssets="runtime"`），项目里只放这一个模块入口。

[!code-csharp[panorama.cs](../../codes/panorama.cs)]

发布部署后进服，在客户端控制台输 `ms_panorama`，或者聊天框输 `.panorama` / `/panorama` / `!panorama`。第一次调用会按需生成一个共享的服务端实体，但每位玩家的打开/关闭状态互不干扰。点 **Acknowledge** 会关掉自己的 HUD 并在聊天里回一条确认；再输一次命令也能关掉 HUD（就算客户端缺资源导致按钮没出来，也能靠这条命令解锁输入捕获）。

这里故意没在构造函数或 `Init` 里创建实体，因为加载模块时地图未必已经就绪，懒加载还能顺便兼容模块热重载。换图或重载后，下一次命令会自动重建实体。玩家进服或断线时也会清掉对应槽位的状态和捕获标记，防止槽位复用串到下一个人身上。

## 点击回调与清理细节

所有 Panorama 管理器和实体操作都必须在**游戏主线程**执行。原生点击钩子挂在 `ProcessClientSvcUserMessage` 之后，同步把 `CS_UM_CustomHudClicked` 转发给托管层，所以回调里不能拦截引擎或 `cs_script` 自己的处理。回调写轻一点，别把玩家或布局对象带到异步代码或下一张图里。

虽然桥接层会校验消息结构、连接状态和实体句柄，管理器也只认自己跟踪的布局，但客户端发来的点击和 `buttonId` 依然不能当权限凭证用。执行敏感操作前，一定要自己核对玩家身份、布局归属、按钮白名单和当前业务状态。这个示例核对了完整实体句柄、按钮 ID 和已打开玩家集合，并且只做一条普通确认。

[管理器源码](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Managers/PanoramaManager.cs)里的几个生命周期细节：

- `GetManagedLayouts` 会返回所有模块创建的受管布局，别一把全删了。
- `InstallClickCallback` 只接受通过本管理器创建的实体，地图里预先摆好的实体不会被自动接管；全局监听器同样只收受管布局的点击。
- 全局监听器先于单布局回调执行。派发前会先拷贝一份处理器快照，异常会被记日志；派发中途移除处理器挡不住本轮快照里的调用，如果前面的回调可能删掉实体，后面的回调里要重新检查状态。
- 实体被删时，它专属的回调会一起清掉；换图/地图关闭时会清掉所有受管布局和单布局回调，但**全局监听器会一直留着**，所以模块 `Shutdown` 时一定要手动移除全局监听器。
- 模块卸载时的兜底清理只会移除漏掉的全局监听器和点击回调，**不会**删除你生成的布局实体，也**不会**关闭玩家的输入捕获。另外，移除回调不会删实体，删实体也不会注销全局监听器，这些都必须在 `Shutdown` 里主动清理干净。

上面的示例只存当前地图有效的完整句柄，每次操作按句柄重新解析实体。地图停用或模块关闭时先拆回调再删实体；实体被外部删掉时清掉本地状态；`OnGameShutdown` 只清托管集合，不碰旧的原生对象。

## 实机测试与参考

上线前记得在真实的 CS2 服务端和干净客户端上把资源编译、VPK 打包挂载、界面渲染、按钮点击、多玩家隔离、观察者可见性、输入恢复、槽位复用、换图和模块热重载都测一遍。

想看 Valve 最新的写法，直接翻 CS2 安装目录下的 `content/csgo_addons/cs_script_demo/panorama/layout/custom_game/welcome.xml`、`styles/custom_game/welcome.css` 和 `maps/scripts/setup.js`。本文的隐藏类和纯 ID 按钮就是按这个官方示例来的。相关源码见 [IPanoramaManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/PanoramaManager.cs)、[ICustomHudLayout](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/GameEntities/ICustomHudLayout.cs) 和[原生点击桥接](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/client.cpp)。
