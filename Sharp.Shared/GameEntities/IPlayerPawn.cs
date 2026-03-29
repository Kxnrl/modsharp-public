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
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameEntities;

[NetClass("CCSPlayerPawn")]
public interface IPlayerPawn : IBasePlayerPawn
{
    /// <summary>
    ///     Slay
    /// </summary>
    void Slay(bool explode = false);

    /// <summary>
    ///     Automatically gets Controller if alive, otherwise OriginalController
    /// </summary>
    /// <returns></returns>
    IPlayerController? GetControllerAuto();

    /// <summary>
    ///     Give weapon to player
    /// </summary>
    IBaseWeapon? GiveNamedItem(string weapon);

    /// <summary>
    ///     Give weapon to player
    /// </summary>
    IBaseWeapon? GiveNamedItem(EconItemId item);

    /// <summary>
    ///     Gets the currently held weapon
    /// </summary>
    /// <returns></returns>
    IBaseWeapon? GetActiveWeapon();

    /// <summary>
    ///     Gets weapon from inventory by slot
    /// </summary>
    IBaseWeapon? GetWeaponBySlot(GearSlot slot, int position = -1);

    /// <summary>
    ///     Remove weapon and destroy it immediately
    /// </summary>
    /// <remarks>
    ///     When looping through <see cref="IWeaponService.GetMyWeapons"/>, use <see cref="IModSharp.InvokeFrameAction"/> 
    ///     to avoid modifying the collection while iterating
    /// </remarks>
    void RemovePlayerItem(IBaseWeapon item);

    /// <summary>
    ///     Remove all items
    /// </summary>
    /// <param name="removeSuit">Include armor</param>
    void RemoveAllItems(bool removeSuit = false);

    /// <summary>
    ///     Force drop weapon
    /// </summary>
    void DropWeapon(IBaseWeapon item);

    /// <summary>
    ///     Switch to weapon/item
    /// </summary>
    bool SelectItem(IBaseWeapon item);

    /// <summary>
    ///     Detach weapon from player
    /// </summary>
    bool DetachWeapon(IBaseWeapon item);

    /// <summary>
    ///     Force switch weapon or go empty-handed (bypasses all checks/conditions)
    /// </summary>
    void SwitchWeapon(IBaseWeapon? weapon);

    /// <summary>
    ///     Give gloves to player
    /// </summary>
    void GiveGloves(int itemDefIndex, int prefab, float wear, int seed);

    /// <summary>
    ///     Give gloves to player
    /// </summary>
    void GiveGloves(EconGlovesId id, int prefab, float wear, int seed);

    /// <summary>
    ///     Change VelocityModifier without calling StateChanged, player won't receive new value
    /// </summary>
    void TransientChangeVelocityModifier(float velocityModifier);

    /// <summary>
    ///     ItemService
    /// </summary>
    IItemService? GetItemService();

    /// <summary>
    ///     WeaponService
    /// </summary>
    IWeaponService? GetWeaponService();

    /// <summary>
    ///     PlayerMoveService
    /// </summary>
    IPlayerMovementService? GetPlayerMovementService();

    /// <summary>
    ///     PlayerUseService
    /// </summary>
    IPlayerUseService? GetPlayerUseService();

    /// <summary>
    ///     PingServices
    /// </summary>
    IPingService? GetPingService();

    /// <summary>
    ///     WaterService
    /// </summary>
    IWaterService? GetWaterService();

    /// <summary>
    ///     BulletService
    /// </summary>
    IBulletService? GetBulletService();

    /// <summary>
    ///     HostageService
    /// </summary>
    IHostageService? GetHostageService();

    /// <summary>
    ///     BuyService
    /// </summary>
    IBuyService? GetBuyService();

    /// <summary>
    ///     ActionTrackingService
    /// </summary>
    IPlayerActionTrackingService? GetActionTrackingService();

    /// <summary>
    ///     RadioService
    /// </summary>
    IRadioService? GetRadioService();

    /// <summary>
    ///     DamageReactService
    /// </summary>
    IDamageReactService? GetDamageReactService();

    /// <summary>
    ///     Glove Econ
    /// </summary>
    IEconItemView GetEconGloves();

    /// <summary>
    ///     Armor value
    /// </summary>
    int ArmorValue { get; set; }

    /// <summary>
    ///     m_fMolotovDamageTime
    /// </summary>
    float MolotovDamageTime { get; set; }

    /// <summary>
    ///     m_flHealthShotBoostExpirationTime
    /// </summary>
    float HealthShotBoostExpirationTime { get; set; }

    /// <summary>
    ///     m_flVelocityModifier
    /// </summary>
    float VelocityModifier { get; set; }

    /// <summary>
    ///     m_iShotsFired
    /// </summary>
    int ShotsFired { get; set; }

    /// <summary>
    ///     m_flFlinchStack
    /// </summary>
    float FlinchStack { get; set; }

    /// <summary>
    ///     m_bInBuyZone
    /// </summary>
    bool InBuyZone { get; }

    /// <summary>
    ///     m_bInHostageRescueZone
    /// </summary>
    bool InHostageRescueZone { get; }

    /// <summary>
    ///     m_bInBombZone
    /// </summary>
    bool InBombZone { get; }

    /// <summary>
    ///     m_bIsBuyMenuOpen
    /// </summary>
    bool IsBuyMenuOpen { get; }

    /// <summary>
    ///     m_bLeftHanded
    /// </summary>
    bool LeftHanded { get; }

    /// <summary>
    ///     m_fSwitchedHandednessTime
    /// </summary>
    float SwitchedHandednessTime { get; set; }

    /// <summary>
    ///     m_vecTotalBulletForce
    /// </summary>
    Vector TotalBulletForce { get; set; }

    /// <summary>
    ///     m_iDeathFlags
    /// </summary>
    int DeathFlags { get; set; }

    /// <summary>
    ///     m_bWaitForNoAttack
    /// </summary>
    bool WaitForNoAttack { get; set; }

    /// <summary>
    ///     m_aimPunchAngle
    /// </summary>
    Vector AimPunchAngle { get; set; }

    /// <summary>
    ///     m_aimPunchAngleVel
    /// </summary>
    Vector AimPunchAngleVelocity { get; set; }

    /// <summary>
    ///     m_aimPunchTickBase
    /// </summary>
    int AimPunchTickBase { get; set; }

    /// <summary>
    ///     m_aimPunchTickFraction
    /// </summary>
    float AimPunchTickFraction { get; set; }

    /// <summary>
    ///     m_aimPunchCache
    /// </summary>
    ISchemaList<Vector> GetAimPunchCache();
}
