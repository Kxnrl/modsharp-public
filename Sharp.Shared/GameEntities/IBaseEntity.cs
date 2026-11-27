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
    ///     Gets the handle cached when this managed entity wrapper was constructed (does not re-read live entity identity).
    /// </summary>
    CEntityHandle<IBaseEntity> Handle { get; }

    /// <summary>
    ///     Gets the true, usable handle for this entity.
    ///     <para>
    ///         Unlike <see cref="Handle" />, this ensures the Serial Number in entity handle matches the global list,
    ///         allowing you to find the entity even if it is currently flagged as INVALID_ENTITY_HANDLE.
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
    ///     Return the index of this entity.
    /// </summary>
    /// <remarks>
    ///     Cannot guarantee it is valid. Please call <see cref="IsValid" />, or ensure this entity is valid in the current context,
    ///     before using this field.
    /// </remarks>
    EntityIndex Index { get; }

    /// <summary>
    ///     Get the entity's classname.
    /// </summary>
    /// <remarks>
    ///     Make sure you call <see cref="IsValid" />, or ensure this entity is valid in the current context, before using this field,
    ///     otherwise it will crash the server.
    /// </remarks>
    string Classname { get; }

    /// <summary>
    ///     Get the designer name of this entity's entity class.
    /// </summary>
    /// <remarks>
    ///     Not affected by <see cref="SetClassname" />; an entity created through an alias returns the designer name
    ///     of the target class. Make sure this entity is valid before calling it, otherwise it will crash the server.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The entity class has no designer name</exception>
    string GetDesignerName();

    /// <summary>
    ///     Get the targetname of this entity.
    /// </summary>
    /// <remarks>
    ///     Make sure you call <see cref="IsValid" />, or ensure this entity is valid in the current context, before using this field,
    ///     otherwise it will crash the server.
    /// </remarks>
    string Name { get; }

    /// <summary>
    ///     Get the HammerId of this entity
    /// </summary>
    string HammerId { get; }

    /// <summary>
    ///     m_iGlobalname
    /// </summary>
    string GlobalName { get; set; }

    /// <summary>
    ///     Is this entity allowed to take damage.
    /// </summary>
    /// <remarks>
    ///     Make sure you call <see cref="IsValid" />, or ensure this entity is valid in the current context, before using this field,
    ///     otherwise it will crash the server.
    /// </remarks>
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
    ///     Get the damage filter entity handle applied to this entity
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
    ///     Velocity vector.
    /// </summary>
    /// <remarks>
    ///     If there is a MoveParent, this velocity is relative to the parent; when MoveType is
    ///     <see cref="Enums.MoveType.VPhysics" />, it reads and writes the physics object's velocity.
    /// </remarks>
    Vector Velocity { get; set; }

    /// <summary>
    ///     Absolute velocity vector.
    /// </summary>
    /// <remarks>Does not take MoveParent into account.</remarks>
    [Obsolete("Use GetAbsVelocity/SetAbsVelocity, will be removed in 2.2", true)]
    Vector AbsVelocity { get; set; }

    /// <summary>
    ///     Base velocity.
    /// </summary>
    /// <remarks>Setting this will apply extra velocity to the final speed.</remarks>
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
    ///     Check if this entity is marked for deletion
    /// </summary>
    bool IsMarkedForDeletion();

    /// <summary>
    ///     Mark this entity for deletion and delete
    /// </summary>
    void Kill();

    /// <summary>
    ///     Update Classname.
    /// </summary>
    /// <remarks>Doing this will impact entity clean up on round end.</remarks>
    void SetClassname(string classname);

    /// <summary>
    ///     Change entity's team. On an <see cref="IPlayerController" />, calls <c>ForceChangeTeam</c> (which slays the player; use <see cref="IPlayerController.SwitchTeam" /> to switch without slaying).
    /// </summary>
    /// <param name="team">Target team</param>
    void ChangeTeam(CStrikeTeam team);

    /// <summary>
    ///     Fires an entity input.
    /// </summary>
    /// <param name="input">Input name, for example SetAngles</param>
    /// <param name="activator">The entity that activates this IO Event</param>
    /// <param name="caller">The entity that calls this IO Event</param>
    /// <param name="value">The string value (can be null).</param>
    /// <param name="outputId">Reserved output ID (currently ignored by the native implementation).</param>
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
    /// <param name="outputId">Reserved output ID (currently ignored by the native implementation).</param>
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
    /// <param name="outputId">Reserved output ID (currently ignored by the native implementation).</param>
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
    /// <param name="outputId">Reserved output ID (currently ignored by the native implementation).</param>
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
    /// <param name="outputId">Reserved output ID (currently ignored by the native implementation).</param>
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
    /// <param name="outputId">Reserved output ID (currently ignored by the native implementation).</param>
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
    ///         Calling this triggers <see cref="Sharp.Shared.Managers.IHookManager.PlayerDispatchTraceAttack" /> (for pawns)
    ///         or
    ///         <see cref="Sharp.Shared.Managers.IHookManager.EntityDispatchTraceAttack" /> (for others), unless
    ///         <paramref name="bypassHook" /> is <see langword="true" />.
    ///     </para>
    /// </remarks>
    /// <param name="info">The damage information to apply.</param>
    /// <param name="bypassHook">If <see langword="true" />, the damage is applied directly without being intercepted by hooks.</param>
    /// <returns>The health lost (<c>CTakeDamageResult.m_nHealthLost</c>), or <c>0</c> if damage was suppressed or skipped.</returns>
    long DispatchTraceAttack(in TakeDamageInfo info, bool bypassHook = false);

    /// <summary>
    ///     Applies damage to this entity using trace attack logic (Unsafe).
    /// </summary>
    /// <remarks>
    ///     This is the programmatic way to inflict damage using a raw pointer.
    ///     <para>
    ///         Calling this triggers <see cref="Sharp.Shared.Managers.IHookManager.PlayerDispatchTraceAttack" /> (for pawns)
    ///         or
    ///         <see cref="Sharp.Shared.Managers.IHookManager.EntityDispatchTraceAttack" /> (for others), unless
    ///         <paramref name="bypassHook" /> is <see langword="true" />.
    ///     </para>
    /// </remarks>
    /// <param name="info">A pointer to the damage information struct. Must not be null.</param>
    /// <param name="bypassHook">If <see langword="true" />, the damage is applied directly without being intercepted by hooks.</param>
    /// <returns>The health lost (<c>CTakeDamageResult.m_nHealthLost</c>), or <c>0</c> if damage was suppressed or skipped.</returns>
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
    ///     Get the absolute velocity in world space, EFL_DIRTY_ABSVELOCITY
    /// </summary>
    Vector GetAbsVelocity();

    /// <summary>
    ///     Set the absolute velocity in world space, EFL_DIRTY_ABSVELOCITY
    /// </summary>
    void SetAbsVelocity(Vector velocity);

    /// <summary>
    ///     Get the local velocity (relative to MoveParent if parented, otherwise world space), networked
    /// </summary>
    /// <remarks>When MoveType is <see cref="Enums.MoveType.VPhysics" />, reads the physics object's velocity.</remarks>
    Vector GetLocalVelocity();

    /// <summary>
    ///     Set the local velocity (relative to MoveParent if parented, otherwise world space), networked
    /// </summary>
    /// <remarks>When MoveType is <see cref="Enums.MoveType.VPhysics" />, writes the physics object's velocity.</remarks>
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
    ///     Set an entity's collision group
    /// </summary>
    void SetCollisionGroup(CollisionGroupType type);

    /// <summary>
    ///     Cast to <see cref="IBasePlayerPawn" />.
    /// </summary>
    /// <remarks>Returns <see langword="null" /> if the entity is not a <c>CBasePlayerPawn</c>.</remarks>
    /// <param name="safeCheck">When false, it creates instance without checks</param>
    IBasePlayerPawn? AsBasePlayerPawn(bool safeCheck = true);

    /// <summary>
    ///     Cast to <see cref="IPlayerPawn" />.
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="null" /> if the entity is not a <c>CCSPlayerPawn</c>.
    ///     To cast to <see cref="IBasePlayerPawn" />, see <seealso cref="AsBasePlayerPawn" />.
    /// </remarks>
    /// <param name="safeCheck">When false, it creates instance without checks</param>
    IPlayerPawn? AsPlayerPawn(bool safeCheck = true);

    /// <summary>
    ///     Cast to PlayerController.
    /// </summary>
    /// <remarks>Returns <see langword="null" /> if the entity is not a PlayerController.</remarks>
    /// <param name="safeCheck">When false, it creates instance without checks</param>
    IPlayerController? AsPlayerController(bool safeCheck = true);

    /// <summary>
    ///     Cast to BaseWeapon.
    /// </summary>
    /// <remarks>Returns <see langword="null" /> if the entity is not a Weapon.</remarks>
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
    ///     Cast to BaseGrenade.
    /// </summary>
    /// <remarks>Returns <see langword="null" /> if the entity is not a grenade.</remarks>
    /// <exception cref="System.Reflection.CustomAttributeFormatException">The entity has no schema binding</exception>
    /// <exception cref="KeyNotFoundException">The entity's schema class is not in the schema table</exception>
    IBaseGrenadeProjectile? AsBaseGrenadeProjectile();

    /// <summary>
    ///     Cast entity to other types
    /// </summary>
    T As<T>() where T : class, IBaseEntity;

    /// <summary>
    ///     Spawn entity with entity field values.
    /// </summary>
    /// <remarks>The keys must be in lowercase, otherwise the game will not recognize them.</remarks>
    /// <exception cref="ArgumentException"><paramref name="keyValues" /> contains more than 128 entries.</exception>
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
