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
using System.Collections.Generic;
using Sharp.Shared.Attributes;
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.GameEntities;

[NetClass("CBaseEntity")]
public interface IBaseEntity : ISchemaObject
{
    /// <summary>
    ///     Handle
    /// </summary>
    CEntityHandle<IBaseEntity> Handle { get; }

    /// <summary>
    ///     Ref EntityHandle <br />
    ///     <remarks>我也不知道是什么玩意, 反正游戏有</remarks>
    /// </summary>
    CEntityHandle<IBaseEntity> RefHandle { get; }

    /// <summary>
    ///     血量
    /// </summary>
    int Health { get; set; }

    /// <summary>
    ///     最大血量
    /// </summary>
    int MaxHealth { get; set; }

    /// <summary>
    ///     是否存活
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    ///     存活状态
    /// </summary>
    LifeState LifeState { get; }

    /// <summary>
    ///     阵营
    /// </summary>
    CStrikeTeam Team { get; }

    /// <summary>
    ///     MoveCollide
    /// </summary>
    MoveCollideType MoveCollide { get; }

    /// <summary>
    ///     MoveType
    /// </summary>
    MoveType MoveType { get; }

    /// <summary>
    ///     ActualMoveType
    /// </summary>
    MoveType ActualMoveType { get; }

    /// <summary>
    ///     Spawn Flags
    /// </summary>
    uint SpawnFlags { get; set; }

    /// <summary>
    ///     返回实体Index <br />
    ///     <remarks>
    ///         该操作不能保证时效性, 要确保时效性请调用<see cref="IsValid" />或你确保它当前有效
    ///     </remarks>
    /// </summary>
    EntityIndex Index { get; }

    /// <summary>
    ///     读取Classname<br />
    ///     <remarks>请在调用该参数前确认你已经调用了实体的<see cref="IsValid" />或你确保它当前有效, 否则你将会崩溃</remarks>
    /// </summary>
    string Classname { get; }

    /// <summary>
    ///     读取实体Name<br />
    ///     <remarks>
    ///         请在调用该参数前确认你已经调用了实体的<see cref="IsValid" />或你确保它当前有效, 否则你将会崩溃
    ///     </remarks>
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     读取实体HammerId<br />
    /// </summary>
    string HammerId { get; }

    /// <summary>
    ///     m_iGlobalname
    /// </summary>
    string GlobalName { get; set; }

    /// <summary>
    ///     是否允许TakeDamage (m_bTakesDamage) <br />
    ///     <remarks>
    ///         请在调用该参数前确认你已经调用了实体的<see cref="IsValid" />或你确保它当前有效, 否则你将会崩溃
    ///     </remarks>
    /// </summary>
    bool AllowTakesDamage { get; set; }

    /// <summary>
    ///     m_hOwnerEntity
    /// </summary>
    IBaseEntity? OwnerEntity { get; }

    /// <summary>
    ///     m_fEffects
    /// </summary>
    EntityEffects Effects { get; set; }

    /// <summary>
    ///     m_fFlags
    /// </summary>
    EntityFlags Flags { get; set; }

    /// <summary>
    ///     m_iEFlags
    /// </summary>
    int EFlags { get; set; }

    /// <summary>
    ///     m_flSpeed
    /// </summary>
    float Speed { get; set; }

    /// <summary>
    ///     m_hGroundEntity Handle
    /// </summary>
    CEntityHandle<IBaseEntity> GroundEntityHandle { get; }

    /// <summary>
    ///     m_hGroundEntity
    /// </summary>
    IBaseEntity? GroundEntity { get; }

    /// <summary>
    ///     m_hEffectEntity
    /// </summary>
    CEntityHandle<IBaseEntity> EffectEntityHandle { get; set; }

    /// <summary>
    ///     m_hEffectEntity
    /// </summary>
    IBaseEntity? EffectEntity { get; }

    /// <summary>
    ///     m_hDamageFilter
    /// </summary>
    CEntityHandle<IBaseFilter> DamageFilterEntityHandle { get; set; }

