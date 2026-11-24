# ConVar

ConVars are engine console variables, separate from the JSON settings in `core.json`. Change them from the server console or server `.cfg` files.

## Module API

- `IConVarManager.FindConVar(name)`: case-sensitive lookup; returns `null` if missing. Pass `useIterator: true` to find hidden ConVars at the cost of a slower scan.
- `CreateConVar`: typed overloads with optional min/max bounds for numeric types. Check the nullable return value.
- `InstallChangeHook` / `RemoveChangeHook`: remove the exact callback in `Shutdown`. Modifying a ConVar from inside its own change callback will recurse until a stack overflow.

Match your read/write calls to `IConVar.Type`. Watch out for `Set(float)`: it targets floating-point ConVars, yet C# will also bind `long`, `uint`, or `ulong` arguments to it, which throws `InvalidOperationException` on integer ConVars. Use `Set(int)` for `int`-range integers, and `Set(string)` for larger integer values.

The two string setters handle invalid input differently:

- `Set(string)` throws `ArgumentException` on parse failures.
- `SetString(string)` converts permissively: invalid numbers become `0`, booleans are true only for `1` or case-insensitive `true`, and invalid color/vector text falls back to type-specific defaults (this does **not** reset to the ConVar's `DefaultValue`).

Prefer `Set(string)` when you want invalid input rejected with an exception. `ReplicateToClient` sends a value to a single client without changing the server value.

ConVars cannot be disposed or destroyed through the public API; you only remove your change hooks.

See [IConVarManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ConVarManager.cs), [IConVar](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Objects/IConVar.cs), and the [ConVar example](../examples/convar.md).

## Built-in Variables

ModSharp's native core registers a set of `ms_` ConVars. Exact defaults can shift across releases, so check the source matching your server build:

| Area | Source and names |
| --- | --- |
| Chat and voice | [hook/client.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/client.cpp): `ms_log_chat`, `ms_chat_block_whitespace`, `ms_fix_voice_chat`, `ms_voice_lag_should_kick` |
| A2S player queries | [hook/engine.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/engine.cpp): `ms_fix_server_query_players` |
| Entity inputs, push, touching | [hook/entity.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/entity.cpp): `ms_trigger_push_fixes_enabled`, `ms_trigger_push_scale`, `ms_entity_io_enhancement`, `ms_entity_io_verbose_logging`, `ms_fix_entities_touching_list` |
| Movement and usercmds | [MovementManager.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/extern/MovementManager.cpp): `ms_disable_usercmd_subtick_moves`, `ms_disable_usercmd_subtick_input`, `ms_fix_usercmd_rapid_fire`, `ms_water_ground_jump_velocity` |
| Network transmission | [TransmitManager.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/extern/TransmitManager.cpp): `ms_transmit_block_dead_player_pawn`, `ms_transmit_block_ownerless_pawn` |
| Valve logging | [ValveConsoleLog.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/extern/ValveConsoleLog.cpp): `ms_block_valve_log` |
| Team limits and GC cooldown | [hook/gamerule.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/gamerule.cpp): `ms_override_team_limit`, `ms_fix_kick_cooldown` |
| Spawn groups | [hook/server.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/server.cpp): `ms_fix_spawngroups_leak` |
| Map music | [hook/sound.cpp](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/sound.cpp): `ms_hook_map_music`, `ms_map_music_threshold` |

A few defaults that often trip people up:

- `ms_log_chat` defaults to **false**.
- `ms_voice_lag_should_kick` defaults to **false**: detected voice-lag packets are dropped instead of kicking the player.
- `ms_water_ground_jump_velocity` defaults to **145** (range **100–1000**).
- `ms_block_valve_log`: `0` uses Valve's default filtering, `1` allows all, `2` blocks all. Defaults to `1` when launched with `-debug`, otherwise `2`. This is separate from managed `Logger.LogLevel` in `core.json`.

Modules may also register their own `ms_` ConVars beyond the core engine ones listed above.
