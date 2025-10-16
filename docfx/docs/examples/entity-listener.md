# Entity Listener

本教程将演示最简的Entity Listener用法。

EntityListenerExample.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>EntityListenerExample</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="false" />
  </ItemGroup>
</Project>
```

EntityListenerExample.cs

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Types;

namespace EntityListener;

public sealed class EntityListener : IModSharpModule, IEntityListener
{
    private readonly ISharedSystem _sharedSystem;
    
    public EntityListener(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        _sharedSystem.GetEntityManager().InstallEntityListener(this);

        // These can be found in base.fgd
        _sharedSystem.GetEntityManager().HookEntityOutput("func_door", "OnClose");
        _sharedSystem.GetEntityManager().HookEntityInput("func_door", "Close");
        return true;
    }

    public void Shutdown()
    {
        _sharedSystem.GetEntityManager().RemoveEntityListener(this);
    }

    public void OnEntityCreated(IBaseEntity entity)
    {
        if (entity.Classname.Equals("cs_gamerules", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("CS Game rules Proxy has been created!");
        }
    }

    public void OnEntityDeleted(IBaseEntity entity)
    {
        if (entity.Classname.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[OnEntityDeleted] prop_dynamic has been deleted.");
        }
    }

    public void OnEntitySpawned(IBaseEntity entity)
    {
        if (entity.Classname.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[OnEntitySpawned] prop_dynamic has been spawned.");
        }
    }

    public void OnEntityFollowed(IBaseEntity entity, IBaseEntity? owner)
    {
        if (entity.Classname.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[OnEntityFollowed] prop_dynamic has been followed.");
        }
    }

    public EHookAction OnEntityFireOutput(IBaseEntity entity, string output, IBaseEntity? activator, float delay)
    {
        if (!entity.Classname.Equals("func_door", StringComparison.OrdinalIgnoreCase))
        {
            return EHookAction.Ignored;
        }
        Console.WriteLine($"[OnEntityFireOutput] {entity.Classname}, output={output}, activator={activator?.Classname}, delay={delay}");
        return EHookAction.Ignored;
    }

    public EHookAction OnEntityAcceptInput(IBaseEntity entity,
        string input,
        in EntityVariant value,
        IBaseEntity? activator,
        IBaseEntity? caller)
    {
        if (!entity.Classname.Equals("func_door", StringComparison.OrdinalIgnoreCase))
        {
            return EHookAction.Ignored;
        }

        Console.WriteLine($"[OnEntityAcceptInput] {entity.Classname}, value={value.AutoCastString()}, activator={activator?.Classname}, caller={activator?.Classname}");

        return EHookAction.Ignored;
    }

    public string DisplayName   => "Entity Listener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    int IEntityListener.ListenerVersion  => IEntityListener.ApiVersion;
    int IEntityListener.ListenerPriority => 0;
}
```
