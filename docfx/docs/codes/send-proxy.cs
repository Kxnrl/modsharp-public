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
    private readonly IClientManager    _clients;
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
        _clients   = sharedSystem.GetClientManager();
    }

    public bool Init() => true;

    // Register per-recipient overrides on a player. The callback fires once per tick and fills the values each
    // client should see; clients you don't set receive the real value. Set the value matching batch.Kind.
    public void Hook(IBaseEntity pawn, IBaseEntity controller)
    {
        _pawn       = pawn;
        _controller = controller;

        // int — show 1 HP to everyone except the player themselves
        _sendProxy.Hook(pawn, "m_iHealth", (entity, batch) =>
        {
            foreach (var client in _clients.GetGameClients())
            {
                if (client.Slot.AsPrimitive() != 0)
                {
                    batch.SetFor(client, 1L);
                }
            }
        });

        // float — fake a scalar float for every client
        _sendProxy.Hook(pawn, "m_flSomeFloat", (entity, batch) =>
        {
            foreach (var client in _clients.GetGameClients())
            {
                batch.SetFor(client, 0.0f);
            }
        });

        // bool — fake a bool field
        _sendProxy.Hook(pawn, "m_bSomeBool", (entity, batch) =>
        {
            foreach (var client in _clients.GetGameClients())
            {
                batch.SetFor(client, true);
            }
        });

        // qangle/vector — fake eye angles
        _sendProxy.Hook(pawn, "m_angEyeAngles", (entity, batch) =>
        {
            foreach (var client in _clients.GetGameClients())
            {
                batch.SetFor(client, new Vector(0, 90, 0));
            }
        });

        // string — show a different player name to everyone else
        _sendProxy.Hook(controller, "m_iszPlayerName", (entity, batch) =>
        {
            foreach (var client in _clients.GetGameClients())
            {
                if (client.Slot.AsPrimitive() != 0)
                {
                    batch.SetFor(client, "Anonymous");
                }
            }
        });
    }

    public void Shutdown()
    {
        // Remove only our own hooks — a module's hooks are also dropped automatically if it unloads.
        if (_pawn is not null) _sendProxy.UnhookEntity(_pawn);
        if (_controller is not null) _sendProxy.UnhookEntity(_controller);
    }
}
