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
using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.Types.Tier;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x100)]
public struct MoveData
{
    [FieldOffset(0x0)]
    public byte MoveDataFlags;

    [FieldOffset(0x8)]
    public Vector AbsViewAngles;

    [FieldOffset(0x14)]
    public Vector ViewAngles;

    [FieldOffset(0x20)]
    public Vector LastMovementImpulses;

    [FieldOffset(0x2c)]
    public float ForwardMove;

    [FieldOffset(0x30)]
    public float SideMove;

    [FieldOffset(0x34)]
    public float UpMove;

    [FieldOffset(0x38)]
    public Vector Velocity;

    [FieldOffset(0x44)]
    public Vector Angles;

    [FieldOffset(0x50)]
    public CUtlVector<SubTickMove> SubTickMoves;

    [FieldOffset(0x68)]
    public CUtlVector<SubTickAttack> SubTickAttacks;

    [FieldOffset(0xb8)]
    public Vector AbsOrigin;

    [FieldOffset(0xd8)]
    public Vector OutWishVel;

    [FieldOffset(0x108)]
    public float MaxSpeed;

    [FieldOffset(0x10c)]
    public float ClientMaxSpeed;

    [FieldOffset(0x114)]
    [Obsolete("用InAir")]
    public bool OnGround;

    [FieldOffset(0x114)]
    public bool InAir;
}

[StructLayout(LayoutKind.Explicit, Size = 24, Pack = 8)]
public struct SubTickAttack
{
    [FieldOffset(0)]
    public float When;

    [FieldOffset(8)]
    public UserCommandButtons Button;

    [FieldOffset(16)]
    public bool Pressed;
}

[StructLayout(LayoutKind.Explicit, Size = 32, Pack = 8)]
public struct SubTickMove
{
    [FieldOffset(0)]
    public float When;

    [FieldOffset(8)]
    public UserCommandButtons Button;

    [FieldOffset(16)]
    public bool Pressed;

    [FieldOffset(16)]
    public float AnalogForwardDelta;

    [FieldOffset(20)]
    public float AnalogLeftDelta;

    [FieldOffset(24)]
    public float AnalogPitchDelta;

    [FieldOffset(28)]
    public float AnalogYawDelta;

    public bool IsAnalogInput => Button == 0;

    public override string ToString()
        => $"When={When:F2}, Button={Button}, Pressed={Pressed}";
};
