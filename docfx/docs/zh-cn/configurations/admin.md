# 管理员配置文件 (Admin FlatFile)

ModSharp 将管理员配置分为两个文件，以便于实现“快速分配”和“复杂权限管理”两种需求。

1.  **简易分配:** `{CS2}/sharp/configs/admins_simple.jsonc`
2.  **高级配置:** `{CS2}/sharp/configs/admins.jsonc`

## 简易配置 (`admins_simple.jsonc`)

使用此文件可通过简单的 **键值对 (Key-Value)** 格式快速将现有的 **身份组 (Roles)** 分配给用户。

*   **键 (Key):** 玩家的 Steam64 ID。
*   **值 (Value):** 身份组名称（字符串）。

**注意:**
1.  身份组名称（例如 "root", "admin"）必须在主文件 `admins.jsonc` 中定义。
2.  如果某个用户同时出现在此文件和主文件 `admins.jsonc` 中，系统优先使用 `admins.jsonc` 中的设置。

**示例:**
```json
{
    // 服主 / Root
    "76561198000000001": "root",

    // 普通管理员
    "76561198000000002": "admin",
    "76561198000000003": "admin"
}
```

---

## 高级配置 (`admins.jsonc`)

此文件包含权限定义、身份组定义以及详细的管理员配置。它分为三个部分：

1.  **PermissionCollection:** 定义具体的权限字符串集合。
2.  **Roles:** 定义一组权限和免疫等级（即 **身份组**），可直接分配给管理员。
3.  **Admins:** 为特定的 SteamID 分配身份组和特定权限。

### 1. 结构

#### Roles (身份组对象)
*   `Name` (必填): 身份组名称（可通过 `@Name` 进行引用/继承）。
*   `Immunity` (选填): 免疫等级 (0-255)。
*   `Permissions` (必填): 权限字符串或继承身份组的列表。

#### Admins (管理员对象)
*   `Identity` (必填): 玩家的 Steam64 ID（推荐使用整数格式）。
*   `Immunity` (选填): 覆盖身份组的免疫等级。系统取所有条目中发现的最高值。
*   `Permissions` (必填): 要继承的身份组列表，或要授予/拒绝的特定权限。

### 2. 权限语法规则

您可以在 **Roles** 和 **Admins** 的 `Permissions` 数组中使用以下语法：

*   **`@身份组名称`**: 继承某个身份组的所有权限和免疫等级（支持递归）。
*   **`!permission`**: 拒绝某项权限。这是全局性的，会覆盖其他任何授权。
*   **`*`**: 授予所有权限。
*   **`module:*`**: 授予特定模块内的所有权限（支持递归，例如同时匹配 `module:a` 和 `module:a:b`）。

### 3. 通用规则
*   **不区分大小写:** `Admin:Kick` 与 `admin:kick` 视为相同。
*   **身份格式:** 必须严格使用 SteamID64（整数格式）。请勿使用旧版格式 (STEAM_0:...) 或 SteamID3 格式 ([U:1:...])。

### 完整示例 (`admins.jsonc`)

```json
{
    "PermissionCollection": {
        "admin": ["admin:kick", "admin:ban", "admin:slay"],
        "vip": ["vip:skins", "vip:reserved_slot"]
    },

    "Roles": [
        {
            "Name": "root",
            "Immunity": 255,
            "Permissions": ["*"]
        },
        {
            "Name": "admin",
            "Immunity": 50,
            "Permissions": ["admin:*"]
        },
        {
            // 继承 'admin' 身份组，但专门移除 ban (封禁) 权限
            "Name": "junior_admin",
            "Immunity": 25,
            "Permissions": ["@admin", "!admin:ban"]
        }
    ],

    "Admins": [
        {
            // 拥有 root 权限的服务器所有者
            "Identity": 76561198000000001,
            "Permissions": ["@root"]
        },
        {
            // 拥有特定设置的特定用户
            "Identity": 76561198000000002,
            "Immunity": 20, 
            "Permissions": ["@junior_admin", "vip:*"]
        }
    ]
}
```