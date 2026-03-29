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

public interface ISchemaArray<T> : INativeObject where T : unmanaged
{
    /// <summary>
    ///     Array size
    /// </summary>
    int Size { get; }

    /// <summary>
    ///     Indexer
    /// </summary>
    T this[int index] { get; set; }

    /// <summary>
    ///     Gets a reference to the element at the specified <paramref name="index"/> <br />
    ///     <remarks>Use the <c>ref</c> keyword</remarks>
    /// </summary>
    ref T GetRef(int index);

    /// <summary>
    ///     Converts to <see cref="IEnumerable{T}" /> for use with Linq/ZLinq
    /// </summary>
    IEnumerator<T> AsEnumerable();

    /// <summary>
    ///     You typically don't need to use this, as it provides zero-allocation enumeration for <c>foreach</c> <seealso cref="Enumerator" />
    /// </summary>
    Enumerator GetEnumerator();

    /// <summary>
    ///     You don't need to use this directly
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ISchemaArray<T> _array;
        private          int             _index;

        public Enumerator(ISchemaArray<T> array)
        {
            _array = array;
            _index = -1;
        }

        public T Current => _array[_index];

        object IEnumerator.Current => Current!;

        public bool MoveNext()
            => ++_index < _array.Size;

        public void Reset()
            => _index = -1;

        public void Dispose()
        {
        }
    }
}