    /// <summary>
    ///     m_hEffectEntity
    /// </summary>
    IBaseFilter? DamageFilterEntity { get; }

    /// <summary>
    ///     检查实体是否有效
    /// </summary>
    bool IsValidEntity { get; }

    /// <summary>
    ///     CollisionProperty
    /// </summary>
    ICollisionProperty? GetCollisionProperty();

    /// <summary>
    ///     速度矢量 <br />
    ///     <remarks>如果存在MoveParent, 这个速度是基于Parent的</remarks>
    /// </summary>
    [Obsolete("Use GetLocalVelocity/GetLocalVelocity")]
    Vector Velocity { get; set; }

    /// <summary>
    ///     绝对的速度矢量 <br />
    ///     <remarks>该矢量不会计算MoveParent</remarks>
    /// </summary>
    [Obsolete("Use GetAbsVelocity/GetAbsVelocity")]
    Vector AbsVelocity { get; set; }

    /// <summary>
    ///     基速度矢量 <br />
    ///     <remarks>该速度为Base, 速度为基于此值计算</remarks>
    /// </summary>
    Vector BaseVelocity { get; set; }

    /// <summary>
    ///     m_nNextThinkTick
    /// </summary>
    int NextThinkTick { get; set; }

    /// <summary>
    ///     m_flCreateTime
    /// </summary>
    float CreateTime { get; }

    /// <summary>
    ///     m_flGravityScale
    /// </summary>
    float GravityScale { get; set; }

    /// <summary>
    ///     m_iszPrivateVScripts
    /// </summary>
    string PrivateVScripts { get; }

    /// <summary>
    ///     m_nSubclassID
    /// </summary>
    uint SubclassID { get; }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    ///     检查实体是否有效
    /// </summary>
    bool IsValid();

    /// <summary>
    ///     判断实体是否被标记为Deletion
    /// </summary>
    /// <returns></returns>
    bool IsMarkedForDeletion();

    /// <summary>
    ///     移除实体并标记为Deletion
    /// </summary>
    void Kill();

    /// <summary>
    ///     覆盖Classname<br />
    ///     <remarks>该操作会影响回合实体结算</remarks>
    /// </summary>
    void SetClassname(string classname);

    /// <summary>
    ///     更改队伍
    /// </summary>
    /// <param name="team">队伍</param>
    void ChangeTeam(CStrikeTeam team);

    /// <summary>
    ///     Input实体IO
    /// </summary>
    /// <param name="input">input名</param>
    /// <param name="activator">触发者</param>
    /// <param name="caller">调用者</param>
    /// <param name="value">Variant值(可为空)</param>
    /// <param name="outputId">不知道干什么的</param>
    bool AcceptInput(string input,
        IBaseEntity?        activator = null,
        IBaseEntity?        caller    = null,
        string?             value     = null,
        int                 outputId  = 0);

    /// <summary>
    ///     Input实体IO
    /// </summary>
    /// <param name="input">input名</param>
    /// <param name="activator">触发者</param>
    /// <param name="caller">调用者</param>
    /// <param name="value">Variant值</param>
    /// <param name="outputId">不知道干什么的</param>
    bool AcceptInput(string input,
        IBaseEntity?        activator,
        IBaseEntity?        caller,
        int                 value,
        int                 outputId = 0);

    /// <summary>
    ///     Input实体IO
    /// </summary>
    /// <param name="input">input名</param>
    /// <param name="activator">触发者</param>
    /// <param name="caller">调用者</param>
    /// <param name="value">Variant值</param>
    /// <param name="outputId">不知道干什么的</param>
    bool AcceptInput(string input,
        IBaseEntity?        activator,
        IBaseEntity?        caller,
        float               value,
        int                 outputId = 0);

