using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;

namespace TransmitHookExample;

public sealed class TransmitHook : IModSharpModule
{
    private readonly IModSharp _modSharp;
    private readonly IEntityManager _entities;
    private readonly ITransmitManager _transmits;
    private readonly HashSet<IPlayerController> _ownedHooks = [];
    private Guid _timer;

    public TransmitHook(ISharedSystem sharedSystem,
        string                        dllPath,
        string                        sharpPath,
        Version                       version,
        IConfiguration                coreConfiguration,
        bool                          hotReload)
    {
        _modSharp = sharedSystem.GetModSharp();
        _entities = sharedSystem.GetEntityManager();
        _transmits = sharedSystem.GetTransmitManager();
    }

    public bool Init()
        => true;

    public void PostInit()
        => _timer = _modSharp.PushTimer(Think, 0.1, GameTimerFlags.Repeatable);

    public void Shutdown()
    {
        _modSharp.StopTimer(_timer);
        foreach (var controller in _ownedHooks)
        {
            if (controller.IsValidEntity && _transmits.IsEntityHooked(controller))
            {
                _transmits.RemoveEntityHooks(controller);
            }
        }

        _ownedHooks.Clear();
    }

    private void Think()
    {
        // Re-evaluate late joins, team changes, map changes and consumer hot reloads.
        _ownedHooks.RemoveWhere(controller => !controller.IsValidEntity);
        var controllers = _entities.GetPlayerControllers().ToArray();

        foreach (var sender in controllers)
        {
            // Hooks are global per entity, not per module. Do not take over another module's hook.
            if (!_ownedHooks.Contains(sender))
            {
                if (_transmits.IsEntityHooked(sender) || !_transmits.AddEntityHooks(sender, true))
                {
                    continue;
                }

                _ownedHooks.Add(sender);
            }

            foreach (var receiver in controllers)
            {
                var isTeammate = sender.Team is CStrikeTeam.TE or CStrikeTeam.CT
                                 && sender.Team == receiver.Team;
                var transmit = sender.Equals(receiver) || !isTeammate;
                // true means visible; false blocks transmission. -1 updates all channels.
                _transmits.SetEntityState(sender.Index, receiver.Index, transmit, -1);
            }
        }
    }

    public string DisplayName   => "Transmit Hook Example";
    public string DisplayAuthor => "ModSharp dev team";
}
