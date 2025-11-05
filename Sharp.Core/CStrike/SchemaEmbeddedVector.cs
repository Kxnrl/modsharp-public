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

using System;
using System.Collections.Generic;
using Sharp.Shared.CStrike;
using Sharp.Shared.Utilities;

namespace Sharp.Core.CStrike;

internal class SchemaEmbeddedVector<TObject, TInterface> : NativeObject, ISchemaEmbeddedList<TInterface>
    where TObject : INativeCreatable<TObject>, INativeSizeable, TInterface
{
    public int Count
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(0);
        }
    }

    private SchemaEmbeddedVector(nint @this) : base(@this)
    {
    }

    public new static SchemaEmbeddedVector<TObject, TInterface>? Create(nint ptr)
        => ptr == nint.Zero ? null : new SchemaEmbeddedVector<TObject, TInterface>(ptr);

    public IEnumerator<TInterface> AsEnumerable()
    {
        var size = Count;

        for (var i = 0; i < size; i++)
        {
            yield return this[i];
        }
    }

    public ISchemaEmbeddedList<TInterface>.Enumerator GetEnumerator()
        => new (this);

    public TInterface this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException();
            }

            CheckDisposed();

            var ptr = _this.Add(8).Dereference().Add(index * (int) TObject.NativeSize);

            return TObject.Create(ptr)!;
        }
        set => throw new NotSupportedException("read-only.");
    }
}
