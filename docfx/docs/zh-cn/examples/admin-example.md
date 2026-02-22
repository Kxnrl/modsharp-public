# 管理员权限与指令

本文介绍了如何使用 `Sharp.Modules.AdminManager` 以编程方式管理指令和权限。

虽然该模块名为 `AdminManager`，但这套系统是处理**任何受限访问**的标准方式，包括但不限于：
*   服务器管理 (踢出/封禁)
*   VIP / 捐助者特权 (自定义皮肤、弹道轨迹)

这种方法允许你绕过静态的 JSON 文件，转而从数据库、外部 API 或复杂的内部逻辑中加载权限。

## 前置条件

模块必须引用并解析 `Sharp.Modules.AdminManager`。

> [!WARNING]
> 请确保你安全地处理依赖注入。参考完整源代码中的 `TryResolveAdminManager` 方法，了解如何优雅地处理模块的加载/卸载。

> [!TIP]
> `MountAdminManifest` 和 `GetCommandRegistry` 请使用同一个、稳定的 `moduleIdentity`，并在多次调用中保持不变。
> 推荐使用模块的 `AssemblyName`：
> `private static readonly string ModuleIdentity = typeof(MyModule).Assembly.GetName().Name ?? "MyModule";`

> [!IMPORTANT]
> `MountAdminManifest` 和 `GetCommandRegistry` 必须在游戏线程内调用。如果你在异步上下文或后台线程中（例如数据库查询回调后），请先通过 `IModSharp.InvokeFrameAction` 或 `IModSharp.InvokeFrameActionAsync` 回到游戏线程再调用。

## 快速开始 — 最简示例

如果你只想注册一个受权限保护的指令，而不需要自己管理管理员或身份组，只需使用 `GetCommandRegistry`：

```csharp
private IModSharpModuleInterface<IAdminManager>? _adminManager;

private static readonly string ModuleIdentity
    = typeof(MyModule).Assembly.GetName().Name ?? "MyModule";

private void InitializeCommands()
{
    if (_adminManager?.Instance is not { } adminManager)
        return;

    var registry = adminManager.GetCommandRegistry(ModuleIdentity);

    registry.RegisterAdminCommand("mycommand", OnMyCommand, ["myplugin:mycommand"]);
    registry.RegisterPermissions(["myplugin:mycommand"]);
}

private void OnMyCommand(IGameClient? issuer, StringCommand cmd)
{
    // 你的逻辑
}
```

服务器管理员通过 `admins.jsonc` 中的 Roles 或 Admins 部分将 `myplugin:mycommand` 分配给对应的管理员。该权限会在你的模块加载并调用 `RegisterPermissions` 后生效——在此之前，服务器日志中会显示为 "unresolved"，这是正常现象。

