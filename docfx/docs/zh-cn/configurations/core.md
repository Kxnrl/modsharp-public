# Core

ModSharp核心配置

## Logger

ModSharp使用了`SeriLog`和`Microsoft.Extensions.Logging`作为日志框架,  
在配置文件中我们为了简化配置, 重新定义了配置格式.

---

### Template

模板, 顾名思义, 这是在格式化日志时使用的模板.  
当前提供了**控制台**(`Console`)和``日志文件``(`File`)两种.

默认配置如下:

```json
"Template": {
  "File": "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}] | {Level} | {SourceContext}{Scope} {MapName} {NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
  "Console": "[{Timestamp:MM/dd HH:mm:ss}] | {Level} | {SourceContext}{Scope} {MapName} {NewLine}{Message:lj}{NewLine}{Exception}{NewLine}"
}
```

> [!TIP]
> 占位符请参阅 [文档](https://github.com/serilog/serilog/wiki/Formatting-Output)

### LogLevel

日志级别, 当日志达到某个级别后才会被记录和输出.  
我们使用了键值对来进行日志过滤,
Key为Namespace, Value为日志级别

日志级别支持:

- Verbose
- Debug
- Information
- Warning
- Error
- Fatal

默认配置如下:

```json
"LogLevel": {
  "Default": "Verbose",
  "Microsoft": "Information",
  "System.Net.Http": "Warning"
}
```

---

## ChatCommand.TriggerPrefixes

这是在玩家使用聊天命令触发器时需要包含的前缀:

- `Public`: 触发后命令文本**仍会**输出在聊天框中
- `Silent`: 出发后命令文本**不会**输出在聊天框中

默认配置如下:

```json
"ChatCommand.TriggerPrefixes": {
  "Public": ["!","."],
  "Silent": ["/","`"]
}
```

---

## ConnectionStrings

连接字符串, 顾名思义, 这是提供于各种模块使用的连接字符串.
