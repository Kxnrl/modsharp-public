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
    ///     Gets the raw handle stored on the entity
    /// </summary>
    CEntityHandle<IBaseEntity> Handle { get; }

    /// <summary>
    ///     Gets the true, usable handle for this entity.
    ///     <para>
    ///     Unlike <see cref="Handle"/>, this ensures the Serial Number in entity handle matches the global list, 
    ///     allowing you to find the entity even if it is currently flagged as INVALID_ENTITY_HANDLE.
    ///     </para>
    /// </summary>
    CEntityHandle<IBaseEntity> RefHandle { get; }

    /// <summary>
    ///     Health
    /// </summary>
    int Health { get; set; }

    /// <summary>
    ///     Max health
    /// </summary>
    int MaxHealth { get; set; }

    /// <summary>
    ///     Is this entity alive
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    ///     Lifestate
    /// </summary>
    LifeState LifeState { get; }

    /// <summary>
    ///     Team
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
    ///     Return the index of this entity <br />
    ///     <remarks>
    ///         Cannot guarantee it is valid. Please call <see cref="IsValid" />, or this entity is valid in current context, before using this field.
    ///     </remarks>
    /// </summary>
    EntityIndex Index { get; }

    /// <summary>
    ///     读取Classname<br />
    ///         Make sure you call <see cref="IsValid" />, or this entity is valid in current context, before using this field, otherwise it will crash the server
    /// </summary>
    string Classname { get; }

    /// <summary>
    ///     Get the targetname of this entity<br />
    ///     <remarks>
    ///         Make sure you call <see cref="IsValid" />, or this entity is valid in current context, before using this field, otherwise it will crash the server
    ///     </remarks>
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Get the HammerId of this entity<br />
    /// </summary>
    string HammerId { get; }

    /// <summary>
    ///     m_iGlobalname
    /// </summary>
    string GlobalName { get; set; }

    /// <summary>
    ///     Is this entity allowed to take damage <br />
    ///     <remarks>
    ///         Make sure you call <see cref="IsValid" />, or this entity is valid in current context, before using this field, otherwise it will crash the server
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
    ///      Get the damage filter entity handle applied to this entity
    /// </summary>
    CEntityHandle<IBaseFilter> DamageFilterEntityHandle { get; set; }

    /// <summary>
    ///     Get the damage filter entity applied to this entity
    /// </summary>
    IBaseFilter? DamageFilterEntity { get; }

    /// <summary>
    ///     Check if this entity is valid
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
    [Obsolete("Use GetLocalVelocity/GetLocalVelocity, will be removed in 2.2")]
    Vector Velocity { get; set; }

    /// <summary>
    ///     绝对的速度矢量 <br />
    ///     <remarks>该矢量不会计算MoveParent</remarks>
    /// </summary>
    [Obsolete("Use GetAbsVelocity/GetAbsVelocity, will be removed in 2.2")]
    Vector AbsVelocity { get; set; }

    /// <summary>
    ///     Base velocity <br />
    ///     <remarks>Setting this will apply extra velocity to the final speed</remarks>
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
    ///     Check if this entity is valid
    /// </summary>
    bool IsValid();

    /// <summary>
    ///     Check if this entity is maked for deletion
    /// </summary>
    /// <returns></returns>
    bool IsMarkedForDeletion();

    /// <summary>
    ///     Mark this entity for deletion and delete
    /// </summary>
    void Kill();

    /// <summary>
    ///     Update Classname<br />
    ///     <remarks>Doing this will impact entity clean up on round end</remarks>
    /// </summary>
    void SetClassname(string classname);

    /// <summary>
    ///     Change entity's team
    /// </summary>
    /// <param name="team">队伍</param>
    void ChangeTeam(CStrikeTeam team);

    /// <summary>
    ///     Fires an entity input.
    /// </summary>
    /// <param name="input">Input name, for example SetAngles</param>
    /// <param name="activator">The entity that activates this IO Event</param>
    /// <param name="caller">The entity that calls this IO Event</param>
    /// <param name="value">The string value (can be null).</param>
    /// <param name="outputId">The unique ID of the output that was fired</param>
    bool AcceptInput(string input,
        IBaseEntity?        activator = null,
        IBaseEntity?        caller    = null,
        string?             value     = null,
        int                 outputId  = 0);

    /// <summary>
    ///     Fires an entity input.
    /// </summary>
    /// <param name="input">Input name, for example SetAngles</param>
    /// <param name="activator">The entity that activates this IO Event</param>
    /// <param name="caller">The entity that calls this IO Event</param>
    /// <param name="value">The integer parameter value</param>
    /// <param name="outputId">The unique ID of the output that was fired</param>
    bool AcceptInput(string input,
        IBaseEntity?        activator,
        IBaseEntity?        caller,
        int                 value,
        int                 outputId = 0);

    /// <summary>
    ///     Fires an entity input.
    /// </summary>
    /// <param name="input">Input name, for example SetAngles</param>
    /// <param name="activator">The entity that activates this IO Event</param>
    /// <param name="caller">The entity that calls this IO Event</param>
    /// <param name="value">The float parameter value</param>
    /// <param name="outputId">The unique ID of the output that was fired</param>
    bool AcceptInput(string input,
        IBaseEntity?        activator,
        IBaseEntity?        caller,
        float               value,
        int                 outputId = 0);

    /// <summary>
    ///     Add an IO event to this entity
    /// </summary>
    /// <param name="delay">Delay</param>
    /// <param name="input">Input name</param>
    /// <param name="activator">The entity that activates this IO Event</param>
    /// <param name="caller">The entity that calls this IO Event</param>
    /// <param name="value">Value</param>
    /// <param name="outputId">The unique ID of the output that was fired</param>
    void AddIOEvent(float delay,
        string            input,
        IBaseEntity?      activator = null,
        IBaseEntity?      caller    = null,
        string?           value     = null,
        int               outputId  = 0);

    /// <summary>
    ///     Add an IO event to this entity
    /// </summary>
    /// <param name="delay">Delay</param>
    /// <param name="input">Input name</param>
    /// <param name="activator">The entity that activates this IO Event</param>
    /// <param name="caller">The entity that calls this IO Event</param>
    /// <param name="value">Value</param>
    /// <param name="outputId">The unique ID of the output that was fired</param>
    void AddIOEvent(float delay,
        string            input,
        IBaseEntity?      activator,
        IBaseEntity?      caller,
        int               value,
        int               outputId = 0);

    /// <summary>
    ///     Add an IO event to this entity
    /// </summary>
    /// <param name="delay">Delay</param>
    /// <param name="input">Input name</param>
    /// <param name="activator">The entity that activates this IO Event</param>
    /// <param name="caller">The entity that calls this IO Event</param>
    /// <param name="value">Value</param>
    /// <param name="outputId">The unique ID of the output that was fired</param>
    void AddIOEvent(float delay,
        string            input,
        IBaseEntity?      activator,
        IBaseEntity?      caller,
        float             value,
        int               outputId = 0);

    /// <summary>
    ///     Applies damage to this entity using trace attack logic.
    /// </summary>
    /// <remarks>
    ///     This is the programmatic way to inflict damage.
    ///     <para>
    ///     Calling this triggers <see cref="IHookManager.PlayerDispatchTraceAttack"/> (for pawns) or 
    ///     <see cref="IHookManager.EntityDispatchTraceAttack"/> (for others), unless <paramref name="bypassHook"/> is <see langword="true"/>.
    ///     </para>
    /// </remarks>
    /// <param name="info">The damage information to apply.</param>
    /// <param name="bypassHook">If <see langword="true"/>, the damage is applied directly without being intercepted by hooks.</param>
    /// <returns>The result returned by the engine (or the hook), typically representing the damage applied.</returns>
    long DispatchTraceAttack(in TakeDamageInfo info, bool bypassHook = false);

    /// <summary>
    ///     Applies damage to this entity using trace attack logic (Unsafe).
    /// </summary>
    /// <remarks>
    ///     This is the programmatic way to inflict damage using a raw pointer.
    ///     <para>
    ///     Calling this triggers <see cref="IHookManager.PlayerDispatchTraceAttack"/> (for pawns) or 
    ///     <see cref="IHookManager.EntityDispatchTraceAttack"/> (for others), unless <paramref name="bypassHook"/> is <see langword="true"/>.
    ///     </para>
    /// </remarks>
    /// <param name="info">A pointer to the damage information struct. Must not be null.</param>
    /// <param name="bypassHook">If <see langword="true"/>, the damage is applied directly without being intercepted by hooks.</param>
    /// <returns>The result returned by the engine (or the hook), typically representing the damage applied.</returns>
    unsafe long DispatchTraceAttack(TakeDamageInfo* info, bool bypassHook = false);

    /// <summary>
    ///     Set entity targetname
    /// </summary>
    void SetName(string name);

    /// <summary>
    ///     Set entity model, Only works on ModelEntity
    /// </summary>
    /// <param name="model">Model path, it should end with .vmdl without '_c'</param>
    void SetModel(string model);

    /// <summary>
    ///     Set entity owner
    /// </summary>
    /// <param name="owner">Owner entity</param>
    void SetOwner(IBaseEntity? owner);

    /// <summary>
    ///     Set GroundEntity
    /// </summary>
    void SetGroundEntity(IBaseEntity? ground, IBaseEntity? unknown);

    /// <summary>
    ///     Set solid type, only works on ModelEntity
    /// </summary>
    /// <param name="solid"></param>
    void SetSolid(SolidType solid);

    /// <summary>
    ///     Teleport
    /// </summary>
    /// <param name="position">Position. When null the game will ignore it</param>
    /// <param name="angles">Angles. When null the game will ignore it</param>
    /// <param name="velocity">Velocity. When null the game will ignore it</param>
    void Teleport(Vector? position = null, Vector? angles = null, Vector? velocity = null);

    /// <summary>
    ///     Set movetype
    /// </summary>
    void SetMoveType(MoveType type);

    /// <summary>
    ///     Set GravityScale
    /// </summary>
    void SetGravityScale(float scale);

    /// <summary>
    ///     Apply an absolute velocity to this entity. CurrentAbsVelocity + velocity = NewVelocity
    /// </summary>
    void ApplyAbsVelocityImpulse(Vector velocity);

    /// <summary>
    ///     SGet the absolute velocity in world space, EFL_DIRTY_ABSVELOCITY
    /// </summary>
    /// <returns></returns>
    Vector GetAbsVelocity();

    /// <summary>
    ///      Set the absolute velocity in world space, EFL_DIRTY_ABSVELOCITY
    /// </summary>
    void SetAbsVelocity(Vector velocity);

    /// <summary>
    ///     Get the relative velocity in world space, networked
    /// </summary>
    /// <returns></returns>
    Vector GetLocalVelocity();

    /// <summary>
    ///     Set the relative velocity in world space, networked
    /// </summary>
    void SetLocalVelocity(Vector velocity);

    /// <summary>
    ///     Get the absolute angle in world space
    /// </summary>
    Vector GetAbsAngles();

    /// <summary>
    ///     Set the absolute angle in world space, EFL_DIRTY_ABSTRANSFORM
    /// </summary>
    void SetAbsAngles(Vector angles);

    /// <summary>
    ///     Get the entity absolute origin in world space
    /// </summary>
    Vector GetAbsOrigin();

    /// <summary>
    ///     Set the entity absolute origin in world space. EFL_DIRTY_ABSTRANSFORM
    /// </summary>
    void SetAbsOrigin(Vector origin);

    /// <summary>
    ///     Get the center position of this entity in world space
    /// </summary>
    Vector GetCenter();

    /// <summary>
    ///     Update collision rules
    /// </summary>
    void CollisionRulesChanged();

    /// <summary>
    ///     Play a sound on this entity
    /// </summary>
    /// <param name="sound">Sound event name. for example: Player.BurnDamage</param>
    /// <param name="volume">Volume</param>
    /// <param name="filter">Receiver</param>
    SoundOpEventGuid EmitSound(string sound, float? volume = null, RecipientFilter filter = default);

    /// <summary>
    ///     Stop the sound from playing on this entity
    /// </summary>
    void StopSound(string sound);

    /// <summary>
    ///     Set an entitiy's collision group
    /// </summary>
    void SetCollisionGroup(CollisionGroupType type);

    /// <summary>
    ///     Cast to <see cref="IBasePlayerPawn" /> <br />
    ///     <remarks>
    ///         It will return null if the entity is not CBasePlayerPawn
    ///     </remarks>
    /// </summary>
    /// <param name="safeCheck">When false, it creates instance without checks</param>
    IBasePlayerPawn? AsBasePlayerPawn(bool safeCheck = true);

    /// <summary>
    ///     Cast to <see cref="IPlayerPawn" /> <br />
    ///     <remarks>
    ///         It will return null if the entity is not CCSPlayerPawn <br />
    ///         If you want to cast to <see cref="IBasePlayerPawn" />, please see <seealso cref="AsBasePlayerPawn" />
    ///     </remarks>
    /// </summary>
    /// <param name="safeCheck">When false, it creates instance without checks</param>
    IPlayerPawn? AsPlayerPawn(bool safeCheck = true);

    /// <summary>
    ///     Cast to PlayerController <br />
    ///     <remarks>It will return null if the entity is not PlayerController</remarks>
    /// </summary>
    /// <param name="safeCheck">When false, it creates instance without checks</param>
    IPlayerController? AsPlayerController(bool safeCheck = true);

    /// <summary>
    ///     Cast to BaseWeapon <br />
    ///     <remarks>It will return null if the entity is not Weapon</remarks>
    /// </summary>
    /// <param name="safeCheck">When false, it creates instance without checks</param>
    IBaseWeapon? AsBaseWeapon(bool safeCheck = true);

    /// <summary>
    ///     Check if the entity is BaseWeapon
    /// </summary>
    bool IsWeapon { get; }

    /// <summary>
    ///     Check if the entity is CCSPlayerController
    /// </summary>
    bool IsPlayerController { get; }

    /// <summary>
    ///     Check if the entity is CBasePlayerPawn
    /// </summary>
    bool IsPlayerPawn { get; }

    /// <summary>
    ///     Cast to BaseGrenade <br />
    ///     <remarks>It will return null if the entity is not a grenade</remarks>
    /// </summary>
    IBaseGrenadeProjectile? AsBaseGrenadeProjectile();

    /// <summary>
    ///     Cast entity to other types
    /// </summary>
    T As<T>() where T : class, IBaseEntity;

    /// <summary>
    ///     Spawn entity with entity field values
    ///     <remarks>
    ///         They keys must be in lowercase, otherwise the game will not recognize
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
