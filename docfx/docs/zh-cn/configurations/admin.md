# 管理员配置文件 (Admin FlatFile)

ModSharp 将管理员配置分为两个文件，以便于实现"快速分配"和"复杂权限管理"两种需求。

1.  **简易分配:** `{CS2}/sharp/configs/admins_simple.jsonc`
2.  **高级配置:** `{CS2}/sharp/configs/admins.jsonc`

## 5 分钟快速上手 — 添加一个 Root 管理员

如果你只是想尽快给自己加上最高权限，只需 3 步：

**第 1 步：复制配置文件**

将 `admins_simple.jsonc.example` 复制并重命名为 `admins_simple.jsonc`（位于 `{CS2}/sharp/configs/` 目录下）。

**第 2 步：填入你的 SteamID64**

打开 `admins_simple.jsonc`，将示例中的 SteamID 替换为你自己的：

```json
{
    "你的SteamID64": "root"
}
```

> [!TIP]
> **如何获取 SteamID64？**
> - 打开 Steam 客户端 → 个人资料 → 右键复制页面 URL，其中的数字即为 SteamID64。
> - 或者访问 [steamid.io](https://steamid.io) 或 [steamid.xyz](https://steamid.xyz)，输入你的 Steam 个人资料链接即可查询。
> - SteamID64 是一串 17 位数字，形如 `76561198000000001`。请勿使用 `STEAM_0:X:Y` 或 `[U:1:...]` 格式。

**第 3 步：让服务器加载配置**

在服务器控制台执行：

```
ms_reload_admins
```

或者直接重启服务器。完成后你就拥有 root 权限了。

> [!NOTE]
> `admins_simple.jsonc` 中使用的身份组名称（如 `root`、`admin`）必须在 `admins.jsonc` 中定义。默认的 `admins.jsonc.example` 已经预定义了 `root`、`senior_admin`、`admin`、`moderator`、`helper` 五个身份组。如果你需要使用这些身份组，请同时将 `admins.jsonc.example` 复制为 `admins.jsonc`。

---

## 服务器控制台命令速查

| 命令 | 说明 |
|------|------|
| `ms_perms` | 列出所有已注册的权限字符串 |
| `ms_admins` | 查看当前已加载的管理员及其权限 |
| `ms_reload_admins` | 热重载管理员配置（无需重启服务器） |

---

## 编程接入提示

如果你的模块还会调用 `IAdminManager.MountAdminManifest(...)` 和 `GetCommandRegistry(...)`，请在两处使用同一个、稳定的 `moduleIdentity`，并在多次调用中保持不变。

> [!IMPORTANT]
> `MountAdminManifest` 和 `GetCommandRegistry` 必须在游戏线程内调用。如果你在异步上下文或后台线程中（例如数据库查询回调后），请先通过 `IModSharp.InvokeFrameAction` 或 `IModSharp.InvokeFrameActionAsync` 回到游戏线程再调用。

推荐使用模块的 `AssemblyName`，例如：

```csharp
private static readonly string ModuleIdentity = typeof(MyModule).Assembly.GetName().Name ?? "MyModule";
```

## 简易配置 (`admins_simple.jsonc`)

使用此文件可通过简单的 **键值对 (Key-Value)** 格式快速将现有的 **身份组 (Roles)** 分配给用户。

*   **键 (Key):** 玩家的 SteamID64，以**字符串**形式书写（JSON 键必须是字符串）。
*   **值 (Value):** 身份组名称（字符串），或身份组名称数组。

**注意:**
1.  身份组名称（例如 "root", "admin"）**必须**在主文件 `admins.jsonc` 中定义。如果身份组名称不存在，该管理员将不会获得该身份组的任何权限，仅在服务器日志中输出警告。
2.  如果某个用户同时出现在此文件和主文件 `admins.jsonc` 中，此文件中的条目将被**跳过**（`admins.jsonc` 优先）。系统会输出日志提示以帮助你注意到这一点。

**示例:**
```json
{
    // 服主 / Root
    "76561198000000001": "root",

    // 普通管理员
    "76561198000000002": "admin",
    "76561198000000003": "admin",

    // 多角色: 管理员 + VIP
    "76561198000000004": ["admin", "vip"]
}
```

---

## 高级配置 (`admins.jsonc`)

此文件包含权限定义、身份组定义以及详细的管理员配置。它分为三个部分：

1.  **PermissionCollection:** 权限字符串的注册表，按键名（如 `"admin"`）分组。每个字符串（如 `"admin:kick"`）代表一个可授予/拒绝的操作。注册后的权限用于通配符展开（`admin:*`）、加载时验证以及 `ms_perms` 控制台命令列出。服务器运营者通常不需要修改此节 — 默认值已覆盖所有内置管理命令。
2.  **Roles:** 定义一组权限和免疫等级（即 **身份组**），可直接分配给管理员。
3.  **Admins:** 为特定的 SteamID 分配身份组和特定权限。

### 1. 结构

#### Roles (身份组对象)
*   `Name` (必填): 身份组名称（可通过 `@Name` 进行引用/继承）。
*   `Immunity` (选填): 免疫等级 (0-255)。
*   `Permissions` (必填): 权限字符串或继承身份组的列表。

#### Admins (管理员对象)
*   `Identity` (必填): 玩家的 SteamID64（如 `76561198000000001`）。JSON 中同时支持整数和 `"字符串"` 格式。
*   `Immunity` (选填): 覆盖身份组的免疫等级。系统取所有条目中发现的最高值。免疫等级高的管理员不会被免疫等级低的管理员执行操作（如踢出、封禁、处死等）。
*   `Permissions` (必填): 要继承的身份组列表，或要授予/拒绝的特定权限。

### 2. 权限语法规则

您可以在 **Roles** 和 **Admins** 的 `Permissions` 数组中使用以下语法：

*   **`@身份组名称`**: 继承某个身份组的所有权限和免疫等级（支持递归）。
*   **`!permission`**: 拒绝某项权限。这是全局性的，会覆盖其他任何授权。
*   **`*`**: 授予所有权限。
*   **`module:*`**: 授予特定模块内的所有权限（支持递归，例如同时匹配 `module:a` 和 `module:a:b`）。

#### 通配符匹配细节

`:` 是段分隔符，通配符的行为取决于其位置：

*   **尾部通配符**（位于模式末尾）：匹配所有剩余段。
    *   `admin:*` 匹配 `admin:ban`、`admin:ban:extended` 等。
    *   单独的 `*` 匹配所有已注册的权限。
*   **中间通配符**：仅匹配恰好一个段。
    *   `admin:*:give` 匹配 `admin:items:give`，但不匹配 `admin:items:sub:give`。

### 3. 通用规则
*   **不区分大小写:** `Admin:Kick` 与 `admin:kick` 视为相同。
*   **身份格式:** 必须使用 SteamID64。同时支持整数格式（`76561198000000001`）和字符串格式（`"76561198000000001"`）。请勿使用旧版格式 (STEAM_0:...) 或 SteamID3 格式 ([U:1:...])。

### 4. 多模块合并

当同一个 SteamID 出现在多个模块配置中时：
1.  **合并:** 用户获得所有条目的权限并集。
2.  **免疫等级:** 取所有条目中的最高值。
3.  **拒绝:** `!permission` 会覆盖来自任何模块的授权。
4.  **角色作用域:** `@RoleName` 仅在定义该管理员条目的模块自身的 Roles 中解析，不会查找其他模块的角色。

**示例:** 模块 A 定义了角色 `admin`，并将用户 `76561198000000001` 分配为 `@admin`。模块 B 定义了角色 `vip`，并将同一用户分配为 `@vip`。结果：
*   `@admin` 在模块 A 的 Roles 中解析 → 授予模块 A 的 admin 权限。
*   `@vip` 在模块 B 的 Roles 中解析 → 授予模块 B 的 vip 权限。
*   用户获得两者的并集。
*   如果模块 B 的管理员条目引用了 `@admin`，它**不会**找到模块 A 的角色 — 只会在模块 B 自己的 Roles 中查找。

### 完整示例 (`admins.jsonc`)

```json
{
    "PermissionCollection": {
        "admin": [
            "admin:kick", "admin:ban", "admin:mute", "admin:slay",
            "admin:freeze", "admin:tp", "admin:map", "admin:noclip",
            "admin:rcon", "admin:cvar"
        ]
    },

    "Roles": [
        {
            "Name": "root",
            "Immunity": 255,
            "Permissions": ["*"]
        },
        {
            "Name": "senior_admin",
            "Immunity": 80,
            "Permissions": ["@admin", "admin:rcon", "admin:cvar"]
        },
        {
            "Name": "admin",
            "Immunity": 60,
            "Permissions": ["@moderator", "admin:map", "admin:slay", "admin:noclip"]
        },
        {
            "Name": "moderator",
            "Immunity": 40,
            "Permissions": ["@helper", "admin:ban", "admin:freeze", "admin:tp"]
        },
        {
            "Name": "helper",
            "Immunity": 20,
            "Permissions": ["admin:kick", "admin:mute"]
        }
    ],

    "Admins": [
        {
            "Identity": 76561198000000001,
            "Permissions": ["@root"]
        },
        {
            // 管理员，但不能封禁
            "Identity": 76561198000000002,
            "Permissions": ["@admin", "!admin:ban"]
        }
    ]
}
```
