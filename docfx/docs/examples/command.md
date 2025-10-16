# 指令

本教程将会演示如何创建指令，其中包含了纯服务端指令和客户端指令。

Command.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="2.0.20" PrivateAssets="false"/>
  </ItemGroup>
</Project>

```

Command.cs
```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Command;

// ReSharper disable once UnusedMember.Global
internal class Command : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;
    
    public Command(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // on server console only
        _sharedSystem.GetConVarManager()
               .CreateServerCommand("ms_echo",
                                    OnServerCommand,
                                    "Command Description",
                                    ConVarFlags.Release);

        // client chat/console
        _sharedSystem.GetClientManager().InstallCommandCallback("hello", OnClientCommand);
        return true;
    }

    public void Shutdown()
    {
        _sharedSystem.GetConVarManager().ReleaseCommand("ms_echo");
        _sharedSystem.GetClientManager().RemoveCommandCallback("hello", OnClientCommand);
    }

    // type 'ms_econ' on server console
    private ECommandAction OnServerCommand(StringCommand command)
    {
        Console.WriteLine("Hello");
        _sharedSystem.GetModSharp().LogMessage($"Trigger command {command.GetCommandString()}");

        return ECommandAction.Stopped;
    }

    // type 'ms_hello' in client console, or chat trigger with '.hello' or '/hello' or '!hello'
    private ECommandAction OnClientCommand(IGameClient client, StringCommand command)
    {
        var name = command.ArgCount > 0 ? command.GetArg(1) : "YunLi";

        // This will print to client console. (aka ~)
        client.ConsolePrint($"Hello, {name}");

        return ECommandAction.Stopped;
    }

    public string DisplayName => "Command Example";
    public string DisplayAuthor => "ModSharp dev team";
}
```