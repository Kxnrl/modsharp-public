# Internationalization (i18n)

Reference `ModSharp.Sharp.Modules.LocalizerManager.Shared` as a host-provided contract (`ExcludeAssets="runtime"`) and install `Sharp.Modules.LocalizerManager` along with its shared contract under `sharp/shared/` (it has no dependencies on other first-party modules). Use the [Hello World project settings](hello-world.md).

Before you start, create an i18n configuration file in `{CS2}/game/sharp/locales`. This tutorial names it `locale-example`.
> The full path is therefore `{CS2}/game/sharp/locales/locale-example.json`.

The configuration used in this tutorial is shown below.

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
> For placeholder format syntax, see:
>
> - [Standard numeric format strings](https://learn.microsoft.com/dotnet/standard/base-types/standard-numeric-format-strings)
> - [Standard date and time format strings](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings)

The sample resolves `ILocalizerManager` in `OnAllModulesLoaded` and when `Sharp.Modules.LocalizerManager` connects, then calls `LoadLocaleFile("locale-example")`. It reloads the file for a replacement provider and clears the wrapper on disconnect. This also works when the consumer itself is reloaded.

Use `localizer.For(client)` for a client-scoped `ILocale`, `Text` for a formatted string, and `Localized(...).Prefix(...).Transform(...).Print()` for fluent output. Use `ForMany` for per-client localization of a group. Do not retain locale views or message builders across provider unload/reload.

[!code-csharp[LocalizerExample.cs](../../codes/locale-example.cs)]
