# ConVar

## 示例

可通过示例项目了解如何操作**ConVar**，[跳转链接](../examples/convar.md)

---

## 内置 ConVars

| ConVar | 描述 | 默认值 | 范围 |
|--------|------|--------|------|
| ms_log_chat | 记录文字聊天消息 | true | - |
| ms_chat_block_whitespace | 屏蔽空格消息 | true | - |
| ms_fix_voice_chat | 修复语音聊天 | true | - |
| ms_fix_server_query_players | 修复A2S查询用户信息 | false | - |
| ms_trigger_push_fixes_enabled | 启用修复：trigger_push | false | - |
| ms_trigger_push_scale | 设置 trigger_push 倍率调整 | 1 | 0.001 - 1000 |
| ms_entity_io_enhancement | 启用实体IO增强 | false | - |
| ms_entity_io_verbose_logging | 启用实体IO调试记录 | false | - |
| ms_fix_entities_touching_list | 修复实体接触列表并打乱顺序 | true | - |
| ms_disable_usercmd_subtick_moves | 移除subtick移动 | false | - |
| ms_disable_usercmd_subtick_input | 移除subtick输入指令 | false | - |
| ms_fix_usercmd_rapid_fire | 修复快速开火指令 | false | - |
| ms_transmit_block_dead_player_pawn | 屏蔽Transmit场景下死亡玩家的pawn | false | - |
| ms_transmit_block_ownerless_pawn | 屏蔽Transmit场景下无主pawn | false | - |
| ms_block_valve_log | 屏蔽V社的日志输出: 0 = 默认, 1 = 全开, 2 = 全关 | -debug启动项下为 1，否则为 2 | 0 - 2 |
| ms_override_team_limit | 覆盖队伍限制 | false | - |
| ms_fix_kick_cooldown | 修GC冷却时间问题 | true | - |
| ms_fix_spawngroups_leak | 修SpawnGroup泄露问题 | false | - |
| ms_hook_map_music | 外挂地图音乐 | false | - |
| ms_map_music_threshold | 外挂地图音乐且转发至Transmit功能中 | 15 | 5 - 300 |
| ms_extra_addons | 服务端额外插件的工坊 ID（逗号分隔），详见[创意工坊插件](workshop-addons.md) | (启动参数值) | - |
| ms_client_extra_addons | 推送给所有客户端的工坊 ID（仅下载） | "" | - |
| ms_extra_addons_timeout | 客户端下载插件期间允许的重连超时（秒） | 10 | 1 - 600 |
| ms_extra_addons_debug | 输出额外插件流程的详细调试日志 | false | - |
| ms_cache_clients_with_addons | 缓存客户端已下载的插件列表，重连时跳过重复下载 | false | - |
| ms_cache_clients_duration | 缓存保留时长（秒，0 表示永久） | 0 | 0 - ∞ |
| ms_block_disconnect_messages | 屏蔽客户端为下载插件而重连时的 "loop shutdown" 消息 | false | - |
| ms_addon_mount_download | 挂载时强制重新下载插件，即使已安装 | false | - |
