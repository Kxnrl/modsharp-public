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
using Sharp.Shared;
using Sharp.Shared.CStrike;

namespace Sharp.Core.CStrike;

internal class SchemaFixedArray<T> : NativeObject, ISchemaArray<T>
    where T : unmanaged
{
    private readonly SchemaField _schemaField;
    private readonly nint        _objectPtr;

    public int Size => _schemaField.ArraySize;

    private SchemaFixedArray(nint @this, SchemaField field, nint objectPtr) : base(@this)
    {
        _schemaField = field;
        _objectPtr   = objectPtr;
    }

    public static SchemaFixedArray<T>? Create(nint ptr, SchemaField field, nint objectPtr)
        => ptr == nint.Zero ? null : new SchemaFixedArray<T>(ptr, field, objectPtr);

    public IEnumerator<T> AsEnumerable()
    {
        for (var i = 0; i < _schemaField.ArraySize; i++)
        {
            yield return this[i];
        }
    }

    public ISchemaArray<T>.Enumerator GetEnumerator()
        => new (this);

    public unsafe T this[int index]
    {
        get
        {
            if (index < 0 || index >= _schemaField.ArraySize)
            {
                throw new IndexOutOfRangeException();
            }

            CheckDisposed();

            return *((T*) _this + index);
        }
        set
        {
            if (index < 0 || index >= _schemaField.ArraySize)
            {
                throw new IndexOutOfRangeException();
            }

            CheckDisposed();

            *((T*) _this + index) = value;

            if (!_schemaField.Networked)
            {
                return;
            }

            if (_schemaField.ChainOffset > 0)
            {
                Bridges.Natives.Entity.NetworkStateChanged(IntPtr.Add(_objectPtr, _schemaField.ChainOffset),
                                                           (ushort) _schemaField.Offset);
            }
            else
            {
                Bridges.Natives.Entity.SetStateChanged(_objectPtr, (ushort) _schemaField.Offset);
            }
        }
    }

    public unsafe ref T GetRef(int index)
    {
        if (index < 0 || index >= _schemaField.ArraySize)
        {
            throw new IndexOutOfRangeException();
        }

        CheckDisposed();

        return ref *((T*) _this + index);
    }
}
