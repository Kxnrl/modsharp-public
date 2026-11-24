# cs_script：C# 与地图脚本互通

`ISharedSystem.GetScriptManager()` 用来把 ModSharp 接入 CS2 服务端的 `cs_script` JavaScript 运行时（也就是地图通过 `point_script` 实体跑的那套脚本）。它和 CS:GO 的 Squirrel VScript（`.nut`）以及客户端 Panorama JS 没有关系；公开接口也没有 `RunScript` 或直接按函数名执行 JS 字符串的方法。

本文基于 SDK `2.1.227` 编写。引擎更新可能会影响 V8 兼容性或可用的原生脚本类，排查问题时除了看托管异常，也要留意服务端启动和方法注入阶段的警告日志。

## 注册 C# 方法

注册必须在游戏主线程执行（通常放在模块的 `Init()` 中），并且要赶在对应 `point_script` 创建脚本上下文之前完成：

| 注册方式 | JavaScript 调用方式 |
| --- | --- |
| `RegisterMethod("MyModule_Method", callback)` | 从 `"cs_script/point_script"` 导入 `Instance`，调用 `Instance.MyModule_Method(...)` |
| `RegisterMethod("Entity", "MyModule_Method", callback)` | 在实体包装对象上调用 `entity.MyModule_Method(...)` |
| `RegisterMethod("CSPlayerController", "MyModule_Method", callback)` | 在玩家控制器包装对象上调用 |

带类名的重载是在引擎已有的 JS 类原型上挂载方法，子类会自动继承，不会新建 JS 类。这里要传 JS 绑定名（如 `CSPlayerController`），而不是 C++ 类名（`CCSPlayerController`）。类名拼错或不存在时，注册调用本身仍会返回一个托管 ID，但注入阶段会打警告并跳过。

使用时有几个细节需要注意：

- 注册表按 `(className, methodName)` 精确索引。只要这个名字还在生效，重复注册就会抛 `InvalidOperationException`（哪怕是同一个模块自己注册的），所以方法名建议加上模块前缀。冲突检查只针对 ModSharp 的托管注册，不会拦住 Valve 原生方法或原生扩展方法，取名时要避开引擎自带的方法名。
- 方法归属按回调方法声明所在的程序集来算。回调写在模块自己的程序集里，模块卸载时会自动清理委托；如果回调写在公共辅助库里，就不会自动归到调用注册的模块名下。
- 接口没有公开 `UnregisterMethod`，返回的整数只是注册 ID。模块卸载后会移除托管委托，但原生槽位和已注入到原型上的方法还会留着；卸载期间如果 JS 再调它，会报错 `managed method dispatch failed`。
- 卸载后重新注册同名方法会复用原 ID，已有的脚本上下文能直接连上新委托。但如果在脚本上下文创建之后才注册**新名字**，旧上下文里不会自动补注，需要重新初始化脚本上下文或换图。

在回调里，`call.Script` 是当前的 `point_script` 实体，`call.Caller` 是调用接收者：`Instance` 方法对应同一个脚本实体，原生类方法对应被调用的实体对象。如果实体已失效，或者 JS 用了奇怪的 `this` 绑定，`call.Caller` 可能为 `null`，用之前记得判空。

另外，ModSharp 默认会往每个 `point_script` 上下文注入三个内置方法：`Instance.ModSharpVersion()`、`Instance.SpawnEntityFromKeyValues(classname, kvObject?)` 和 `CSPlayerController.GetSteamId()`；同时会把 `Console.ServerCommand`（包括已弃用的 `Instance.ServerCommand`）转发给 `IHookManager.PointServerCommand`。

## 参数、返回值与错误

取参数前先看 `ArgCount`，越界会直接抛托管参数异常。类型化 getter 不会做隐式转换（比如字符串不会自动转数字，数字也不会自动转布尔）：JS 传入 `null` 或 `undefined` 时返回 C# `null`；传入其他不匹配的非空类型时，除 `GetFunction` 返回 `null` 外，其余类型化 getter 都会抛 `ScriptThrowException`。

| JS 输入 | C# 读取方式 |
| --- | --- |
| 布尔 / 数字 / 字符串 | `GetBool`、`GetNumber`（`double`）、`GetString` |
| 带数值 `x`、`y`、`z` 的对象 | `GetVector`（`Sharp.Shared.Types.Vector`） |
| 实体包装对象 | `GetEntity`（内部会校验实体句柄序列号） |
| 数组（包括 `[1, 2, 3]`） | `GetArray`（数值数组不会被当作向量） |
| 其他对象 | `GetObject` |
| JS 函数 | `GetFunction`（`GetArgType` 对函数返回 `ScriptValueType.Object`，没有单独的 `Function` 枚举值） |

