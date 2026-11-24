# 管理员配置（AdminManager）

管理员权限由 **AdminManager** 负责，读取两个配置文件：

- `sharp/configs/admins.jsonc`：定义权限集合、身份组和详细的管理员配置。
- `sharp/configs/admins_simple.jsonc`：可选的快捷配置，把已有身份组直接绑给指定 SteamID64。

两个文件都支持注释、尾随逗号，属性名不区分大小写。完整的内置身份组和权限见[默认 `admins.jsonc`](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/admins.jsonc)、[完整注释示例](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/admins.jsonc.example)和[简易示例](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/admins_simple.jsonc.example)。

> [!WARNING]
> 旧版 **AdminFlatFile** 读的是 `admins.json`，基于已废弃的核心管理员 API，计划在 2.2 移除。它不认 `.jsonc` 文件，升级时请按新格式迁移，别直接把旧文件改后缀。

## 添加 Root 管理员

1. 保留发行包自带的 `admins.jsonc`（丢了就从 `admins.jsonc.example` 拷一份），确保 `Roles` 里有一个带 `"*"` 权限的 `root` 身份组，并把 `Admins` 里的示例 SteamID 清掉或改掉。
2. 把 `admins_simple.jsonc.example` 复制为 `admins_simple.jsonc`，填上你自己的 SteamID64：

   ```json
   {
     "76561198000000001": "root"
   }
   ```

   这里必须填 64 位数字 SteamID，不能填自定义个人资料 URL，也不能填 `STEAM_0:...` 或 `[U:1:...]` 格式。
3. 在服务端控制台跑 `ms_reload_admins`（或重启），再跑 `ms_admins <steamid64>` 确认权限已经生效。

配合 **CommandCenter** 加载时，**服务端控制台**可用以下三条指令：

| 指令 | 用途 |
| --- | --- |
| `ms_perms` | 按模块和集合列出所有已注册权限 |
| `ms_admins [steamid64]` | 列出所有管理员，或查某个管理员的权限及来源 |
| `ms_reload_admins` | 重新读取 `admins.jsonc` 和 `admins_simple.jsonc` |

改完配置文件不会自动生效，需要手动 `ms_reload_admins`，重载后记得查看日志。只有当 `admins.jsonc` 和 `admins_simple.jsonc` 合并后的结果完全为空（没有任何权限集合、身份组和管理员）时，内存里原来生效的清单才会保持不动；如果 `admins.jsonc` 解析失败而 `admins_simple.jsonc` 仍有有效条目，重载仍会挂载这份缺少身份组定义的清单，导致这些管理员暂时失去权限。

## 简易分配（`admins_simple.jsonc`）

键是 SteamID64 字符串，值是单个身份组名或身份组名数组：

```json
{
  "76561198000000001": "root",
  "76561198000000002": ["moderator", "helper"]
}
```

这里填的身份组都必须在 `admins.jsonc` 里定义过，填了不存在的身份组会打警告且不给任何权限。如果同一个 SteamID 已经在 `admins.jsonc` 里配置过了，`admins_simple.jsonc` 里对应的那条会被直接跳过，不会合并。

## 完整清单（`admins.jsonc`）

顶层分三块：

- `PermissionCollection`：给一组权限字符串起个集合名。权限可以在这里登记，也可以由模块代码注册；后面的通配符规则就是基于已注册的权限表展开的。
- `Roles`：身份组列表，每项含 `Name`、`Immunity`（0–255，不填默认 0）和 `Permissions`。
- `Admins`：管理员列表，每项含 `Identity`（SteamID64，写 JSON 数字或字符串都行）、`Immunity`（0–255，不填默认 0）和 `Permissions`。

管理员最终生效的免疫等级，取他自身填的数值和所有继承身份组数值里的**最大值**（自身填低了不会压掉身份组的高免疫）。如果 `admins.jsonc` 里对同一个 SteamID 写了多条记录，权限会合并，免疫等级取最高值。免疫检查由具体执行踢人/封禁等目标操作的指令或模块负责。

下面是一个精简示例（展示结构用，完整默认权限以自带文件为准）：

```json
{
  "PermissionCollection": {
    "admin": ["admin:kick", "admin:ban"]
  },
  "Roles": [
    { "Name": "root", "Immunity": 255, "Permissions": ["*"] },
    { "Name": "moderator", "Immunity": 40, "Permissions": ["admin:kick", "admin:ban"] }
  ],
  "Admins": [
    { "Identity": "76561198000000001", "Permissions": ["@root"] },
    { "Identity": "76561198000000002", "Permissions": ["@moderator", "!admin:ban"] }
  ]
}
```

### 权限规则

权限名和身份组名都不区分大小写：

| 写法 | 效果 |
| --- | --- |
| `@RoleName` | 递归继承该身份组的权限和免疫等级 |
| `!admin:ban` | 拒绝该权限，优先级高于任何来源的授权 |
| `*` | 授予全部已注册权限 |
| `admin:*` | 授予 `admin:` 下所有已注册权限（含更深子级） |
| `admin:*:give` | 只匹配中间一段，如匹配 `admin:items:give`，不匹配 `admin:items:sub:give` |

`*` 是按冒号分段匹配的，不是子串通配，拒绝规则 `!` 同样支持通配写法。注意 `@RoleName` 只在当前模块自己的清单里找身份组，不会跨模块借用别的模块定义的身份组。

当多个模块同时给同一个 SteamID 分配了权限时，最终结果是“合并所有授权 -> 减掉所有拒绝项”，免疫等级取最大值。之后新注册进来的权限会自动补进直接写在管理员上的通配规则里；只通过 `@RoleName` 继承来的通配规则会在重新挂载清单或执行 `ms_reload_admins` 时重新展开。随时可以用 `ms_perms` 和 `ms_admins` 查看实际生效结果。

## 在模块里接入

请使用 `Sharp.Modules.AdminManager.Shared` 的 `IAdminManager`（不要用已废弃的 `IClientManager` 管理员方法）。调用 `MountAdminManifest(moduleIdentity, ...)` 和 `GetCommandRegistry(moduleIdentity)` 时请传同一个稳定的模块标识（通常用程序集名）。如果模块要提供自己的权限集合、身份组或管理员清单，先调 `MountAdminManifest`；只注册指令的模块可以直接调 `GetCommandRegistry`，再通过 `RegisterPermissions` 登记权限后注册指令（指令注册表需要同时加载 CommandCenter）。

这两个方法都要在游戏主线程调用；如果从数据库或后台任务拉完数据，先用 `IModSharp.InvokeFrameAction` 或 `InvokeFrameActionAsync` 切回主线程。完整代码见[管理员示例](../examples/admin-example.md)和 [IAdminManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/AdminManager/Shared/IAdminManager.cs)。
