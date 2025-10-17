# EventListener

本教程将会演示如何使用EventListener。

EventListenerExample.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>EventListenerExample</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

EventListenerExample.cs

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace EventListener;

public sealed class EventListener : IModSharpModule, IEventListener
{
    private readonly ISharedSystem _sharedSystem;

    public EventListener(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        _sharedSystem.GetEventManager().InstallEventListener(this);
        return true;
    }

    public void Shutdown()
    {
        _sharedSystem.GetEventManager().RemoveEventListener(this);
    }

    public string DisplayName => "EventListener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    public void FireGameEvent(IGameEvent e)
    {
        if (e is IEventPlayerDeath death)
        {
            var victim = death.VictimController?.PlayerName;
            var killer = death.KillerController?.PlayerName ?? "World";
            Console.WriteLine($"{victim} was killed by {killer}");
        }
        else if (e.Name.Equals("player_spawn"))
        {
            Console.WriteLine($"Player slot[{e.GetInt("userid")}] spawned");
        }
        else
        {
            Console.WriteLine($"GameEvent {e.Name} fired");
        }
    }

    // 默认情况下HookFireEvent不需要实现
    public bool HookFireEvent(IGameEvent e, ref bool serverOnly)
    {
        if (e.Name.Equals("player_say", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Blocked GameEvent fire: {e.Name}");

            // 阻止事件发射
            return false;
        }

        if (e.Name.Equals("player_changename", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Silence GameEvent: {e.Name}");

            // 将事件设置为仅限服务器而不下发至客户端
            serverOnly = true;
        }

        return true;
    }

    int IEventListener.ListenerVersion  => IEventListener.ApiVersion;
    int IEventListener.ListenerPriority => 0;
}
```
