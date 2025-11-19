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
using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameObjects;
using Sharp.Core.Utilities;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameEntities;

internal abstract partial class BasePlayerPawn : BaseCombatCharacter, IBasePlayerPawn
{
    public new static BasePlayerPawn? Create(nint ptr)
    {
        if (ptr == nint.Zero)
        {
            return null;
        }

        if (Player.PawnIsPlayer(ptr))
        {
            return PlayerPawn.Create(ptr);
        }

        return ObserverPawn.Create(ptr);
    }

    public void Print(HudPrintChannel channel,
        string                        message,
        string?                       param1 = null,
        string?                       param2 = null,
        string?                       param3 = null,
        string?                       param4 = null)
        => Player.PawnPrint(_this, channel, message, param1, param2, param3, param4);

    protected abstract bool IsObserver();

    public bool IsPlayer(bool nativeCall = false)
        => nativeCall ? Player.PawnIsPlayer(_this) : !IsObserver();

    public virtual IPlayerPawn? AsPlayer()
        => null;

    public virtual IObserverPawn? AsObserver()
        => null;

    public IPlayerController? GetController()
        => PlayerController.Create(Player.PawnGetController(_this));

    public IPlayerController? GetOriginalController()
        => PlayerController.Create(OriginalControllerHandle.GetEntityPtr());

    public unsafe Vector GetEyeAngles()
        => *Entity.GetEyeAngles(_this);

    public unsafe Vector GetEyePosition()
        => *Entity.GetEyePosition(_this);

    public unsafe SoundOpEventGuid EmitSoundClient(string sound, float? volume = null)
        => Player.PawnEmitSoundClient(_this, sound, &volume);

    public void TransientChangeTeam(CStrikeTeam team)
        => SetTeamLocal(team);

#region Schemas

    [NativeSchemaField("CCSPlayerPawnBase", "m_hOriginalController", typeof(CEntityHandle<IPlayerController>))]
    private partial SchemaField GetOriginalControllerHandleField();

    [NativeSchemaField("CBasePlayerPawn", "m_iHideHUD", typeof(uint))]
    private partial SchemaField GetHideHudField();

    [NativeSchemaField("CBasePlayerPawn", "m_fTimeLastHurt", typeof(float))]
    private partial SchemaField GetTimeLastHurtField();

    [NativeSchemaField("CBasePlayerPawn", "m_flDeathTime", typeof(float))]
    private partial SchemaField GetDeathTimeField();

    [NativeSchemaField("CBasePlayerPawn", "m_fNextSuicideTime", typeof(float))]
    private partial SchemaField GetNextSuicideTimeField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_iPlayerState", typeof(PlayerState))]
    private partial SchemaField GetStateField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_bRespawning", typeof(bool))]
    private partial SchemaField GetRespawningField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_iNumSpawns", typeof(int))]
    private partial SchemaField GetNumSpawnsField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_flFlashMaxAlpha", typeof(float))]
    private partial SchemaField GetFlashMaxAlphaField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_flFlashDuration", typeof(float))]
    private partial SchemaField GetFlashDurationField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_fNextRadarUpdateTime", typeof(float))]
    private partial SchemaField GetNextRadarUpdateTimeField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_flProgressBarStartTime", typeof(float))]
    private partial SchemaField GetProgressBarStartTimeField();

    [NativeSchemaField("CCSPlayerPawnBase", "m_iProgressBarDuration", typeof(int))]
    private partial SchemaField GetProgressBarDurationField();

#endregion

#region Service Schema

    public unsafe ICameraService? GetCameraService()
        => CameraService.Create(*(nint*) IntPtr.Add(_this, GetCameraServiceField().Offset));

    public abstract IMovementService? GetMovementService();

#if USE_SCHEMA_GET_SERVICE
    [NativeSchemaField("CBasePlayerPawn", "m_pItemServices", typeof(ItemService))]
    private partial SchemaField GetItemServiceField();

    [NativeSchemaField("CBasePlayerPawn", "m_pMovementServices", typeof(MovementService))]
    private partial SchemaField GetMovementServiceField();

    [NativeSchemaField("CBasePlayerPawn", "m_pWeaponServices", typeof(WeaponService))]
    private partial SchemaField GetWeaponServiceField();

    [NativeSchemaField("CBasePlayerPawn", "m_pCameraServices", typeof(CameraService))]
    private partial SchemaField GetCameraServiceField();

    [NativeSchemaField("CBasePlayerPawn", "m_pObserverServices", typeof(ObserverService))]
    private partial SchemaField GetObserverServiceField();
#endif

    private static SchemaField? _itemServiceSchemaField;

    protected static SchemaField GetItemServiceField()
    {
        _itemServiceSchemaField ??= Helpers.SchemaSystem.GetSchemaField("CBasePlayerPawn", "m_pItemServices");

        return _itemServiceSchemaField;
    }

    private static SchemaField? _movementServiceSchemaField;

    protected static SchemaField GetMovementServiceField()
    {
        _movementServiceSchemaField ??= Helpers.SchemaSystem.GetSchemaField("CBasePlayerPawn", "m_pMovementServices");

        return _movementServiceSchemaField;
    }

    private static SchemaField? _weaponServiceSchemaField;

    protected static SchemaField GetWeaponServiceField()
    {
        _weaponServiceSchemaField ??= Helpers.SchemaSystem.GetSchemaField("CBasePlayerPawn", "m_pWeaponServices");

        return _weaponServiceSchemaField;
    }

    private static SchemaField? _cameraServiceSchemaField;

    protected static SchemaField GetCameraServiceField()
    {
        _cameraServiceSchemaField ??= Helpers.SchemaSystem.GetSchemaField("CBasePlayerPawn", "m_pCameraServices");

        return _cameraServiceSchemaField;
    }

    private static SchemaField? _observerServiceSchemaField;

    protected static SchemaField GetObserverServiceField()
    {
        _observerServiceSchemaField ??= Helpers.SchemaSystem.GetSchemaField("CBasePlayerPawn", "m_pObserverServices");

        return _observerServiceSchemaField;
    }

#endregion
}
