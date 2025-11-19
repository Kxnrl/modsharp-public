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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Core.GameObjects;
using Sharp.Core.Helpers;
using Sharp.Core.Utilities;
using Sharp.Core.VirtualCalls;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;
using Sharp.Shared.Utilities;

namespace Sharp.Core.GameEntities;

internal partial class BaseEntity : SchemaObject, IBaseEntity, INativeCreatable<BaseEntity>
{
    private const uint EntityFlagsInvalidEntityHandle = 0x001;
    private const uint EntityFlagsKillMe              = 0x200;

    protected BaseEntity(nint ptr) : base(ptr)
        => Handle = GetHandle(_this);

    public CEntityHandle<IBaseEntity> Handle { get; }

    public unsafe CEntityHandle<IBaseEntity> RefHandle
    {
        get
        {
            var identity = GetIdentity(_this);
            var handle   = identity->Handle.GetValue();
            var flags    = identity->Flags;

            var lo     = handle != 0xFFFFFFFF ? (int) (handle & 0x7FFF) : 0x7FFF;
            var hi     = ((int) (handle >> 15) - (int) (flags & EntityFlagsInvalidEntityHandle)) << 15;
            var result = lo | hi;
            var val    = *(uint*) &result;

            return new CEntityHandle<IBaseEntity>(val);
        }
    }

    public bool IsAlive => LifeState == LifeState.Alive;

    public EntityIndex Index => Handle.GetEntryIndex();

    public unsafe string Classname => GetIdentity(_this)->Classname.Get();

    public unsafe string Name => GetIdentity(_this)->Name.Get();

    public IBaseEntity? OwnerEntity
    {
        get => Create(OwnerEntityHandle.GetEntityPtr());
        set => Entity.SetOwner(_this, value?.GetAbsPtr() ?? nint.Zero);
    }

    public IBaseEntity? GroundEntity => Create(GroundEntityHandle.GetEntityPtr());

    public IBaseEntity? EffectEntity => Create(EffectEntityHandle.GetEntityPtr());

    public IBaseFilter? DamageFilterEntity => BaseFilter.Create(DamageFilterEntityHandle.GetEntityPtr());

    public bool IsValidEntity => Handle.IsValid() && IsValid();

    public string PrivateVScripts => SchemaSystem.GetNetVarUtlSymbolLarge(_this, "CBaseEntity", "m_iszPrivateVScripts");

    ///////////////////////////////////////////////////////////////////////////////////

    public bool IsValid()
        => !IsDisposed && Entity.IsValid(Handle.GetValue());

    public bool IsMarkedForDeletion()
        => IsMarkedForDelete(_this);

    public void Kill()
        => Entity.Kill(_this);

    public unsafe void SetClassname(string name)
        => GetIdentity(_this)->Classname = new CUtlSymbolLarge(Entity.AllocPooledString(name));

    public void ChangeTeam(CStrikeTeam team)
        => Entity.ChangeTeam(_this, team);

    public bool AcceptInput(string input,
        IBaseEntity?               activator = null,
        IBaseEntity?               caller    = null,
        string?                    value     = null,
        int                        outputId  = 0)
        => Entity.AcceptInput(_this,
                              input,
                              activator?.GetAbsPtr() ?? nint.Zero,
                              caller?.GetAbsPtr()    ?? nint.Zero,
                              value,
                              outputId);

    public bool AcceptInput(string input,
        IBaseEntity?               activator,
        IBaseEntity?               caller,
        int                        value,
        int                        outputId = 0)
        => Entity.AcceptInputInt(_this,
                                 input,
                                 activator?.GetAbsPtr() ?? nint.Zero,
                                 caller?.GetAbsPtr()    ?? nint.Zero,
                                 value,
                                 outputId);

    public bool AcceptInput(string input,
        IBaseEntity?               activator,
        IBaseEntity?               caller,
        float                      value,
        int                        outputId = 0)
        => Entity.AcceptInputFloat(_this,
                                   input,
                                   activator?.GetAbsPtr() ?? nint.Zero,
                                   caller?.GetAbsPtr()    ?? nint.Zero,
                                   value,
                                   outputId);

