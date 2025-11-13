# Native Call

> [!NOTE]
> You should install [Sharp.Generator.Sdk](https://www.nuget.org/packages/ModSharp.Sharp.Generator.Sdk) package first. 

You should preconfigure your gamedata file.
> [!NOTE]
> This tutorial is only for Windows, and the gamedata in this example may be outdated due to CS2 update. Use at your own risk.
```json
{
  "Addresses": {
    "CCSPlayer_WeaponServices::GetWeaponByName": {
      "library": "server",
      "windows": "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 41 56 41 57 48 83 EC ? 45 33 FF 4C 8B F2"
    }
  }
}
```

[!code-csharp[NativeCall.cs](../../codes/native-call.cs)]
