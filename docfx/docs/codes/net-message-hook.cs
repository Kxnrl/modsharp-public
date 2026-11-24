using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
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
        // Remove the callback to release this module's delegate on unload.
        _hooks.PostEventAbstract.RemoveHookPre(OnSendNetMessage);
    }

    private HookReturnValue<NetworkReceiver> OnSendNetMessage(IPostEventAbstractHookParams param,
        HookReturnValue<NetworkReceiver>                                                   previousResult)
    {
        if (previousResult.Action is EHookAction.SkipCallReturnOverride)
        {
            return default;
        }

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

                var amplitude = param.Data.ReadFloat("amplitude"); // Reflection uses the .proto field name.
                _logger.LogInformation("Prepare NetMessage<{r}>: Amplitude = {a}", param.Data.MessageId, amplitude);

                param.Data.SetFloat("amplitude", 100);

                // Filter recipients from param.Receivers (note: if an earlier higher-priority hook already returned
                // ChangeParamReturnDefault, the core keeps the earlier hook's result on equal EHookAction).
                var originalReceivers = previousResult.Action is EHookAction.ChangeParamReturnDefault
                    ? previousResult.ReturnValue
                    : param.Receivers;
                NetworkReceiver receivers = 0UL;
                foreach (var controller in _entities.GetPlayerControllers())
                {
                    if (controller.Team is CStrikeTeam.CT
                        && controller.GetPlayerPawn() is { IsAlive: true }
                        && originalReceivers.HasClient(controller.PlayerSlot))
                    {
                        receivers = receivers.Append(controller.PlayerSlot);
                    }
                }

                return new HookReturnValue<NetworkReceiver>(EHookAction.ChangeParamReturnDefault, receivers);
            }
        }

        return default;
    }


    public string DisplayName   => "NetMessage Hook Example";
    public string DisplayAuthor => "ModSharp dev team";
}