# 多语言翻译

本教程将会教你如何使用翻译系统。

在开始之前，你需要在`{CS2}/game/sharp/locales`中注册翻译文件，本文的文件名为`locale-example`。
> 因此，本教程的路径为`{CS2}/game/sharp/locales/locale-example.json`。

本教程所使用的翻译文本如片段所示。

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
> 为演示方便，本文使用了CommandManager扩展包。

[!code-csharp[LocalizerExample.cs](../../codes/locale-example.cs)]
