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
using Sharp.Shared.Enums;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Size = 40)]
public unsafe struct TakeDamageResult
{
    [FieldOffset(0)]
    public TakeDamageInfo* OriginatingInfo;

    [FieldOffset(8)]
    public int HealthLost;

    [FieldOffset(12)]
    public int HealthBefore;

    [FieldOffset(16)]
    public int DamageDealt;

    [FieldOffset(20)]
    public float PreModifiedDamage;

    [FieldOffset(24)]
    public int TotalledHealthLost;

    [FieldOffset(28)]
    public int TotalledDamageDealt;

    [FieldOffset(32)]
    public float m_flTotalledPreModifiedDamage;

    [FieldOffset(36)]
    public bool WasDamageSuppressed;

    [FieldOffset(37)]
    public bool SuppressFlinch;

    [FieldOffset(40)]
    private HitGroupType OverrideFlinchHitGroup;
}
