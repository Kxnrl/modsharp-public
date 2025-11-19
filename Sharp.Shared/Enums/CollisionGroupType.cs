/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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

using System;

namespace Sharp.Shared.Enums;

public enum CollisionGroupType : byte
{
    /// <summary>
    ///     Default layer, always collides with everything.
    /// </summary>
    Always = 0,

    /// <summary>
    ///     This is how you turn off all collisions for an object - move it to this group
    /// </summary>
    [Obsolete("use NonPhysical instead, will be removed in 2.2")]
    Never,

    /// <summary>
    ///     This is how you turn off all collisions for an object - move it to this group
    /// </summary>
    NonPhysical = 1,

    /// <summary>
    ///     Trigger layer, never collides with anything, only triggers/interacts.  Use when querying for interaction layers.
    /// </summary>
    Trigger,

    /// <summary>
    ///     Conditionally solid means that the collision response will be zero or as defined in the table when there are
    ///     matching interactions
    /// </summary>
    ConditionallySolid,

    Default,

    /// <summary>
    ///     Collides with nothing but world, static stuff and trigger
    /// </summary>
    Debris,

    /// <summary>
    ///     Collides with everything except other interactive debris or debris
    /// </summary>
    InteractiveDebris,

    /// <summary>
    ///     Collides with everything except interactive debris or debris
    /// </summary>
    Interactive,

    Player,
    BreakableGlass,
    Vehicle,

    /// <summary>
    ///     For HL2, same as Collision_Group_Player, <br />
    ///     For TF2, this filters out other players and CBaseObjects
    /// </summary>
    PlayerMovement,

    /// <summary>
    ///     Generic Npc group
    /// </summary>
    Npc,

    /// <summary>
    ///     for any entity inside a vehicle
    /// </summary>
    InVehicle,

    /// <summary>
    ///     for any weapons that need collision detection
    /// </summary>
    Weapon,

    /// <summary>
    ///     vehicle clip brush to restrict vehicle movement
    /// </summary>
    VehicleClip,

    /// <summary>
    ///     Projectiles!
    /// </summary>
    Projectile,

    /// <summary>
    ///     Blocks entities not permitted to get near moving doors
    /// </summary>
    DoorBlocker,

    /// <summary>
    ///     Doors that the player shouldn't collide with
    /// </summary>
    PassableDoor,

    /// <summary>
    ///     Things that are dissolving are in this group
    /// </summary>
    Dissolving,

    /// <summary>
    ///     Nonsolid on client and server, pushaway in player code
    /// </summary>
    PushAway,

    /// <summary>
    ///     Used so Npcs in scripts ignore the player.
    /// </summary>
    NpcActor,

    /// <summary>
    ///     Used for Npcs in scripts that should not collide with each other
    /// </summary>
    NpcScripted,

    PZClip,
    Props,

    LastSharedCollisionGroup,
}
