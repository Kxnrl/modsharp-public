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
    ///     无
    /// </summary>
    None = 0,

    /// <summary>
    ///     可重复调用
    /// </summary>
    Repeatable = 1 << 0,

    /// <summary>
    ///     需要实现IDisposable
    /// </summary>
    [Obsolete("Removed since 1.6", true)]
    Disposable = 1 << 1,

    /// <summary>
    ///     回合交替时停止
    /// </summary>
    StopOnRoundEnd = 1 << 2,

    /// <summary>
    ///     地图交替时停止
    /// </summary>
    StopOnMapEnd = 1 << 3,

    /// <summary>
    ///     模块退出时强制执行一次
    /// </summary>
    ForceCallBeforeShutdown = 1 << 4,

    /// <summary>
    ///     在 Timer 被强制停止前强制执行一次
    /// </summary>
    /// <remarks>
    ///     仅在以下情况生效:
    ///     <list type="bullet">
    ///         <item>直接调用<see cref="IModSharp.StopTimer"/></item>
    ///         <item>Flag里包含<see cref="GameTimerFlags.StopOnMapEnd"/>, 并在服务器更换地图时</item>
    ///         <item>Flag里包含<see cref="GameTimerFlags.StopOnRoundEnd"/>, 并在回合结束时</item>
    ///     </list>
    ///     <para>
    ///         重要提示：如果计时器是因其回调函数返回 <see cref="TimerAction.Stop"/> 而正常结束的, 则不会触发此强制执行.
    ///     </para>
    /// </remarks>
    ForceCallOnStop = 1 << 5,
}