凡是带数值 `x`、`y`、`z` 字段的对象都会优先按向量识别；如果要按普通 `ScriptObject` 接收，换个字段结构就行。symbol、BigInt 这类不支持的 JS 值会被转成 `ScriptValueType.Null`，并在原生层打一条警告。

委托返回值类型是 `ScriptReturnValue?`，支持 bool、double、string、向量、实体，或者在**当前这次调用**里用 `NewObject()` / `NewArray()` 构造出的 `ScriptObject` / `ScriptArray`。返回 C# `null` 在 JS 里就是 `null`（无法返回 `undefined`）。不要返回上一次调用里缓存下来的对象或数组句柄。

需要向 JS 抛异常时，调 `call.ThrowError(message)` 或直接 `throw new ScriptThrowException(message)`，JS 侧会收到正常的 `Error`（类型不匹配时 getter 走的也是这条路径）。`ThrowError` 会直接中断委托执行，别在 C# 里把它 `catch` 吞掉。其他未处理的 C# 异常只会写进服务端日志，JS 侧只能看到通用的 `managed method dispatch failed`，不会泄漏内部异常堆栈。JS 调用桥接方法时最好套一层 `try/catch` 来接参数校验错误。

`SetArg` / `SetArgNull` 改的是当前原生回调里的参数槽位，主要用于 detour 拦截场景；它不会改 JS 调用方的局部变量，但直接修改收到的对象或数组内容会真实反映到 JS 对象上。

## 从 C# 回调 JavaScript

想从 C# 反调 JS，做法是让 JS 把函数作为参数传进注册好的 C# 方法里：在 C# 里调一次 `GetFunction(index)` 拿到 `IScriptCallback`，然后在游戏主线程上调 `Invoke(...)`。调用是同步的，允许 JS 再次重入 C#，也可以把回调存下来留到后面再用。

- 每次 `GetFunction` 成功都会分配一个独立的原生持久句柄，用完必须 `Dispose`（哪怕当场调完就扔、或者准备替换旧回调）。参数不是函数时会返回 `null`。
- `Invoke` 执行时会切入对应脚本的作用域，但 `this` 指向的是全局对象，不是原来挂载该函数的对象。如果 JS 函数依赖 `this`，传箭头函数、闭包或提前 `.bind(...)`。
- 传参用 `ScriptArgument`，支持 bool / int / double / string / 向量、`Null`、`Entity(...)`，以及嵌套的 `Object(...)` / `Array(...)`。这些描述会在调用时现场构造新的 JS 值，不能携带 JS 函数。
- 回调返回值只支持 bool / 数字 / 字符串 / 向量 / 实体。如果 JS 返回 `null`、`undefined`、对象、数组或 Promise，C# 侧拿到的都是 `null`（没有等待 Promise 的机制）。
- JS 回调里如果抛了异常，原生桥接会捕获并记日志，然后让 `Invoke` 返回 `null`，不会往 C# 抛异常。所以单看返回值是 `null` 分不清是正常返回空、返回了对象还是出了错；有需要的话可以让 JS 明确返回一个标量确认值，具体原因查服务端日志。
- `Owner` 每次访问都会重新解析所属实体。回调被释放、实体被删除或换图后，`IsAvailable` 会变为 `false`。调用失效回调会抛 `InvalidOperationException` 并释放句柄，调用已 `Dispose` 的包装对象会抛 `ObjectDisposedException`。不要跨图保留实体索引或回调。

## 线程与生命周期

所有脚本操作都必须在游戏主线程执行，包括注册、读写变量、检查回调状态、调用回调以及 **`Dispose` 回调**。别放到 `Task.Run` 里跑，别跨 `await` 捏着调用上下文，也别在后台线程的 continuation 里释放 V8 句柄。在非主线程调用 `Dispose` 会直接抛异常且不释放句柄，必须切回主线程处理。

| 对象 | 生命周期与释放方式 |
| --- | --- |
| `RegisterMethod` 收到的 `IScriptCallContext` | 仅在这次同步调用内有效，框架自动管理，不要保存也不要手动 `Dispose`。 |
| `CreateCallContext(pointer)` 的返回值 | 仅用于 detour 中传入当前有效的 V8 function-callback-info 指针。用 `using` 管理，嵌套时按先内后外顺序释放，期间要保证指针和脚本作用域一直有效。 |
| `ScriptObject`、`ScriptArray` 及其嵌套值/原始句柄 ID | 绑定到当前调用帧的轻量视图，没有单独的 `Dispose`。结构体复制不会克隆 JS 对象，也不会延长生命周期，切忌缓存或跨上下文混用。 |
| `IScriptCallback` | 独立持有的持久句柄，必须在游戏主线程手动 `Dispose`（模块卸载和替换缓存时都要记得释放）。实体删除或换图时原生层会兜底回收，但别指望兜底代替主动释放。 |

