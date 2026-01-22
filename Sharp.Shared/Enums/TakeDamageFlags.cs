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
public enum TakeDamageFlags : ulong
{
    None                       = 0,       // 0x0
    SuppressHealthChanges      = 1 << 0,  // 0x1
    SuppressPhysicsForce       = 1 << 1,  // 0x2
    SuppressEffects            = 1 << 2,  // 0x4
    PreventDeath               = 1 << 3,  // 0x8
    ForceDeath                 = 1 << 4,  // 0x10
    AlwaysGib                  = 1 << 5,  // 0x20
    NeverGib                   = 1 << 6,  // 0x40
    RemoveNoRagdoll            = 1 << 7,  // 0x80
    SuppressDamageModification = 1 << 8,  // 0x100
    AlwaysFireDamageEvents     = 1 << 9,  // 0x200
    RadiusDamage               = 1 << 10, // 0x400
    ForceReduceArmorDamage     = 1 << 11, // 0x800
    SuppressInterruptFlinch    = 1 << 12, // 0x1000
    IgnoreDestructibleParts    = 1 << 13, // 0x2000
    SuppressBreakables         = 1 << 14, // 0x4000
    ForcePhysicsForce          = 1 << 15, // 0x8000
    ScreenSpaceDamageFx        = 1 << 16, // 0x10000
    AllowNonAuthoritative      = 1 << 17, // 0x20000
    IgnoreArmor                = 1 << 18, // 0x40000
    SuppressUtilRemove         = 1 << 19, // 0x80000
}