    public void AddIOEvent(float delay,
        string                   input,
        IBaseEntity?             activator = null,
        IBaseEntity?             caller    = null,
        string?                  value     = null,
        int                      outputId  = 0)
        => Entity.AddIOEvent(_this,
                             delay,
                             input,
                             activator?.GetAbsPtr() ?? nint.Zero,
                             caller?.GetAbsPtr()    ?? nint.Zero,
                             value,
                             outputId);

    public void AddIOEvent(float delay, string input, IBaseEntity? activator, IBaseEntity? caller, int value, int outputId = 0)
        => Entity.AddIOEventInt(_this,
                                delay,
                                input,
                                activator?.GetAbsPtr() ?? nint.Zero,
                                caller?.GetAbsPtr()    ?? nint.Zero,
                                value,
                                outputId);

    public void AddIOEvent(float delay,
        string                   input,
        IBaseEntity?             activator,
        IBaseEntity?             caller,
        float                    value,
        int                      outputId = 0)
        => Entity.AddIOEventFloat(_this,
                                  delay,
                                  input,
                                  activator?.GetAbsPtr() ?? nint.Zero,
                                  caller?.GetAbsPtr()    ?? nint.Zero,
                                  value,
                                  outputId);

    public unsafe long DispatchTraceAttack(in TakeDamageInfo info, bool bypassHook = false)
    {
        fixed (TakeDamageInfo* pInfo = &info)
        {
            return Entity.DispatchTraceAttack(_this, pInfo, bypassHook);
        }
    }

    public unsafe long DispatchTraceAttack(TakeDamageInfo* info, bool bypassHook = false)
        => Entity.DispatchTraceAttack(_this, info, bypassHook);

    public void SetName(string name)
        => Entity.SetName(_this, name);

    public void SetModel(string model)
        => Entity.SetModel(_this, model);

    public void SetOwner(IBaseEntity? owner)
        => Entity.SetOwner(_this, owner?.GetAbsPtr() ?? nint.Zero);

    public void SetGroundEntity(IBaseEntity? ground, IBaseEntity? unknown)
        => Entity.SetGroundEntity(_this, ground?.GetAbsPtr() ?? nint.Zero, unknown?.GetAbsPtr() ?? nint.Zero);

    public void SetSolid(SolidType solid)
        => Entity.SetSolid(_this, solid);

    public unsafe void Teleport(Vector? position = null, Vector? angles = null, Vector? velocity = null)
        => Entity.Teleport(_this, &position, &angles, &velocity);

    public void SetMoveType(MoveType type)
        => Entity.SetMoveType(_this, type);

    public void SetGravityScale(float scale)
        => Entity.SetGravityScale(_this, scale);

    public unsafe void ApplyAbsVelocityImpulse(Vector velocity)
        => Entity.ApplyAbsVelocityImpulse(_this, &velocity);

    public unsafe Vector GetCenter()
        => *Entity.GetCenter(_this);

    public unsafe Vector GetAbsVelocity()
        => *Entity.GetAbsVelocity(_this);

    public unsafe void SetAbsVelocity(Vector velocity)
        => Entity.SetAbsVelocity(_this, &velocity);

    public unsafe Vector GetLocalVelocity()
        => *Entity.GetLocalVelocity(_this);

    public unsafe void SetLocalVelocity(Vector velocity)
        => Entity.SetLocalVelocity(_this, &velocity);

    public unsafe Vector GetAbsAngles()
        => *Entity.GetAbsAngles(_this);

    public unsafe void SetAbsAngles(Vector angles)
        => Entity.SetAbsAngles(_this, &angles);

    public unsafe Vector GetAbsOrigin()
        => *Entity.GetAbsOrigin(_this);

    public unsafe void SetAbsOrigin(Vector origin)
        => Entity.SetAbsOrigin(_this, &origin);

    public void CollisionRulesChanged()
        => Entity.CollisionRulesChanged(_this);

    public unsafe SoundOpEventGuid EmitSound(string sound, float? volume = null, RecipientFilter filter = default)
        => Entity.EmitSound(_this, sound, &volume, &filter);

    public void StopSound(string sound)
        => Entity.StopSound(_this, sound);

    public void SetCollisionGroup(CollisionGroupType type)
    {
        var property = GetCollisionProperty();

        if (property is null)
        {
            return;
        }

        property.CollisionGroup = type;
        CollisionRulesChanged();
    }

    public IBasePlayerPawn? AsBasePlayerPawn(bool safeCheck = true)
        => BasePlayerPawn.Create(safeCheck ? Player.PawnFromEntity(_this) : _this);