`ScriptObject` 提供类型化 getter/setter 以及 `Has`、`Keys`、`Count`、`Remove`、`Clear`，只操作对象**自身**的可枚举字符串属性，不走原型链。`ScriptArray` 提供类型化读取、`Set*`、`Push*`、`Insert*`、`Remove` 和 `Clear`（删除会移动后续元素，越界写入可能扩充长度）。它们都是 JS 对象的实时代理，不是反序列化出来的 C# 集合；尽量用普通的、可变的 JS 数据对象和数组，遇到冻结属性、getter/setter、Proxy 或被魔改的数组方法时，底层可能会失败或意外触发 JS 代码，而且不是所有底层失败都会抛成 C# 异常。

底层 `ScriptValue.String` 是指向原生复用缓冲区的 UTF-8 指针，必须在下一次编组调用前把内容拷出来（遍历时也是）；直接用类型化 `GetString` 已经帮你拷好了。同理，`ScriptMarshal.Encode` / `EncodeString` 用的是托管侧线程局部复用的非托管缓冲区，也要在下一次编码前立刻消费掉，不要缓存或释放指针。日常写代码直接用类型化 setter 和 `ScriptReturnValue(string)` 就够了。实体引用在调用结束后仍需按常规方式检查是否存活。

C# 返回的对象和数组写入 JS 返回槽之后，在 JS 侧可以正常存活，但对应的 C# 句柄在回调结束时就会失效。返回的对象可以和传入的数组共享引用，下面的示例就是直接把原数组放进返回对象里的。

## 读写模块变量

`TryReadVariable(script, name, out value)` 和 `TryWriteVariable(script, name, value)` 面向正在运行的 `point_script`。底层是从引擎已注册的 JS 回调出发去翻可达的作用域（跳过全局作用域），所以**如果脚本一个回调都没注册，哪怕文件跑完了也可能找不到它的作用域**。建议先注册好 `OnScriptInput` 这类回调，等顶层初始化跑完再交互。它只做作用域查找，不会执行 JS 表达式，也读不到被 `import` 的模块里的私有变量。

| 目标 | 行为 |
| --- | --- |
| 顶层 `let`、`var`、`const` | 可以读取已初始化的模块作用域变量；打算改写的变量请用 `let` 或 `var`。 |
| 顶层写入 | 走 V8 调试器的作用域 setter 并返回其布尔结果。变量不存在或被拒绝时会返回 `false`，桥接层不会单独拦截 `const`。别把 `const` 当防改写的保护机制，也别指望不同 CS2 版本对它的行为一致。 |
| `checkpointState.total` | 先找根变量，再逐级取 JS 属性（`const` 对象本身不锁属性）。中间某级对象不存在会失败；最后一级在 JS 允许时会新建属性。 |
| 数组下标（点分数字） | 像 `state.items.0` 会直接当属性名 `"0"` 访问，不支持方括号语法或表达式。 |
| 最终目标是对象或数组 | 返回 `true`，但 `value` 是 `null`，不会给出持久包装对象。要读就读底下的标量字段，或者让 JS 直接把对象传给 C# 方法。 |
| 根变量不存在 / 作用域不可用 | 返回 `false`。如果路径有效但叶子本身是 `null`/`undefined`，或者已有对象上缺最后一个属性，可能返回 `true` 且 `value == null`，所以布尔值和结果值要分开判断。 |

写入接口收的是 `ScriptReturnValue`（支持标量、向量、实体，`default(ScriptReturnValue)` 表示 JS 的 `null`），不支持传入整个 `ScriptArgument` 树，也不能拿旧调用帧里的句柄去覆盖整个对象。需要传结构化数据时，直接走回调参数，或者在被调方法里原地修改。另外，点分路径走的是常规 JS 属性访问，会触发 getter/setter。

## 完整示例：检查点积分

下面做一个简单的互通例子：地图触发检查点奖励，C# 负责校验数值、读取地图里的积分上限、更新标量计数、反调 JS 做确认，最后返回一张结构化收据。注意这只是给地图和服务端配合用的，没有事务回滚，积分在通知回调跑之前就已经改好了。

