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

using System.Collections;
using System.Collections.Generic;

namespace Sharp.Shared.CStrike;

public interface ISchemaEmbeddedList<T> : INativeObject
{
    /// <summary>
    ///     The number of elements
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Indexer
    /// </summary>
    T this[int index] { get; set; }

    /// <summary>
    ///     Convert to <see cref="IEnumerable{T}" /> for use with Linq/ZLinq
    /// </summary>
    IEnumerator<T> AsEnumerable();

    /// <summary>
    ///     You typically don't need to use this, as it provides zero-allocation enumeration for <c>foreach</c> <seealso cref="Enumerator" />
    /// </summary>
    Enumerator GetEnumerator();

    /// <summary>
    ///     You don't need to use this
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ISchemaEmbeddedList<T> _list;
        private          int                    _index;

        public Enumerator(ISchemaEmbeddedList<T> list)
        {
            _list  = list;
            _index = -1;
        }

        public T Current => _list[_index];

        object IEnumerator.Current => Current!;

        public bool MoveNext()
            => ++_index < _list.Count;

        public void Reset()
            => _index = -1;

        public void Dispose()
        {
        }
    }
}
