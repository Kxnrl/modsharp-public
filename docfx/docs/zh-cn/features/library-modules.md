# 原生库模块

`ILibraryModule` 对应 CS2 服务端进程里已经加载好的原生游戏库（比如 Windows 上的 `server.dll`、Linux 上的 `libserver.so`）。它用来查导出符号、扫特征码、找 RTTI/虚表和读映像信息，不能用来加载托管模块，也不能打开任意路径的 DLL/SO。

注意区分代码里的两种“模块”：

| 类型 | 说明 |
| --- | --- |
| `IModSharpModule` | 你的托管模块入口，带 `Init` / `Shutdown`，由托管加载器管理生命周期，见[编写模块](../guides/writing-a-module.md)。 |
| `ILibraryModule` | 宿主管理的原生库只读包装。模块只借用它和它返回的地址，不能释放或卸载。 |

`OnLibraryConnected` / `OnLibraryDisconnect` 通知的是托管模块的加载和卸载，不是原生库事件。如果是跨平台、需要长期维护的原生函数地址或偏移，优先配到 [GameData](gamedata.md) 里；拿到地址后的调用和挂钩参见[原生调用](../examples/native-call.md)和[原生挂钩](../examples/native-hook.md)。

## 获取原生库管理器

从 **`ISharedSystem`** 上调 `sharedSystem.GetLibraryModuleManager()`（不在 `IModSharp` 上）。返回的 `ILibraryModuleManager` 提供这些属性：

| 属性 | 对应的 GameData `library` 名 |
| --- | --- |
| `Server` | `server` |
| `Engine` | `engine`（engine2） |
| `Tier0` | `tier0` |
| `Schema` | `schemasystem` |
| `Resource` | `resourcesystem` |
| `VScript` | `vscript` |
| `VPhysics` | `vphysics2` |
| `SoundSystem` | `soundsystem` |
| `NetworkSystem` | `networksystem` |
| `WorldRenderer` | `worldrenderer` |
| `MatchMaking` | `matchmaking` |
| `FileSystem` | `filesystem`（filesystem_stdio） |
| `SteamNetworkingSocket` | `steamsockets`（steamnetworkingsockets） |
| `MaterialSystem` | `materialsystem2` |

直接通过这些属性拿原生库即可。GameData 里还支持 `animationsystem`，但公开管理器目前没有 `AnimationSystem` 属性，也没有按路径加载/卸载任意库的方法。

每个库对象提供 `nint BaseAddress`、`nuint ImageSize` 和 `string ModuleName`（原生模块名，不是托管程序集名）。算地址偏移时会用到基址和大小，但映像内部各段的读写执行权限并不一样，不能随便解引用。

## 查询导出符号：用 `GetExportFunction`

公开接口里没有 `FindSymbol`，查导出符号请用这两个方法：

- `nint GetExportFunction(string exportName)`：先查原始导出名，查不到再查导出索引里的反修饰/规范化别名。不存在或者反修饰别名有歧义时返回 `0`，遇到重载优先传原始符号或完整签名。
- `NativeFunctionInfo[] FindExportFunctions(string exportName)`：返回匹配的导出条目数组（含 `RawName`、`Signature`、`Address`）。这里是精确或规范化别名匹配，不是通配符或子串搜索；找不到返回空数组。

`GetFunctionByName` 已经过时，新代码别再用。

比如宿主自己在 `log.games.jsonc` 里用到了 tier0 的导出符号 `LoggingSystem_IsChannelEnabled`，下面的示例就会直接查它。注意**这里不要加 `@` 前缀**：`@` 只属于 GameData 的配置语法，`GetExportFunction` 和特征码扫描都不认它。

Windows 走 PE 导出表加规范化别名回退，Linux 走收集到的 ELF 导出名加别名查询。双平台的 C++ 名称修饰和 ABI 不同（像内置 `CVProfile::OutputReport` 在 Windows 和 Linux 上就是两套符号），不要拿一个平台的结果去猜另一个平台。

