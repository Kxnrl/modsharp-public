using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace NetMessageExample;

public sealed class NetMsg : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    public NetMsg(ISharedSystem sharedSystem,
        string                  dllPath,
        string                  sharpPath,
        Version                 version,
        IConfiguration          coreConfiguration,
        bool                    hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // client chat/console
        _sharedSystem.GetClientManager().InstallCommandCallback("sendnetmsg", OnCommandSendNetMsg);

        return true;
    }

    public void Shutdown()
    {
        // don't forget to remove the command callback
        _sharedSystem.GetClientManager().RemoveCommandCallback("sendnetmsg", OnCommandSendNetMsg);
    }

    // type 'ms_sendnetmsg' in client console, or chat trigger with '.sendnetmsg' or '/sendnetmsg' or '!sendnetmsg'
    private ECommandAction OnCommandSendNetMsg(IGameClient client, StringCommand command)
    {
        var entityManager = _sharedSystem.GetEntityManager();
        var modSharp      = _sharedSystem.GetModSharp();

        // create protobuf message
        var sayText2Msg = new CUserMessageSayText2
        {
            Chat        = true,
            Entityindex = client.ControllerIndex,
            Messagename = "Dbg.NetMsg Invoked",
            Param1      = string.Empty,
            Param2      = string.Empty,
            Param3      = string.Empty,
            Param4      = string.Empty,
        };

        // to special player
        modSharp.SendNetMessage(new RecipientFilter(client), sayText2Msg);

        // to team
        modSharp.SendNetMessage(new RecipientFilter(CStrikeTeam.CT), sayText2Msg);

        // to all
        modSharp.SendNetMessage(default, sayText2Msg);

        // return stop to block 'unknown command' message in client console
        return ECommandAction.Stopped;
    }

    public string DisplayName   => "NetMsg Example";
    public string DisplayAuthor => "ModSharp dev team";
}
