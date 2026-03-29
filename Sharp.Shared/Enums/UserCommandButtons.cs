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

namespace Sharp.Shared.Enums;

[Flags]
public enum UserCommandButtons : ulong
{
    /// <summary>
    ///     Primary attack
    /// </summary>
    Attack = 1 << 0,

    /// <summary>
    ///     Jump
    /// </summary>
    Jump = 1 << 1,

    /// <summary>
    ///     Duck
    /// </summary>
    Duck = 1 << 2,

    /// <summary>
    ///     Move forward
    /// </summary>
    Forward = 1 << 3,

    /// <summary>
    ///     Move backwards
    /// </summary>
    Back = 1 << 4,

    /// <summary>
    ///     +Use
    /// </summary>
    Use = 1 << 5,

    /// <summary>
    ///     Turn left
    /// </summary>
    TurnLeft = 1 << 7,

    /// <summary>
    ///     Turn right
    /// </summary>
    TurnRight = 1 << 8,

    /// <summary>
    ///     Move left
    /// </summary>
    MoveLeft = 1 << 9,

    /// <summary>
    ///     Move right
    /// </summary>
    MoveRight = 1 << 10,

    /// <summary>
    ///     Secondary attack
    /// </summary>
    Attack2 = 1 << 11,

    /// <summary>
    ///     Reload
    /// </summary>
    Reload = 1 << 13,

    /// <summary>
    ///     Walk
    /// </summary>
    Speed = 1 << 16,

    /// <summary>
    ///     User or Reload
    /// </summary>
    UserOrReload = 1ul << 32,

    /// <summary>
    ///     Show scoreboard
    /// </summary>
    Scoreboard = 1ul << 33,

    /// <summary>
    ///     Zoom
    /// </summary>
    Zoom = 1ul << 34,

    /// <summary>
    ///     Inspect
    /// </summary>
    LookAtWeapon = 1ul << 35,
}
