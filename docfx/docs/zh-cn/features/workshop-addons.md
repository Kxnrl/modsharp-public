# 创意工坊插件 (Workshop Addons)

ModSharp 可以在地图之外向客户端分发创意工坊插件。

## 用法

`Sharp.Modules.AddonManager` 模块默认启用，请在 `core.json` 中列出插件：

```json
{
  "AddonManager": {
    "Addons": [123123123123, 123123123456],
    "ClientAddons": []
  }
}
```

`core.json` 在第一张地图加载前读取，因此不需要任何启动参数。
运行时也可以通过下方的 ConVar / 命令，或模块接口 `IAddonManager` 修改插件。

也可以改用启动参数 `-addons`，它会覆盖 `core.json` 中的 `Addons`：

```text
./cs2 -dedicated ... +host_workshop_map 300123123123 -addons "123123123123,123123123456"
```

> [!WARNING]
> 旧的启动参数 `-dual_addon` 已被移除，带此参数时服务端将无法启动。请改用 `-addons` 或 `core.json`。

## 分发流程

分发流程基于 [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager)：
客户端每缺一个插件重连一次，工坊地图与官方地图均可用。
服务端缺少的插件会自动下载，下载完成后重新加载地图。

默认情况下，客户端在每次换图和重进时都会重新经历重连。
设置 `ms_cache_clients_with_addons 1` 可让已拥有插件的客户端跳过重连，但带缓存重进时会一次性收到全部插件，
若期间有插件更新，可能导致客户端卡死。

## 不使用模块

不安装模块时 `-addons` 依然可用：服务端仍会挂载（并下载）插件，并以相同流程分发给客户端。
模块提供的功能均不可用：`core.json` 配置、`ClientAddons`、下方的 ConVar / 命令以及模块接口 `IAddonManager`。

> [!WARNING]
> 不使用模块时客户端缓存为**启用**状态（600 秒），与模块默认的 `ms_cache_clients_with_addons 0` 不同，
> 因此在此期间重进的客户端会一次性收到全部插件（见上文）。
> 连接超时同样为禁用状态，与模块默认的 `ms_addon_connection_timeout 30` 不同。

## 仅服务端文件（`sharp/assets`）

存在 `sharp/assets` 文件夹时，会将其加入服务端的 `GAME` 搜索路径。
其中的文件只由服务端读取，不会发送给客户端。

若希望服务端从散文件（例如由插件解包）读取插件内容而不挂载其 VPK，
请将文件放入 `sharp/assets`，并把该插件写入 `ClientAddons`：客户端仍从创意工坊下载，服务端既不挂载也不下载。

## ConVar

| ConVar | 默认值 | 说明 |
|---|---|---|
| `ms_extra_addons` | `core.json` 中的值 | 服务端插件，逗号分隔，下次换图生效。 |
| `ms_client_extra_addons` | `core.json` 中的值 | 分发给所有客户端的插件（仅下载）。 |
| `ms_block_disconnect_messages` | `false` | 客户端为插件重连时隐藏 "loop shutdown" 断开消息。 |
| `ms_addon_mount_download` | `false` | 每次地图开始时重新下载（更新）服务端插件。 |
| `ms_extra_addons_timeout` | `10` | 下载下一个插件时允许的重连间隔（秒）。 |
| `ms_addon_connection_timeout` | `30` | 接收第一个插件的超时时间（秒），超时踢出，0 为禁用。 |
| `ms_cache_clients_with_addons` | `false` | 记住客户端已下载的插件，换图 / 重进时跳过重连。 |
| `ms_cache_clients_duration` | `0` | 记住的时长（秒），0 为永久。 |
| `ms_addon_debug` | `false` | 输出下载流程的详细调试信息。 |

## 命令

| 命令 | 说明 |
|---|---|
| `ms_add_addon <id>` / `ms_remove_addon <id>` | 编辑服务端插件。 |
| `ms_add_client_addon <id>` / `ms_remove_client_addon <id>` | 编辑全局客户端插件。 |
| `ms_download_addon <id>` | 在服务端下载插件。 |
| `ms_reload_map` | 重新加载当前地图以应用修改。 |
| `ms_addon_refresh`（客户端） | 插件未能送达时重新获取。 |

## 运行时 API

其他模块通过模块接口 `IAddonManager`（`Sharp.Modules.AddonManager.Shared`）使用：

| 方法 | 用途 |
|---|---|
| `GetAddons()` / `AddAddon` / `RemoveAddon` / `ClearAddons` | 服务端插件，下次换图生效，或传入 `reloadMap: true` 立即生效。 |
| `GetClientAddons` / `AddClientAddon` / `RemoveClientAddon` / `ClearClientAddons` | 分发给所有客户端（`default` SteamID）或单个客户端的仅下载插件。 |
| `RefreshClient(steamId)` | 向未能收到插件的在线客户端重新发送全部插件（客户端会重连）。 |
| `DownloadAddon(id, reloadMap)` | 在服务端下载（更新）插件。 |
| `ReloadMap()` | 重新加载当前地图。服务端已有的工坊地图使用 `ds_workshop_changelevel`，否则使用 `host_workshop_map`。 |

> [!NOTE]
> `ISharedSystem.GetAddonManager()`（`Sharp.Shared.Managers.IAddonManager`）是模块所依赖的底层 API，
> 仅在编写自己的插件管理器时使用。若同时引用两个命名空间，请为其中一个起别名
> （例如 `using IAddonManager = Sharp.Modules.AddonManager.Shared.IAddonManager;`）。

参见示例：[IAddonManager](../examples/addon-manager.md)
