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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Logging;

namespace Sharp.Core.Logging;

internal sealed class RuntimeLogDispatcher : RuntimeLogSource
{
    private const int BatchSize = 1024;

    private const int PollInterval = 100;

    internal sealed class Registration
    {
        private bool _removed;

        public Registration(ILoggingListener listener, int priority, int minimumLevel)
        {
            Listener     = listener;
            Priority     = priority;
            MinimumLevel = minimumLevel;
            Buffer       = new RuntimeLogEntry[BatchSize];
        }

        public ILoggingListener  Listener     { get; }
        public int               Priority     { get; }
        public int               MinimumLevel { get; }
        public RuntimeLogEntry[] Buffer       { get; }

        public bool Removed
        {
            get => Volatile.Read(ref _removed);
            set => Volatile.Write(ref _removed, value);
        }
    }

    private readonly ILogger                _logger;
    private readonly RuntimeLogDispatchSink _sink;
    private readonly LogEvent[]             _batch;

    private Registration[] _registrations;
    private Thread?        _thread;
    private int            _generation;

    public RuntimeLogDispatcher(ILogger logger, RuntimeLogDispatchSink sink)
    {
        _logger        = logger;
        _sink          = sink;
        _batch         = new LogEvent[BatchSize];
        _registrations = [];
    }

    public void Publish(Registration[] registrations)
    {
        var minimumLevel = RuntimeLogDispatchSink.Disabled;

        foreach (var registration in registrations)
        {
            minimumLevel = Math.Min(minimumLevel, registration.MinimumLevel);
        }

        Volatile.Write(ref _registrations, registrations);
        _sink.SetMinimumLevel(minimumLevel);

        if (_thread is not null || registrations.Length == 0)
        {
            return;
        }

        _thread = new Thread(Run) { IsBackground = true, Name = "ModSharp RuntimeLog" };
        _sink.SetDispatchThread(_thread.ManagedThreadId);

        _thread.UnsafeStart();
    }

    private void Run()
    {
        while (true)
        {
            Thread.Sleep(PollInterval);

            try
            {
                DispatchPending();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while dispatching runtime logs");
            }
        }
    }

    private void DispatchPending()
    {
        int count;

        do
        {
            count = 0;

            while (count < _batch.Length && _sink.TryDequeue(out var logEvent))
            {
                _batch[count++] = logEvent;
            }

            if (count == 0)
            {
                return;
            }

            try
            {
                Dispatch(count);
            }
            finally
            {
                Array.Clear(_batch, 0, count);
            }
        }
        while (count == _batch.Length);
    }

