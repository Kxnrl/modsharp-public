# GameData

GameData 用来把原生内存地址、字节偏移和虚表槽位从 C# 代码里抽离出来。通过 `sharedSystem.GetModSharp().GetGameData()` 拿到的 `IGameData` 是全宿主共用的注册表，不包含 Schema 字段访问，也不负责声明函数签名。

能用宿主内置的键就尽量直接用；内置数据没覆盖到时，再注册自定义文件，并且给键名加上自己的模块前缀。如果只是想直接查导出符号或做特征码扫描，可以看[原生库模块](library-modules.md)。拿到地址之后，调用或挂钩还需要匹配正确的 ABI，参见[原生调用](../examples/native-call.md)和[原生挂钩](../examples/native-hook.md)。

## 内置数据与查询 API

在加载托管模块之前，原生宿主会先从 `game/sharp/gamedata` 目录载入五个内置文件：

| 文件 | 内容示例 |
| --- | --- |
| `core.games.jsonc` | `INetChannel::SendNetMessage`、`IScriptVM::CreateVM`、公共接口虚表槽位 |
| `tier0.games.jsonc` | `CCommand::Tokenize`、`CVProfile::OutputReport` |
| `engine.games.jsonc` | 网络服务端/客户端地址、`CServerSideClient::m_SignonState`、引擎虚表槽位 |
| `server.games.jsonc` | 实体函数、`g_pPhysicsQuery`、`CBaseEntity::CollisionRulesChanged` |
| `log.games.jsonc` | `LogDirect`、`LoggingSystem_IsChannelEnabled` 及其 C++ 重载 |

启动时部分原生解析器还会动态覆写某些内置偏移、索引或地址。代码里应该直接调 API 取值，不要把文件里的数字硬编码进去，也不要去注册或注销这五个宿主文件。

`IGameData` 的公开方法：

| API | 说明 |
| --- | --- |
| `bool GetOffset(string name, out int offset)` | 查 `Offsets` 里的字节偏移。 |
| `bool GetAddress(string name, out nint address)` | 解析并缓存 `Addresses` 里的地址；失败返回 `false` 并置零（原生层仍可能输出诊断日志）。 |
| `bool GetVFuncIndex(string name, out int index)` | 查 `VFuncs` 里从 0 开始的虚表槽位索引（不是字节偏移）。 |
| `int GetOffset(string name)` | 返回字节偏移，找不到抛 `KeyNotFoundException`。 |
| `nint GetAddress(string name)` | 返回解析后的地址，找不到抛 `KeyNotFoundException`。 |
| `int GetVFuncIndex(string name)` | 返回虚表槽位索引，找不到抛 `KeyNotFoundException`。 |
| `GetOffset(classname, name)`、`GetAddress(classname, name)`、`GetVFuncIndex(classname, name)` | 先查 `classname::name`，查不到再沿着 Schema 基类往上找。需要有效的 Schema 类信息，不是简单的字符串拼接。 |
| `void Register(string path)` / `void Unregister(string path)` | 往共享数据库添加或移除自定义文件的条目。 |

像 `GameDataExample::LoggingSystem_IsChannelEnabled` 这种自定义命名空间键并不是 Schema 类，直接用单字符串重载去查即可。

## `games.jsonc` 格式说明

文件根节点是一个 JSON 对象，里面可以放 `Addresses`、`Offsets`、`VFuncs`、`Patches` 四个区分大小写的节（没有外层 `Games` 包装，也没有 `Signatures` 或 `Library` 节）。支持 JSON 注释，但**不能写尾随逗号**。解析器对未知字段不会严格报错，字段名自己要核对仔细。

各节的结构如下：

| 节 | 字段说明 |
| --- | --- |
| `Offsets` | `"windows"` 和/或 `"linux"`，填 32 位有符号 JSON 整数（不能加引号），表示结构体里的字节偏移。 |
| `VFuncs` | 结构同 `Offsets`，但填的是从 0 开始的虚表**槽位索引**。x64 下虚表里的实际字节偏移是 `index * 8`，配置里不要乘 8。 |
| `Addresses` | 小写的 `"library"` 等通用字段，加上 `"windows"` / `"linux"` 的字符串或平台对象（详见下文）。 |
| `Patches` | `"address"` 指向某个地址键；平台对象里放十六进制字节串 `"patch"` 和可选的 `"validate"`。注册时会直接改内存，注销时还原原始字节。 |

