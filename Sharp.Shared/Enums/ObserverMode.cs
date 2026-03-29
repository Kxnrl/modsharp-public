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

namespace Sharp.Shared.Enums;

/// <summary>
///     Spectator Movement modes
/// </summary>
public enum ObserverMode
{
    /// <summary>
    ///     not in spectator mode
    /// </summary>
    None,

    /// <summary>
    ///     view from a fixed camera position
    /// </summary>
    Fixed,

    /// <summary>
    ///     follow a player in first person view
    /// </summary>
    InEye,

    /// <summary>
    ///     follow a player in third person view
    /// </summary>
    Chase,

    /// <summary>
    ///     free roaming
    /// </summary>
    Roaming,

    /// <summary>
    ///     No idea
    /// </summary>
    Directed,
}
