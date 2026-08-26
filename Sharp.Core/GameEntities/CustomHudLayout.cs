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
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;
using Native = Sharp.Core.Bridges.Natives.CustomHud;

namespace Sharp.Core.GameEntities;

internal partial class CustomHudLayout : BaseEntity, ICustomHudLayout
{
    public void SetClassOverride(string panelId, string className, CustomHudClassOverride classOverride)
    {
        ValidateIdentifier(panelId, nameof(panelId));
        ValidateIdentifier(className, nameof(className));

        Native.SetHasClass(_this, panelId, className, ToNativeOverride(classOverride));
    }

    public void SetClassOverrideForPlayer(PlayerSlot playerSlot,
        string                                        panelId,
        string                                        className,
        CustomHudClassOverride                        classOverride)
    {
        ValidatePlayerSlot(playerSlot);
        ValidateIdentifier(panelId, nameof(panelId));
        ValidateIdentifier(className, nameof(className));

        Native.SetHasClassForPlayer(_this, playerSlot, panelId, className, ToNativeOverride(classOverride));
    }

    public void SetDialogVariableString(string panelId, string variableName, string value)
    {
        ValidateIdentifier(panelId, nameof(panelId));
        ValidateIdentifier(variableName, nameof(variableName));
        ArgumentNullException.ThrowIfNull(value);

        Native.SetDialogVariableString(_this, panelId, variableName, value);
    }

    public void SetDialogVariableStringForPlayer(PlayerSlot playerSlot,
        string                                         panelId,
        string                                         variableName,
        string                                         value)
    {
        ValidatePlayerSlot(playerSlot);
        ValidateIdentifier(panelId, nameof(panelId));
        ValidateIdentifier(variableName, nameof(variableName));
        ArgumentNullException.ThrowIfNull(value);

        Native.SetDialogVariableStringForPlayer(_this, playerSlot, panelId, variableName, value);
    }

    public void ClearDialogVariableStringForPlayer(PlayerSlot playerSlot, string panelId, string variableName)
    {
        ValidatePlayerSlot(playerSlot);
        ValidateIdentifier(panelId, nameof(panelId));
        ValidateIdentifier(variableName, nameof(variableName));

        Native.ClearDialogVariableStringForPlayer(_this, playerSlot, panelId, variableName);
    }

    public void SetInputCaptureEnabled(PlayerSlot playerSlot, bool enabled)
    {
        ValidatePlayerSlot(playerSlot);

        Native.SetInputCaptureEnabled(_this, playerSlot, enabled);
    }

    public bool IsInputCaptureEnabled(PlayerSlot playerSlot)
    {
        ValidatePlayerSlot(playerSlot);

        return Native.IsInputCaptureEnabled(_this, playerSlot);
    }

    private static int ToNativeOverride(CustomHudClassOverride classOverride)
        => classOverride switch
        {
            CustomHudClassOverride.Inherit => -1,
            CustomHudClassOverride.Present => 1,
            CustomHudClassOverride.Absent  => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(classOverride),
                                                       classOverride,
                                                       "Unknown custom HUD class override."),
        };

    private static void ValidateIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Custom HUD identifiers cannot be null or empty.", parameterName);
        }
    }

    private static void ValidatePlayerSlot(PlayerSlot playerSlot)
    {
        if (!playerSlot.IsValid())
        {
            throw new ArgumentOutOfRangeException(nameof(playerSlot), playerSlot, "Player slot must be between 0 and 63.");
        }
    }

#region Schemas

    [NativeSchemaField("CCSCustomHudLayout", "m_strLayout", typeof(CUtlSymbolLarge))]
    private partial SchemaField GetLayoutField();

#endregion
}
