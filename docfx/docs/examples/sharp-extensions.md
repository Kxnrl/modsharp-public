# Sharp Extensions

本教学将演示如何使用ModSharp扩展包。

SharpExtensions.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
	<ItemGroup>
		<PackageReference Include="ModSharp.Sharp.Extensions.EntityHookManager" Version="2.0.20" />
		<PackageReference Include="ModSharp.Sharp.Extensions.GameEventManager" Version="2.0.20" />
		<PackageReference Include="ModSharp.Sharp.Shared" Version="2.0.20" PrivateAssets="false" />
	</ItemGroup>
</Project>
```

SharpExtensions.cs
```cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.EntityHookManager;
using Sharp.Extensions.GameEventManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace SharpExtensions;

// ReSharper disable once UnusedMember.Global
internal class SharpExtensions : IModSharpModule
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

    public string DisplayName => "Sharp Extensions Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
```