    public IPlayerPawn? AsPlayerPawn(bool safeCheck = true)
    {
        if (!safeCheck)
        {
            return PlayerPawn.Create(_this);
        }

        if (BasePlayerPawn.Create(Player.PawnFromEntity(_this)) is PlayerPawn pawn)
        {
            return pawn;
        }

        return null;
    }

    public IPlayerController? AsPlayerController(bool safeCheck = true)
        => PlayerController.Create(safeCheck ? Player.ControllerFromEntity(_this) : _this);

    public IBaseWeapon? AsBaseWeapon(bool safeCheck = true)
        => BaseWeapon.Create(safeCheck ? Entity.WeaponFromEntity(_this) : _this);

    public bool IsWeapon => Entity.IsWeapon(_this);

    public bool IsPlayerPawn => Entity.IsPlayerPawn(_this);

    public bool IsPlayerController => Entity.IsPlayerController(_this);

    public IBaseGrenadeProjectile? AsBaseGrenadeProjectile()
        => Classname.EndsWith("_projectile", StringComparison.OrdinalIgnoreCase) ? BaseGrenadeProjectile.Create(_this) : null;

    public unsafe void DispatchSpawn(IReadOnlyDictionary<string, KeyValuesVariantValueItem>? keyValues = null)
    {
        if (keyValues is null)
        {
            Entity.DispatchSpawn(_this, null, 0);

            return;
        }

        var items = keyValues.Select((x) => new KeyValuesVariantItem(x.Key, x.Value)).ToArray();
        var count = items.Length;

        fixed (KeyValuesVariantItem* ptr = items)
        {
            Entity.DispatchSpawn(_this, ptr, count);
        }
    }

    public string GlobalName
    {
        get => SchemaSystem.GetNetVarUtlSymbolLarge(_this, "CBaseEntity", "m_iGlobalname");
        set => SchemaSystem.SetNetVarUtlSymbolLarge(_this, "CBaseEntity", "m_iGlobalname", value);
    }

#region Schemas

    [NativeSchemaField("CBaseEntity", "m_iHealth", typeof(int))]
    private partial SchemaField GetHealthField();

    [NativeSchemaField("CBaseEntity", "m_iMaxHealth", typeof(int))]
    private partial SchemaField GetMaxHealthField();

    [NativeSchemaField("CBaseEntity", "m_lifeState", typeof(LifeState))]
    private partial SchemaField GetLifeStateField();

    [NativeSchemaField("CBaseEntity", "m_iTeamNum", typeof(CStrikeTeam))]
    private partial SchemaField GetTeamField();

    [NativeSchemaField("CBaseEntity", "m_MoveCollide", typeof(MoveCollideType))]
    private partial SchemaField GetMoveCollideField();

    [NativeSchemaField("CBaseEntity", "m_MoveType", typeof(MoveType))]
    private partial SchemaField GetMoveTypeField();

    [NativeSchemaField("CBaseEntity", "m_nActualMoveType", typeof(MoveType))]
    private partial SchemaField GetActualMoveTypeField();

    [NativeSchemaField("CBaseEntity", "m_spawnflags", typeof(uint))]
    private partial SchemaField GetSpawnFlagsField();

    [NativeSchemaField("CBaseEntity", "m_bTakesDamage", typeof(bool))]
    private partial SchemaField GetAllowTakesDamageField();

    [NativeSchemaField("CBaseEntity", "m_flGravityScale", typeof(float))]
    private partial SchemaField GetGravityScaleField();

    [NativeSchemaField("CBaseEntity", "m_flCreateTime", typeof(float))]
    private partial SchemaField GetCreateTimeField();

    [NativeSchemaField("CBaseEntity", "m_flSpeed", typeof(float))]
    private partial SchemaField GetSpeedField();

