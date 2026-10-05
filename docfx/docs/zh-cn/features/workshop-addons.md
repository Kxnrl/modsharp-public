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
| `RefreshClient(steamId)` | 向未能收到插件的游戏中客户端重新发送插件（客户端会重连），可自行绑定到命令。 |
| `InstallAddonListener(listener)` | 通过 `IAddonListener.OnClientQueryAddons` 向指定客户端额外分发插件。 |

没有 ConVar 和控制台命令，更多功能请做成模块。

示例：[IAddonManager](../examples/addon-manager.md)
