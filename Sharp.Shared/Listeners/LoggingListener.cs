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
using Microsoft.Extensions.Logging;
using Sharp.Shared.Logging;

namespace Sharp.Shared.Listeners;

public interface ILoggingListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     Listener version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     Read only once, at install time. Events below the Logger:LogLevel configured in core.json
    ///     are never produced, so a lower value here still does not receive them
    /// </summary>
    LogLevel RuntimeMinimumLevel { get; }

    /// <summary>
    ///     Called on core's log dispatch thread, not the game main thread;
    ///     use <see cref="IModSharp.InvokeAction" /> to return to the main thread before touching game objects <br />
    ///     <paramref name="entries" /> and the <see cref="RuntimeLogEntry" /> and <see cref="LogValue" /> values taken
    ///     from them are only valid during this callback; objects referenced by <see cref="LogValue.Scalar" /> and
    ///     <see cref="RuntimeLogEntry.Exception" /> may belong to other modules, and holding on to them prevents those
    ///     modules from unloading <br />
    ///     All listeners share one dispatch thread, so the callback should return quickly; once the backlog exceeds
    ///     its limit, new logs are dropped without notice. Logs written on the dispatch thread are not dispatched
    ///     to any listener
    /// </summary>
    void OnRuntimeLogEntries(ReadOnlySpan<RuntimeLogEntry> entries);
}