    /// <summary>
    ///     添加实体IO事件
    /// </summary>
    /// <param name="delay">延迟</param>
    /// <param name="input">input名</param>
    /// <param name="activator">触发者</param>
    /// <param name="caller">调用者</param>
    /// <param name="value">Variant值(可为空)</param>
    /// <param name="outputId">不知道干什么的</param>
    void AddIOEvent(float delay,
        string            input,
        IBaseEntity?      activator = null,
        IBaseEntity?      caller    = null,
        string?           value     = null,
        int               outputId  = 0);

    /// <summary>
    ///     添加实体IO事件
    /// </summary>
    /// <param name="delay">延迟</param>
    /// <param name="input">input名</param>
    /// <param name="activator">触发者</param>
    /// <param name="caller">调用者</param>
    /// <param name="value">Variant值</param>
    /// <param name="outputId">不知道干什么的</param>
    void AddIOEvent(float delay,
        string            input,
        IBaseEntity?      activator,
        IBaseEntity?      caller,
        int               value,
        int               outputId = 0);

    /// <summary>
    ///     添加实体IO事件
    /// </summary>
    /// <param name="delay">延迟</param>
    /// <param name="input">input名</param>
    /// <param name="activator">触发者</param>
    /// <param name="caller">调用者</param>
    /// <param name="value">Variant值</param>
    /// <param name="outputId">不知道干什么的</param>
    void AddIOEvent(float delay,
        string            input,
        IBaseEntity?      activator,
        IBaseEntity?      caller,
        float             value,
        int               outputId = 0);

    /// <summary>
    ///     TakeDamage
    /// </summary>
    /// <returns>造成的伤害数值</returns>
    long DispatchTraceAttack(in TakeDamageInfo info, bool bypassHook = false);

    /// <summary>
    ///     TakeDamage
    /// </summary>
    /// <returns>造成的伤害数值</returns>
    unsafe long DispatchTraceAttack(TakeDamageInfo* info, bool bypassHook = false);

    /// <summary>
    ///     设置Targetname
    /// </summary>
    void SetName(string name);

    /// <summary>
    ///     设置模型, 仅生效于ModelEntity
    /// </summary>
    /// <param name="model">模型路径, (vmdl结尾, 不含_c)</param>
    void SetModel(string model);

    /// <summary>
    ///     设置父级
    /// </summary>
    /// <param name="owner">父级实体</param>
    void SetOwner(IBaseEntity? owner);

    /// <summary>
    ///     设置GroundEntity
    /// </summary>
    void SetGroundEntity(IBaseEntity? ground, IBaseEntity? unknown);

    /// <summary>
    ///     设置实体Solid, 仅生效于ModelEntity
    /// </summary>
    /// <param name="solid"></param>
    void SetSolid(SolidType solid);

    /// <summary>
    ///     祖传Teleport
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="angles">角度</param>
    /// <param name="velocity">速度</param>
    void Teleport(Vector? position = null, Vector? angles = null, Vector? velocity = null);

    /// <summary>
    ///     设置移动模式
    /// </summary>
    void SetMoveType(MoveType type);

    /// <summary>
    ///     设置GravityScale
    /// </summary>
    void SetGravityScale(float scale);

    /// <summary>
    ///     附加一个绝对的速度
    /// </summary>
    void ApplyAbsVelocityImpulse(Vector velocity);

    /// <summary>
    ///     绝对世界速度, EFL_DIRTY_ABSVELOCITY
    /// </summary>
    /// <returns></returns>
    Vector GetAbsVelocity();

    /// <summary>
    ///     绝对世界速度, EFL_DIRTY_ABSVELOCITY
    /// </summary>
    void SetAbsVelocity(Vector velocity);

    /// <summary>
    ///     相对世界速度, 联网
    /// </summary>
    /// <returns></returns>
    Vector GetLocalVelocity();

    /// <summary>
    ///     相对世界速度, 联网
    /// </summary>
    void SetLocalVelocity(Vector velocity);

    /// <summary>
    ///     绝对世界角度
    /// </summary>
    Vector GetAbsAngles();

    /// <summary>
    ///     绝对世界角度, EFL_DIRTY_ABSTRANSFORM
    /// </summary>
    void SetAbsAngles(Vector angles);

