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
    /// <remarks>
    ///     Stopped in OnRoundRestart (before RestartRound); on map change it is stopped by the rules of
    ///     <see cref="GameTimerFlags.StopOnMapEnd" />. It still fires between round_end and the restart. Timers pushed
    ///     during OnRoundRestart are removed with a warning when it returns; push timers for the new round in
    ///     OnRoundRestarted or later.
    /// </remarks>
    StopOnRoundEnd = 1 << 2,

    /// <summary>
    ///     Stop when map changes
    /// </summary>
    /// <remarks>
    ///     Stopped after the OnGamePreShutdown callbacks. Timers pushed after that are removed with a warning at
    ///     OnGameShutdown; push timers for the new map in OnServerInit or later.
    /// </remarks>
    StopOnMapEnd = 1 << 3,

    /// <summary>
    ///     Force execute callback once before module shutdown
    /// </summary>
    ForceCallBeforeShutdown = 1 << 4,

    /// <summary>
    ///     Force execute callback once when the timer is stopped
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             When <see cref="IModSharp.StopTimer" /> is called on the main thread, the callback runs immediately.
    ///             When it is called from another thread it does not wait: the main thread runs the callback at whichever
    ///             comes first of the end of a frame, a round restart, map cleanup, module unload or server shutdown, which
    ///             may be before or after <see cref="IModSharp.StopTimer" /> returns. There is no end of frame while the
    ///             server is hibernating or loading a map.
    ///         </item>
    ///         <item>
    ///             Also applies when the timer is stopped automatically at round or map end
    ///             (<see cref="GameTimerFlags.StopOnRoundEnd" /> / <see cref="GameTimerFlags.StopOnMapEnd" />); timers that
    ///             those two flags describe as removed directly do not run.
    ///         </item>
    ///         <item>
    ///             Does not run when the callback returns <see cref="TimerAction.Stop" />, when a one-shot timer has fired,
    ///             or when the timer is removed because its callback threw. It also does not run when a one-shot timer is
    ///             stopped while it is firing, or when the callback already ran for
    ///             <see cref="GameTimerFlags.ForceCallBeforeShutdown" /> at shutdown.
    ///         </item>
    ///         <item>
    ///             Does not run when the owning module has been unloaded (ownership is decided by the module's
    ///             AssemblyLoadContext, except for delegates from shared libraries or generated dynamically). A callback
    ///             that runs during a hot reload at map change has no map objects: <see cref="IModSharp.GetGameRules" />
    ///             and <see cref="IModSharp.GetGlobals" /> throw.
    ///         </item>
    ///     </list>
    /// </remarks>
    ForceCallOnStop = 1 << 5,
}
