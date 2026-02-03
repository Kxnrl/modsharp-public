# Admin Commands SQL Storage

AdminCommands 的可选 SQL 存储模块（ban、mute、gag 等）。
该模块也可作为自定义存储实现的参考。

## 启用

加载器只扫描 `sharp/modules` 的顶层目录。
官方构建会把模块放在 `sharp/modules/AdminCommands.SQLStorage`，并通过 `.disabled` 标记默认禁用。
要启用，请删除 `.disabled` 文件。

## 连接字符串

格式：

```text
{schema}://{connectionString}
```

支持的 schema：

- `mysql`
- `pgsql` / `postgres` / `postgresql`

示例：

```json
"ConnectionStrings": {
  "AdminCommands.SQLStorage": "mysql://Server=localhost;Port=3306;Database=modsharp;User ID=modsharp;Password=secret;"
}
```

## 自定义存储

实现 `Sharp.Modules.AdminCommands.Shared` 中的 `IAdminOperationStorageService`，
并使用 `IAdminOperationStorageService.Identity` 注册。
完整示例可参考 `AdminCommands.SQLStorage` 模块源码。
