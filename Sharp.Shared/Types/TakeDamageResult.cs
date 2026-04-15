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

using System;
using System.Runtime.InteropServices;
using Sharp.Shared.Enums;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Size = 0x50)]
public unsafe struct TakeDamageResult
{
    [FieldOffset(0x00)]
    public TakeDamageInfo* OriginatingInfo;

    // 0x08: CUtlLeanVector<DestructiblePartDamageRequest_t> (16 bytes, skipped)

    [FieldOffset(0x18)]
    public int HealthLost;

    [FieldOffset(0x1C)]
    public int HealthBefore;

    [FieldOffset(0x20)]
    public float DamageDealt;

    [FieldOffset(0x24)]
    public float PreModifiedDamage;

    [FieldOffset(0x28)]
    public int TotalledHealthLost;

    [FieldOffset(0x2C)]
    public float TotalledDamageDealt;

    [FieldOffset(0x30)]
    public float TotalledPreModifiedDamage;

    [FieldOffset(0x34)]
    public float NewDamageAccumulatorValue;

    [FieldOffset(0x38)]
    public ulong DamageFlags;

    [FieldOffset(0x40)]
    public bool WasDamageSuppressed;

    [FieldOffset(0x41)]
    public bool SuppressFlinch;

    [FieldOffset(0x44)]
    private HitGroupType OverrideFlinchHitGroup;

    [Obsolete("Use TotalledPreModifiedDamage instead")]
    public float m_flTotalledPreModifiedDamage => TotalledPreModifiedDamage;
}
