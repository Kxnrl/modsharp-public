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
using System.Runtime.CompilerServices;
using Sharp.Core.Helpers;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Utilities;

namespace Sharp.Core.GameObjects;

internal partial class MovementService : PlayerPawnComponent, IMovementService
{
    public UserCommandButtons KeyButtons
    {
        get => (UserCommandButtons) _this.GetInt64(GetButtonsSchemaFieldOffset() + 8);
        set => _this.WriteInt64(GetButtonsSchemaFieldOffset() + 8, (long) value);
    }

    public UserCommandButtons KeyChangedButtons
    {
        get => (UserCommandButtons) _this.GetInt64(GetButtonsSchemaFieldOffset() + 16);
        set => _this.WriteInt64(GetButtonsSchemaFieldOffset() + 16, (long) value);
    }

    public UserCommandButtons ScrollButtons
    {
        get => (UserCommandButtons) _this.GetInt64(GetButtonsSchemaFieldOffset() + 24);
        set => _this.WriteInt64(GetButtonsSchemaFieldOffset() + 24, (long) value);
    }

#region Schemas

    private static SchemaField? __ButtonsSchemaField;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetButtonsSchemaFieldOffset()
    {
        __ButtonsSchemaField ??= SchemaSystem.GetSchemaField("CPlayer_MovementServices", "m_nButtons");

        return __ButtonsSchemaField.Offset;
    }

    [NativeSchemaField("CPlayer_MovementServices", "m_flMaxspeed", typeof(float), IsStruct = true)]
    private partial SchemaField GetMaxSpeedField();

#endregion

    public override string GetSchemaClassname()
        => "CPlayer_MovementServices";

    public IPlayerMovementService? AsPlayerMovementService(bool reinterpret = false)
    {
        if (GetPlayerMovementService() is { } mv)
        {
            return mv;
        }

        return reinterpret ? PlayerMovementService.Create(_this) : null;
    }

    public void TransientChangeMaxSpeed(float speed)
        => SetMaxSpeedLocal(speed);

    protected virtual IPlayerMovementService? GetPlayerMovementService()
        => null;
}

internal partial class PlayerMovementService : MovementService, IPlayerMovementService
{    
    private static readonly Lazy<int> LegacyJumpOffset
        = new (() => SchemaSystem.GetNetVarOffset("CCSPlayer_MovementServices", "m_LegacyJump"));

    private static readonly Lazy<int> OldJumpPressedOffset
        = new (() => SchemaSystem.GetNetVarOffset("CCSPlayerLegacyJump", "m_bOldJumpPressed"));

    private static readonly Lazy<int> JumpPressedTimeOffset
        = new (() => SchemaSystem.GetNetVarOffset("CCSPlayerLegacyJump", "m_flJumpPressedTime"));


    public void TransientChangeStamina(float stamina)
        => SetStaminaLocal(stamina);

    protected override IPlayerMovementService? GetPlayerMovementService()
        => this;

#region Schemas

    public bool OldJumpPressed
    {
        get => _this.GetBool(LegacyJumpOffset.Value   + OldJumpPressedOffset.Value);
        set => _this.WriteBool(LegacyJumpOffset.Value + OldJumpPressedOffset.Value, value);
    }

    public float JumpPressedTime
    {
        get => _this.GetFloat(LegacyJumpOffset.Value   + JumpPressedTimeOffset.Value);
        set => _this.WriteFloat(LegacyJumpOffset.Value + JumpPressedTimeOffset.Value, value);
    }

    [NativeSchemaField("CCSPlayer_MovementServices", "m_flDuckSpeed", typeof(float), IsStruct = true)]
    private partial SchemaField GetDuckSpeedField();
    
    [NativeSchemaField("CCSPlayer_MovementServices", "m_flStamina", typeof(float))]
    private partial SchemaField GetStaminaField();

    [NativeSchemaField("CCSPlayer_MovementServices", "m_bDuckOverride", typeof(bool))]
    private partial SchemaField GetDuckOverrideField();

#endregion

    public override string GetSchemaClassname()
        => "CCSPlayer_MovementServices";
}