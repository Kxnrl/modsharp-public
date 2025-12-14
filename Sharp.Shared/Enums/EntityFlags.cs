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

/// <summary>
///     Entity flags for a base entity.
/// </summary>
[Flags]
public enum EntityFlags : uint
{
    /// <summary>
    ///     At rest / on the ground.
    /// </summary>
    OnGround = 1 << 0,

    /// <summary>
    ///     Player flag -- Player is fully crouched.
    /// </summary>
    Ducking = 1 << 1,

    /// <summary>
    ///     Player jumping out of water.
    /// </summary>
    WaterJump = 1 << 2,

    /// <summary>
    ///     Is FakeClient <br />
    ///     <remarks>Only PlayerPawn has this flag</remarks>
    /// </summary>
    Bot = 1 << 4,

    /// <summary>
    ///     Player is frozen for 3rd person camera.
    /// </summary>
    Frozen = 1 << 5,

    /// <summary>
    ///     Player can't move, but keeps key inputs for controlling another entity.
    /// </summary>
    AtControls = 1 << 6,

    /// <summary>
    ///     Is a player. <br />
    ///     <remarks>Only PlayerPawn has this flag</remarks>
    /// </summary>
    Client = 1 << 7,

    /// <summary>
    ///     Fake client.<br />
    ///     <remarks>Only PlayerController has this flag</remarks>
    /// </summary>
    FakeClient = 1 << 8,

    /// <summary>
    ///     Changes the SV_Movestep() behavior to not need to be on ground.
    /// </summary>
    Fly = 1 << 10,

    /// <summary>
    /// </summary>
    SuppressSave = 1 << 11,

    /// <summary>
    /// </summary>
    InVehicle = 1 << 12,

    /// <summary>
    ///     God mode.
    /// </summary>
    GodMode = 1 << 14,

    /// <summary>
    ///     No target.
    /// </summary>
    NoTarget = 1 << 15,

    /// <summary>
    ///     Set if the crosshair needs to aim onto the entity.
    /// </summary>
    AimTarget = 1 << 16,

    /// <summary>
    ///     Eetsa static prop!
    /// </summary>
    StaticProp = 1 << 18,

    /// <summary>
    ///     Grenade.
    /// </summary>
    Grenade = 1 << 20,

    /// <summary>
    ///     Doesn't generate touch functions, generates Untouch() for anything it was touching when this flag was set.
    /// </summary>
    DontTouch = 1 << 22,

    /// <summary>
    ///     Base velocity has been applied this frame (used to convert base velocity into momentum).
    /// </summary>
    BaseVelocity = 1 << 23,

    /// <summary>
    ///     Conveyor
    /// </summary>
    Conveyor = 1 << 24,

    /// <summary>
    ///     Terrible name. This is an object that NPCs should see. Missiles, for example.
    /// </summary>
    Object = 1 << 25,

    /// <summary>
    ///     You know...
    /// </summary>
    OnFire = 1 << 27,

    /// <summary>
    ///     We're dissolving!
    /// </summary>
    Dissolving = 1 << 28,

    /// <summary>
    ///     In the process of turning into a client side ragdoll.
    /// </summary>
    TransRagdoll = 1 << 29,

    /// <summary>
    ///     Pusher that can't be blocked by the player.
    /// </summary>
    UnBlockableByPlayer = 1 << 30,
}
