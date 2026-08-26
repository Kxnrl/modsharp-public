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

/// <summary>
///     Controls whether a custom-HUD panel has a pre-defined CSS class.
/// </summary>
/// <remarks>
///     This only changes class membership. For the change to have a visual effect, the compiled
///     VCSS must contain a matching rule for the class.
/// </remarks>
public enum CustomHudClassOverride
{
    /// <summary>
    ///     Removes the current override. A global operation restores the class membership
    ///     declared by VXML; a per-player operation inherits the global value.
    /// </summary>
    Inherit,

    /// <summary>Forces the panel to have the specified class.</summary>
    Present,

    /// <summary>Forces the panel not to have the specified class.</summary>
    Absent,
}
