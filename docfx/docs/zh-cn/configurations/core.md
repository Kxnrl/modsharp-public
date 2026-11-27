# 核心配置

`Sharp.Core` 启动时必须读取 `sharp/configs/core.json`，缺了这个文件会直接初始化失败。建议直接拿[发行包自带的 `core.json`](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/core.json)来改，里面带注释说明，配置加载器支持 JSON 注释。

文件改动会被监听，不过是否实时生效要看对应模块：聊天前缀和 `MenuManager` 设置会自动热重载，日志配置和数据库连接字符串改完需要重启服务器。

## Logger

ModSharp 通过 `Microsoft.Extensions.Logging` 接入 Serilog。

- `Logger.Template.File`：写入 `sharp/logs` 日志文件的格式模板。
- `Logger.Template.Console`：控制台输出模板。核心会自动在前面拼上 `L<CoreCLR> `，不用自己加。
- `Logger.LogLevel.Default`：全局最低日志级别。
- `Logger.LogLevel` 下的其他键：按来源上下文（如 `Microsoft`、`System.Net.Http`）单独设置的最低级别。

级别名区分大小写，只能填 `Verbose`、`Debug`、`Information`、`Warning`、`Error`、`Fatal`（`Default` 缺失或写错会回退到 `Verbose`，其他来源写错会被忽略）。默认配置如下：

```json
{
  "Logger": {
    "LogLevel": {
      "Default": "Verbose",
      "Microsoft": "Information",
      "System.Net.Http": "Warning"
    }
  }
}
```

模板占位符支持 `Timestamp`、`Level`、`SourceContext`、`Scope`、`MapName`、`Message`、`NewLine` 和 `Exception`。上面只截取了级别部分，改配置时保留自带文件里的模板即可。

不同输出目标还有各自的分流规则：控制台最低只打到 `Information`；`Verbose`、`Debug`、`Information`、`Warning` 各写各的按天日志文件，`Error` 和 `Fatal` 合写到同一个错误日志里。因此就算把 `Default` 调成 `Verbose`，控制台也不会刷 `Verbose`/`Debug`；同样，被 `Logger.LogLevel` 过滤掉的日志也不会派发给运行时日志监听器。

## ChatCommand.TriggerPrefixes

注意 `ChatCommand.TriggerPrefixes` 是一个**名字本身带点号的顶层键**，不是嵌套在 `ChatCommand` 对象里的子属性：

```json
{
  "ChatCommand.TriggerPrefixes": {
    "Public": ["!", "."],
    "Silent": ["/", "`"]
  }
}
```

每个前缀必须是单个非空白 ASCII 字符，无效项会打警告并跳过；如果整组列表没配，也不会自动回退成默认前缀。

- `Public`：触发指令时不隐藏原聊天消息（其他聊天监听器仍可拦截）。
- `Silent`：只要消息以该前缀开头就隐藏，哪怕后面的指令名不存在。
- 同一个字符两边都写了，按 `Silent` 处理。

客户端虚拟指令的注册和用法见[指令](../features/command.md)。

## MenuManager

这一节由官方自带的 MenuManager 模块读取。

### KeyBindings

`MenuManager.KeyBindings` 下每个操作支持两种写法：

- `"Type": "Command"`（或 `0`），配一个 `Command` 字符串。
- `"Type": "Button"`（或 `1`），配一个 [UserCommandButtons](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Enums/UserCommandButtons.cs) 里的 `Button` 名称。

枚举名不区分大小写，也能直接填对应整数值（按钮必须是单个已定义的值，不能传组合标志）。哪项没填或填错，哪项就退回默认值：

| 操作 | 默认绑定 |
| --- | --- |
| `MoveUpCursor` | 指令 `autobuy` |
| `MoveDownCursor` | 指令 `rebuy` |
| `GoBack` | 按钮 `Speed` |
| `Confirm` | 按钮 `LookAtWeapon` |
| `Exit` | 按钮 `Scoreboard` |

指令必须是会发到服务端的，像 `slot0`–`slot13`、`cancelselect` 这类纯客户端指令没法用。玩家自己在客户端里要给对应指令或动作绑好键。

只改确认键的例子：

```json
{
  "MenuManager": {
    "KeyBindings": {
      "Confirm": { "Type": "Button", "Button": "Use" }
    }
  }
}
```

### Colors

`MenuManager.Colors` 包含 `Key`、`Text`、`Disabled`、`Cursor` 四项，默认依次为 `#DDAA11`、`#ffffff`、`#888888`、`#3399FF`。支持 `#RGB`、`#RGBA`、`#RRGGBB`、`#RRGGBBAA`，漏写 `#` 会自动补上，填错颜色会回退为对应默认值。

保存 `core.json` 后，键位和颜色会自动重载并刷新已打开的菜单（如果删掉整个 `MenuManager.Colors` 节点，会沿用上一次加载的颜色而不会重置回默认）。详细解析见 [MenuKeyBindings.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/MenuManager/src/MenuKeyBindings.cs) 和 [MenuColor.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/MenuManager/src/MenuColor.cs)。

## ConnectionStrings

这里的连接字符串由各自模块读取：

- `ClientPreferences`：[玩家偏好存储](client-preferences.md)
- `AdminCommands.SQLStorage`：[管理员处罚 SQL 存储](admin-commands-sqlstorage.md)

自带配置里配了一个本地 LiteDB 供 `ClientPreferences` 开箱使用，以及一个 MySQL 占位串供可选的 SQL 存储参考，启用 SQL 存储前记得把占位凭据换成真实数据库信息。