按[编写模块](../guides/writing-a-module.md)建一个独立项目：`net10.0`、C# 13、x64、开启 nullable、关闭隐式 using、`EnableDynamicLoading=true`，引用 `ModSharp.Sharp.Shared`（版本 `[2.1.227]`，带 `PrivateAssets="all" ExcludeAssets="runtime"`）。把 `AssemblyName` 设为 `CheckpointBridge`，用下面的代码替换默认类，编译发布到 `game/sharp/modules/CheckpointBridge/`，项目里只留这一个 `IModSharpModule`。

[!code-csharp[检查点 C# 桥接模块](../../codes/cs-script.cs)]

这个模块不缓存任何实体、监听器或回调。回调句柄用 `using` 保证无论正常返回还是抛异常都会释放，模块卸载时宿主会自动清理注册的方法委托。

### 地图配置与加载顺序

1. 安装 CS2 Workshop Tools，新建或打开一个 addon，把下面的脚本保存为 `{CS2}/content/csgo_addons/<addon>/scripts/modsharp_docs_checkpoint.js`（这是 addon 源码资源，别放到 `sharp/modules` 或 Panorama 目录里）。
2. 在 Hammer 里放一个 `point_script`，把 `targetname` 设为 `modsharp_docs_checkpoint`，在 `cs_script` 字段的资源浏览器里选 `scripts/modsharp_docs_checkpoint.vjs`（源文件是 `.js`，编辑器里会显示为编译后的 `.vjs`）。
3. 在 Workshop Tools 里编译并运行地图。如果要上专用服务器，按正常创意工坊流程打包发布，连同引用的脚本一起挂载。光放个 C# DLL 或者散落的 `.js` 源文件是不行的，脚本管理器不会替你编译或挂载地图资源。
4. 保证 ModSharp 和 `CheckpointBridge` 在**地图和脚本上下文创建之前**就已经加载完成。纯原版 Workshop Tools 环境虽然能跑这个 JS，但不会有 `Instance.ModSharpDocs_CheckpointAwardV1`。
5. 在地图里放两个测试按钮，把 `OnPressed` 输出连到这个 `point_script`，输入选 `RunScriptInput`，参数分别填 `award` 和 `invalid_award`。这些参数用来匹配 JS 里注册的回调名，不是要执行的脚本代码。

[下载地图 JavaScript](../../codes/cs-script.js)。

[!code-javascript[检查点地图脚本](../../codes/cs-script.js)]

地图启动后 JS 会打印 `Ready`。按 `award` 会进 C# 校验、更新 `checkpointState.total`、回调 `acknowledgeAward`，并拿到 `awarded=25`、`acknowledged=true` 的收据；按 `invalid_award` 会打出校验报错，积分和计数不变；超过上限的奖励也会被拒掉。

### 重载与注意事项

只替换模块 DLL 不会让地图 JS 重新加载。按[模块重载指南](../guides/reload-module.md)重载模块后，原来同名的方法槽位会自动接上新委托，但新增的方法名必须重建脚本上下文才会生效。在 Workshop Tools 里重新跑图会加载修改过的 JS；工具模式下的脚本热重载走的是引擎自己的 `OnScriptReload`，和 ModSharp 模块重载是两码事。上面这个例子没有在脚本重跑时持久化状态，顶层积分和计数都会重置。想要干净地从头来过，先加载模块再重新加载地图。如果你的模块缓存了 JS 回调，记得在脚本重初始化前后主动释放并重新获取，别以为实体还在，旧回调就一定还指着最新闭包。

实际开发时，方法注入、VJS 打包挂载、作用域读写和重载行为都需要在真实的 CS2 服务端配合 Workshop Tools 编译出的地图实测，Node 或浏览器里没有 `cs_script/point_script` 运行环境。

## 参考资料

- Valve：[CS2 脚本概览](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Scripting)、[源资源与 Hammer 设置](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Scripting/Hello_Gordon)、[point_script 实体和输入](https://developer.valvesoftware.com/wiki/Point_script_(Source_2))、[当前脚本 API](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Scripting_API)（本例使用 API 文档里的 `Console.Log`，而非旧教程中的 `Instance.Msg`）。
- ModSharp 源码：[ScriptManager 接口](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ScriptManager.cs)、[托管分发与清理](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Managers/ScriptManager.cs)、[原生注入](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/cs_script.cpp)、[原生参数/对象桥接](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/bridge/natives/ScriptNatives.cpp)、[V8 回调与变量实现](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/v8proxy.cpp)。
