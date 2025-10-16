# Entity Hook Manager

本教程将演示如何使用EntityHookManager扩展。

EntityHookManager.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net9.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="ModSharp.Sharp.Extensions.EntityHookManager" Version="2.0.20" />
		<PackageReference Include="ModSharp.Sharp.Shared" Version="2.0.20" PrivateAssets="false" />
	</ItemGroup>
</Project>
```

EntityHookManager.cs
```cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.EntityHookManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace EntityHookManager;

// ReSharper disable once UnusedMember.Global
internal class EntityHookManager : IModSharpModule
{
    private readonly IServiceProvider _provider;
    private readonly IEntityHookManager _entityHookManager;

    public EntityHookManager(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sharedSystem);
        services.AddEntityHookManager();

        _provider = services.BuildServiceProvider();
        _entityHookManager = _provider.GetRequiredService<IEntityHookManager>();
    }

    public bool Init()
    {
        _provider.LoadAllSharpExtensions();
        _entityHookManager.HookEntityInput("func_door", "Close", OnFuncDoorInputClose);
        _entityHookManager.HookEntityOutput("func_door", "OnClose", OnFuncDoorOutputClose);
        _entityHookManager.ListenEntityCreate("cs_gamerules", OnCsGameRulesCreated);
        _entityHookManager.ListenEntityDelete("prop_dynamic", OnPropDynamicDeleted);
        _entityHookManager.ListenEntitySpawn("prop_dynamic", OnPropDynamicSpawned);
        _entityHookManager.ListenWeaponSpawn(OnWeaponSpawned);
        return true;
    }

    public void Shutdown()
    {
        _provider.ShutdownAllSharpExtensions();
    }

    private void OnWeaponSpawned(string classname, IBaseWeapon weapon)
    {
        Console.WriteLine($"[OnWeaponSpawned] classname={classname}, weapon={weapon.GetWeaponClassname()}");
    }

    private void OnPropDynamicSpawned(string classname, IBaseEntity entity)
    {
        Console.WriteLine($"[OnPropDynamicSpawned] classname={classname}, entity={entity.Classname}");

    }

    private void OnPropDynamicDeleted(string classname, IBaseEntity entity)
    {
        Console.WriteLine($"[OnPropDynamicDeleted] classname={classname}, entity={entity.Classname}");
    }

    private void OnCsGameRulesCreated(string classname, IBaseEntity entity)
    {
        Console.WriteLine($"[OnCsGameRulesCreated] classname={classname}, entity={entity.Classname}");
    }

    private void OnFuncDoorInputClose(string classname, string input, in EntityVariant value, IBaseEntity entity, IBaseEntity? activator, IBaseEntity? caller, ref EHookAction result)
    {
        Console.WriteLine($"[OnFuncDoorInputClose] classname={classname}, input={input}, value={value.AutoCastString()}, entity={entity.Classname}, activator={activator?.Classname}, caller={caller?.Classname}, result={result}");
    }

    private void OnFuncDoorOutputClose(string classname, string output, IBaseEntity entity, IBaseEntity? activator, float delay, ref EHookAction result)
    {
        Console.WriteLine($"[OnFuncDoorOutputClose] classname={classname}, output={output}, entity={entity.Classname}, activator={activator?.Classname} delay={delay}, result={result}");
    }

    public string DisplayName => "EntityHookManager Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
```