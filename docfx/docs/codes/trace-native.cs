using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace TraceNativeExample;

public sealed class TraceNativeExample : IModSharpModule
{
    public string DisplayName   => "Trace (Native) example";
    public string DisplayAuthor => "Modsharp dev team";

    private readonly ISharedSystem _sharedSystem;

    private static TraceNativeExample _instance = null!;

    public TraceNativeExample(ISharedSystem sharedSystem,
        string                              dllPath,
        string                              sharpPath,
        Version                             version,
        IConfiguration                      coreConfiguration,
        bool                                hotReload)
    {
        _sharedSystem = sharedSystem;
        _instance     = this;
    }

    public bool Init()
    {
        // client chat/console
        _sharedSystem.GetClientManager().InstallCommandCallback("trace_line",   OnCommandTraceLine);
        _sharedSystem.GetClientManager().InstallCommandCallback("trace_custom", OnCommandTraceCustom);

        return true;
    }

    public void Shutdown()
    {
        // must uninstall the callbacks on shutdown
        _sharedSystem.GetClientManager().RemoveCommandCallback("trace_line",   OnCommandTraceLine);
        _sharedSystem.GetClientManager().RemoveCommandCallback("trace_custom", OnCommandTraceCustom);
    }

    private unsafe ECommandAction OnCommandTraceLine(IGameClient client, StringCommand command)
    {
        if (client.GetPlayerController() is not { } controller
            || controller.GetPlayerPawn() is not { IsValidEntity: true, IsAlive: true } pawn)
        {
            return ECommandAction.Handled;
        }

        var eyeAngles = pawn.GetEyeAngles();

        // we start from player's eye position
        var start     = pawn.GetEyePosition();
        var direction = eyeAngles.AnglesToVectorForward();

        const float maxDistance = 4096.0f;

        var end = start + (direction * maxDistance);

        // we use bullet attribute here
        var attribute = RnQueryShapeAttr.Bullets();

        // make it ignore the player themselves, because we start tracing from player's
        // eye position and that is within the player's hitbox
        attribute.SetEntityToIgnore(pawn, 0);

        // this function is called for every entity the trace HITS, letting you decide if you
        // want to IGNORE that hit and continue tracing. we'll only use it if the
        // command has an argument (e.g., "trace_line filter_on").
        // the `delegate* unmanaged` syntax creates a function pointer that the game engine can call directly,
        // without .NET runtime overhead.
        nint? filter = command.ArgCount > 0
            ? (nint) (delegate* unmanaged<CTraceFilter*, nint, bool>) (&CTraceFilter_ShouldHitPlayerOnCT)
            : null;

        controller.Print(HudPrintChannel.Chat, $"Tracing {(filter == null ? "without" : "with")} ShouldHitPlayerOnCT filter");

        var traceResult = _sharedSystem.GetPhysicsQueryManager()
                                       .TraceLine(start,
                                                  end,
                                                  attribute,
                                                  filter);

        if (traceResult.DidHit())
        {
            var hitEntity = _sharedSystem.GetEntityManager().MakeEntityFromPointer<IBaseEntity>(traceResult.Entity);

            controller.Print(HudPrintChannel.Chat,
                             $" Hit entity!!! {ChatColor.NewLine}"
                             + $" entity classname: {hitEntity.Classname} {ChatColor.NewLine}"
                             + $" hit position: {traceResult.EndPosition} {ChatColor.NewLine}"
                             + $" fraction: {traceResult.Fraction}");
        }
        else
        {
            controller.Print(HudPrintChannel.Chat, "Failed to hit any entity.");
        }

        return ECommandAction.Handled;
    }

