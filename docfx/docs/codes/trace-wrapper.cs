using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace TraceWrapperExample;

public sealed class TraceWrapperExample : IModSharpModule
{
    public string DisplayName   => "Trace (Wrapper) Example";
    public string DisplayAuthor => "Modsharp dev team";

    private readonly IModSharp            _modSharp;
    private readonly IPhysicsQueryManager _traces;

    private readonly IConVar _aimTargetType;

    public TraceWrapperExample(ISharedSystem sharedSystem,
        string                               dllPath,
        string                               sharpPath,
        Version                              version,
        IConfiguration                       coreConfiguration,
        bool                                 hotReload)
    {
        _aimTargetType
            = sharedSystem.GetConVarManager()
                          .CreateConVar("ms_show_aim_target_type", 0, "0 = all, 1 = teammate, 2 = enemy", ConVarFlags.Release)
              ?? throw new EntryPointNotFoundException("Failed to create convar");

        _modSharp = sharedSystem.GetModSharp();
        _traces   = sharedSystem.GetPhysicsQueryManager();
    }

    public bool Init()
        => true;

    public void Shutdown()
    {
    }

    public void PostInit()
        => _modSharp.PushTimer(Think, 0.1, GameTimerFlags.Repeatable);

    private void Think()
    {
        // you also can use other way to get clients or controllers
        var clients = _modSharp.GetIServer().GetGameClients(true, true);

        var type = _aimTargetType.GetInt32();

        foreach (var client in clients)
        {
            if (client.GetPlayerController() is not { } controller)
            {
                continue;
            }

            if (controller.GetPlayerPawn() is not { IsAlive: true } pawn)
            {
                continue;
            }

            ShowAimTarget(controller, pawn, type);
            ShowAimPosition(controller, pawn);
            CheckStuck(controller, pawn);
        }
    }

    private void ShowAimTarget(IPlayerController controller, IPlayerPawn pawn, int type)
    {
        // you can make it what you want
        // but the max size of world is 16384*16384*16384
        // bigger than that is probably useless
        const float maxAimTargetDistance = 4096;

        // the beginning of ray is your eyes
        var startPos = pawn.GetEyePosition();

        // build the end position of the ray
        var endPos   = startPos + (pawn.GetEyeAngles().AnglesToVectorForward() * maxAimTargetDistance);

        TraceResult trace;

        switch (type)
        {
            case 0:
            {
                trace = _traces.TraceLine(startPos,
                                          endPos,
                                          UsefulInteractionLayers.FireBullets, // use FireBullet (Game builtin) layers
                                          CollisionGroupType.Default,          // mostly you should use default
                                          TraceQueryFlag.All,                  // trace against all entities and static
                                          InteractionLayers.None,              // the ignored layers
                                          pawn                                 // start ignore self
                );

                break;
            }
            case 1 or 2:
            {
                var team = type == 1
                    ? pawn.Team
                    : pawn.Team is CStrikeTeam.CT
                        ? CStrikeTeam.TE
                        : CStrikeTeam.CT;

                // the arguments are similar to above
                trace = _traces.TraceLineFilter(startPos,
                                                endPos,
                                                UsefulInteractionLayers.FireBullets,
                                                CollisionGroupType.Default,
                                                TraceQueryFlag.All,
                                                InteractionLayers.None,

                                                // use custom filter here
                                                entity =>
                                                {
                                                    if (entity.Team != team)
                                                    {
                                                        return false;
                                                    }

                                                    if (!entity.IsPlayerPawn)
                                                    {
                                                        return false;
                                                    }

                                                    return entity.IsAlive;
                                                });

                break;
            }
            default:
                throw new ArgumentException($"Invalid trace type: {type}");
        }

        // if hit nothing or target entity is not player
        if (trace.HitEntity?.AsPlayerPawn() is not { } targetPawn
            || targetPawn.GetOriginalController() is not { } targetController)
        {
            return;
        }

        var message = $"{targetController.PlayerName}\n Health: {pawn.Health}/{pawn.MaxHealth}";

        controller.Print(HudPrintChannel.Center, message);
    }

    private void ShowAimPosition(IPlayerController controller, IPlayerPawn pawn)
    {
        var eye = pawn.GetEyePosition();
        var end = eye + (pawn.GetEyeAngles().AnglesToVectorForward() * 3248);

        // use TraceLineNoPlayers to ignore all players
        var trace = _traces.TraceLineNoPlayers(eye,
                                               end,
                                               UsefulInteractionLayers.PlayerPing, // use PlayerPing (Game builtin) layers
                                               CollisionGroupType.Default,
                                               TraceQueryFlag.All);

        // null the hitting entity, it should be entity or world
        if (trace.HitEntity is null)
        {
            return;
        }

        // start in solid :(
        if (trace.StartInSolid)
        {
            return;
        }

        var position = trace.EndPosition;
        var distance = eye.DistTo(position);

        var message = $"Position: {{ {position.X:F2}, {position.Y:F2}, {position.Z:F2} }}\nDistance: {distance:F2}";

        controller.Print(HudPrintChannel.Hint, message);
    }

    private void CheckStuck(IPlayerController controller, IPlayerPawn pawn)
    {
        if (pawn.GetCollisionProperty() is not { } cp)
        {
            return;
        }

        // get the player's hull
        var hull     = new TraceShapeHull { Mins = cp.Mins, Maxs = cp.Maxs };
        var position = pawn.GetAbsOrigin();

        var trace = _traces.TraceShapeNoPlayers(

            // create shape
            new TraceShapeRay(hull),

            // start and end position mostly are the same
            position,
            position,

            // you can use other layers what you want
            // it's came from movement game code.
            InteractionLayers.Solid
            | InteractionLayers.Sky
            | InteractionLayers.PlayerClip
            | InteractionLayers.WorldGeometry
            | InteractionLayers.Slime
            | InteractionLayers.Player
            | InteractionLayers.PhysicsProp,
            CollisionGroupType.Default,

            // query against all entities and static
            TraceQueryFlag.All);

        if (!trace.DidHit())
        {
            return;
        }

        controller.Print(HudPrintChannel.Chat, "You are stuck, femboy @Nuko will kill you.");

        pawn.Slay(true);
    }
}
