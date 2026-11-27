# 管理指令 SQL 存储

**AdminCommands.SQLStorage** 是 AdminCommands 的可选数据库后端，用来持久化封禁、语音禁言、文字禁言等处罚记录（不启用时，AdminCommands 默认存在本地 `sharp/data/punishments.json`）。管理员身份组和权限不归它管，那部分在 [AdminManager](admin.md) 里配置。

## 启用方式

发行包里的 `sharp/modules/AdminCommands.SQLStorage` 默认带一个 `.disabled` 文件。要启用它，先在 `core.json` 里配好真实的数据库连接，删掉 `.disabled`，再重启服务器即可（`AdminCommands` 及它依赖的官方模块都要保持启用）。

模块会在 `Init` 里自动建表，前提是目标数据库本身已经创建好，且账号有建表和读写权限；连不上或建表失败会阻止模块启动。

## 连接字符串

在 `sharp/configs/core.json` 的 `ConnectionStrings` 下配置键名带点号的 `"AdminCommands.SQLStorage"`（不要拆成嵌套对象）：

```json
{
  "ConnectionStrings": {
    "AdminCommands.SQLStorage": "mysql://Server=localhost;Port=3306;Database=modsharp;User ID=modsharp;Password=replace-with-your-password;"
  }
}
```

格式同样是 `{schema}://{connectionString}`，schema 不区分大小写，支持两种数据库：

- `mysql`：MySQL / MariaDB
- `pgsql`、`postgres` 或 `postgresql`：PostgreSQL

如果完全没写 `AdminCommands.SQLStorage` 这个键，代码会回退去读 `ConnectionStrings.AdminCommands`；但如果写了模块专用键却留空，会直接判定配置无效，不再回退。

这里不支持 LiteDB、SQLite、RESP、HTTP，也不支持 `{sharp::data}` 占位符。连接串只在启动构造时读一次，修改后需要重启服务器。解析逻辑见 [SQLStorage.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/AdminCommands.SQLStorage/AdminCommands.SQLStorage/SQLStorage.cs)。

## 存储行为与自定义后端

内置实现只查询当前有效记录（未被解除且未过期）。给同一个 SteamID 新增同类型处罚时，会把之前生效的旧记录标为结束；解除处罚时也会记录解除信息，不会物理删除历史记录。表结构和查询见[模块源码](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Modules/AdminCommands.SQLStorage/AdminCommands.SQLStorage)。

想换其他存储后端的话，实现 `Sharp.Modules.AdminCommands.Shared` 里的 `IAdminOperationStorageService`，并用 `IAdminOperationStorageService.Identity` 注册出去就行。接口方法全是异步的，实现里不要直接碰游戏对象。参考 [IAdminOperationStorageService](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/AdminCommands/Shared/IAdminOperationStorageService.cs)。
