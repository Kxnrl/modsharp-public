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
    public void SetClassOverride(string panelId, string className, HudPanelClassStatus classStatus)
        => Native.SetHasClass(_this, panelId, className, classStatus);

    public void SetClassOverrideForPlayer(PlayerSlot playerSlot,
        string                                       panelId,
        string                                       className,
        HudPanelClassStatus                          classStatus)
        => Native.SetHasClassForPlayer(_this, playerSlot, panelId, className, classStatus);

    public void SetDialogVariableString(string panelId, string variableName, string value)
        => Native.SetDialogVariableString(_this, panelId, variableName, value);

    public void SetDialogVariableStringForPlayer(PlayerSlot playerSlot,
        string                                              panelId,
        string                                              variableName,
        string                                              value)
        => Native.SetDialogVariableStringForPlayer(_this, playerSlot, panelId, variableName, value);

    public void ClearDialogVariableStringForPlayer(PlayerSlot playerSlot, string panelId, string variableName)
        => Native.ClearDialogVariableStringForPlayer(_this, playerSlot, panelId, variableName);

    public void SetInputCaptureEnabled(PlayerSlot playerSlot, bool enabled)
        => Native.SetInputCaptureEnabled(_this, playerSlot, enabled);

    public bool IsInputCaptureEnabled(PlayerSlot playerSlot)
        => Native.IsInputCaptureEnabled(_this, playerSlot);

#region Schemas

    [NativeSchemaField("CCSCustomHudLayout", "m_strLayout", typeof(CUtlSymbolLarge))]
    private partial SchemaField GetLayoutField();

#endregion
}
