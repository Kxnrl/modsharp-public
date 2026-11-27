# Core Configuration

`Sharp.Core` requires `sharp/configs/core.json` on startup and fails to initialize if it is missing. Start from the [bundled `core.json`](https://github.com/Kxnrl/modsharp-public/blob/master/.asset/configs/core.json), which includes comments explaining each option (JSON comments are supported).

File changes are watched, though reload behavior depends on the consumer: chat prefixes and `MenuManager` settings apply live, while logger settings and database connection strings require a server restart.

## Logger

ModSharp uses Serilog behind `Microsoft.Extensions.Logging`.

- `Logger.Template.File`: output template for daily log files under `sharp/logs`.
- `Logger.Template.Console`: console template. The core automatically prepends `L<CoreCLR> `, so omit it from your template.
- `Logger.LogLevel.Default`: global minimum log level.
- Other keys under `Logger.LogLevel`: per-source overrides such as `Microsoft` or `System.Net.Http`.

Level names are case-sensitive: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, or `Fatal` (missing or invalid `Default` falls back to `Verbose`; invalid overrides are ignored). Default levels:

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

Available template tokens include `Timestamp`, `Level`, `SourceContext`, `Scope`, `MapName`, `Message`, `NewLine`, and `Exception`. Keep the bundled templates intact when editing levels.

Sinks apply their own thresholds on top: the console starts at `Information`; `Verbose`, `Debug`, `Information`, and `Warning` each go to separate daily files, while `Error` and `Fatal` share one error file. Setting `Default` to `Verbose` will not print `Verbose` or `Debug` to the console, and events filtered out by `Logger.LogLevel` never reach runtime log listeners.

## ChatCommand.TriggerPrefixes

Note that `ChatCommand.TriggerPrefixes` is a **literal top-level key with a dot in its name**, not a nested `ChatCommand` object:

```json
{
  "ChatCommand.TriggerPrefixes": {
    "Public": ["!", "."],
    "Silent": ["/", "`"]
  }
}
```

Each prefix must be a single non-whitespace ASCII character; invalid entries are logged and skipped, and omitting a list does not restore the bundled defaults.

- `Public`: keeps the chat message visible (other chat listeners can still block it).
- `Silent`: hides any chat message starting with the prefix, even if the command name is unknown.
- A character listed in both arrays is treated as silent.

See [Commands](../features/command.md) for registering virtual commands.

## MenuManager

Read by the bundled first-party MenuManager module.

### KeyBindings

Each action under `MenuManager.KeyBindings` accepts either form:

- `"Type": "Command"` (or `0`) with a `Command` string.
- `"Type": "Button"` (or `1`) with a `Button` from [UserCommandButtons](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Enums/UserCommandButtons.cs).

Enum names are case-insensitive and defined numeric values also work (buttons must be a single defined value, not combined bitflags). Missing or invalid entries fall back to defaults:

| Action | Default |
| --- | --- |
| `MoveUpCursor` | command `autobuy` |
| `MoveDownCursor` | command `rebuy` |
| `GoBack` | button `Speed` |
| `Confirm` | button `LookAtWeapon` |
| `Exit` | button `Scoreboard` |

Bound commands must actually be sent to the server, so client-only commands like `slot0`–`slot13` and `cancelselect` will not work. Players still need a client key bound to the chosen command or button.

Example overriding only `Confirm`:

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

`MenuManager.Colors` configures `Key`, `Text`, `Disabled`, and `Cursor` (defaults `#DDAA11`, `#ffffff`, `#888888`, `#3399FF`). Supports `#RGB`, `#RGBA`, `#RRGGBB`, and `#RRGGBBAA`; a missing leading `#` is added automatically, and invalid values fall back to that key's default.

Saving `core.json` reloads key bindings and colors and refreshes open menus (deleting the entire `MenuManager.Colors` section keeps the last loaded colors instead of resetting to defaults). See [MenuKeyBindings.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/MenuManager/src/MenuKeyBindings.cs) and [MenuColor.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/MenuManager/src/MenuColor.cs).

## ConnectionStrings

Connection strings consumed by individual modules:

- `ClientPreferences`: [Player preference storage](client-preferences.md)
- `AdminCommands.SQLStorage`: [Admin punishment SQL storage](admin-commands-sqlstorage.md)

The bundled config ships a working local LiteDB connection for `ClientPreferences` and a placeholder MySQL string for the optional SQL storage module. Replace the placeholder credentials before enabling SQL storage.
