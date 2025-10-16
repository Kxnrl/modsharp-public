# Dependency Injection

本教程将演示如何使用Dependency Injection。

DependencyInjection.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net9.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>
	
	<ItemGroup>
		<PackageReference Include="ModSharp.Sharp.Shared" Version="2.0.20" PrivateAssets="false" />
	</ItemGroup>
</Project>

```

DependencyInjection.cs
```cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;

namespace DependencyInjection;

// Or you can use this annotation if you have ReSharper.
// [UsedImplicitly]
// Recommend you use this comment.
// ReSharper disable once UnusedMember.Global
internal class DependencyInjection : IModSharpModule
{
    private readonly IServiceProvider _provider;

    public DependencyInjection(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sharedSystem);
        _provider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        var sharedSystem = _provider.GetRequiredService<ISharedSystem>();
        sharedSystem.GetModSharp().LogMessage("Hello World!");
        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName => "Dependency Injection Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
```