## 特征码与数据搜索

| API | 行为 |
| --- | --- |
| `FindPattern(string pattern, nint startAddress = 0)` | 返回可执行段里找到的第一个匹配，找不到返回 `0`，不要求唯一。传了非零 `startAddress` 时会枚举全部匹配，取第一个地址 **大于等于** `startAddress` 的结果（包含边界本身）。 |
| `FindPatternMulti(string pattern)` | 以 `List<nint>` 返回所有匹配，找不到返回空列表。 |
| `FindPatternExactly(string pattern)` | 找不到返回 `0`；恰好匹配一处返回地址；**多处匹配时抛 `InvalidOperationException`**。 |
| `FindData(ReadOnlySpan<byte> data, bool readOnly)` | 在非可执行段里搜精确字节序列；空输入抛 `ArgumentException`。 |
| `FindString(string str)` / `FindStringExact(string str)` | 在非可执行段里搜以空字符结尾的字符串；`Exact` 会检查开头边界，避免误中别的字符串的后缀。 |
| `FindString(string str, bool readOnly, bool exact)` | 显式指定只读限制和精确匹配选项。 |
| `FindPtr(nint ptr)` / `FindPointers(nint ptr)` | 在非可执行段里找存放指定指针值的位置（不解引用该指针），前者找不到返回 `0`，后者返回空数组。 |

`readOnly: true` 会跳过可写段，`false` 则同时扫描可写数据段；无论怎么设，`FindData` 和字符串搜索都不扫可执行段。

