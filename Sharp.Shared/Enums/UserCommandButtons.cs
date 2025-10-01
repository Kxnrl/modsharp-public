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
    ///     开火
    /// </summary>
    Attack = 1 << 0,

    /// <summary>
    ///     跳
    /// </summary>
    Jump = 1 << 1,

    /// <summary>
    ///     蹲
    /// </summary>
    Duck = 1 << 2,

    /// <summary>
    ///     前进
    /// </summary>
    Forward = 1 << 3,

    /// <summary>
    ///     后退
    /// </summary>
    Back = 1 << 4,

    /// <summary>
    ///     +Use
    /// </summary>
    Use = 1 << 5,

    /// <summary>
    ///     左转
    /// </summary>
    TurnLeft = 1 << 7,

    /// <summary>
    ///     右转
    /// </summary>
    TurnRight = 1 << 8,

    /// <summary>
    ///     左移
    /// </summary>
    MoveLeft = 1 << 9,

    /// <summary>
    ///     右移
    /// </summary>
    MoveRight = 1 << 10,

    /// <summary>
    ///     武器特殊模式
    /// </summary>
    Attack2 = 1 << 11,

    /// <summary>
    ///     换弹
    /// </summary>
    Reload = 1 << 13,

    /// <summary>
    ///     静步
    /// </summary>
    Speed = 1 << 16,

    /// <summary>
    ///     假蹲?
    /// </summary>
    BullRush = 1 << 22,

    /// <summary>
    ///     User or Reload
    /// </summary>
    UserOrReload = 1ul << 32,

    /// <summary>
    ///     计分板
    /// </summary>
    Scoreboard = 1ul << 33,

    /// <summary>
    ///     Zoom
    /// </summary>
    Zoom = 1ul << 34,

    /// <summary>
    ///     监视
    /// </summary>
    LookAtWeapon = 1ul << 35,
}
