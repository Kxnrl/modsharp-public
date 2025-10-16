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
using Sharp.Shared.Types.Tier;

namespace Sharp.Shared.CStrike;

public interface ISchemaList<T> : INativeObject where T : unmanaged
{
    /// <summary>
    ///     元素的数量
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Indexer
    /// </summary>
    T this[int index] { get; set; }

    /// <summary>
    ///     用<paramref name="index"></paramref>获取元素的引用 <br />
    ///     <remarks>请使用<c>ref</c>关键字</remarks>
    /// </summary>
    ref T GetRef(int index);

    /// <summary>
    ///     获取<see cref="Shared.Types.Tier.CUtlVector{T}" />指针
    /// </summary>
    unsafe CUtlVector<T>* GetUtlVector();

    /// <summary>
    ///     转换为 <see cref="IEnumerable{T}" /> 以使用 Linq/ZLinq
    /// </summary>
    IEnumerator<T> AsEnumerable();

    /// <summary>
    ///     通常情况下你不需要使用它, 因为这是为<c>foreach</c>提供的0分配 <seealso cref="Enumerator" />
    /// </summary>
    Enumerator GetEnumerator();

    /// <summary>
    ///     调用Schema StateChanged
    /// </summary>
    void SetStateChanged();

    /// <summary>
    ///     你不需要使用它
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ISchemaList<T> _list;
        private          int            _index;

        public Enumerator(ISchemaList<T> list)
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
