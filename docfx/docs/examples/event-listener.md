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
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="false" />
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

    // by default, Event hook implement isn't needed.
    public bool HookFireEvent(IGameEvent e, ref bool serverOnly)
    {
        if (e.Name.Equals("player_say", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Blocked GameEvent fire: {e.Name}");

            // block event
            return false;
        }

        return true;
    }

    int IEventListener.ListenerVersion  => IEventListener.ApiVersion;
    int IEventListener.ListenerPriority => 0;
}
```
