/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.TargetingManager.Resolvers;

// A simple TraceLine and check if the hit entity is a player.
// this only provides basic functionality, you should bring your own implementation
// if this does not fit your need
internal sealed class Aim : BaseResolver
{
    public Aim(ISharedSystem sharedSystem) : base(sharedSystem)
    {
    }

    public override string GetTarget()
        => PredefinedTargets.Aim;

    public override IEnumerable<IGameClient> Resolve(IGameClient? activator)
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

        var trace = PhysicsQueryManager.TraceLine(start, end, attr);

        if (!trace.DidHit())
        {
            return [];
        }

        // IsPlayerPawn will check if the entity is actually a player (not controller)
        if (EntityManager.MakeEntityFromPointer<IPlayerPawn>(trace.Entity) is not { IsPlayerPawn: true } tracePawn)
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
