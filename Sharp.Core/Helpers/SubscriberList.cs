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

namespace Sharp.Core.Helpers;

internal sealed class SubscriberList<T> where T : class
{
    private readonly List<T>        _items;
    private readonly Comparison<T>? _order;
    private          T[]            _snapshot;

    public SubscriberList(Func<T, int>? priority)
    {
        _items    = [];
        _order    = priority is null ? null : (x, y) => priority(y).CompareTo(priority(x));
        _snapshot = [];
    }

    public int Count => _items.Count;

    public bool Exists(Predicate<T> match)
        => _items.Exists(match);

    public T? Find(Predicate<T> match)
        => _items.Find(match);

    public void Add(T item)
    {
        T[] next = [.. _items, item];

        if (_order is not null)
        {
            Array.Sort(next, _order);
        }

        _items.Clear();
        _items.AddRange(next);
        _snapshot = next;
    }

    public bool Remove(T item)
    {
        if (!_items.Remove(item))
        {
            return false;
        }

        _snapshot = [.. _items];

        return true;
    }

    public void Edit(Action<List<T>> edit)
    {
        try
        {
            edit(_items);
        }
        finally
        {
            _snapshot = [.. _items];
        }
    }

    private bool ContainsInstance(T item)
    {
        foreach (var x in _snapshot)
        {
            if (ReferenceEquals(x, item))
            {
                return true;
            }
        }

        return false;
    }

    public Enumerator GetEnumerator()
        => new(this);

    public struct Enumerator
    {
        private readonly SubscriberList<T> _owner;
        private readonly T[]               _snapshot;
        private          int               _index;

        public Enumerator(SubscriberList<T> owner)
        {
            _owner    = owner;
            _snapshot = owner._snapshot;
            _index    = -1;
        }

        public readonly T Current => _snapshot[_index];

        public bool MoveNext()
        {
            while (++_index < _snapshot.Length)
            {
                if (ReferenceEquals(_snapshot, _owner._snapshot) || _owner.ContainsInstance(_snapshot[_index]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
