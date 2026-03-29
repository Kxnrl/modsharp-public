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
using Sharp.Shared.Attributes;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameEntities;

[NetClass("CCSWeaponBase")]
public interface IBaseWeapon : IEconEntity
{
    /// <summary>
    ///     Check if it is knife
    /// </summary>
    bool IsKnife { get; }

    /// <summary>
    ///     m_iItemDefinitionIndex
    /// </summary>

    ushort ItemDefinitionIndex { get; }

    /// <summary>
    ///     weapon classname
    /// </summary>
    /// <returns>The classname is based on ItemDefinitionIndex</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not a weapon</exception>
    string GetWeaponClassname();

    /// <summary>
    ///     ItemDefinitionName
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not a valid weapon/item</exception>
    string GetItemDefinitionName();

    /// <summary>
    ///     ItemDefinition
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not a valid weapon/item</exception>
    IEconItemDefinition GetItemDefinition();

    /// <summary>
    ///     WeaponVData
    /// </summary>
    IWeaponData GetWeaponData();

    /// <summary>
    ///     m_nNextPrimaryAttackTick
    /// </summary>
    int NextPrimaryAttackTick { get; set; }

    /// <summary>
    ///     m_nNextSecondaryAttackTick
    /// </summary>
    int NextSecondaryAttackTick { get; set; }

    /// <summary>
    ///     Weapon clip, -1 means it does not use Clip1, -2 when it is not a weapon <br />
    ///     <remarks>If exceeds <see cref="MaxClip"/> limit, will be reset to <see cref="MaxClip"/> when firing</remarks>
    /// </summary>
    int Clip { get; set; }

    /// <summary>
    ///     Weapon reserve ammunition. Returns -1 if ReserveAmmo1 is not used, -2 if not a gun <br />
    ///     <remarks>If exceeds ReserveAmmo limit, will be reset to <see cref="PrimaryReserveAmmoMax"/> when firing</remarks>
    /// </summary>
    int ReserveAmmo { get; set; }

    /// <summary>
    ///     Maximum bullets in magazine (from VData)
    /// </summary>
    int MaxClip { get; }

    /// <summary>
    ///     Max primary reserve ammo (From VData)
    /// </summary>
    int PrimaryReserveAmmoMax { get; }

    /// <summary>
    ///     Weapon slot (From VData)
    /// </summary>
    GearSlot Slot { get; }

    /// <summary>
    ///     m_hPrevOwner
    /// </summary>
    IPlayerPawn? PrevOwnerEntity { get; }

    /// <summary>
    ///     m_iWeaponGameplayAnimState
    /// </summary>
    WeaponGameplayAnimState WeaponGameplayAnimState { get; set; }

    /// <summary>
    ///     m_flWeaponGameplayAnimStateTimestamp
    /// </summary>
    float WeaponGameplayAnimStateTimestamp { get; set; }

    /// <summary>
    ///     m_weaponMode
    /// </summary>
    CStrikeWeaponMode WeaponMode { get; set; }

    /// <summary>
    ///     m_hPrevOwner
    /// </summary>
    CEntityHandle<IPlayerPawn> PrevOwnerEntityHandle { get; set; }

    /// <summary>
    ///     m_nextOwnerTouchTime
    /// </summary>
    float NextOwnerTouchTime { get; set; }

    /// <summary>
    ///     m_nextPrevOwnerTouchTime
    /// </summary>
    float NextPrevOwnerTouchTime { get; set; }

    /// <summary>
    ///     m_bCanBePickedUp
    /// </summary>
    bool CanBePickedUp { get; set; }

    /// <summary>
    ///     m_fAccuracyPenalty
    /// </summary>
    float AccuracyPenalty { get; set; }

    /// <summary>
    ///     m_flLastAccuracyUpdateTime
    /// </summary>
    float LastAccuracyUpdateTime { get; set; }

    /// <summary>
    ///     m_flRecoilIndex
    /// </summary>
    float RecoilIndex { get; set; }

    /// <summary>
    ///     m_bInReload
    /// </summary>
    bool InReload { get; set; }

    /// <summary>
    ///     m_bSilencerOn
    /// </summary>
    bool SilencerOn { get; set; }

    /// <summary>
    ///     m_flTimeSilencerSwitchComplete
    /// </summary>
    float TimeSilencerSwitchComplete { get; set; }

    /// <summary>
    ///     m_fLastShotTime
    /// </summary>
    float LastShotTime { get; set; }

    /// <summary>
    ///     Holster
    /// </summary>
    void Holster();

    /// <summary>
    ///     Deploy
    /// </summary>
    void Deploy();
}