    private void Dispatch(int count)
    {
        var registrations = Volatile.Read(ref _registrations);

        for (var i = 0; i < registrations.Length; i++)
        {
            var registration = registrations[i];

            if (registration.Removed)
            {
                continue;
            }

            var buffer     = registration.Buffer;
            var generation = _generation;
            var length     = 0;

            for (var j = 0; j < count; j++)
            {
                if ((int) _batch[j].Level >= registration.MinimumLevel)
                {
                    buffer[length++] = CreateEntry(_batch[j], generation);
                }
            }

            if (length == 0)
            {
                continue;
            }

            try
            {
                registration.Listener.OnRuntimeLogEntries(new ReadOnlySpan<RuntimeLogEntry>(buffer, 0, length));
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(ILoggingListener.OnRuntimeLogEntries),
                                 registration.Listener.GetType().Name);
            }
            finally
            {
                Volatile.Write(ref _generation, unchecked(generation + 1));
                Array.Clear(buffer, 0, length);
            }
        }
    }

    private void Check(int generation)
    {
        if (generation != Volatile.Read(ref _generation))
        {
            throw new ObjectDisposedException(nameof(RuntimeLogEntry),
                                              "Runtime log entries are only valid inside OnRuntimeLogEntries.");
        }
    }

    private LogEvent Event(object @event, int generation)
    {
        Check(generation);

        return (LogEvent) @event;
    }

    private LogEventPropertyValue Node(object node, int generation)
    {
        Check(generation);

        return (LogEventPropertyValue) node;
    }

    private T Node<T>(object node, int generation, LogValueKind kind) where T : LogEventPropertyValue
    {
        Check(generation);

        return node as T ?? throw new InvalidOperationException($"LogValue is not {kind}");
    }

    private static TKey ElementAt<TKey>(IReadOnlyDictionary<TKey, LogEventPropertyValue> source,
        int                                                                              index,
        out LogEventPropertyValue                                                        value) where TKey : notnull
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, source.Count);

        if (source is Dictionary<TKey, LogEventPropertyValue> dictionary)
        {
            foreach (var pair in dictionary)
            {
                if (index-- == 0)
                {
                    value = pair.Value;

                    return pair.Key;
                }
            }
        }
        else
        {
            foreach (var pair in source)
            {
                if (index-- == 0)
                {
                    value = pair.Value;

                    return pair.Key;
                }
            }
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override DateTimeOffset GetTimestamp(object @event, int generation)
        => Event(@event, generation).Timestamp;

    protected override LogLevel GetLevel(object @event, int generation)
        => (LogLevel) (int) Event(@event, generation).Level;

    protected override string? GetCategory(object @event, int generation)
        => Event(@event, generation).Properties.TryGetValue(Constants.SourceContextPropertyName, out var value)
           && value is ScalarValue { Value: string category }
            ? category
            : null;

    protected override string GetMessageTemplate(object @event, int generation)
        => Event(@event, generation).MessageTemplate.Text;

    protected override Exception? GetException(object @event, int generation)
        => Event(@event, generation).Exception;

    protected override ActivityTraceId GetTraceId(object @event, int generation)
        => Event(@event, generation).TraceId ?? default;

    protected override ActivitySpanId GetSpanId(object @event, int generation)
        => Event(@event, generation).SpanId ?? default;

    protected override int GetPropertyCount(object @event, int generation)
        => Event(@event, generation).Properties.Count;

    protected override object GetProperty(object @event, int generation, int index, out string name)
    {
        name = ElementAt(Event(@event, generation).Properties, index, out var value);

        return value;
    }

    protected override bool TryGetProperty(object @event, int generation, string name, [MaybeNullWhen(false)] out object value)
    {
        if (Event(@event, generation).Properties.TryGetValue(name, out var node))
        {
            value = node;

            return true;
        }

        value = null;

        return false;
    }

    protected override string RenderMessage(object @event, int generation)
        => Event(@event, generation).RenderMessage();

    protected override LogValueKind GetKind(object node, int generation)
        => Node(node, generation) switch
        {
            ScalarValue     => LogValueKind.Scalar,
            StructureValue  => LogValueKind.Structure,
            SequenceValue   => LogValueKind.Sequence,
            DictionaryValue => LogValueKind.Dictionary,
            var other       => throw new NotSupportedException($"Unsupported log property value {other.GetType().Name}"),
        };

    protected override object? GetScalar(object node, int generation)
        => Node<ScalarValue>(node, generation, LogValueKind.Scalar).Value;

    protected override string? GetTypeTag(object node, int generation)
        => Node<StructureValue>(node, generation, LogValueKind.Structure).TypeTag;

    protected override int GetCount(object node, int generation)
        => Node(node, generation) switch
        {
            StructureValue structure   => structure.Properties.Count,
            SequenceValue sequence     => sequence.Elements.Count,
            DictionaryValue dictionary => dictionary.Elements.Count,
            _                          => throw new InvalidOperationException("LogValue is Scalar"),
        };

    protected override object GetMember(object node, int generation, int index, out string name)
    {
        var property = Node<StructureValue>(node, generation, LogValueKind.Structure).Properties[index];

        name = property.Name;

        return property.Value;
    }

    protected override object GetElement(object node, int generation, int index)
        => Node<SequenceValue>(node, generation, LogValueKind.Sequence).Elements[index];

    protected override object GetEntry(object node, int generation, int index, out object key)
    {
        key = ElementAt(Node<DictionaryValue>(node, generation, LogValueKind.Dictionary).Elements, index, out var value);

        return value;
    }

    protected override string RenderValue(object node, int generation)
        => Node(node, generation).ToString();
}
