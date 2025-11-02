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

using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers.Hooks;

internal class PlayerDispatchTraceAttackHook
    : HookType<PlayerDispatchTraceAttackHookParams, long, PlayerDispatchTraceAttackHook>
{
    public PlayerDispatchTraceAttackHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Extern.DamageProcessor.OnPlayerDispatchTraceAttackPre  += CCSPlayerPawn_DispatchTraceAttackPre;
        Bridges.Forwards.Extern.DamageProcessor.OnPlayerDispatchTraceAttackPost += CCSPlayerPawn_DispatchTraceAttackPost;
    }

    private EHookAction CCSPlayerPawn_DispatchTraceAttackPre(nint ptrClient,
        nint                                                      ptrController,
        nint                                                      ptrPawn,
        nint                                                      ptrInfo,
        nint                                                      ptrResult)
    {
        if (!IsHookInvokeRequired(false))
        {
            return EHookAction.Ignored;
        }

        var param  = new PlayerDispatchTraceAttackHookParams(false, ptrClient, ptrController, ptrPawn, ptrInfo, ptrResult);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result.Action;
    }

    private void CCSPlayerPawn_DispatchTraceAttackPost(nint ptrClient,
        nint                                                ptrController,
        nint                                                ptrPawn,
        nint                                                ptrInfo,
        nint                                                ptrResult,
        EHookAction                                         action)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param  = new PlayerDispatchTraceAttackHookParams(true, ptrClient, ptrController, ptrPawn, ptrInfo, ptrResult);
        var result = new HookReturnValue<long>(action, param.WasDamageSuppressed ? 0 : param.HealthLost);

        InvokeHookPost(param, result);
        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;
}

internal sealed unsafe class PlayerDispatchTraceAttackHookParams : PlayerPawnFunctionParams,
    IPlayerDispatchTraceAttackHookParams
{
    private readonly TakeDamageInfo*   _ptrInfo;
    private readonly TakeDamageResult* _ptrResult;

    public PlayerDispatchTraceAttackHookParams(bool postHook, nint client, nint controller, nint pawn, nint info, nint result) :
        base(postHook,
             client,
             controller,
             pawn)
    {
        _ptrInfo   = (TakeDamageInfo*) info;
        _ptrResult = (TakeDamageResult*) result;
    }

    public TakeDamageInfo* Info
    {
        get
        {
            CheckDisposed();

            return _ptrInfo;
        }
    }

    /* GameTrace */

    public Vector TraceEndPosition => Trace == null ? new Vector() : Trace->EndPosition;

    public GameTrace* Trace => Info->Trace;

    public HitGroupType HitGroup
    {
        get
        {
            if (Trace == null)
            {
                return HitGroupType.Generic;
            }

            if (Trace->HitBoxData == null)
            {
                return HitGroupType.Generic;
            }

            return Trace->HitBoxData->HitGroup;
        }
    }

    public int HitBoxId
    {
        get
        {
            if (Trace == null)
            {
                return -1;
            }

            if (Trace->HitBoxData == null)
            {
                return -1;
            }

            return Trace->HitBoxData->HitBoxId;
        }
    }

    /* End */

    public Vector DamageForce
    {
        get => Info->DamageForce;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->DamageForce = value;
        }
    }

    public Vector DamagePosition
    {
        get => Info->DamagePosition;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->DamagePosition = value;
        }
    }

    public Vector ReportedPosition
    {
        get => Info->ReportedPosition;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->ReportedPosition = value;
        }
    }

    public Vector DamageDirection
    {
        get => Info->DamageDirection;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->DamageDirection = value;
        }
    }

    public CEntityHandle<IBaseEntity> InflictorHandle
    {
        get => Info->Inflictor;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->Inflictor = value;
        }
    }

    public CEntityHandle<IBaseEntity> AttackerHandle
    {
        get => Info->Attacker;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->Attacker = value;
        }
    }

    public CEntityHandle<IBaseEntity> AbilityHandle
    {
        get => Info->Ability;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->Ability = value;
        }
    }

    public float Damage
    {
        get => Info->Damage;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->Damage = value;
        }
    }

    public DamageFlagBits DamageType
    {
        get => Info->DamageType;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->DamageType = value;
        }
    }

    public int DamageCustom
    {
        get => Info->DamageCustom;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->DamageCustom = value;
        }
    }

    public byte AmmoType
    {
        get => Info->AmmoType;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->AmmoType = value;
        }
    }

    public float OriginalDamage
    {
        get => Info->OriginalDamage;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->OriginalDamage = value;
        }
    }

    public bool ShouldBleed
    {
        get => Info->ShouldBleed;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->ShouldBleed = value;
        }
    }

    public bool ShouldSpark
    {
        get => Info->ShouldSpark;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->ShouldSpark = value;
        }
    }

    public TakeDamageFlags TakeDamageFlags
    {
        get => Info->TakeDamageFlags;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->TakeDamageFlags = value;
        }
    }

    public int NumObjectsPenetrated
    {
        get => Info->NumObjectsPenetrated;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->NumObjectsPenetrated = value;
        }
    }

    public bool InTakeDamageFlow
    {
        get => Info->InTakeDamageFlow;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->InTakeDamageFlow = value;
        }
    }

    public bool IsPawn
    {
        get => Info->IsPawn;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->IsPawn = value;
        }
    }

    public bool IsWorld
    {
        get => Info->IsWorld;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->IsWorld = value;
        }
    }

    public int AttackerPlayerSlot
    {
        get => Info->AttackerPlayerSlot;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->AttackerPlayerSlot = value;
        }
    }

    public CEntityHandle<IPlayerPawn> AttackerPawnHandle
    {
        get => Info->AttackerPawnHandle;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->AttackerPawnHandle = value;
        }
    }

    public int TeamChecked
    {
        get => Info->TeamChecked;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->TeamChecked = value;
        }
    }

    public int Team
    {
        get => Info->Team;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->Team = value;
        }
    }

    public TakeDamageResult* Result
    {
        get
        {
            CheckDisposed();

            return _ptrResult;
        }
    }

    public int HealthLost
    {
        get => Result->HealthLost;
        set
        {
            CheckEditable();
            Result->HealthLost = value;
        }
    }

    public int HealthBefore
    {
        get => Result->HealthBefore;
        set
        {
            CheckEditable();
            Result->HealthBefore = value;
        }
    }

    public int DamageDealt
    {
        get => Result->HealthLost;
        set
        {
            CheckEditable();
            Result->HealthLost = value;
        }
    }

    public float PreModifiedDamage
    {
        get => Result->PreModifiedDamage;
        set
        {
            CheckEditable();
            Result->PreModifiedDamage = value;
        }
    }

    public int TotalledHealthLost
    {
        get => Result->TotalledHealthLost;
        set
        {
            CheckEditable();
            Result->TotalledHealthLost = value;
        }
    }

    public int TotalledDamageDealt
    {
        get => Result->TotalledDamageDealt;
        set
        {
            CheckEditable();
            Result->TotalledDamageDealt = value;
        }
    }

    public bool WasDamageSuppressed
    {
        get => Result->WasDamageSuppressed;
        set
        {
            CheckEditable();
            Result->WasDamageSuppressed = value;
        }
    }
}
