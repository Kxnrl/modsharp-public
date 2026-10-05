# 创意工坊插件 (Workshop Addons)

ModSharp 可以在地图之外向客户端分发创意工坊插件。

## 用法

在启动参数中加上 `-dual_addon`：

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -dual_addon 123123123123
```

多个插件用逗号分隔：

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -dual_addon "123123123123,123123123456"
```

使用插件功能（包括运行时 API）必须指定 `-dual_addon`。

## 分发流程

每次换图时自动选择：

| 插件数量 | 流程 |
|---|---|
| 1 | **DualAddon**：久经考验的单插件流程，仅在工坊地图上生效。 |
| 2 个以上，或安装了 `IAddonListener` | **MultiAddon**：基于 [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager)，客户端每缺一个插件重连一次，官方地图同样可用。 |

大多数服务器只需要一个插件，所以默认就是 DualAddon。

已下载过插件的客户端会被缓存 10 分钟，重连时跳过下载流程。
服务端缺少的插件会自动下载，全部完成后自动重载地图。

## 运行时 API

`IAddonManager`（通过 `ISharedSystem.GetAddonManager()` 获取）只保留最小功能：

| 方法 | 用途 |
|---|---|
| `GetAddons()` | 下次换图时生效的服务端插件。 |
| `SetAddons(ids)` | 替换服务端插件，需要自行换图才会生效（最好重启）。 |
| `ResetClientCache(steamId)` | 清除某个客户端（`default` 为全部）的已下载缓存。 |
| `RefreshClient(steamId, resetCache)` | 向游戏中的客户端重新发送插件（客户端会重连）。`resetCache: true` 用于未收到插件时全部重发，`false` 只发送尚未拥有的插件。 |
| `UpdateAddon(id)` | 强制更新工坊插件，期间会卸载并重新挂载（Windows 会锁定已挂载文件）。 |
| `ReloadMap()` | 重载当前地图。服务器已有的工坊地图使用 `ds_workshop_changelevel`，否则使用 `host_workshop_map`。 |
| `SetOptions(options)` | MultiAddon 流程的超时、客户端缓存与调试日志。 |
| `InstallAddonListener(listener)` | 通过 `IAddonListener.OnClientQueryAddons` 向指定客户端额外分发插件。 |

没有 ConVar 和控制台命令，更多功能请做成模块。

## Extra Addon Manager 模块

`Sharp.Modules.ExtraAddonManager` 基于 `IAddonManager` 提供 [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager) 的功能。
安装后始终使用 MultiAddon 流程，其他模块可通过 `IExtraAddonManager` 调用。

| ConVar | 默认值 | 说明 |
|---|---|---|
| `ms_extra_addons` | `-dual_addon` 的值 | 服务端插件，逗号分隔，下次换图生效。 |
| `ms_client_extra_addons` | `""` | 分发给所有客户端的插件（仅下载）。 |
| `ms_block_disconnect_messages` | `false` | 屏蔽客户端为下载插件重连时的 "loop shutdown" 消息。 |
| `ms_addon_mount_download` | `false` | 每次开图时重新下载（更新）服务端插件。 |
| `ms_extra_addons_timeout` | `10` | 客户端下载下一个插件时允许的重连间隔（秒）。 |
| `ms_addon_connection_timeout` | `30` | 接受第一个插件的超时时间（秒），超时踢出，0 为禁用。 |
| `ms_cache_clients_with_addons` | `false` | 记住客户端已下载的插件，换图 / 重进时跳过重连。 |
| `ms_cache_clients_duration` | `0` | 记住的时长（秒），0 为永久。 |
| `ms_addon_debug` | `false` | 输出下载流程的详细信息。 |

| 命令 | 说明 |
|---|---|
| `ms_add_addon <id>` / `ms_remove_addon <id>` | 修改服务端插件。 |
| `ms_add_client_addon <id>` / `ms_remove_client_addon <id>` | 修改全局客户端插件。 |
| `ms_download_addon <id>` | 在服务端下载插件。 |
| `ms_reload_map` | 重载当前地图以应用修改。 |
| `ms_addon_refresh`（客户端） | 插件未收到时重新获取。 |

不提供搜索路径打印。

示例：[IAddonManager](../examples/addon-manager.md)
