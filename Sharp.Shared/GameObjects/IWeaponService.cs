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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameObjects;

/// <summary>
///     WeaponServices is only for CCSPlayerPawn
/// </summary>
[NetClass("CCSPlayer_WeaponServices")]
public interface IWeaponService : IPlayerPawnComponent
{
    /* CCSPlayer_WeaponServices */

    /// <summary>
    ///     m_flNextAttack
    /// </summary>
    float NextAttack { get; set; }

    /// <summary>
    ///     m_bIsLookingAtWeapon
    /// </summary>
    bool IsLookingAtWeapon { get; }

    /// <summary>
    ///     m_bIsHoldingLookAtWeapon
    /// </summary>
    bool IsHoldingLookAtWeapon { get; }

    /// <summary>
    ///     m_hSavedWeapon
    /// </summary>
    CEntityHandle<IBaseWeapon> SavedWeaponHandle { get; set; }

    /// <summary>
    ///     m_bIsPickingUpItemWithUse
    /// </summary>
    bool IsPickingUpItemWithUse { get; set; }

    /// <summary>
    ///     m_bPickedUpWeapon
    /// </summary>
    bool PickedUpWeapon { get; set; }

    /// <summary>
    ///     m_bDisableAutoDeploy
    /// </summary>
    bool DisableAutoDeploy { get; set; }

    /// <summary>
    ///     m_bIsPickingUpGroundWeapon
    /// </summary>
    bool IsPickingUpGroundWeapon { get; set; }

    /* CPlayer_WeaponServices */

    /// <summary>
    ///     m_hMyWeapons
    /// </summary>
    ISchemaList<CEntityHandle<IBaseWeapon>> GetMyWeapons();

    /// <summary>
    ///     ActiveWeapon
    /// </summary>
    IBaseWeapon? ActiveWeapon { get; }

    /// <summary>
    ///     m_hActiveWeapon
    /// </summary>
    CEntityHandle<IBaseWeapon> ActiveWeaponHandle { get; set; }

    /// <summary>
    ///     Last used Weapon
    /// </summary>
    IBaseWeapon? LastWeapon { get; }

    /// <summary>
    ///     m_hLastWeapon
    /// </summary>
    CEntityHandle<IBaseWeapon> LastWeaponHandle { get; set; }

    /// <summary>
    ///     m_iAmmo[32]
    /// </summary>
    ISchemaArray<ushort> GetAmmo();

    /// <summary>
    ///     m_bPreventWeaponPickup
    /// </summary>
    bool PreventWeaponPickup { get; set; }
}
