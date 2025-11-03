using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.HookParams;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace NetMessageHookExample;

public sealed class NetMessageHook : IModSharpModule
{
    private readonly ILogger<NetMessageHook> _logger;
    private readonly IHookManager            _hooks;
    private readonly IModSharp               _modSharp;
    private readonly IEntityManager          _entities;

    public NetMessageHook(ISharedSystem sharedSystem,
        string                          dllPath,
        string                          sharpPath,
        Version                         version,
        IConfiguration                  coreConfiguration,
        bool                            hotReload)
    {
        _logger   = sharedSystem.GetLoggerFactory().CreateLogger<NetMessageHook>();
        _hooks    = sharedSystem.GetHookManager();
        _modSharp = sharedSystem.GetModSharp();
        _entities = sharedSystem.GetEntityManager();
    }

    public bool Init()
    {
        // you only can install the Pre hooks for NetMessage.
        _hooks.PostEventAbstract.InstallHookPre(OnSendNetMessage);

        // add message type to modsharp
        _modSharp.HookNetMessage(ProtobufNetMessageType.UM_Shake);
        _modSharp.HookNetMessage(ProtobufNetMessageType.UM_Fade);

        return true;
    }

    public void Shutdown()
    {
        // must remove the hooks in Shutdown
        // otherwise you will get fucked after reloaded.
        _hooks.PostEventAbstract.RemoveHookPre(OnSendNetMessage);
    }

    private HookReturnValue<NetworkReceiver> OnSendNetMessage(IPostEventAbstractHookParams param,
        HookReturnValue<NetworkReceiver>                                                   previousResult)
    {
        switch (param.MsgId)
        {
            case ProtobufNetMessageType.UM_Fade:
            {
                // example to deserialize the message

                var message = param.Data.Deserialize<CUserMessageFade>();

                _logger.LogInformation("Prepare NetMessage<{r}>: {@m}", param.Data.MessageId, message);

                // override color
                message.Color = new Color32(1, 2, 3, 4);

                // send it back
                param.Data.CopyFromOtherMessage(message);

                // if you don't modify the Receivers, you can just ignore it.
                return new HookReturnValue<NetworkReceiver>(EHookAction.Ignored);
            }
            case ProtobufNetMessageType.UM_Shake:
            {
                // example to use protobuf reflection

                var amplitude = param.Data.ReadFloat("Amplitude"); // returns null means the optional value is null
                _logger.LogInformation("Prepare NetMessage<{r}>: Amplitude = {a}", param.Data.MessageId, amplitude);

                param.Data.SetFloat("Amplitude", 100); // override Amplitude field

                // make it to alive CTs
                var cts = GetControllers()
                          .Where(x => x.Team is CStrikeTeam.CT)
                          .Where(x => x.GetPlayerPawn() is { IsAlive: true })
                          .Select(x => x.PlayerSlot)
                          .ToArray();

                var receivers = new NetworkReceiver(cts);

                return new HookReturnValue<NetworkReceiver>(EHookAction.ChangeParamReturnDefault, receivers);
            }
        }

        return default;
    }

    private IEnumerable<IPlayerController> GetControllers()
    {
        var max = new PlayerSlot((byte) _modSharp.GetGlobals().MaxClients);

        for (PlayerSlot slot = 0; slot <= max; slot++)
        {
            if (_entities.FindPlayerControllerBySlot(slot) is { ConnectedState: PlayerConnectedState.PlayerConnected } c)
            {
                yield return c;
            }
        }
    }

    public string DisplayName   => "NetMessage Hook Example";
    public string DisplayAuthor => "ModSharp dev team";
}