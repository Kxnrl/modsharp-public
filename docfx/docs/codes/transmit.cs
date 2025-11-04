using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace TransmitHookExample;

public sealed class TransmitHook : IModSharpModule, IClientListener
{
    private readonly IModSharp        _modSharp;
    private readonly IClientManager   _clients;
    private readonly IEntityManager   _entities;
    private readonly ITransmitManager _transmits;

    public TransmitHook(ISharedSystem sharedSystem,
        string                        dllPath,
        string                        sharpPath,
        Version                       version,
        IConfiguration                coreConfiguration,
        bool                          hotReload)
    {
        _modSharp  = sharedSystem.GetModSharp();
        _clients   = sharedSystem.GetClientManager();
        _entities  = sharedSystem.GetEntityManager();
        _transmits = sharedSystem.GetTransmitManager();
    }

    public bool Init()
    {
        // install listener, any class what inherits IClientListener can be a listener.
        _clients.InstallClientListener(this);

        return true;
    }

    public void PostInit()
        => _modSharp.PushTimer(Think, 0.1, GameTimerFlags.Repeatable);

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _clients.RemoveClientListener(this);
    }

    public void OnClientPutInServer(IGameClient client)
        => _modSharp.PushTimer(() =>
                               {
                                   if (!client.IsValid)
                                   {
                                       return;
                                   }

                                   if (_entities.FindPlayerControllerBySlot(client.Slot) is not
                                   {
                                       ConnectedState: PlayerConnectedState.PlayerConnected,
                                   } controller)
                                   {
                                       return;
                                   }

                                   _transmits.AddEntityHooks(controller, true);
                               },
                               5); 
                               // hook player after 5 seconds to make sure all data is ready
                               // the delay time can be adjusted according to your needs

    // just write it according to the example.
    int IClientListener.ListenerVersion => IClientListener.ApiVersion;

    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int IClientListener.ListenerPriority => 0;

    private void Think()
    {
        // hide teammate logic here.
        // why this use interval Think?
        // because maybe some player change team / late join in game after game started,
        // if your gamemode does not allow that, just call once on RoundRestarted is enough.

        var controllers = GetControllers().ToArray();

        foreach (var sender in controllers)
        {
            if (!_transmits.IsEntityHooked(sender))
            {
                continue;
            }

            var st = sender.Team;

            foreach (var receiver in controllers)
            {
                // ignore self
                if (sender.Equals(receiver))
                {
                    continue;
                }

                var state = st == receiver.Team;

                _transmits.SetEntityState(sender.Index, receiver.Index, state, -1);
            }
        }
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

    public string DisplayName   => "Transmit Hook Example";
    public string DisplayAuthor => "ModSharp dev team";
}