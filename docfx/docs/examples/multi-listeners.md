# Multi Listeners

本教程将演示如何在一个Module中同时实现多个Listener。

> [!TIP]
>
> 因为多个Listener中均包含``ListenerVersion``和``ListenerPriority``，  
> 因此我们显式实现接口，即可区分每个Listener的优先级和版本

MultiListenerExample.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>MultiListenerExample</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

MultiListenerExample.cs

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace MultiListenerExample;

public sealed class  MultiListener : IModSharpModule, 
    IClientListener, 
    IEntityListener,
    IEventListener,
    IGameListener,
{
    private readonly ISharedSystem _sharedSystem;

    public  MultiListener(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        _sharedSystem.GetClientManager().InstallClientListener(this);
        _sharedSystem.GetEntityManager().InstallEntityListener(this);
        _sharedSystem.GetEventManager().InstallEventListener(this);
        _sharedSystem.GetModSharp().InstallGameListener(this);

        return true;
    }

    public void Shutdown()
    {
        _sharedSystem.GetClientManager().RemoveClientListener(this);
        _sharedSystem.GetEntityManager().RemoveEntityListener(this);
        _sharedSystem.GetEventManager().RemoveEventListener(this);
        _sharedSystem.GetModSharp().RemoveGameListener(this);
    }

    public string DisplayName => "Multiple Listener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;
    int IEntityListener.ListenerVersion  => IEntityListener.ApiVersion;
    int IEntityListener.ListenerPriority => 0;
    int IEventListener.ListenerVersion   => IEventListener.ApiVersion;
    int IEventListener.ListenerPriority  => 0;
    int IGameListener.ListenerVersion    => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority   => 0;
}
```
