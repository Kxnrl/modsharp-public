# Sharp Extensions

本教学将演示如何使用**ModSharp**扩展包。

SharpExtensionsExample.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>SharpExtensionsExample</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Extensions.EntityHookManager" Version="*" />
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="all" />
    <PackageReference Include="ModSharp.Sharp.Extensions.GameEventManager" Version="*" />
  </ItemGroup>
</Project>
```

SharpExtensionsExample.cs

```cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.EntityHookManager;
using Sharp.Extensions.GameEventManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace SharpExtensions;

public sealed class SharpExtensions : IModSharpModule
{
    private readonly IServiceProvider _provider;

    public SharpExtensions(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sharedSystem);
        services.AddEntityHookManager();
        services.AddGameEventManager();

        _provider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        _provider.LoadAllSharpExtensions();
        return true;
    }

    public void Shutdown()
    {
        _provider.ShutdownAllSharpExtensions();
    }

    public string DisplayName   => "Sharp Extensions Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
```
