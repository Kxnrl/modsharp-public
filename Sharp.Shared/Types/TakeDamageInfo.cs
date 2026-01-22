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

using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 288)]
public unsafe struct TakeDamageInfo
{
    [FieldOffset(0)]
    public void* pVTable;

    [FieldOffset(8)]
    public Vector DamageForce;

    [FieldOffset(20)]
    public Vector DamagePosition;

    [FieldOffset(32)]
    public Vector ReportedPosition;

    [FieldOffset(44)]
    public Vector DamageDirection;

    [FieldOffset(56)]
    public CEntityHandle<IBaseEntity> Inflictor;

    [FieldOffset(60)]
    public CEntityHandle<IBaseEntity> Attacker;

    [FieldOffset(64)]
    public CEntityHandle<IBaseEntity> Ability;

    [FieldOffset(68)]
    public float Damage;

    [FieldOffset(72)]
    public float TotalledDamage;

    [FieldOffset(76)]
    public DamageFlagBits DamageType;

    [FieldOffset(80)]
    public int DamageCustom;

    [FieldOffset(84)]
    public byte AmmoType;

    [FieldOffset(96)]
    public float OriginalDamage;

    [FieldOffset(100)]
    public bool ShouldBleed;

    [FieldOffset(101)]
    public bool ShouldSpark;

    [FieldOffset(104)]
    private GameTrace* _trace;

    // ReSharper disable once ConvertToAutoProperty
    public readonly GameTrace* Trace => _trace;

    [FieldOffset(112)]
    public TakeDamageFlags TakeDamageFlags;

    [FieldOffset(120)]
    public HitGroupType HitGroupId;

    [FieldOffset(124)]
    public int NumObjectsPenetrated;

    [FieldOffset(128)]
    public float FriendlyFireDamageReductionRatio;

    [FieldOffset(132)]
    public bool StoppedBullet;

    [FieldOffset(136)]
    public ShootInfo ShootInfo;

    [FieldOffset(224)]
    public void* ScriptInstance;

    /* CCSGOAttackerInfo */

    [FieldOffset(232)]
    public bool NeedInit;

    [FieldOffset(233)]
    public bool IsPawn;

    [FieldOffset(234)]
    public bool IsWorld;

    [FieldOffset(236)]
    public CEntityHandle<IPlayerPawn> AttackerPawnHandle;

    /// <summary>
    ///     Attacker PlayerSlot
    /// </summary>
    [FieldOffset(240)]
    public int AttackerPlayerSlot;

    [FieldOffset(244)]
    public int TeamChecked;

    [FieldOffset(248)]
    public int Team;

    /* End */

    [FieldOffset(280)]
    public bool InTakeDamageFlow;

    [FieldOffset(284)]
    public int UnknownFinal;

    private static readonly CEntityHandle<IBaseEntity> InvalidEntityHandle = new (uint.MaxValue);
    private static readonly CEntityHandle<IPlayerPawn> InvalidPawnHandle   = new (uint.MaxValue);

    public TakeDamageInfo()
    {
        NeedInit    = true;
        IsWorld     = true;
        TeamChecked = -1;

        AttackerPlayerSlot = -1;
        AttackerPawnHandle = InvalidPawnHandle;

        Inflictor = InvalidEntityHandle;
        Ability   = InvalidEntityHandle;

        UnknownFinal = -1;
    }

    public TakeDamageInfo(IPlayerPawn killer) : this()
    {
        NeedInit = false;
        Attacker = killer.Handle;

        if (killer.GetController() is not { } c)
        {
            return;
        }

        IsWorld            = false;
        IsPawn             = true;
        Team               = (int) killer.Team;
        TeamChecked        = (int) killer.Team;
        AttackerPlayerSlot = c.PlayerSlot.AsPrimitive();
        AttackerPawnHandle = killer.Handle.As<IPlayerPawn>();
    }

    public static TakeDamageInfo CreateFromFire(IBaseEntity inflictor, IPlayerPawn killer)
        => new (killer)
        {
            Inflictor       = inflictor.Handle,
            Ability         = inflictor.Handle,
            DamageType      = DamageFlagBits.Burn,
            TakeDamageFlags = TakeDamageFlags.IgnoreArmor,
        };

    public static TakeDamageInfo CreateFromSlash(IPlayerPawn killer, IBaseEntity weapon)
        => new (killer)
        {
            Inflictor = killer.Handle, Ability = weapon.Handle, DamageType = DamageFlagBits.Slash,
        };

    public static TakeDamageInfo CreateFromBullet(IPlayerPawn killer, IBaseWeapon weapon, bool headShot)
    {
        var info = new TakeDamageInfo(killer)
        {
            Inflictor = killer.Handle, Ability = weapon.Handle, DamageType = DamageFlagBits.Bullet,
        };

        if (headShot)
        {
            info.AddDamageType(DamageFlagBits.Headshot);
        }

        return info;
    }

    public static TakeDamageInfo CreateCustom(IPlayerPawn killer, IBaseEntity weapon, DamageFlagBits damageType)
        => new (killer)
        {
            Inflictor = killer.Handle, Ability = weapon.Handle, DamageType = damageType,
        };

    public TakeDamageInfo AddDamageType(DamageFlagBits bits)
    {
        DamageType |= bits;

        return this;
    }

    public TakeDamageInfo AddDamageFlags(TakeDamageFlags flags)
    {
        TakeDamageFlags |= flags;

        return this;
    }

    public TakeDamageInfo SetDamage(float damage)
    {
        Damage = damage;

        return this;
    }

    public TakeDamageInfo SetInitDamage(float damage)
    {
        Damage         = damage;
        OriginalDamage = damage;

        return this;
    }
}