如果你需要从代码中分配管理员或管理权限，请参阅下方的[注册权限](#注册权限)和[构建清单 (Manifest)](#3-构建清单-manifest)。

如需完整控制身份组、权限集合以及从代码中分配管理员，请继续阅读。

## 1. 定义权限

ModSharp 对权限使用 `组:动作` (group:action) 的字符串格式。这种结构对于支持 **通配符** 至关重要。

在本例中，我们定义两个不同的类别：

*   **Offensive:** 伤害玩家的行为 (`slay`, `kill`)。
*   **Medic:** 帮助玩家的行为 (`heal`)。

```csharp
// 类别 1: Offensive
private const string SlayPermission = "admin_offensive:slay";
private const string KillPermission = "admin_offensive:kill";

// 类别 2: Medic
// 我们使用不同的前缀 ('admin_medic') 将其与Offensive的通配符区分开。
private const string HealPermission = "admin_medic:heal";
```

## 2. 注册指令

使用 `CommandRegistry` 将你的权限字符串连接到聊天指令。

```csharp
var registry = _adminManager.GetCommandRegistry(ModuleIdentity);

// 指令字符串, 处理函数, 所需权限列表
registry.RegisterAdminCommand("slay", OnCommandSlay,   [SlayPermission]);
registry.RegisterAdminCommand("kill", OnCommandKill,   [KillPermission]);
registry.RegisterAdminCommand("heal", OnCommandHealth, [HealPermission]);
// 示例: registry.RegisterAdminCommand("vip_gold", OnGoldGun, ["vip:gold_gun"]);
```

> [!IMPORTANT]
> `permissions` 参数使用的是 **OR（或）** 逻辑：玩家只需拥有列表中的**任意一个**权限即可执行该指令，而非全部。
> 例如，`["admin:mute", "admin:silence"]` 表示拥有 `admin:mute` *或* `admin:silence` 的玩家都可以执行该指令。
> 如果你需要 AND（与）逻辑（要求玩家拥有指定权限），请在指令处理函数内通过 `IAdmin.HasPermission` 进行额外检查。

### 注册权限

`RegisterAdminCommand` **不会**自动将权限注册到全局索引中。如果你希望拥有通配符规则（如 `admin_offensive:*`）的管理员能正确获得你的指令权限，需要自己维护一个权限列表并调用 `RegisterPermissions`：

```csharp
registry.RegisterPermissions([SlayPermission, KillPermission, HealPermission]);
```

> [!NOTE]
> 这是可选的。如果你的权限已经通过 `MountAdminManifest` 的 `PermissionCollection` 注册过了，就不需要再调用 `RegisterPermissions`。当你想用轻量的方式将权限加入索引、而不需要构建完整的 manifest 时，可以使用它。

## 3. 构建清单 (Manifest)

`BuildAdminManifest` 函数用于定义服务器的层级结构。它依赖于 `AdminTableManifest`，主要包含三个部分：

### A. 权限集合 (Permission Collections)
集合允许你将多个权限字符串归为一个名称（快捷方式）。

```csharp
var permissionCollection = new Dictionary<string, HashSet<string>>
{
    // 将 Slay 和 Kill 逻辑分组到一起
    ["admin_group_offensive"] = [SlayPermission, KillPermission],

    // 医疗逻辑分组
    ["admin_group_medic"] = [HealPermission],
};
```

### B. 身份组 (Roles)
身份组是命名实体（如 "VIP", "Moderator", "Admin"），包含一组权限和一个免疫等级。

```csharp
// 创建一个拥有所有权限的 "GeneralAdmin" 身份组
HashSet<string> allPermissions = [SlayPermission, KillPermission, HealPermission];

var roles = new List<RoleManifest> 
{
    new RoleManifest("GeneralAdmin", immunity: 1, allPermissions) 
};
```

### C. 管理员分配策略
有四种主要方式将权限分配给特定的 SteamID。

| ID 示例 | 策略 | 语法 | 结果 | 逻辑 |
| :--- | :--- | :--- | :--- | :--- |
| **User 1** | **身份组 (Role)** | `["@GeneralAdmin"]` | **完全访问** | 从身份组定义继承 Slay, Kill, 和 Heal。 |
| **User 2** | **原始 (Raw)** | `["admin_medic:heal"]` | **仅治疗** | 直接授予特定的权限字符串。无法使用 Slay。 |
| **User 3** | **否定 (Negation)** | `["@GeneralAdmin", "!admin_medic:heal"]` | **仅 Slay/Kill** | 继承身份组，但 `!` 显式**移除**了医疗权限。 |
| **User 4** | **通配符 (Wildcard)** | `["admin_offensive:*"]` | **仅 Slay/Kill** | 匹配 `admin_offensive:slay` 和 `:kill`。**不**匹配 `admin_medic:heal`。 |

## 4. 使用外部数据源 (数据库 / API)

虽然上面的示例为了简单起见将用户硬编码到列表中，但此模块的主要用例是动态加载数据。

你可以将 `BuildAdminManifest` 中的硬编码列表/字典替换为从你自己的服务（如 MySQL, HTTP 请求）获取的数据。

### 使用现有身份组
如果你只想将管理员分配给已经定义好（例如在 `admins.jsonc` 或其他模块中）的身份组，你**不需要**重新填充权限或身份组列表，只需传递空集合即可。

```csharp
private AdminTableManifest BuildAdminManifest()
{
    // 1. 权限与身份组：
    // 如果使用的是在别处定义的身份组/权限，将这些留空。
    Dictionary<string, HashSet<string>> myPermissions = _requestManager.GetPermissions();
    List<RoleManifest> myRoles = _requestManager.GetRoles();

    // 2. 管理员：
    // 从数据库获取管理员列表
    List<AdminManifest> myAdmins = _requestManager.GetAdmins(); 

    return new AdminTableManifest(myPermissions, myRoles, myAdmins);
}
```

> [!CAUTION]
> 如果 `myRoles` 留空，但 `myAdmins` 为用户分配了 `["@SuperAdmin"]` 身份组，必须确保 `@SuperAdmin` 已由其他来源**全局**定义。如果该身份组不存在，用户将不会获得对应的权限。

## 完整源代码

[!code-csharp[Main](../../codes/admin-example.cs)]
