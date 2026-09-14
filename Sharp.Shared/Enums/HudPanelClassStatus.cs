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

namespace Sharp.Shared.Enums;

public enum HudPanelClassStatus : uint
{
    /// <summary>
    ///     Clears any override for this class, so the panel keeps its compiled-in default membership.
    ///     Maps to the native <c>k_eHudPanelClassStatus_Undefined</c>.
    /// </summary>
    Undefined    = 0xFFFFFFFF,

    /// <summary>
    ///     Forces the class off the panel. Maps to the native <c>k_eHudPanelClassStatus_DoesNotHaveClass</c>.
    /// </summary>
    ForceDisable = 0,

    /// <summary>
    ///     Forces the class on the panel. Maps to the native <c>k_eHudPanelClassStatus_HasClass</c>.
    /// </summary>
    ForceEnable  = 1,
}
