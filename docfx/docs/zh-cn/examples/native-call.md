# Native 调用

> [!NOTE]
> 你需要提前安装好[Sharp.Generator.Sdk](https://www.nuget.org/packages/ModSharp.Sharp.Generator.Sdk)。

你需要提前写好gamedata配置。
> [!NOTE]
> 本教程的GameData文件名为``native-call.games.jsonc``  
> 本教程仅示例Windows的GameData，且该教程的GameData可能会因为CS2版本更新而过时，如果你真的要用，请自行更新。

```json
{
  "Addresses": {
    "CCSPlayer_WeaponServices::GetWeaponByName": {
      "library": "server",
      "windows": "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 41 56 41 57 48 83 EC ? 45 33 FF 4C 8B F2"
    }
  },
  "Offsets": {
    "CBaseEntity::PassesDamageFilter": {
      "windows": 100
    }
  }
}
```

[!code-csharp[NativeCall.cs](../../codes/native-call.cs)]
