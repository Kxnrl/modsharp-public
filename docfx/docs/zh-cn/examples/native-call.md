# Native 调用

本例使用 SDK 生成的签名调用和虚函数调用，所用的键均由 ModSharp 自带的 `server.games.jsonc` 提供：`CBaseEntity::SetGravityScale` 和 `CBaseEntity::CollisionRulesChanged`。因此不再需要旧版示例中自定义的 `native-call.games.jsonc`，也不再需要硬编码的虚表索引。

请参照 [Hello World 的项目设置](hello-world.md)，将 `ModSharp.Sharp.Generator.Sdk` 作为分析器包引入（`PrivateAssets="all"`），并启用 `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`。即使手写的接口本身不含 `unsafe`，生成的实现中仍会包含非托管函数指针。请保留示例中的 `[assembly: DisableRuntimeMarshalling]` 特性。

`IVirtualCall<T>` 用于获取虚表索引；`ISignatureCall<T>` 会根据 `T` 对应原生类型的 `NetClass` 名称与方法名拼接 `NetClass::Method` 键来解析地址（其中 `[AddressKey]` 仅覆盖方法名部分）。使用非泛型 `ISignatureCall`（不提供 `T`）时，`[AddressKey]` 必须填写完整的键。请在 DI 中注册 `IGameData`，并解析与注册时相同的接口类型。

当前的虚函数调用生成器要求第一个参数必须命名为 `instance`。声明虚函数调用接口时请保留这个名称，否则生成的实现将无法编译。

客户端聊天指令 `.native_gravity`（或客户端控制台指令 `ms_native_gravity`）会把存活调用者的重力倍率设为 `0.5`，并调用碰撞规则相关的虚函数。这会改变游戏行为，请在开发服务器上测试。普通模块开发中，应优先使用 SDK 已有的高级实体方法。

[!code-csharp[NativeCall.cs](../../codes/native-call.cs)]

如果要调用你自己的目标函数，请先注册自定义 GameData，再构造生成的调用包装对象，并在 `Shutdown` 中注销该自定义 GameData。原生签名和索引因平台和游戏版本而异，ABI 不正确会导致服务器崩溃，因此不要直接沿用旧版 CS2 的偏移。

自定义配置及其加载生命周期的规则见 [GameData](../features/gamedata.md)；更底层的符号和地址查找见[原生库访问](../features/library-modules.md)。
