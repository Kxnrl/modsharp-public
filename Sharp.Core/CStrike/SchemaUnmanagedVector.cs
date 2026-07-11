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
using Sharp.Shared.Types.Tier;

namespace Sharp.Core.CStrike;

internal unsafe class SchemaUnmanagedVector<T> : NativeObject, ISchemaList<T>
    where T : unmanaged
{
    private readonly SchemaField    _schemaField;
    private readonly SchemaObject   _owner;
    private readonly bool           _isStruct;
    private readonly CUtlVector<T>* _utlVector;

    public int Count
    {
        get
        {
            CheckDisposed();

            return _utlVector->Count;
        }
    }

    private SchemaUnmanagedVector(nint @this, SchemaField field, SchemaObject owner, bool isStruct) : base(@this)
    {
        _schemaField = field;
        _owner       = owner;
        _isStruct    = isStruct;
        _utlVector   = (CUtlVector<T>*) @this;
    }

    public static SchemaUnmanagedVector<T>? Create(nint ptr, SchemaField field, SchemaObject owner, bool isStruct)
        => ptr == nint.Zero ? null : new SchemaUnmanagedVector<T>(ptr, field, owner, isStruct);

    public CUtlVector<T>* GetUtlVector()
    {
        CheckDisposed();

        return _utlVector;
    }

    public IEnumerator<T> AsEnumerable()
    {
        CheckDisposed();

        var size = Count;

        for (var i = 0; i < size; i++)
        {
            CheckDisposed();

            yield return this[i];
        }
    }

    public ISchemaList<T>.Enumerator GetEnumerator()
        => new (this);

    public T this[int index]
    {
        get
        {
            CheckDisposed();

            return _utlVector->Element(index);
        }
        set
        {
            CheckDisposed();

            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException();
            }

            ref var element = ref _utlVector->Element(index);

            element = value;

            SetStateChanged();
        }
    }

    public ref T GetRef(int index)
    {
        CheckDisposed();

        if (index < 0 || index >= Count)
        {
            throw new IndexOutOfRangeException();
        }

        return ref _utlVector->Element(index);
    }

    public void SetStateChanged()
    {
        CheckDisposed();

        _owner.SchemaStateChanged(_schemaField, _isStruct);
    }
}
