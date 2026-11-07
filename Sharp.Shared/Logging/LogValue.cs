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
using Sharp.Shared.Enums;

namespace Sharp.Shared.Logging;

/// <summary>
///     Lives as long as the <see cref="RuntimeLogEntry" /> it belongs to
/// </summary>
public readonly record struct LogValue
{
    private readonly RuntimeLogSource _source;
    private readonly object           _node;
    private readonly int              _generation;

    internal LogValue(RuntimeLogSource source, object node, int generation)
    {
        _source     = source;
        _node       = node;
        _generation = generation;
    }

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public LogValueKind Kind => _source.GetKind(_node, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    /// <exception cref="InvalidOperationException">Kind is not Scalar</exception>
    public object? Scalar => _source.GetScalar(_node, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    /// <exception cref="InvalidOperationException">Kind is not Structure</exception>
    public string? TypeTag => _source.GetTypeTag(_node, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    /// <exception cref="InvalidOperationException">Kind is Scalar</exception>
    public int Count => _source.GetCount(_node, _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    /// <exception cref="InvalidOperationException">Kind is not Structure</exception>
    public LogValue GetMember(int index, out string name)
        => new (_source, _source.GetMember(_node, _generation, index, out name), _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    /// <exception cref="InvalidOperationException">Kind is not Sequence</exception>
    public LogValue GetElement(int index)
        => new (_source, _source.GetElement(_node, _generation, index), _generation);

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    /// <exception cref="InvalidOperationException">Kind is not Dictionary</exception>
    public LogValue GetEntry(int index, out LogValue key)
    {
        var value = _source.GetEntry(_node, _generation, index, out var k);

        key = new LogValue(_source, k, _generation);

        return new LogValue(_source, value, _generation);
    }

    /// <exception cref="ObjectDisposedException">Accessed after the callback has returned</exception>
    public override string ToString()
        => _source.RenderValue(_node, _generation);
}
