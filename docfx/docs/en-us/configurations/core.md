# Core

ModSharp core configuration

## Logger

ModSharp uses `SeriLog` and `Microsoft.Extensions.Logging` as the logging framework.  
In the configuration file, we have redefined the configuration format to simplify it.

--

### Template

Template, as the name suggests, is the template used when formatting logs.  
Currently, two types are provided: **Console** (`Console`) and **Log File** (`File`).

Default configuration:

```json
"Template": {
  "File": "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}] | {Level} | {SourceContext}{Scope} {MapName} {NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
  "Console": "[{Timestamp:MM/dd HH:mm:ss}] | {Level} | {SourceContext}{Scope} {MapName} {NewLine}{Message:lj}{NewLine}{Exception}{NewLine}"
}
```

> [!TIP]
> For placeholders, please refer to the [documentation](https://github.com/serilog/serilog/wiki/Formatting-Output)

### LogLevel

Log level, logs will only be recorded and output when they reach a certain level.  
We use key-value pairs for log filtering,
Key is the Namespace, Value is the log level

Supported log levels:

- Verbose
- Debug
- Information
- Warning
- Error
- Fatal

Default configuration:

```json
"LogLevel": {
  "Default": "Verbose",
  "Microsoft": "Information",
  "System.Net.Http": "Warning"
}
```

---

## ChatCommand.TriggerPrefixes

These are the prefixes required when players use chat command triggers:

- `Public`: After triggering, the command text **will** be displayed in the chat box
- `Silent`: After triggering, the command text **will not** be displayed in the chat box

Default configuration:

```json
"ChatCommand.TriggerPrefixes": {
  "Public": ["!","."],
  "Silent": ["/","`"]
}
```

---

## ConnectionStrings

Connection strings, as the name suggests, are provided for use by various modules.
