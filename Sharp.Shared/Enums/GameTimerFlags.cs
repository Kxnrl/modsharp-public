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
public enum GameTimerFlags
{
    /// <summary>
    ///     None
    /// </summary>
    None = 0,

    /// <summary>
    ///     Called repeatedly
    /// </summary>
    Repeatable = 1 << 0,

    /// <summary>
    ///     Requires IDisposable implementation
    /// </summary>
    [Obsolete("Removed since 1.6", true)]
    Disposable = 1 << 1,

    /// <summary>
    ///     Stop when round ends
    /// </summary>
    StopOnRoundEnd = 1 << 2,

    /// <summary>
    ///     Stop when map changes
    /// </summary>
    StopOnMapEnd = 1 << 3,

    /// <summary>
    ///     Force execute callback once before module shutdown
    /// </summary>
    ForceCallBeforeShutdown = 1 << 4,

    /// <summary>
    ///     Force execute callback once before timer is forcibly stopped
    /// </summary>
    /// <remarks>
    ///     Only takes effect in the following cases:
    ///     <list type="bullet">
    ///         <item>Timer is stopped by <see cref="IModSharp.StopTimer"/></item>
    ///         <item>Flag contains <see cref="GameTimerFlags.StopOnMapEnd"/> and server changes map</item>
    ///         <item>Flag contains <see cref="GameTimerFlags.StopOnRoundEnd"/> and round ends</item>
    ///     </list>
    ///     <para>
    ///         Note: If the timer ends normally where the callback function returns <see cref="TimerAction.Stop"/>, it will not call the callback even the flag is set.
    ///     </para>
    /// </remarks>
    ForceCallOnStop = 1 << 5,
}
