# 控制台变量

请参照 [Hello World 的项目设置](hello-world.md)。

请根据值的类型选择对应的 `CreateConVar` 重载，并检查其可能为空的返回值。本例创建整数变量 `example_value`，用 `Set(int)` 写入、`GetInt32` 读取，并读取 `sv_cheats` 的值（不会在服务器上开启作弊）。

已知类型时直接用对应的强类型 setter。`Set(string)` 会严格解析文本，格式不对直接抛 `ArgumentException`；`SetString` 则走宽松回退（字符串类型会截断到 255 UTF-8 字节），不会帮你重置回 ConVar 的 `DefaultValue`。

[!code-csharp[convar.cs](../../codes/convar.cs)]
