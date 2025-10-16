# ConVar

本教程将教你如何创建一个ConVar。

ConVar.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="2.0.20" PrivateAssets="false"/>
  </ItemGroup>
</Project>

```

ConVar.cs
```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Objects;

namespace ConVar;

// ReSharper disable once UnusedMember.Global
internal class ConVar : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    public ConVar(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // That's it. Very easy.
        _sharedSystem.GetConVarManager().CreateConVar("my_cvar", 0, "This is my cvar.");
        if (_sharedSystem.GetConVarManager().FindConVar("sv_cheats") is { } cheats)
        {
            cheats.SetString("1");
        }
        return true;
    }



    public void Shutdown()
    {
    }

    public string DisplayName => "CVar Example";
    public string DisplayAuthor => "ModSharp dev team";
}

```