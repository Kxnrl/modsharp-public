# CVar

你需要提前定义好如下变量：

```cs
private readonly ISharedSystem _sharedSystem;
```

> [!NOTE]
> 在插件的构造函数中获取，详情请看示例，这里不做说明。

创建CVar

```cs
_sharedSystem.GetConVarManager().CreateConVar("my_cvar", 0, "This is my cvar.");
```

查找并修改

```cs
if (_sharedSystem.GetConVarManager().FindConVar("sv_cheats") is { } cheats)
{
    cheats.SetString("1");
}
```
