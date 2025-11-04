# Internationalization (i18n)

本教程将会教你如何使用多语言系统。

在开始之前，你需要在`{CS2}/game/sharp/locales`中创建i18n配置文件，本文的文件名为`locale-example`。
> 因此，本教程的路径为`{CS2}/game/sharp/locales/locale-example.json`

本教程所使用的配置如片段所示。

```json
{
  "Generic.HelloWorld": {
    "en-us": "Test case A",
    "zh-cn": "测试用例A"
  },
  "Hello": {
    "en-us": "Param A",
    "zh-cn": "参数A"
  },
  "World": {
    "en-us": "Phrase, Param A={0}",
    "zh-cn": "语句, 参数A={0}"
  },
  "Time": {
    "en-us": "Time = {0:F2}",
    "zh-cn": "时间 = {0:F6}"
  },
  "Date": {
    "en-us": "Date is {0:d}",
    "zh-cn": "日期: {0:yyyy/MM/dd HH:mm}"
  }
}
```

> [!NOTE]
> 占位符格式可以参阅:  
>
> - [标准数字格式字符串](https://learn.microsoft.com/dotnet/standard/base-types/standard-numeric-format-strings)
> - [标准日期和时间格式字符串](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings)

[!code-csharp[LocalizerExample.cs](../../codes/locale-example.cs)]
