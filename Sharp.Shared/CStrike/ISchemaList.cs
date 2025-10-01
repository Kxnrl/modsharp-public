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
    int Count { get; }

    T this[int index] { get; set; }

    ref T GetRef(int index);

    unsafe CUtlVector<T>* GetUtlVector();

    IEnumerator<T> AsEnumerable();

    Enumerator GetEnumerator();

    void SetStateChanged();

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
