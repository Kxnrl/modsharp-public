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
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Helpers;
using Sharp.Shared.Objects;
using Sharp.Shared.Utilities;

namespace Sharp.Shared.Types;

public readonly unsafe struct ScriptObject
{
    private readonly IScriptCallContext _context;

    public ScriptObject(IScriptCallContext context, uint handle)
    {
        _context = context;
        Handle   = handle;
    }

    private IScriptCallContext Context
        => _context
           ?? throw new InvalidOperationException(
               "ScriptObject is not initialized — obtain it from IScriptCallContext.GetObject/GetArray/NewObject, not default(ScriptObject).");

    public uint Handle { get; }

    /// <summary>Number of own enumerable keys.</summary>
    public int Count => Context.ObjCount(Handle);

    public ScriptArray Keys()
        => new (_context, Context.ObjKeys(Handle));

    /// <remarks>
    ///     Raw <see cref="ScriptValue" />: a String value's <see cref="ScriptValue.String" /> pointer is only
    ///     valid until the next value is read on this thread. Use <see cref="GetString" /> (copies) to keep a
    ///     string, or copy before the next access; non-string values are self-contained.
    /// </remarks>
    public ScriptValue this[string key] => Context.ObjGet(Handle, key);

    /// <summary>
    ///     Enumerate the (key, value) entries. The key list is materialized once from <see cref="Keys" />;
    ///     values yield raw <see cref="ScriptValue" /> whose String aliases a reused native buffer (see the
    ///     indexer), so copy a string value before advancing; for strings prefer GetString.
    /// </summary>
    public Enumerator GetEnumerator()
        => new (this);

    public struct Enumerator
    {
        private readonly ScriptObject _object;
        private readonly ScriptArray  _keys;
        private readonly int          _length;
        private          int          _index;

        internal Enumerator(ScriptObject obj)
        {
            _object = obj;
            _keys   = obj.Keys();
            _length = _keys.Length;
            _index  = -1;
        }

        public readonly KeyValuePair<string, ScriptValue> Current
        {
            get
            {
                var key = _keys.GetString(_index) ?? string.Empty;

                return new (key, _object[key]);
            }
        }

        public bool MoveNext()
            => ++_index < _length;
    }

#region Read

    public bool Has(string key)
        => Context.ObjHas(Handle, key);

    public ScriptValueType TypeOf(string key)
        => Context.ObjGet(Handle, key).Type;

    public bool? GetBool(string key)
    {
        var sv = Context.ObjGet(Handle, key);

        return sv.Type switch
        {
            ScriptValueType.Bool => sv.Bool,
            ScriptValueType.Null => null,
            _                    => throw Mismatch(key, ScriptValueType.Bool, sv.Type),
        };
    }

    public double? GetNumber(string key)
    {
        var sv = Context.ObjGet(Handle, key);

        return sv.Type switch
        {
            ScriptValueType.Number => sv.Number,
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(key, ScriptValueType.Number, sv.Type),
        };
    }

    public string? GetString(string key)
    {
        var sv = Context.ObjGet(Handle, key);

        return sv.Type switch
        {
            ScriptValueType.String => sv.String is not null ? NativeString.ReadString(sv.String) : null,
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(key, ScriptValueType.String, sv.Type),
        };
    }

    public Vector? GetVector(string key)
    {
        var sv = Context.ObjGet(Handle, key);

        return sv.Type switch
        {
            ScriptValueType.Vector => sv.Vector,
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(key, ScriptValueType.Vector, sv.Type),
        };
    }

    public IBaseEntity? GetEntity(string key)
    {
        var sv = Context.ObjGet(Handle, key);

        return sv.Type switch
        {
            ScriptValueType.Entity => Context.ResolveEntity(in sv),
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(key, ScriptValueType.Entity, sv.Type),
        };
    }

    public ScriptObject? GetObject(string key)
    {
        var sv = Context.ObjGet(Handle, key);

        return sv.Type switch
        {
            ScriptValueType.Object => new ScriptObject(_context, sv.Handle),
            ScriptValueType.Null   => null,
            _                      => throw Mismatch(key, ScriptValueType.Object, sv.Type),
        };
    }

    public ScriptArray? GetArray(string key)
    {
        var sv = Context.ObjGet(Handle, key);

        return sv.Type switch
        {
            ScriptValueType.Array => new ScriptArray(_context, sv.Handle),
            ScriptValueType.Null  => null,
            _                     => throw Mismatch(key, ScriptValueType.Array, sv.Type),
        };
    }

#endregion

#region Write

    public ScriptObject SetNull(string key)
    {
        var sv = new ScriptValue { Type = ScriptValueType.Null };
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public ScriptObject SetBool(string key, bool value)
    {
        var sv = new ScriptValue { Type = ScriptValueType.Bool, Bool = value };
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public ScriptObject SetNumber(string key, double value)
    {
        var sv = new ScriptValue { Type = ScriptValueType.Number, Number = value };
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public ScriptObject SetString(string key, string value)
    {
        var sv = ScriptMarshal.EncodeString(value);
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public ScriptObject SetVector(string key, Vector value)
    {
        var sv = new ScriptValue { Type = ScriptValueType.Vector, Vector = value };
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public ScriptObject SetEntity(string key, IBaseEntity value)
    {
        var sv = new ScriptValue { Type = ScriptValueType.Entity, Entity = value.Handle.GetValue() };
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public ScriptObject SetObject(string key, ScriptObject value)
    {
        var sv = new ScriptValue { Type = ScriptValueType.Object, Handle = value.Handle };
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public ScriptObject SetArray(string key, ScriptArray value)
    {
        var sv = new ScriptValue { Type = ScriptValueType.Array, Handle = value.Handle };
        Context.ObjSet(Handle, key, in sv);

        return this;
    }

    public bool Remove(string key)
        => Context.ObjDelete(Handle, key);

    public void Clear()
        => Context.ObjClear(Handle);

#endregion

    private static ScriptThrowException Mismatch(string key, ScriptValueType expected, ScriptValueType actual)
        => new ($"cs_script object key '{key}': expected {expected} or null, got {actual}");
}
