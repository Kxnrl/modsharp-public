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

namespace Sharp.Core.Managers.Forwards;

internal class PlayerKilledPreForward : ForwardType<PlayerKilledForwardParams, PlayerKilledPreForward>
{
    public PlayerKilledPreForward(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerKilledPre += CCSPlayerPawn_PlayerKilledPre;

    private void CCSPlayerPawn_PlayerKilledPre(nint ptrClient, nint ptrController, nint ptrPawn, nint ptrResult)
    {
        if (!IsForwardInvokeRequired())
        {
            return;
        }

        var param = new PlayerKilledForwardParams(false, ptrClient, ptrController, ptrPawn, ptrResult);
        InvokeForward(param);
        param.MarkAsDisposed();
    }
}

internal class PlayerKilledPostForward : ForwardType<PlayerKilledForwardParams, PlayerKilledPostForward>
{
    public PlayerKilledPostForward(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerKilledPost += CCSPlayerPawn_PlayerKilledPost;

    private void CCSPlayerPawn_PlayerKilledPost(nint ptrClient, nint ptrController, nint ptrPawn, nint ptrResult)
    {
        if (!IsForwardInvokeRequired())
        {
            return;
        }

        var param = new PlayerKilledForwardParams(true, ptrClient, ptrController, ptrPawn, ptrResult);
        InvokeForward(param);
        param.MarkAsDisposed();
    }
}

internal sealed unsafe class PlayerKilledForwardParams : PlayerPawnFunctionParams, IPlayerKilledForwardParams
{
    private readonly TakeDamageResult* _ptrResult;

    public PlayerKilledForwardParams(bool postHook, nint client, nint controller, nint pawn, nint result) : base(postHook,
        client,
        controller,
        pawn)
        => _ptrResult = (TakeDamageResult*) result;

    public TakeDamageInfo* Info => Result->OriginatingInfo;

    public TakeDamageResult* Result
    {
        get
        {
            CheckDisposed();

            return _ptrResult;
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
            _ptrResult->OriginatingInfo->DamageForce = value;
        }
    }

    public Vector DamagePosition
    {
        get => Info->DamagePosition;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->DamagePosition = value;
        }
    }

    public Vector ReportedPosition
    {
        get => Info->ReportedPosition;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->ReportedPosition = value;
        }
    }

    public Vector DamageDirection
    {
        get => Info->DamageDirection;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->DamageDirection = value;
        }
    }

    public CEntityHandle<IBaseEntity> InflictorHandle
    {
        get => Info->Inflictor;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->Inflictor = value;
        }
    }

    public CEntityHandle<IBaseEntity> AttackerHandle
    {
        get => Info->Attacker;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->Attacker = value;
        }
    }

    public CEntityHandle<IBaseEntity> AbilityHandle
    {
        get => Info->Ability;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->Ability = value;
        }
    }

    public float Damage
    {
        get => Info->Damage;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->Damage = value;
        }
    }

    public DamageFlagBits DamageType
    {
        get => Info->DamageType;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->DamageType = value;
        }
    }

    public int DamageCustom
    {
        get => Info->DamageCustom;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->DamageCustom = value;
        }
    }

    public byte AmmoType
    {
        get => Info->AmmoType;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->AmmoType = value;
        }
    }

    public float OriginalDamage
    {
        get => Info->OriginalDamage;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->OriginalDamage = value;
        }
    }

    public bool ShouldBleed
    {
        get => Info->ShouldBleed;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->ShouldBleed = value;
        }
    }

    public bool ShouldSpark
    {
        get => Info->ShouldSpark;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->ShouldSpark = value;
        }
    }

    public TakeDamageFlags TakeDamageFlags
    {
        get => Info->TakeDamageFlags;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->TakeDamageFlags = value;
        }
    }

    public int NumObjectsPenetrated
    {
        get => Info->NumObjectsPenetrated;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->NumObjectsPenetrated = value;
        }
    }

    public bool InTakeDamageFlow
    {
        get => Info->InTakeDamageFlow;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->InTakeDamageFlow = value;
        }
    }

    public bool IsPawn
    {
        get => Info->IsPawn;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->IsPawn = value;
        }
    }

    public bool IsWorld
    {
        get => Info->IsWorld;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->IsWorld = value;
        }
    }

    public int AttackerPlayerSlot
    {
        get => Info->AttackerPlayerSlot;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->AttackerPlayerSlot = value;
        }
    }

    public CEntityHandle<IPlayerPawn> AttackerPawnHandle
    {
        get => Info->AttackerPawnHandle;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->AttackerPawnHandle = value;
        }
    }

    public int TeamChecked
    {
        get => Info->TeamChecked;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->TeamChecked = value;
        }
    }

    public int Team
    {
        get => Info->Team;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrResult->OriginatingInfo->Team = value;
        }
    }

    public int HealthLost
    {
        get => Result->HealthLost;
        set
        {
            CheckDisposed();
            CheckEditable();
            Result->HealthLost = value;
        }
    }

    public int HealthBefore
    {
        get => Result->HealthBefore;
        set
        {
            CheckDisposed();
            CheckEditable();
            Result->HealthBefore = value;
        }
    }

    public int DamageTaken
    {
        get => Result->DamageDealt;
        set
        {
            CheckDisposed();
            CheckEditable();
            Result->DamageDealt = value;
        }
    }

    public int TotalledHealthLost
    {
        get => Result->TotalledHealthLost;
        set
        {
            CheckDisposed();
            CheckEditable();
            Result->TotalledHealthLost = value;
        }
    }

    public int TotalledDamageTaken
    {
        get => Result->TotalledDamageDealt;
        set
        {
            CheckDisposed();
            CheckEditable();
            Result->TotalledDamageDealt = value;
        }
    }
}