    private unsafe ECommandAction OnCommandTraceCustom(IGameClient client, StringCommand command)
    {
        if (client.GetPlayerController() is not { } controller
            || controller.GetPlayerPawn() is not { IsValidEntity: true, IsAlive: true } pawn)
        {
            return ECommandAction.Handled;
        }

        var eyeAngles = pawn.GetEyeAngles();

        // we start from player's eye position
        var start     = pawn.GetEyePosition();
        var direction = eyeAngles.AnglesToVectorForward();

        const float maxDistance = 4096.0f;

        var end = start + (direction * maxDistance);

        // we create our custom attribute here.
        // if you don't want to create a custom one, modsharp has a few built-in attributes:
        // RnQueryShapeAttr.Bullets(),
        // RnQueryShapeAttr.PlayerMovement(pawn.InteractsWith),
        // RnQueryShapeAttr.Knife()
        var attribute = new RnQueryShapeAttr
        {
            // what layers should this query attribute look for
            // you can think of it as a "whitelist", if an entity/object
            // doesn't have one of the flags in it, the trace will ignore it

            // here it is looking for anything that a bullet can interact with
            m_nInteractsWith = UsefulInteractionLayers.FireBullets,

            // what layers should this query attribute ignore
            // think of it as a "blacklist", if an entity/object has one of
            // the flags, the trace will ignore it
            // we don't want to hurt chickens, let's ignore it :)
            m_nInteractsExclude = InteractionLayers.CStrikeChicken,

            // what layers should this query represents as
            // here we are representing this layer as a player in T
            m_nInteractsAs = InteractionLayers.Player | InteractionLayers.CStrikeTeam1,

            // hit game entities and static entities
            m_nObjectSetMask  = RnQueryObjectSet.All,
            m_nCollisionGroup = CollisionGroupType.ConditionallySolid,

            // if true, will hit solid geometry and entities
            HitSolid = true,

            // if true, HitSolid will require the query and shape have contacts
            HitSolidRequiresGenerateContacts = true,

            // if true, will hit trigger entities
            HitTrigger = false,

            // if true, then ignores if the query and shape entity IDs are in collision pairs
            // in other words, it will respect the entities you set to ignore with SetEntityToIgnore
            ShouldIgnoreDisabledPairs = true,

            // if true, then ignores if both query and shape interact with InteractionLayers.HitBoxes
            IgnoreIfBothInteractWithHitBoxes = false,

            // if true, will hit any objects in any conditions
            ForceHitEverything = false,

            // not sure what it does but the game sets it to true by default
            Unknown = true,
        };

        // ignore ourselves because the query starts from ourselves
        // you can only set the index with 0 or 1
        var index = 0;
        attribute.SetEntityToIgnore(pawn, index);

        // we create a hull shape here
        // if mins and maxs are identical the game will treat it as ShapeLine
        var hull = new TraceShapeHull { Mins = new Vector(-32, -32, -32), Maxs = new Vector(32, 32, 32) };

        // or you can create the following shapes
        /*
        var line = new TraceShapeLine
        {
        };

        var sphere = new TraceShapeSphere
        {
        };

        var capsule = new TraceShapeCapsule
        {
        };
        */

        nint? filter = command.ArgCount > 0
            ? (nint) (delegate* unmanaged<CTraceFilter*, nint, bool>) (&CTraceFilter_ShouldHitPlayerOnCT)
            : null;

        controller.Print(HudPrintChannel.Chat, $"Tracing {(filter == null ? "without" : "with")} ShouldHitPlayerOnCT filter");

        var traceResult = _sharedSystem.GetPhysicsQueryManager()
                                       .TraceShape(new TraceShapeRay(hull), start, end, attribute, filter);

        if (traceResult.DidHit())
        {
            var hitEntity = _sharedSystem.GetEntityManager().MakeEntityFromPointer<IBaseEntity>(traceResult.Entity);

            controller.Print(HudPrintChannel.Chat,
                             $" Hit entity!!! {ChatColor.NewLine}"
                             + $" entity classname: {hitEntity.Classname} {ChatColor.NewLine}"
                             + $" hit position: {traceResult.EndPosition} {ChatColor.NewLine}"
                             + $" fraction: {traceResult.Fraction}");
        }
        else
        {
            controller.Print(HudPrintChannel.Chat, "Failed to hit any entity.");
        }

        return ECommandAction.Handled;
    }

    [UnmanagedCallersOnly]
    private static unsafe bool CTraceFilter_ShouldHitPlayerOnCT(CTraceFilter* filter, nint entityPtr)
    {
        // not a valid entity, don't hit, although this should never happen, just a safeguard
        if (_instance._sharedSystem.GetEntityManager().MakeEntityFromPointer<IBaseEntity>(entityPtr) is not
        {
            IsValidEntity: true,
        } entity)
        {
            return false;
        }

        // custom logic here
        // if we hit a player pawn
        if (entity is { IsPlayerPawn: true, IsAlive: true })
        {
            // we only want to accept the hit if they are on the CT team.
            // if they are on the T team, this function returns false, and the trace will continue
            // through them until it hits a CT player or a wall.
            return entity.Team == CStrikeTeam.CT;
        }

        return true;
    }
}
