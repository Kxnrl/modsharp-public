# 玩家偏好（Client Preferences）

官方 **ClientPreferences** 模块用来持久化玩家 Cookie。只要加载了这个模块，就必须在 `sharp/configs/core.json` 里配置 `ConnectionStrings.ClientPreferences`。

## 连接串格式

```text
{schema}://{connectionString}
```

schema 不区分大小写，目前支持四种后端：

| Schema | 存储后端 | 连接串内容 |
| --- | --- | --- |
| `litedb` | 本地 LiteDB 文件 | LiteDB 连接字符串，`{sharp::data}` 会自动替换成当前服的 `sharp/data` 目录 |
| `resp` | Redis、Garnet 等兼容 RESP 的服务 | StackExchange.Redis 配置字符串 |
| `mysql` | MySQL / MariaDB | MySqlConnector 连接字符串 |
| `http` | 自定义 HTTP 服务 | 分号分隔的 `Host=...;Authorization=...` |

驱动选择见 [ClientPreferences.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/ClientPreferences/src/ClientPreferences.cs)，各后端源码在 [Storages 目录](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Modules/ClientPreferences/src/Storages)。注意 `AdminCommands.SQLStorage` 支持的 PostgreSQL 在这里不能用。

## 默认配置

```json
{
  "ConnectionStrings": {
    "ClientPreferences": "litedb://Filename={sharp::data}/client-preferences.db;Mode=Exclusive;Flush=true"
  }
}
```

这是发行包 `core.json` 里自带的写法（删掉这个键不会自动回退）。LiteDB 驱动会自动建 `sharp/data` 目录，也只有它支持 `{sharp::data}` 占位符。

默认的 `Mode=Exclusive` 是独占模式，多台服务器不能共用同一个本地文件。多服部署请给每台服配独立文件，或者直接上 Redis / MySQL / HTTP；跑在容器里的话记得把数据目录挂载持久化。

运行中改连接串时，模块只会打一条警告、不会实时切换，改完需要重启服务器。

## HTTP 后端

`Host` 必填，`Authorization` 可选（默认 `modsharp`），选项名不区分大小写。开头的 `http://` 只是用来选中 HTTP 驱动，`Host` 本身照样可以写 `https://`：

```text
http://Host=https://preferences.example.com;Authorization=replace-with-your-value
```

自建后端按 [RestStorage.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/ClientPreferences/src/Storages/RestStorage.cs) 实现两个接口即可：

- `GET /cookie/{identity}`：返回该玩家的 Cookie 数组。
- `PUT /cookie/{identity}`：请求体为该玩家的 Cookie 数组。

`identity` 是玩家的 SteamID64，JSON 字段名和类型要和 [CookieModel.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/ClientPreferences/src/Models/CookieModel.cs) 保持一致。

## 在模块里使用

引用 `Sharp.Modules.ClientPreferences.Shared` 获取 `IClientPreference`。玩家数据是异步加载的，读写前先查 `IsLoaded` 或监听 `ListenOnLoad`（加载完成前 `GetCookie` 返回 `null`，`SetCookie` 会抛异常），模块卸载时释放 `ListenOnLoad` 返回的订阅句柄。详见[玩家偏好示例](../examples/client-preferences.md)。
