# 创意工坊插件 (Workshop Addons)

ModSharp 提供两种向客户端分发创意工坊插件的方式：
**DualAddon** 适用于附加单个插件的场景，**ExtraAddon**（移植自
[MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager)）适用于其他所有场景。

两者互斥，请根据需求选择其一。

## 如何选择

| 你的需求 | 推荐 |
|---|---|
| 创意工坊地图 + **1 个**额外插件 | **DualAddon** |
| 创意工坊地图 + **多个**额外插件 | **ExtraAddon** |
| 给不同玩家分发不同插件 | **ExtraAddon** |
| 运行时添加 / 移除插件（无需重启） | **ExtraAddon** |
| 在 V 社官方地图上分发插件 | **ExtraAddon** |

对于大多数只需要在工坊地图上挂载一个内容包的服务器，DualAddon 更轻量也更稳定。

> [!WARNING]
> `-dual_addon` 与 `-extra_addons` 不能同时使用，否则会在启动时触发致命错误。

---

## DualAddon

在工坊地图上额外分发**一个**工坊插件。

### 用法

在启动参数中加上 `-dual_addon <workshopId>`：

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -dual_addon 123123123123
```

仅此而已，没有 ConVar，也没有运行时 API。

### 说明

- 客户端连接时分发该插件。玩家会经历两次重连：第一次下载工坊地图，第二次下载 dual addon。
- DualAddon 仅在服务器运行**工坊地图**时生效。在 V 社官方地图（如 `de_dust2`）上不会分发插件，
  此时请使用 ExtraAddon。
- 该插件会被自动挂载到服务端。

---

## ExtraAddon

支持任意数量的工坊插件，可针对特定客户端分发，并提供运行时 API 可在不重启服务器的情况下增删插件。

### 用法

在启动参数中加上 `-extra_addons <id1>,<id2>,...`：

```text
./cs2 -dedicated -port 27015 ... +host_workshop_map 300123123123 -extra_addons "123123123123,123123123456,123123123789"
```

同样的列表也通过 `ms_extra_addons` ConVar 公开，可通过控制台命令或 C# API 在运行时修改。

### 重连次数

每个需要下载的插件都需要客户端重连一次。开启缓存相关的 ConVar 后（生产环境推荐），
仅首次访问时需要重连，后续连接可直接进入。

### ConVars

| ConVar | 默认值 | 描述 |
|---|---|---|
| `ms_extra_addons` | (启动参数值) | 服务端额外插件的工坊 ID，逗号分隔，修改时会触发重新挂载。 |
| `ms_client_extra_addons` | `""` | 推送给所有客户端的工坊 ID（仅下载，不在服务端挂载）。 |
| `ms_extra_addons_timeout` | `10` | 客户端下载插件期间允许的重连超时（秒），范围 1 – 600。 |
| `ms_extra_addons_debug` | `false` | 输出插件分发流程的详细调试日志。 |
| `ms_cache_clients_with_addons` | `false` | 缓存每个客户端已下载的插件，重连时跳过重复下载。 |
| `ms_cache_clients_duration` | `0` | 客户端缓存保留时长（秒），`0` 表示永久。 |
| `ms_block_disconnect_messages` | `false` | 屏蔽客户端为下载插件而重连时产生的 "loop shutdown" 断开消息。 |
| `ms_addon_mount_download` | `false` | 挂载时强制重新下载插件，即使已安装。 |

### 控制台命令

| 命令 | 描述 |
|---|---|
| `ms_add_addon <id>` | 向服务端额外插件列表添加工坊 ID。 |
| `ms_remove_addon <id>` | 从服务端额外插件列表移除工坊 ID。 |
| `ms_add_client_addon <id>` | 向全局客户端插件列表添加工坊 ID。 |
| `ms_remove_client_addon <id>` | 从全局客户端插件列表移除工坊 ID。 |
| `ms_download_addon <id>` | 手动加入下载队列。 |
| `ms_print_searchpaths` | 打印当前文件系统搜索路径（服务端控制台）。 |
| `ms_print_searchpaths_client` | 打印当前文件系统搜索路径（客户端 / listenserver）。 |

### 运行时 API

`IAddonManager` 接口（通过 `ISharedSystem.GetAddonManager()` 获取）暴露了 ExtraAddon 的完整 API，常用方法如下：

| 方法 | 用途 |
|---|---|
| `ExtraAddonAddAddon(id, refresh)` | 运行时添加服务端插件。 |
| `ExtraAddonRemoveAddon(id, refresh)` | 运行时移除服务端插件。 |
| `ExtraAddonAddClientAddon(id, steamId, refresh)` | 给特定客户端（`steamId=0` 表示所有客户端）添加插件。 |
| `ExtraAddonRemoveClientAddon(id, steamId)` | 移除客户端插件。 |
| `ExtraAddonRefreshAddons(reloadMap)` | 重新挂载所有服务端插件。 |
| `ExtraAddonReloadMap()` | 重载当前地图。 |
| `ExtraAddonGetMountedAddons()` | 查看当前已挂载的插件。 |
| `ExtraAddonHasUGCConnection()` | 检查 SteamUGC 是否可用。 |

完整使用示例参见：[Extra Addon Example](../examples/extra-addon.md)
