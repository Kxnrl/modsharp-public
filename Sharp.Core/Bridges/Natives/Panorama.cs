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

using Sharp.Shared.Enums;
using Sharp.Shared.Units;

namespace Sharp.Core.Bridges.Natives;

internal static partial class Panorama
{
    public static partial void SetHasClass(nint layout, string panelId, string className, HudPanelClassStatus hasClass);

    public static partial void SetHasClassForPlayer(nint layout,
        PlayerSlot                                       playerSlot,
        string                                           panelId,
        string                                           className,
        HudPanelClassStatus                              hasClass);

    public static partial void SetDialogVariableString(nint layout, string panelId, string variableName, string value);

    public static partial void SetDialogVariableStringForPlayer(nint layout,
        PlayerSlot                                                   playerSlot,
        string                                                       panelId,
        string                                                       variableName,
        string                                                       value);

    public static partial void ClearDialogVariableStringForPlayer(nint layout,
        PlayerSlot                                                     playerSlot,
        string                                                         panelId,
        string                                                         variableName);

    public static partial void SetInputCaptureEnabled(nint layout, PlayerSlot playerSlot, bool enabled);

    public static partial bool IsInputCaptureEnabled(nint layout, PlayerSlot playerSlot);
}