    /// <summary>
    ///     绝对世界位置
    /// </summary>
    Vector GetAbsOrigin();

    /// <summary>
    ///     绝对世界位置, EFL_DIRTY_ABSTRANSFORM
    /// </summary>
    void SetAbsOrigin(Vector origin);

    /// <summary>
    ///     实体中心坐标 (世界绝对值)
    /// </summary>
    Vector GetCenter();

    /// <summary>
    ///     更新Collision
    /// </summary>
    void CollisionRulesChanged();

    /// <summary>
    ///     在这个实体上播放音频
    /// </summary>
    /// <param name="sound">音频</param>
    /// <param name="volume">音量</param>
    /// <param name="filter">接受者</param>
    SoundOpEventGuid EmitSound(string sound, float? volume = null, RecipientFilter filter = default);

    /// <summary>
    ///     在这个实体上停止音频
    /// </summary>
    void StopSound(string sound);

    /// <summary>
    ///     设置CollisionGroup
    /// </summary>
    void SetCollisionGroup(CollisionGroupType type);

    /// <summary>
    ///     转换为<see cref="IBasePlayerPawn" /> <br />
    ///     <remarks>
    ///         如果实体不是CBasePlayerPawn直接返回空
    ///     </remarks>
    /// </summary>
    /// <param name="safeCheck">False时直接创建实例, 不进行检查</param>
    IBasePlayerPawn? AsBasePlayerPawn(bool safeCheck = true);

    /// <summary>
    ///     转换为<see cref="IPlayerPawn" /> <br />
    ///     <remarks>
    ///         如果实体不是CCSPlayerPawn直接返回空 <br />
    ///         如果你要转换为<see cref="IBasePlayerPawn" />请查看<seealso cref="AsBasePlayerPawn" />
    ///     </remarks>
    /// </summary>
    /// <param name="safeCheck">False时直接创建实例, 不进行检查</param>
    IPlayerPawn? AsPlayerPawn(bool safeCheck = true);

    /// <summary>
    ///     转换为PlayerController <br />
    ///     <remarks>如果实体不是PlayerController则返回空</remarks>
    /// </summary>
    /// <param name="safeCheck">False时直接创建实例, 不进行检查</param>
    IPlayerController? AsPlayerController(bool safeCheck = true);

    /// <summary>
    ///     转换为BaseWeapon <br />
    ///     <remarks>如果实体不是Weapon则返回空</remarks>
    /// </summary>
    /// <param name="safeCheck">False时直接创建实例, 不进行检查</param>
    IBaseWeapon? AsBaseWeapon(bool safeCheck = true);

    /// <summary>
    ///     判断BaseWeapon
    /// </summary>
    bool IsWeapon { get; }

    /// <summary>
    ///     判断CCSPlayerController
    /// </summary>
    bool IsPlayerController { get; }

    /// <summary>
    ///     判断CCSPlayerPawn
    /// </summary>
    bool IsPlayerPawn { get; }

    /// <summary>
    ///     转换为BaseGrenade <br />
    ///     <remarks>如果实体不是Grenade则返回空</remarks>
    /// </summary>
    IBaseGrenadeProjectile? AsBaseGrenadeProjectile();

    /// <summary>
    ///     转换实体
    /// </summary>
    T As<T>() where T : class, IBaseEntity;

    /// <summary>
    ///     将实体生成到游戏世界中, 并加载其所有与之相关的数据
    ///     <remarks>
    ///         实体的kv中的key必须要小写, 否则会不认
    ///     </remarks>
    /// </summary>
    void DispatchSpawn(IReadOnlyDictionary<string, KeyValuesVariantValueItem>? keyValues = null);

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    ///     m_CBodyComponent
    /// </summary>
    IBodyComponent GetBodyComponent();

    /// <summary>
    ///     m_ResponseContexts
    /// </summary>
    ISchemaList<ResponseContext> GetResponseContexts();
}
