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
using Sharp.Shared.Listeners;

namespace Sharp.Shared.Managers;

public interface ILoggingManager
{
    /// <exception cref="InvalidOperationException">
    ///     Not called on the main thread, or ListenerVersion does not match
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">RuntimeMinimumLevel is not a defined value</exception>
    void InstallLoggingListener(ILoggingListener listener);

    /// <summary>
    ///     Does not wait for the dispatch thread: after it returns, the listener may still receive at most one more
    ///     callback (which may start after the return), and none after that
    /// </summary>
    /// <exception cref="InvalidOperationException">Not called on the main thread</exception>
    void RemoveLoggingListener(ILoggingListener listener);
}
