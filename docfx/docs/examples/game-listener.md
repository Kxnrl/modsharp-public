# Game Listener

本教程将会演示如何使用GameListener。

GameListenerExample.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>GameListenerExample</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

GameListenerExample.cs

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;

namespace GameListenerExample;

public sealed class GameListener : IModSharpModule, IGameListener
{
    private readonly ISharedSystem _sharedSystem;

    public GameListener(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        _sharedSystem.GetModSharp().InstallGameListener(this);
        return true;
    }

    public void Shutdown()
    {
        _sharedSystem.GetModSharp().RemoveGameListener(this);
    }

    public string DisplayName => "GameListener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    // safe to get sv/globals
    public void OnServerInit()
    {
        Console.WriteLine("[OnServerInit] You can get GlobalVars at here.");
    }

    // safe to execute .cfg
    public void OnServerSpawn()
    {
        Console.WriteLine("[OnServerSpawn]");
    }

    public void OnServerActivate()
    {
        Console.WriteLine("[OnServerActivate] Or you can treat this as OnMapStart. This is after OnGameInit");
    }

    public void OnResourcePrecache()
    {
        // 只有这里可以预加载资源！
        Console.WriteLine("[OnResourcePrecache] Precaching resources...");
        _sharedSystem.GetModSharp().PrecacheResource("models/foo/bar.vmdl");
    }

    // safe to get GameRules
    public void OnGameInit()
    {
        Console.WriteLine("[OnGameInit] You can treat this is OnMapStart if you want to migrate from CS#/SM");
    }

    public void OnGamePostInit()
    {
        Console.WriteLine("[OnGamePostInit]");
    }

    public void OnGameActivate()
    {
        Console.WriteLine("[OnGameActivate]");
    }

    public void OnGameDeactivate()
    {
        Console.WriteLine("[OnGameDeactivate] You can treat this is OnMapEnd.");
    }

    public void OnGamePreShutdown()
    {
        Console.WriteLine("[OnGamePreShutdown]");
    }

    public void OnGameShutdown()
    {
        Console.WriteLine("[OnGameShutdown] or you can use it as OnMapEnd. But in this term you cannot use GameRules");
    }

    public void OnRoundRestart()
    {
        Console.WriteLine("[OnRoundRestart] This and OnRoundRestarted can replace round_prestart/round_start stuff.");
    }

    public void OnRoundRestarted()
    {
        Console.WriteLine("[OnRoundRestarted] This and OnRoundRestart can replace round_prestart/round_start stuff.");
    }

    public ECommandAction ConsoleSay(string message)
    {
        Console.WriteLine($"[ConsoleSay] {message}");
        return ECommandAction.Skipped;
    }

    int IGameListener.ListenerVersion  => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority => 0;
}
```
