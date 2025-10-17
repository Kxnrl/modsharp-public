# NetMessage

本教程将会教你如何发送一个Net Message。

NetMessageExample.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>NetMessageExample</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

NetMessageExample.cs

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace NetMessageExample;

public sealed class NetMsg : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    public NetMsg(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // client chat/console
        _sharedSystem.GetClientManager().InstallCommandCallback("sendnetmsg", OnCommandSendNetMsg);
        return true;
    }

    public void Shutdown()
    {
        _sharedSystem.GetClientManager().RemoveCommandCallback("sendnetmsg", OnCommandSendNetMsg);
    }

    // type 'ms_sendnetmsg' in client console, or chat trigger with '.sendnetmsg' or '/sendnetmsg' or '!sendnetmsg'
    private ECommandAction OnCommandSendNetMsg(IGameClient client, StringCommand command)
    {
        var entityManager = _sharedSystem.GetEntityManager();
        var modSharp = _sharedSystem.GetModSharp();
        if (entityManager.FindPlayerPawnBySlot(client.Slot) is not { } pawn)
        {
            return ECommandAction.Stopped;
        }
        var sayText2Msg = new CUserMessageSayText2
        {
            Chat = true,
            Entityindex = pawn.Index,
            Messagename = "Dbg.NetMsg Invoked",
            Param1 = string.Empty,
            Param2 = string.Empty,
            Param3 = string.Empty,
            Param4 = string.Empty
        };

        // to special player
        modSharp.SendNetMessage(new RecipientFilter(client), nameof(CUserMessageSayText2), sayText2Msg);

        // to team
        modSharp.SendNetMessage(new RecipientFilter(CStrikeTeam.CT), nameof(CUserMessageSayText2), sayText2Msg);

        // to all
        modSharp.SendNetMessage(default, nameof(CUserMessageSayText2), sayText2Msg);

        return ECommandAction.Stopped;
    }

    public string DisplayName => "NetMsg Example";
    public string DisplayAuthor => "ModSharp dev team";
}
```