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

using System.Runtime.InteropServices;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 0x58)]
public struct ShootInfo
{
    [FieldOffset(0)]
    public int AttackTickCount;

    [FieldOffset(4)]
    public float AttackTickFraction;

    [FieldOffset(8)]
    public float AttackTime;

    [FieldOffset(0xc)]
    public int RenderTickCount;

    [FieldOffset(0x10)]
    public float RenderTickFraction;

    [FieldOffset(0x14)]
    public Vector AttackPos;

    [FieldOffset(0x20)]
    public Vector AttackAngles;

    [FieldOffset(0x2c)]
    public Vector PunchAngles;

    [FieldOffset(0x38)]
    public bool FirstShot;

    [FieldOffset(0x3c)]
    public int State;

    [FieldOffset(0x40)]
    public int ServerTick;

    [FieldOffset(0x44)]
    public float Inaccuracy;

    [FieldOffset(0x48)]
    public float GroundInaccuracy;

    [FieldOffset(0x4c)]
    public float AirInaccuracy;

    [FieldOffset(0x50)]
    public float RecoilIndex;

    [FieldOffset(0x54)]
    private int Unknown1;
}
