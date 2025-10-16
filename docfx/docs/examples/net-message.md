# NetMessage

本教程将会教你如何发送一个Net Message。

NetMessage.csproj
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

NetMessage.cs
```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace NetMessage;

// ReSharper disable once UnusedMember.Global
internal class NetMsg : IModSharpModule
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

        modSharp.SendNetMessage(new RecipientFilter([client.Slot]), nameof(CUserMessageSayText2), sayText2Msg);

        return ECommandAction.Stopped;
    }

    public string DisplayName => "NetMsg Example";
    public string DisplayAuthor => "ModSharp dev team";
}
```