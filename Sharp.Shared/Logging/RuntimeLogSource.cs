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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;

namespace Sharp.Shared.Logging;

/// <summary>
///     The data source behind <see cref="RuntimeLogEntry" /> and <see cref="LogValue" />,
///     implemented by core; modules should not inherit from it
/// </summary>
public abstract class RuntimeLogSource
{
    protected RuntimeLogEntry CreateEntry(object @event, int generation)
        => new (this, @event, generation);

    protected internal abstract DateTimeOffset GetTimestamp(object @event, int generation);

    protected internal abstract LogLevel GetLevel(object @event, int generation);

    protected internal abstract string? GetCategory(object @event, int generation);

    protected internal abstract string GetMessageTemplate(object @event, int generation);

    protected internal abstract Exception? GetException(object @event, int generation);

    protected internal abstract ActivityTraceId GetTraceId(object @event, int generation);

    protected internal abstract ActivitySpanId GetSpanId(object @event, int generation);

    protected internal abstract int GetPropertyCount(object @event, int generation);

    protected internal abstract object GetProperty(object @event, int generation, int index, out string name);

    protected internal abstract bool TryGetProperty(object @event,
        int                                                generation,
        string                                             name,
        [MaybeNullWhen(false)] out object                  value);

    protected internal abstract string RenderMessage(object @event, int generation);

    protected internal abstract LogValueKind GetKind(object node, int generation);

    protected internal abstract object? GetScalar(object node, int generation);

    protected internal abstract string? GetTypeTag(object node, int generation);

    protected internal abstract int GetCount(object node, int generation);

    protected internal abstract object GetMember(object node, int generation, int index, out string name);

    protected internal abstract object GetElement(object node, int generation, int index);

    protected internal abstract object GetEntry(object node, int generation, int index, out object key);

    protected internal abstract string RenderValue(object node, int generation);
}