加载时只读取当前平台的配置。缺当前平台的整数条目会被跳过；带引用定位的地址条目即便没写当前平台特征码也可能解析出来。如果整个文件在当前平台下没有任何可用条目，注册会失败。

### 目标库与平台配置

`Addresses[key].library` 要填下面这些精确的小写名称，不要填 DLL/SO 文件名：

```text
engine, server, tier0, schemasystem, resourcesystem, vscript, vphysics2,
soundsystem, networksystem, worldrenderer, matchmaking, filesystem,
steamsockets, materialsystem2, animationsystem
```

注意它们和托管库管理器的属性名不完全一致：比如这里写 `schemasystem`、`resourcesystem`、`soundsystem`、`networksystem`，C# 里对应的属性则是 `Schema`、`Resource`、`SoundSystem`、`NetworkSystem`。

平台字段（`"windows"` / `"linux"`）支持两种格式：

- **字符串**：IDA 风格字节特征码，或者以 `@` 开头的导出符号名（不能带空格，`@` 表示按导出表查符号）。
- **对象**：`"signature"` 填上面同样的字符串，`"factory"` 填地址变换步骤，可选的 `"base"` 用来覆盖通用的基地址键。

Windows 和 Linux 的指令或 C++ 修饰符号不同时，要分开配置。下面这个示例文件在双平台都用真实的未修饰导出符号 `LoggingSystem_IsChannelEnabled`（从内置 `log.games.jsonc` 拷贝并改了键名）：

[!code-json[GameData 配置](../../codes/gamedata-example.games.jsonc)]

[下载配置文件](../../codes/gamedata-example.games.jsonc)。CS2 版本更新后符号和特征码都可能变动，自定义 GameData 文件要跟着服务端和框架版本一起维护。

### 区分特征码、偏移与指针

字节特征码由空格分隔的十六进制字节组成，`?` 或 `??` 代表一个整字节通配，不支持 `A?` 这种半字节通配。末尾多余的通配符会被扫描器裁掉。GameData 的特征码必须在可执行段里**唯一命中**，找不到或命中多处都会失败。

扫出来的初始地址指向匹配到的第一条指令字节，不会自动解引用或追跳转。需要算目标地址时，在平台对象里配 `factory`，它会按从左到右的顺序变换当前地址 `A`：

| 操作 | 含义 |
| --- | --- |
| `+N` / `-N` | 加减十进制字节数 `N`（`factory` 写字符串；写成 JSON 数字虽兼容但会打警告）。 |
| `r` | 读取 `A` 处的 32 位有符号相对偏移 `displacement`，把 `A` 设为 `A + 4 + displacement`（用之前先用 `+N` 把 `A` 移到偏移量字段开头）。 |
| `d` | 解引用一次，读取 `A` 处存放的原生指针作为新地址。 |

拿内置 `server.games.jsonc` 里的 `g_pPhysicsQuery` 举例，它的 `factory` 是 `"+3 r d"`：先往后移 3 字节到 RIP 相对偏移字段，用 `r` 算出全局变量地址，再用 `d` 读出里面存的接口指针（Windows 特征码以 `48 8B 0D` 开头，Linux 以 `48 8D 05` 开头）。最终拿到的是物理查询接口指针，直接用内置键 `g_pPhysicsQuery` 就行，别自己去复制特征码。

配置里没有 `offset` 或 `pointer` 这类替代字段，一切地址变换都走 `factory`。`factory` 读的是原始原生内存，不做越界或空指针校验，算错步数会直接崩溃；另外 `GetAddress` 返回非零只说明定位到了某个地址，函数签名和调用约定还是得自己保证正确。

### 引用定位与高级配置

`Addresses` 条目还支持几个高级字段：

- `"refs"`：按引用找函数，里面的 `strings`、`cvars`、`vtables` 都可以填单个字符串或数组。单数 `vtable` 用来把候选函数限定在指定类的虚表内，和复数 `vtables` 作用不同。
- `"vscript"`：原生 VScript 绑定名，只支持 `library: "server"`（不是填 `cs_script` 文件名）。
- `"on_demand": true`：推迟到调 `GetAddress` 时再解析（默认在 `Register` 时立即解析）。
- `"base"`：先从另一个地址键起步，再执行 `factory`。依赖的键要放在同一个文件里（注册期查的是当前文件的临时表，不是全局表），别写成循环依赖。

