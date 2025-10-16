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

[Flags]
public enum InteractionLayers : ulong
{
    None                  = 0,
    Solid                 = 1UL << 0,  // 0x00000001
    HitBoxes              = 1UL << 1,  // 0x00000002
    Trigger               = 1UL << 2,  // 0x00000004
    Sky                   = 1UL << 3,  // 0x00000008
    PlayerClip            = 1UL << 4,  // 0x00000010
    NpcClip               = 1UL << 5,  // 0x00000020
    BlockLos              = 1UL << 6,  // 0x00000040
    BlockLight            = 1UL << 7,  // 0x00000080
    Ladder                = 1UL << 8,  // 0x00000100
    Pickup                = 1UL << 9,  // 0x00000200
    BlockSound            = 1UL << 10, // 0x00000400
    NoDraw                = 1UL << 11, // 0x00000800
    Window                = 1UL << 12, // 0x00001000
    PassBullets           = 1UL << 13, // 0x00002000
    WorldGeometry         = 1UL << 14, // 0x00004000
    Water                 = 1UL << 15, // 0x00008000
    Slime                 = 1UL << 16, // 0x00010000
    TouchAll              = 1UL << 17, // 0x00020000
    Player                = 1UL << 18, // 0x00040000
    Npc                   = 1UL << 19, // 0x00080000
    Debris                = 1UL << 20, // 0x00100000
    PhysicsProp           = 1UL << 21, // 0x00200000
    NavIgnore             = 1UL << 22, // 0x00400000
    NavLocalIgnore        = 1UL << 23, // 0x00800000
    PostProcessingVolume  = 1UL << 24, // 0x01000000
    VehicleClip           = 1UL << 25, // 0x02000000
    CarriedObject         = 1UL << 26, // 0x04000000
    PushAway              = 1UL << 27, // 0x08000000
    ServerEntityOnClient  = 1UL << 28, // 0x10000000
    CarriedWeapon         = 1UL << 29, // 0x20000000
    StaticLevel           = 1UL << 30, // 0x40000000
    CStrikeTeam1          = 1UL << 31, // 0x80000000
    CStrikeTeam2          = 1UL << 32, // 0x100000000
    CStrikeGrenadeClip    = 1UL << 33, // 0x200000000
    CStrikeDroneClip      = 1UL << 34, // 0x400000000
    CStrikeMoveable       = 1UL << 35, // 0x800000000
    CStrikeOpaque         = 1UL << 36, // 0x1000000000
    CStrikeMonster        = 1UL << 37, // 0x2000000000
    CStrikeChicken        = 1UL << 38, // 0x4000000000
    CStrikeThrownGrenade  = 1UL << 39, // 0x8000000000
    CStrikeDroppedHostage = 1UL << 40, // 0x10000000000
}
