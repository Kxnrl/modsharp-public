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
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Listeners;

namespace Sharp.Shared.Logging;

/// <summary>
///     Only valid inside the <see cref="ILoggingListener.OnRuntimeLogEntries" /> callback;
///     equality comparison and hashing are not restricted by this
/// </summary>
public readonly record struct RuntimeLogEntry
{
    private readonly RuntimeLogSource _source;
    private readonly object           _event;
    private readonly int              _generation;

    internal RuntimeLogEntry(RuntimeLogSource source, object @event, int generation)
    {
        _source     = source;
        _event      = @event;
        _generation = generation;
    }

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public DateTimeOffset Timestamp => _source.GetTimestamp(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public LogLevel Level => _source.GetLevel(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public string? Category => _source.GetCategory(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public string MessageTemplate => _source.GetMessageTemplate(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public Exception? Exception => _source.GetException(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public ActivityTraceId TraceId => _source.GetTraceId(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public ActivitySpanId SpanId => _source.GetSpanId(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public int PropertyCount => _source.GetPropertyCount(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public LogValue GetProperty(int index, out string name)
        => new (_source, _source.GetProperty(_event, _generation, index, out name), _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public bool TryGetProperty(string name, out LogValue value)
    {
        if (_source.TryGetProperty(_event, _generation, name, out var node))
        {
            value = new LogValue(_source, node, _generation);

            return true;
        }

        value = default;

        return false;
    }

    /// <summary>
    ///     Allocates a new string on every call
    /// </summary>
    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public string RenderMessage()
        => _source.RenderMessage(_event, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public override string ToString()
        => RenderMessage();
}
