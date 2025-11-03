# ConVar

## Examples

You can learn how to work with **ConVar** through the example project, [Jump Link](../examples/convar.md)

## Built-in ConVars

| ConVar | Description | Default | Range |
|--------|-------------|---------|-------|
| ms_log_chat | Log text chat messages | true | - |
| ms_chat_block_whitespace | Block whitespace messages | true | - |
| ms_fix_voice_chat | Fix voice chat | true | - |
| ms_fix_server_query_players | Fix A2S query user info | false | - |
| ms_trigger_push_fixes_enabled | Enable fix: trigger_push | false | - |
| ms_trigger_push_scale | Set trigger_push scale adjustment | 1 | 0.001 - 1000 |
| ms_entity_io_enhancement | Enable entity IO enhancement | false | - |
| ms_entity_io_verbose_logging | Enable entity IO verbose logging | false | - |
| ms_fix_entities_touching_list | Fix entity touching list and randomize order | true | - |
| ms_disable_usercmd_subtick_moves | Remove subtick moves | false | - |
| ms_disable_usercmd_subtick_input | Remove subtick input commands | false | - |
| ms_fix_usercmd_rapid_fire | Fix rapid fire commands | false | - |
| ms_transmit_block_dead_player_pawn | Block dead player pawn in Transmit scenarios | false | - |
| ms_transmit_block_ownerless_pawn | Block ownerless pawn in Transmit scenarios | false | - |
| ms_block_valve_log | Block Valve's log output: 0 = default, 1 = all on, 2 = all off | 1 with -debug launch option, otherwise 2 | 0 - 2 |
| ms_override_team_limit | Override team limit | false | - |
| ms_fix_kick_cooldown | Fix GC cooldown issue | true | - |
| ms_fix_spawngroups_leak | Fix SpawnGroup leak issue | false | - |
| ms_hook_map_music | Hook map music | false | - |
| ms_map_music_threshold | Hook map music and forward to Transmit functionality | 15 | 5 - 300 |
