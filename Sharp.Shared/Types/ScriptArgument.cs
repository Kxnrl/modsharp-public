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
using Sharp.Shared.Objects;

namespace Sharp.Shared.Types;

/// <summary>
///     <see cref="IScriptCallback.Invoke" />
/// </summary>
public readonly struct ScriptArgument
{
    private readonly double                                _number; // Bool(0/1) / Number
    private readonly Vector                                _vector;
    private readonly string?                               _string;
    private readonly IBaseEntity?                          _entity;
    private readonly (string Key, ScriptArgument Value)[]? _object;
    private readonly ScriptArgument[]?                     _array;

    private ScriptArgument(ScriptValueType type, double number)
    {
        Type    = type;
        _number = number;
    }

    private ScriptArgument(Vector vector)
    {
        Type    = ScriptValueType.Vector;
        _vector = vector;
    }

    private ScriptArgument(string value)
    {
        Type    = ScriptValueType.String;
        _string = value;
    }

    private ScriptArgument(IBaseEntity entity)
    {
        Type    = ScriptValueType.Entity;
        _entity = entity;
    }

    private ScriptArgument((string Key, ScriptArgument Value)[] fields)
    {
        Type    = ScriptValueType.Object;
        _object = fields;
    }

    private ScriptArgument(ScriptArgument[] elements)
    {
        Type   = ScriptValueType.Array;
        _array = elements;
    }

    public ScriptValueType Type { get; }

    public static implicit operator ScriptArgument(bool value)
        => new (ScriptValueType.Bool, value ? 1d : 0d);

    public static implicit operator ScriptArgument(int value)
        => new (ScriptValueType.Number, value);

    public static implicit operator ScriptArgument(double value)
        => new (ScriptValueType.Number, value);

    public static implicit operator ScriptArgument(string value)
        => new (value);

    public static implicit operator ScriptArgument(Vector value)
        => new (value);

    public static ScriptArgument Null => new (ScriptValueType.Null, 0d);

    public static ScriptArgument Entity(IBaseEntity entity)
        => new (entity);

    public static ScriptArgument Object(params (string Key, ScriptArgument Value)[] fields)
        => new (fields);

    public static ScriptArgument Array(params ScriptArgument[] elements)
        => new (elements);

    public bool AsBool => Type == ScriptValueType.Bool ? _number != 0d : throw Mismatch(ScriptValueType.Bool);

    public double AsNumber => Type == ScriptValueType.Number ? _number : throw Mismatch(ScriptValueType.Number);

    public Vector AsVector => Type == ScriptValueType.Vector ? _vector : throw Mismatch(ScriptValueType.Vector);

    public string? AsString => Type == ScriptValueType.String ? _string : throw Mismatch(ScriptValueType.String);

    public IBaseEntity? AsEntity => Type == ScriptValueType.Entity ? _entity : throw Mismatch(ScriptValueType.Entity);

    public ReadOnlySpan<(string Key, ScriptArgument Value)> AsObject
        => Type == ScriptValueType.Object ? _object : throw Mismatch(ScriptValueType.Object);

    public ReadOnlySpan<ScriptArgument> AsArray
        => Type == ScriptValueType.Array ? _array : throw Mismatch(ScriptValueType.Array);

    private InvalidOperationException Mismatch(ScriptValueType expected)
        => new ($"ScriptArgument is {Type}, not {expected}.");
}
