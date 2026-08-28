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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Helpers;
using Sharp.Shared.Objects;
using Sharp.Shared.Utilities;

namespace Sharp.Shared.Types;

public readonly unsafe struct ScriptArray
{
    private readonly IScriptCallContext _context;

    public ScriptArray(IScriptCallContext context, uint handle)
    {
        _context = context;
        Handle   = handle;
    }

    private IScriptCallContext Context
        => _context
           ?? throw new InvalidOperationException(
               "ScriptArray is not initialized — obtain it from IScriptCallContext.GetArray/GetObject/NewArray, not default(ScriptArray).");

    public uint Handle { get; }

    public int Length => Context.ArrLength(Handle);

    /// <remarks>
    ///     Raw <see cref="ScriptValue" />: a String element's <see cref="ScriptValue.String" /> pointer is
    ///     only valid until the next element is read on this thread. Use <see cref="GetString" /> (copies) to
    ///     keep a string, or copy before the next access; non-string values are self-contained.
    /// </remarks>
    public ScriptValue this[int index] => Context.ArrGet(Handle, index);

    /// <summary>
    ///     Enumerate the elements. Yields raw <see cref="ScriptValue" /> — a String element aliases a reused
    ///     native buffer (see the indexer), so copy it before advancing; for strings prefer GetString.
    /// </summary>
    public Enumerator GetEnumerator()
        => new (this);

    public struct Enumerator
    {
        private readonly ScriptArray _array;
        private readonly int         _length;
        private          int         _index;

        internal Enumerator(ScriptArray array)
        {
            _array  = array;
            _length = array.Length;
            _index  = -1;
        }

        public readonly ScriptValue Current
            => _array[_index];

        public bool MoveNext()
            => ++_index < _length;
    }

#region Read

    public ScriptValueType TypeOf(int index)
        => Context.ArrGet(Handle, index).Type;

    public bool? GetBool(int index)
    {
        var sv = Context.ArrGet(Handle, index);

        return sv.Type switch
        {
            ScriptValueType.Bool => sv.Bool,
            ScriptValueType.Null => null,
            _                    => throw Mismatch(index, ScriptValueType.Bool, sv.Type),
        };
    }

    public double? GetNumber(int index)
    {
        var sv = Context.ArrGet(Handle, index);

        return sv.Type switch
        {
            ScriptValueType.Number => sv.Number,
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(index, ScriptValueType.Number, sv.Type),
        };
    }

    public string? GetString(int index)
    {
        var sv = Context.ArrGet(Handle, index);

        return sv.Type switch
        {
            ScriptValueType.String => sv.String is not null ? NativeString.ReadString(sv.String) : null,
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(index, ScriptValueType.String, sv.Type),
        };
    }

    public Vector? GetVector(int index)
    {
        var sv = Context.ArrGet(Handle, index);

        return sv.Type switch
        {
            ScriptValueType.Vector => sv.Vector,
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(index, ScriptValueType.Vector, sv.Type),
        };
    }

    public IBaseEntity? GetEntity(int index)
    {
        var sv = Context.ArrGet(Handle, index);

        return sv.Type switch
        {
            ScriptValueType.Entity => Context.ResolveEntity(in sv),
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(index, ScriptValueType.Entity, sv.Type),
        };
    }

    public ScriptObject? GetObject(int index)
    {
        var sv = Context.ArrGet(Handle, index);

        return sv.Type switch
        {
            ScriptValueType.Object => new ScriptObject(_context, sv.Handle),
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(index, ScriptValueType.Object, sv.Type),
        };
    }

    public ScriptArray? GetArray(int index)
    {
        var sv = Context.ArrGet(Handle, index);

        return sv.Type switch
        {
            ScriptValueType.Array => new ScriptArray(_context, sv.Handle),
            ScriptValueType.Null  => null,
            _                     => throw Mismatch(index, ScriptValueType.Array, sv.Type),
        };
    }

#endregion

#region Write

    public ScriptArray SetNull(int index)
        => Set(index, new ScriptValue { Type = ScriptValueType.Null });

    public ScriptArray SetBool(int index, bool value)
        => Set(index, new ScriptValue { Type = ScriptValueType.Bool, Bool = value });

    public ScriptArray SetNumber(int index, double value)
        => Set(index, new ScriptValue { Type = ScriptValueType.Number, Number = value });

    public ScriptArray SetString(int index, string value)
        => Set(index, ScriptMarshal.EncodeString(value));

    public ScriptArray SetVector(int index, Vector value)
        => Set(index, new ScriptValue { Type = ScriptValueType.Vector, Vector = value });

    public ScriptArray SetEntity(int index, IBaseEntity value)
        => Set(index, new ScriptValue { Type = ScriptValueType.Entity, Entity = value.Handle.GetValue() });

    public ScriptArray SetObject(int index, ScriptObject value)
        => Set(index, new ScriptValue { Type = ScriptValueType.Object, Handle = value.Handle });

    public ScriptArray SetArray(int index, ScriptArray value)
        => Set(index, new ScriptValue { Type = ScriptValueType.Array, Handle = value.Handle });

    public ScriptArray PushNull()
        => Push(new ScriptValue { Type = ScriptValueType.Null });

    public ScriptArray PushBool(bool value)
        => Push(new ScriptValue { Type = ScriptValueType.Bool, Bool = value });

    public ScriptArray PushNumber(double value)
        => Push(new ScriptValue { Type = ScriptValueType.Number, Number = value });

    public ScriptArray PushString(string value)
        => Push(ScriptMarshal.EncodeString(value));

    public ScriptArray PushVector(Vector value)
        => Push(new ScriptValue { Type = ScriptValueType.Vector, Vector = value });

    public ScriptArray PushEntity(IBaseEntity value)
        => Push(new ScriptValue { Type = ScriptValueType.Entity, Entity = value.Handle.GetValue() });

    public ScriptArray PushObject(ScriptObject value)
        => Push(new ScriptValue { Type = ScriptValueType.Object, Handle = value.Handle });

    public ScriptArray PushArray(ScriptArray value)
        => Push(new ScriptValue { Type = ScriptValueType.Array, Handle = value.Handle });

    public bool InsertNull(int index)
        => Insert(index, new ScriptValue { Type = ScriptValueType.Null });

    public bool InsertBool(int index, bool value)
        => Insert(index, new ScriptValue { Type = ScriptValueType.Bool, Bool = value });

    public bool InsertNumber(int index, double value)
        => Insert(index, new ScriptValue { Type = ScriptValueType.Number, Number = value });

    public bool InsertString(int index, string value)
        => Insert(index, ScriptMarshal.EncodeString(value));

    public bool InsertVector(int index, Vector value)
        => Insert(index, new ScriptValue { Type = ScriptValueType.Vector, Vector = value });

    public bool InsertEntity(int index, IBaseEntity value)
        => Insert(index, new ScriptValue { Type = ScriptValueType.Entity, Entity = value.Handle.GetValue() });

    public bool InsertObject(int index, ScriptObject value)
        => Insert(index, new ScriptValue { Type = ScriptValueType.Object, Handle = value.Handle });

    public bool InsertArray(int index, ScriptArray value)
        => Insert(index, new ScriptValue { Type = ScriptValueType.Array, Handle = value.Handle });

    public void Clear()
        => Context.ArrClear(Handle);

    public bool Remove(int index)
        => Context.ArrRemove(Handle, index);

    private ScriptArray Set(int index, ScriptValue value)
    {
        Context.ArrSet(Handle, index, in value);

        return this;
    }

    private ScriptArray Push(ScriptValue value)
    {
        Context.ArrPush(Handle, in value);

        return this;
    }

    private bool Insert(int index, ScriptValue value)
        => Context.ArrInsert(Handle, index, in value);

#endregion

    private static ScriptThrowException Mismatch(int index, ScriptValueType expected, ScriptValueType actual)
        => new ($"cs_script array index {index}: expected {expected} or null, got {actual}");
}
