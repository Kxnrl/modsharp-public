# Internationalization (i18n)

引用 `ModSharp.Sharp.Modules.LocalizerManager.Shared`（加 `ExcludeAssets="runtime"`），服务端在 `sharp/modules/` 和 `sharp/shared/` 装好 `Sharp.Modules.LocalizerManager` 及其共享接口程序集即可（不依赖其他第一方模块）。项目设置见 [Hello World](hello-world.md)。

开始之前，需要先在 `{CS2}/game/sharp/locales` 目录下创建 i18n 配置文件。本文使用的文件名为 `locale-example`。
> 因此，本教程中的文件路径为 `{CS2}/game/sharp/locales/locale-example.json`。

本教程使用的配置如下所示。

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
> 占位符的格式请参阅：
>
> - [标准数字格式字符串](https://learn.microsoft.com/dotnet/standard/base-types/standard-numeric-format-strings)
> - [标准日期和时间格式字符串](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings)

本例在 `OnAllModulesLoaded` 以及 `Sharp.Modules.LocalizerManager` 连接时解析 `ILocalizerManager`，然后调用 `LoadLocaleFile("locale-example")`。提供方被替换时会重新加载该文件，断开连接时则清除包装对象；使用方自身被重载时，这套流程同样适用。

使用 `localizer.For(client)` 可以获得绑定到单个客户端的 `ILocale`；用 `Text` 获取格式化后的字符串；用 `Localized(...).Prefix(...).Transform(...).Print()` 进行链式输出。需要为一组客户端分别做本地化时，使用 `ForMany`。提供方卸载或重载之后，不要继续持有 locale 视图和消息 builder。

[!code-csharp[LocalizerExample.cs](../../codes/locale-example.cs)]