解析顺序依次为 VScript、引用定位、平台特征码。引用定位出多个候选时可以用特征码筛掉多余项，调试模式下也会交叉比对。目前解析器要求每个条目至少有非空特征码或有效 `refs`，只写 `base` 或只写 `vscript` 的条目会被跳过。

## 部署与所有权管理

1. 按 [Hello World 设置](../examples/hello-world.md)建独立模块项目：`net10.0`、C# 13、x64、开启 nullable、关闭隐式 using，引用与服务端一致的 `ModSharp.Sharp.Shared`（本文为 `2.1.227`）。不需要源生成器或 `unsafe`。
2. 把 `gamedata-example.games.jsonc` 放到 `{CS2}/game/sharp/gamedata/gamedata-example.games.jsonc`（**不要**和模块 DLL 放一起），模块按[编写模块](../guides/writing-a-module.md)发布。
3. 在 `Init` 里先调 `Register`，再去构造依赖自定义键的组件；内置键直接查即可。
4. 在 `Shutdown` 里先停掉命令、监听器和所有用到自定义地址的 Hook，最后再 `Unregister`。

路径解析有个容易踩坑的地方：托管层只要看到路径不是以 `.jsonc` 结尾，就会在末尾补 `.jsonc`（不会自动补 `.games.jsonc`），所以 `Register("example.games")` 会找 `example.games.jsonc`，而 `Register("example.json")` 会变成 `example.json.jsonc`。原生层注册时会把路径拼到 `../../sharp/gamedata/` 下并规范化 `.jsonc` 扩展名。直接传简单的相对文件名，注册和注销保持拼写完全一致就行，别用绝对路径或 `..`。

[!code-csharp[GameData.cs](../../codes/gamedata.cs)]

[下载 C# 示例](../../codes/gamedata.cs)。加载时会打印几个内置偏移、虚表索引、日志函数地址和自定义别名的地址。

## 常见踩坑与生命周期限制

- **全局键冲突**：所有模块共用同一张表，每个节里的键名全局唯一（包含内置键）。不同文件定义了同名键会让注册直接失败，不会覆盖旧值。自定义键一定要加前缀，每个自定义文件只归一个模块管。
- **没有引用计数**：托管层按进程范围（忽略大小写）记录已注册路径。重复 `Register` 同一个路径会直接返回，但只要任何一方调了 `Unregister`，共用条目就会被清掉。别指望靠重复注册在多个模块间共享文件。
- **失败后的路径残留**：路径是在调用原生注册**之前**加进跟踪集合的。如果 `Register` 中途失败，路径可能还留在集合里，导致下次再调直接被跳过。上面示例在调 `Register` 前先置标记，这样即使初始化失败，`Shutdown` 也能把自己独占的路径注销干净（千万别对内置文件或别的模块的文件这么干）。
- **按文件批量注销**：未跟踪的路径调 `Unregister` 会直接跳过；已跟踪的会按当初记录的文件路径整批移除条目。托管集合不做路径规范化，Linux 下文件名大小写依然敏感。
- **报错排查**：原生注册失败会抛 `InvalidOperationException`，但异常文本未必全，排错时要一起看原生控制台日志。`Unregister` 在原生出错时也会抛异常。
- **Patches 非原子性**：语法错、键重复或立即解析失败会在合并数据表前中止；但包含 `Patches` 的文件是先合并数据表再逐个打补丁，后面某个补丁失败时，前面的改动可能已经生效了。
- **不自动热加载**：改完自定义文件后，需要停掉使用方、`Unregister`、部署新文件、重新 `Register` 并重建使用方，或者直接按[模块重载流程](../guides/reload-module.md)重载模块。

注册表修改和原生对象操作都要放在游戏主线程，API 不保证线程安全。重启、游戏更新或换平台都会让旧地址失效，改完 GameData 后请在对应的 Windows / Linux 服务端上实测确认。

## 相关源码

- [GameData 托管实现](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Bridges/Interfaces/GameData.cs)、[IGameData 接口](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/IGameData.cs)
- [原生解析与注册实现](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/gamedata.cpp)、[启动注册与自动解析](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/address.cpp)
- [五个内置 GameData 文件](https://github.com/Kxnrl/modsharp-public/tree/master/.asset/gamedata)
