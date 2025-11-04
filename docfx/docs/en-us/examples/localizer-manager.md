# Internationalization (i18n)

This tutorial will teach you how to use the i18n system.

Before you start, you need to create an i18n configuration file in `{CS2}/game/sharp/locales`. The file name used in this article is `locale-example`.
> Therefore, the path of this tutorial is `{CS2}/game/sharp/locales/locale-example.json`.

The configuration used in this tutorial is shown in the snippet below.

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
> For placeholder formats, please refer to:  
>
> - [Standard numeric format strings](https://learn.microsoft.com/dotnet/standard/base-types/standard-numeric-format-strings)
> - [Standard date and time format strings](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings)

[!code-csharp[LocalizerExample.cs](../../codes/locale-example.cs)]