特征码格式是空格分隔的十六进制字节加整字节通配符 `?` / `??`（不支持半字节通配），规则见 [GameData 特征码说明](gamedata.md#区分特征码偏移与指针)。迭代查找下一个匹配时，下一次的 `startAddress` 要传**大于**上次命中地址的值（比如 `last + 1`），传相等的地址会原地打转。

> [!IMPORTANT]
> 原生特征码扫描跑的是提前采集的可执行段映像：磁盘原文件数据可用时优先扫原始字节，否则回退到运行时映像。因此它看不到映像采集之后才装上去的 detour 修改字节，但返回的地址会换算成当前已加载模块里的真实地址。

扫描放在初始化或手动诊断命令里做，别放到每帧回调里。另外，特征码唯一命中只能说明这串字节只出现了一次，函数原型和入口偏移还得自己对照反汇编确认。

## 虚表、接口与函数引用

| API | 用途与失败行为 |
| --- | --- |
| `TryGetVirtualTableByName(string tableName, out nint address, bool decorated = false)` | 查可选虚表优先用这个，找不到返回 `false` / `0`，支持 `CCSPlayerPawn` 这类普通类名。 |
| `GetVirtualTableByName(string tableName, bool decorated = false)` | 强制查询：找不到会直接触发原生 `FatalError`，不会抛可捕获的托管异常。 |
| `GetVirtualFunctions(string tableName)` | 返回已知虚函数地址数组的副本，没有则返回空数组（不是某个对象实例的 vptr）。 |
| `FindVirtualTablesPartial(string str)` | 按类名片段模糊搜 RTTI/虚表元数据。 |
| `TryGetTypeInfo(string typeName, out nint typeInfo)` | 查 RTTI 类型信息，找不到返回 `false` / `0`。 |
| `IsPointerDerivedFrom(nint ptr, string name)` | 用 RTTI 检查一个**已知有效的对象实例**是否派生自指定类，不能拿来验野指针。 |
| `FindInterface(string interfaceName)` | 调该库的 `CreateInterface` 工厂拿带版本号的引擎接口。版本不对可能返回 `0`；如果该库压根没有工厂，可能触发原生断言，只请求库里确实存在的接口。 |
| `GetReferencesFromPointer(nint ptr)` | 返回引用该地址的指令地址（不是所属函数入口）。 |
| `FindFunction(...)` / `FindFunctions(...)` | 反查引用了指定字符串或指针的函数。传多个输入时取交集（同时引用全部输入）。找不到会抛异常；单数版 `FindFunction` 匹配出多个函数也会抛异常。 |
| `GetFunctionRange(nint middle, out nint start, out nint end)` | 如果地址落在某个已知索引函数内，返回 `true` 和 `[start, end)` 区间，否则返回 `false`。 |
| `GetKnownFunctionRanges()` | 返回原生函数索引里的 `NativeFunctionRange[]` 副本，不保证覆盖二进制里所有函数或优化过的代码段。 |

`decorated: true` 传的是平台编码后的 RTTI 类型名（底层比对 RTTI 名称并取偏移为 0 的主虚表），不是随便一个导出的虚表符号都能认。多重继承、thunk、编译器优化和被裁剪的元数据都会影响识别。注意分清虚表地址、槽位索引、虚函数地址和对象实例指针这四个概念。

## RVA、读取与段权限

`ILibraryModule` 没有公开 `GetRva`、`Read<T>`、`Write<T>` 或改内存权限的接口：

- 算 RVA 时，先确认地址确实在当前库映像内，再减去 `BaseAddress`；在**同一版本二进制**里加上当前基址就能还原。别跨进程复制绝对地址，游戏更新后旧 RVA 也要重新核对。
- `GetSegments()` 返回 `ModuleSegmentInfo[]`，包含 `Address`、`Size` 和 `Access`（`MemoryAccess` 标志位：`Read = 1`、`Write = 2`、`Execute = 4`）。这是扫描器采集的段属性，不是实时查的 OS 页权限，也不能改权限。
- 真要读内存时，用 .NET 的 [Marshal.ReadByte](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.readbyte?view=net-10.0) 或匹配类型的 `unsafe` 读取。访问非法地址会直接触发访问冲突，托管 `try/catch` 拦不住。

读多字节数据时要校验整个范围、对齐和对象生命周期，不能只看首地址。另外导出符号理论上也可能被转发到别的模块映像里，下面示例在前提不满足时会跳过读取。

## 完整示例

按 [Hello World 设置](../examples/hello-world.md)新建模块项目：`net10.0`、C# 13、x64、开启 nullable、关闭隐式 using，引用与宿主一致的 `ModSharp.Sharp.Shared`（本文为 `2.1.227`），不需要源生成器或 `unsafe`。

示例会在启动时打印 tier0 的基本信息，并注册一个仅限服务端控制台执行的 `ms_library_module_info` 命令方便随时查看。它会查找日志导出符号、算当前 RVA、列出各段和导出别名，并在确认地址落在映像和可读段内后再读一个实时字节（不会调用该函数，也不会改内存）。因为别的 Hook 可能已经改过入口字节，所以示例不对字节值或 RVA 做硬编码断言。

[!code-csharp[LibraryModule.cs](../../codes/library-module.cs)]

[下载 C# 示例](../../codes/library-module.cs)，按[编写模块](../guides/writing-a-module.md)发布部署即可，不需要额外的 GameData 文件。

`Shutdown` 里先移除命令再清空库引用，返回的原生地址都不需要释放。自己的模块也要注意处理半初始化情况，因为 `Init` 失败后框架依然会调 `Shutdown`。原生操作统一放游戏主线程里跑；如果挂了 Hook，务必先卸载释放 Hook，再释放依赖的 GameData 或卸载模块，避免非托管回调指回已卸载的程序集。实际查符号和扫描结果请在 Windows 和 Linux 服务端上分别测试确认。

## 相关源码

- [ILibraryModule](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/ILibraryModule.cs)、[ILibraryModuleManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/LibraryModuleManager.cs)、[ISharedSystem](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/ISharedSystem.cs)
- [托管包装实现](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Bridges/Interfaces/LibraryModule.cs)、[原生扫描逻辑](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/module.cpp)
- [Windows 实现](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/module_windows.cpp)、[Linux 实现](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/module_linux.cpp)