    [NativeSchemaField("CBaseEntity", "m_hOwnerEntity", typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetOwnerEntityHandleField();

    [NativeSchemaField("CBaseEntity", "m_sUniqueHammerID", typeof(CUtlString))]
    private partial SchemaField GetHammerIdField();

    [NativeSchemaField("CBaseEntity", "m_vecVelocity", typeof(Vector))]
    private partial SchemaField GetVelocityField();

    [NativeSchemaField("CBaseEntity", "m_vecAbsVelocity", typeof(Vector))]
    private partial SchemaField GetAbsVelocityField();

    [NativeSchemaField("CBaseEntity", "m_vecBaseVelocity", typeof(Vector))]
    private partial SchemaField GetBaseVelocityField();

    [NativeSchemaField("CBaseEntity", "m_fEffects", typeof(EntityEffects))]
    private partial SchemaField GetEffectsField();

    [NativeSchemaField("CBaseEntity", "m_fFlags", typeof(EntityFlags))]
    private partial SchemaField GetFlagsField();

    [NativeSchemaField("CBaseEntity", "m_iEFlags", typeof(int))]
    private partial SchemaField GetEFlagsField();

    [NativeSchemaField("CBaseEntity", "m_CBodyComponent", typeof(BodyComponent))]
    private partial SchemaField GetBodyComponentField();

    [NativeSchemaField("CBaseEntity", "m_hGroundEntity", typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetGroundEntityHandleField();

    [NativeSchemaField("CBaseEntity", "m_pCollision", typeof(CollisionProperty), Nullable = true)]
    private partial SchemaField GetCollisionPropertyField();

    [NativeSchemaField("CBaseEntity", "m_hEffectEntity", typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetEffectEntityHandleField();

    [NativeSchemaField("CBaseEntity", "m_hDamageFilter", typeof(CEntityHandle<IBaseFilter>))]
    private partial SchemaField GetDamageFilterEntityHandleField();

    [NativeSchemaField("CBaseEntity", "m_nNextThinkTick", typeof(int))]
    private partial SchemaField GetNextThinkTickField();

    [NativeSchemaField("CBaseEntity", "m_nSubclassID", typeof(uint))]
    private partial SchemaField GetSubclassIDField();

    [NativeSchemaField("CBaseEntity",
                       "m_ResponseContexts",
                       typeof(SchemaUnmanagedVector<ResponseContext>),
                       InlineObject = true,
                       ReturnType = typeof(ISchemaList<ResponseContext>))]
    private partial SchemaField GetResponseContextsField();

#endregion

    public override int GetHashCode()
        => HashCode.Combine(_this, Handle);

    public override bool Equals(object? obj)
    {
        if (obj is IBaseEntity entity)
        {
            return _this == entity.GetAbsPtr() && Handle.GetValue() == entity.Handle.GetValue();
        }

        return base.Equals(obj);
    }

    public sealed override unsafe string GetSchemaClassname()
    {
        if (SchemaClassname is not null)
        {
            return SchemaClassname;
        }

        var result = BaseEntityVirtualCall.GetDynamicBinding(_this);

        if (result is null)
        {
            throw new CustomAttributeFormatException("Failed to get schema class name.");
        }

        var className = NativeString.ReadString((byte*) *((void**) result + 1));

        if (string.IsNullOrEmpty(className))
        {
            throw new CustomAttributeFormatException("NetClass's ClassName is empty.");
        }

        if (!SharedGameObject.SchemaInfo.ContainsKey(className))
        {
            throw new KeyNotFoundException($"""Schema class "{className}" does not found.""");
        }

        SchemaClassname = className;

        return SchemaClassname;
    }

#region Static

    internal static unsafe CEntityHandle<IBaseEntity> GetHandle(nint pEntity)
        => GetIdentity(pEntity)->Handle;

    internal static unsafe bool IsMarkedForDelete(nint pEntity)
        => (GetIdentity(pEntity)->Flags & EntityFlagsKillMe) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe EntityIdentity* GetIdentity(nint pEntity)
        => *(EntityIdentity**) (pEntity + 0x10);

    [StructLayout(LayoutKind.Explicit)]
    private ref struct EntityIdentity
    {
        [FieldOffset(0x10)]
        public readonly CEntityHandle<IBaseEntity> Handle;

        [FieldOffset(0x14)]
        public readonly int StringTableNameIndex;

        [FieldOffset(0x18)]
        public CUtlSymbolLarge Name;

        [FieldOffset(0x20)]
        public CUtlSymbolLarge Classname;

        [FieldOffset(0x30)]
        public readonly int Flags;

        [FieldOffset(0x34)]
        public readonly int SpawnGroup;

        [FieldOffset(0x38)]
        public readonly int WorldGroup;
    }

#endregion
}
