# Native Call

> [!NOTE]
> You need to install [Sharp.Generator.Sdk](https://www.nuget.org/packages/ModSharp.Sharp.Generator.Sdk) in advance.

You need to prepare your gamedata configuration in advance.
> [!NOTE]
> The GameData file name for this tutorial is ``native-call.games.jsonc``  
> This tutorial only demonstrates Windows GameData, and the GameData in this tutorial may become outdated due to CS2 version updates. If you really want to use it, please update it yourself.

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
