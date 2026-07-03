using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace SendProxyExample;

public sealed class SendProxyExample : IModSharpModule
{
    private readonly ISendProxyManager _sendProxy;
    private IBaseEntity?               _pawn;
    private IBaseEntity?               _controller;

    public SendProxyExample(ISharedSystem sharedSystem,
        string                            dllPath,
        string                            sharpPath,
        Version                           version,
        IConfiguration                    coreConfiguration,
        bool                              hotReload)
    {
        _sendProxy = sharedSystem.GetSendProxyManager();
    }

    public bool Init() => true;

    // Register per-client overrides on a player. The callback runs per recipient; return false to send the
    // real value. Set the value with the method matching value.Kind.
    public void Hook(IBaseEntity pawn, IBaseEntity controller)
    {
        _pawn       = pawn;
        _controller = controller;

        // int — everyone but the player sees 1 HP
        _sendProxy.Hook(pawn, "m_iHealth", (client, entity, ref value) =>
        {
            if (client.Slot.AsPrimitive() == 0) return false;
            value.SetInt(1);
            return true;
        });

        // float — fake a scalar float field
        _sendProxy.Hook(pawn, "m_flSomeFloat", (client, entity, ref value) =>
        {
            value.SetFloat(0.0f);
            return true;
        });

        // bool — fake a bool field
        _sendProxy.Hook(pawn, "m_bSomeBool", (client, entity, ref value) =>
        {
            value.SetBool(true);
            return true;
        });

        // qangle/vector — fake eye angles
        _sendProxy.Hook(pawn, "m_angEyeAngles", (client, entity, ref value) =>
        {
            value.SetVector(new Vector(0, 90, 0));
            return true;
        });

        // string — show a different player name to everyone else
        _sendProxy.Hook(controller, "m_iszPlayerName", (client, entity, ref value) =>
        {
            if (client.Slot.AsPrimitive() == 0) return false;
            value.SetString("Anonymous");
            return true;
        });
    }

    public void Shutdown()
    {
        // Remove only our own hooks — never call Clear() here, it wipes every module's hooks.
        if (_pawn is not null) _sendProxy.UnhookEntity(_pawn);
        if (_controller is not null) _sendProxy.UnhookEntity(_controller);
    }
}
