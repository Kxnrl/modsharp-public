using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

// A simple traceline and check if the hit entity is a player.
// this only provides basic functionality, you should bring your own implementation
// if this does not fit your need
internal class Aim(ISharedSystem shared) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.Aim;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        if (activator?.GetPlayerController()?.GetPlayerPawn() is not { IsAlive: true } pawn)
        {
            return [];
        }

        var start = pawn.GetEyePosition();
        var fwd   = pawn.GetEyeAngles().AnglesToVectorForward();
        var end   = start + (fwd * 8192.0f);

        var attr = RnQueryShapeAttr.Bullets();
        attr.SetEntityToIgnore(pawn, 0);

        var trace = shared.GetPhysicsQueryManager().TraceLine(start, end, attr);

        if (!trace.DidHit())
        {
            return [];
        }

        // IsPlayerPawn will check if the entity is actually a player (not controller)
        if (shared.GetEntityManager().MakeEntityFromPointer<IPlayerPawn>(trace.Entity) is not { IsPlayerPawn: true } tracePawn)
        {
            return [];
        }

        if (tracePawn.GetControllerAuto() is not { IsValidEntity: true } traceController)
        {
            return [];
        }

        // NOTE: even if controller is a valid entity, if the corresponding IGameClient does not exist, it will still return null 
        if (traceController.GetGameClient() is { } traceClient)
        {
            return [traceClient];
        }

        return [];
    }
}
