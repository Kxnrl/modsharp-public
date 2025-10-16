# ConVar

本教程将教你如何创建一个ConVar。

ConVarExample.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>ConVarExample</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="false" />
  </ItemGroup>
</Project>
```

ConVarExample.cs

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Objects;

namespace ConVarExample;

public sealed class ConVar : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    public ConVar(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // That's it. Very easy.
        _sharedSystem.GetConVarManager().CreateConVar("my_cvar", 0, "This is my cvar.", ConVarFlags.Release);
        if (_sharedSystem.GetConVarManager().FindConVar("sv_cheats") is { } cheats)
        {
            cheats.Set(true);
        }
        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName   => "ConVar Example";
    public string DisplayAuthor => "ModSharp dev team";
}

```
