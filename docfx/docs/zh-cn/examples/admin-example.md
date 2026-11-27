# 管理员权限与指令

将 `ModSharp.Sharp.Modules.AdminManager.Shared` 作为宿主提供的契约引用（`ExcludeAssets="runtime"`）。服务器上必须安装 `Sharp.Modules.AdminManager` 和 **`Sharp.Modules.CommandCenter`**，仅引用 Shared 包并不会提供命令服务。项目设置请参照 [Hello World 的项目设置](hello-world.md)。

完整示例注册了三条受权限保护的指令：`example_slay` 杀死所有存活玩家，`example_kill` 杀死执行指令的玩家，`example_heal` 将所有存活玩家的生命值和生命上限设为 1337。在聊天中使用 `.example_kill`，在控制台中使用 `ms_example_kill`。由服务端控制台执行指令时，issuer 为 null，因此会绕过玩家权限检查。示例指令带有 `example` 前缀，以免与第一方管理指令重名。部署前请替换示例中的 SteamID64。

## 所有权与生命周期

请使用真实的程序集名称作为稳定的 `moduleIdentity`，同时传给 `MountAdminManifest` 和 `GetCommandRegistry`。使用方模块断开连接后，AdminManager 和 CommandCenter 会据此移除它注册的权限、管理员来源和指令。

在 `OnAllModulesLoaded` 中，以及 `Sharp.Modules.AdminManager` 或 `Sharp.Modules.CommandCenter` 连接时，解析提供方的包装对象。示例能够处理 CommandCenter 暂时不可用的情况，同时不会吞掉清单挂载或指令注册中的真实错误。使用方模块被重载时，同样会触发 `OnAllModulesLoaded`。

> [!WARNING]
> 热重载提供方后，原有的指令注册表会失效：当前 AdminManager 会缓存注册表，已注册的指令委托也会捕获旧的 AdminManager 实例。重载 CommandCenter 或 AdminManager 时，需要同时重载依赖它们的模块，或者直接重启服务器。光清掉接口包装对象没法修复已经绑在旧提供方上的指令。

`MountAdminManifest` 和 `GetCommandRegistry` 必须在游戏线程上调用。数据库或 HTTP 等异步操作完成后，请通过 `IModSharp.InvokeFrameAction` 或 `InvokeFrameActionAsync` 切回游戏线程执行安装，并在该 action 内重新获取当前的提供方。

## 权限与指令

权限是形如 `组:动作` 的具体键，例如 `admin_offensive:slay`、`admin_offensive:kill` 和 `admin_medic:heal`。指令的权限列表采用 **OR（或）** 逻辑，满足其中任意一项即可。如需 AND（与）逻辑，请获取调用者的 `IAdmin`，在处理函数中逐项检查所需权限。拒绝规则的优先级高于授权规则。

`RegisterAdminCommand` **不会**为传入的权限字符串建立索引，可根据需要选择以下方式之一：

| 方式 | 行为 |
|---|---|
| `MountAdminManifest` → `PermissionCollection` | 随完整清单发布具体权限键；清单还可以包含身份组和管理员分配。重新挂载会替换该模块原有的清单。 |
| `registry.RegisterPermissions([...])` | 只发布具体权限键，不提供身份组或管理员。多次调用的结果会在使用方的作用域内累积，使用方断开连接时一并移除。 |

完整示例使用的是 `PermissionCollection`，因此没有再重复调用 `RegisterPermissions`。如果使用方只注册指令，可以先用 `RegisterPermissions` 发布具体权限键，再注册指令。直接权限规则和通配符都是基于已知的具体权限索引来解析的；在所属使用方发布权限键之前，运维人员可能会看到 unresolved-permission 诊断信息。

## 清单作用域

清单包含权限集合、身份组和管理员分配。示例定义了 `GeneralAdmin` 身份组，并演示以下规则：

| 分配规则 | 结果 |
|---|---|
| `@GeneralAdmin` | 继承该身份组的 slay、kill 和 heal 权限。 |
| `admin_medic:heal` | 仅拥有 heal 权限。 |
| `@GeneralAdmin`、`!admin_medic:heal` | 拥有 slay/kill，但 heal 被拒绝。 |
| `admin_offensive:*` | 匹配 slay/kill，不匹配 heal。 |

**身份组名称按模块隔离**，并非全局名称。清单中的 `@RoleName` 必须引用**同一清单所属模块的作用域**内定义的身份组；`admins.jsonc` 或其他模块中的同名身份组无法满足该引用。相比之下，权限键使用全局索引。同一 SteamID 在多个模块中的管理员来源会合并：授权取并集，免疫等级取最大值，拒绝规则在全局范围内优先生效。

## 外部数据

请先基于自己的数据库或 API 数据准备好 `AdminTableManifest`，再回到游戏线程挂载这份完整快照。传给 `MountAdminManifest` 的工厂回调（示例中的 `BuildAdminManifest`）是同步执行的，不要在其中阻塞等待数据库或网络请求。如果只需分配其他位置已经注册的具体权限，可以传入空的权限集合和空的身份组集合；如果要分配身份组，则必须在自己的清单中定义该身份组。重新挂载新的完整快照即可替换本模块之前的所有分配，包括删除已被移除的分配。

[!code-csharp[AdminExample.cs](../../codes/admin-example.cs)]
