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
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Serilog.Core;
using Serilog.Events;

namespace Sharp.Core.Logging;

internal sealed class RuntimeLogDispatchSink : ILogEventSink
{
    public const int Disabled = int.MaxValue;

    private const int Capacity = 65536;

    private readonly ConcurrentQueue<LogEvent> _queue;

    private int _count;
    private int _minimumLevel;
    private int _dispatchThreadId;

    public RuntimeLogDispatchSink()
    {
        _queue = new ConcurrentQueue<LogEvent>(new LogEvent[Capacity]);

        while (_queue.TryDequeue(out _))
        {
        }

        _minimumLevel = Disabled;
    }

    public void Emit(LogEvent logEvent)
    {
        if ((int) logEvent.Level < Volatile.Read(ref _minimumLevel))
        {
            return;
        }

        if (Environment.CurrentManagedThreadId == Volatile.Read(ref _dispatchThreadId))
        {
            return;
        }

        if (Interlocked.Increment(ref _count) > Capacity)
        {
            Interlocked.Decrement(ref _count);

            return;
        }

        _queue.Enqueue(logEvent);
    }

    public void SetMinimumLevel(int level)
        => Volatile.Write(ref _minimumLevel, level);

    public void SetDispatchThread(int threadId)
        => Volatile.Write(ref _dispatchThreadId, threadId);

    public bool TryDequeue([NotNullWhen(true)] out LogEvent? logEvent)
    {
        if (!_queue.TryDequeue(out logEvent))
        {
            return false;
        }

        Interlocked.Decrement(ref _count);

        return true;
    }
}
