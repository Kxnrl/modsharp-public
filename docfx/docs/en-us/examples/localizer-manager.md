# Locale Translation

This tutorial will teach you how to use the translation system.

Before you start, you need to register the translation file in `{CS2}/game/sharp/locales`. The file name used in this article is `locale-example`.
> Therefore, the path of this tutorial is `{CS2}/game/sharp/locales/locale-example.json`.

The translation text used in this tutorial is shown in the snippet below.

```json
{
  "Generic.HelloWorld": {
    "zh-cn": "测试用例A",
    "en-us": "Test case A"
  },
  "PhraseA.ParamA": {
    "zh-cn": "参数A",
    "en-us": "Param A"
  },
  "PhraseA": {
    "zh-cn": "语句，参数A={0}",
    "en-us": "Phrase, Param A={0}"
  }
}
```

> [!NOTE]
> For demonstration convenience, this article uses the CommandManager extension package.

[!code-csharp[LocalizerExample.cs](../../codes/locale-example.cs)]