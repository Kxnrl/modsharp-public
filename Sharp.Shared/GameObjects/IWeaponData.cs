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

using Sharp.Shared.Attributes;
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;

namespace Sharp.Shared.GameObjects;

[NetClass("CCSWeaponBaseVData")]
public interface IWeaponData : IEntitySubclassVData
{
    /// <summary>
    ///     Maximum bullets in magazine
    /// </summary>
    int MaxClip { get; set; }

    /// <summary>
    ///     Weapon type
    /// </summary>
    CStrikeWeaponType WeaponType { get; }

    /// <summary>
    ///     Weapon category
    /// </summary>
    CStrikeWeaponCategory WeaponCategory { get; }

    /// <summary>
    ///     Equipment slot
    /// </summary>
    GearSlot GearSlot { get; }

    /// <summary>
    ///     Purchase price
    /// </summary>
    int Price { get; set; }

    /// <summary>
    ///     Maximum reserve ammunition
    /// </summary>
    int PrimaryReserveAmmoMax { get; set; }

    /// <summary>
    ///     Whether this is a melee weapon
    /// </summary>
    bool IsMeleeWeapon { get; }

    /// <summary>
    ///     Whether weapon is fully automatic
    /// </summary>
    bool IsFullAuto { get; }

    /// <summary>
    ///     Number of projectiles per shot
    /// </summary>
    int NumBullets { get; set; }

    /// <summary>
    ///     How long does it take to perform a primary attack
    /// </summary>
    float CycleTime { get; set; }

    /// <summary>
    ///     How long does it take to perform a secondary attack
    /// </summary>
    float CycleTimeAlt { get; set; }

    /// <summary>
    ///     Max walk speed when holding it
    /// </summary>
    float MaxSpeed { get; set; }

    /// <summary>
    ///     Max alt walk speed when holding it, for example awp with scope on
    /// </summary>
    float MaxSpeedAlt { get; set; }

    /// <summary>
    ///     Damage
    /// </summary>
    int Damage { get; set; }

    /// <summary>
    ///     Headshot damage multiplier
    /// </summary>
    float HeadshotMultiplier { get; set; }

    /// <summary>
    ///     Armor penetration ratio
    /// </summary>
    float ArmorRatio { get; set; }

    /// <summary>
    ///     Weapon range
    /// </summary>
    float Range { get; set; }

    /// <summary>
    ///     Distance damage falloff modifier
    /// </summary>
    float RangeModifier { get; set; }
}
