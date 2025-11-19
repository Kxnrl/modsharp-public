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
    ///     处死
    /// </summary>
    /// <param name="explode">原地爆炸</param>
    void Slay(bool explode = false);

    /// <summary>
    ///     自动识别存活状态就拿Controller, 否则Original
    /// </summary>
    /// <returns></returns>
    IPlayerController? GetControllerAuto();

    /// <summary>
    ///     发枪
    /// </summary>
    IBaseWeapon? GiveNamedItem(string weapon);

    /// <summary>
    ///     发枪
    /// </summary>
    IBaseWeapon? GiveNamedItem(EconItemId item);

    /// <summary>
    ///     获取当前手上的武器
    /// </summary>
    /// <returns></returns>
    IBaseWeapon? GetActiveWeapon();

    /// <summary>
    ///     通过槽位获取身上的武器
    /// </summary>
    IBaseWeapon? GetWeaponBySlot(GearSlot slot, int position = -1);

    /// <summary>
    ///     移除武器并立即销毁
    /// </summary>
    void RemovePlayerItem(IBaseWeapon item);

    /// <summary>
    ///     移除所有物品
    /// </summary>
    /// <param name="removeSuit">包括护甲</param>
    void RemoveAllItems(bool removeSuit = false);

    /// <summary>
    ///     强制丢掉武器
    /// </summary>
    void DropWeapon(IBaseWeapon item);

    /// <summary>
    ///     切换武器/道具
    /// </summary>
    bool SelectItem(IBaseWeapon item);

    /// <summary>
    ///     分离武器
    /// </summary>
    bool DetachWeapon(IBaseWeapon item);

    /// <summary>
    ///     强制切换武器或空手 (无视一切检测/条件判断)
    /// </summary>
    void SwitchWeapon(IBaseWeapon? weapon);

    /// <summary>
    ///     发手套
    /// </summary>
    void GiveGloves(int itemDefIndex, int prefab, float wear, int seed);

    /// <summary>
    ///     发手套
    /// </summary>
    void GiveGloves(EconGlovesId id, int prefab, float wear, int seed);

    /// <summary>
    ///     瞬态修改VelocityModifier,不发送网络消息
    /// </summary>
    void TransientChangeVelocityModifier(float velocityModifier);

    /// <summary>
    ///     ItemService实例
    /// </summary>
    IItemService? GetItemService();

    /// <summary>
    ///     WeaponService实例
    /// </summary>
    IWeaponService? GetWeaponService();

    /// <summary>
    ///     PlayerMoveService实例
    /// </summary>
    IPlayerMovementService? GetPlayerMovementService();

    /// <summary>
    ///     PlayerUseService实例
    /// </summary>
    IPlayerUseService? GetPlayerUseService();

    /// <summary>
    ///     PingServices实例
    /// </summary>
    IPingService? GetPingService();

    /// <summary>
    ///     WaterService实例
    /// </summary>
    IWaterService? GetWaterService();

    /// <summary>
    ///     BulletService实例
    /// </summary>
    IBulletService? GetBulletService();

    /// <summary>
    ///     HostageService实例
    /// </summary>
    IHostageService? GetHostageService();

    /// <summary>
    ///     BuyService实例
    /// </summary>
    IBuyService? GetBuyService();

    /// <summary>
    ///     ActionTrackingService实例
    /// </summary>
    IPlayerActionTrackingService? GetActionTrackingService();

    /// <summary>
    ///     RadioService实例
    /// </summary>
    IRadioService? GetRadioService();

    /// <summary>
    ///     DamageReactService实例
    /// </summary>
    IDamageReactService? GetDamageReactService();

    /// <summary>
    ///     Glove Econ
    /// </summary>
    IEconItemView GetEconGloves();

    /// <summary>
    ///     护甲值
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
