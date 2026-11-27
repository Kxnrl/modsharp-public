# ConVar

ConVar 是引擎层的控制台变量，和 `core.json` 里的配置项是两回事，修改它们直接在服务端控制台或 `.cfg` 文件里设就行。

## 模块 API

- `IConVarManager.FindConVar(name)`：区分大小写，找不到返回 `null`。加 `useIterator: true` 能找到隐藏的 ConVar，不过查找会慢一些。
- `CreateConVar`：按类型创建 ConVar，数值类型重载支持指定上下限。返回可空对象，记得判空。
- `InstallChangeHook` / `RemoveChangeHook`：监听值变化，模块 `Shutdown` 时移除同一个委托。别在变更回调里直接再改同一个 ConVar，不然很容易无限递归栈溢出。

读写时按 `IConVar.Type` 选对应的重载。有个容易中招的地方：`Set(float)` 本来是给浮点型 ConVar 用的，但直接传 `long`、`uint` 或 `ulong` 时 C# 会隐式匹配到它，在整型 ConVar 上调用会直接抛 `InvalidOperationException`。`int` 范围内的整数用 `Set(int)`，超出 `int` 范围的整数格式化成字符串走 `Set(string)`。

两个字符串写入方法对非法输入的行为不一样：

- `Set(string)`：解析失败会抛 `ArgumentException`。
- `SetString(string)`：走宽松转换，非法数字会变成 `0`，布尔只认 `1` 和不区分大小写的 `true`，颜色和向量解析失败会退回对应类型的默认回退串（注意这不会恢复 ConVar 设定的 `DefaultValue`）。

需要严格校验输入并捕获非法值时用 `Set(string)`。`ReplicateToClient` 只把值发给指定客户端，不会改动服务端上的实际值。

ConVar 本身没有 `Dispose` 或按名销毁的接口，公开 API 只支持移除你挂上去的变更回调。

相关接口和示例见 [IConVarManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ConVarManager.cs)、[IConVar](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Objects/IConVar.cs) 以及 [ConVar 示例](../examples/convar.md)。

## 内置变量

引擎层内置了一批 `ms_` 变量，具体默认值可能随版本变动，直接对照部署版本的源码最准：

| 分类 | 源码位置与变量名 |
| --- | --- |
| 聊天与语音 | [hook/client.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/client.cpp)：`ms_log_chat`、`ms_chat_block_whitespace`、`ms_fix_voice_chat`、`ms_voice_lag_should_kick` |
| A2S 玩家查询 | [hook/engine.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/engine.cpp)：`ms_fix_server_query_players` |
| 实体输入、推力与接触 | [hook/entity.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/entity.cpp)：`ms_trigger_push_fixes_enabled`、`ms_trigger_push_scale`、`ms_entity_io_enhancement`、`ms_entity_io_verbose_logging`、`ms_fix_entities_touching_list` |
| 移动与玩家指令 | [MovementManager.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/extern/MovementManager.cpp)：`ms_disable_usercmd_subtick_moves`、`ms_disable_usercmd_subtick_input`、`ms_fix_usercmd_rapid_fire`、`ms_water_ground_jump_velocity` |
| 网络传输 | [TransmitManager.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/extern/TransmitManager.cpp)：`ms_transmit_block_dead_player_pawn`、`ms_transmit_block_ownerless_pawn` |
| Valve 日志 | [ValveConsoleLog.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/extern/ValveConsoleLog.cpp)：`ms_block_valve_log` |
| 队伍限制与 GC 冷却 | [hook/gamerule.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/gamerule.cpp)：`ms_override_team_limit`、`ms_fix_kick_cooldown` |
| SpawnGroup | [hook/server.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/server.cpp)：`ms_fix_spawngroups_leak` |
| 地图音乐 | [hook/sound.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/sound.cpp)：`ms_hook_map_music`、`ms_map_music_threshold` |

几个容易记错的默认值：

- `ms_log_chat` 默认是 **false**。
- `ms_voice_lag_should_kick` 默认是 **false**：检测到语音卡服包时只丢包，不踢人。
- `ms_water_ground_jump_velocity` 默认是 **145**，范围 **100–1000**。
- `ms_block_valve_log`：`0` 按 Valve 默认过滤，`1` 全放行，`2` 全屏蔽。带 `-debug` 启动时默认 `1`，否则默认 `2`。这和托管层 `core.json` 的 `Logger.LogLevel` 互不影响。

其他模块也会自行注册 `ms_` 开头的 ConVar，上面只列出了核心引擎层定义的变量。
