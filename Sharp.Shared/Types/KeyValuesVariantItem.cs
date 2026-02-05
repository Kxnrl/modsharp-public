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
using Sharp.Shared.Enums;

namespace Sharp.Shared.Types;

public struct KeyValuesVariantValueItem
{
    public EntityKeyValuesVariantType Type;

    private bool    _bValue  = false;
    private int     _iValue  = 0;
    private float   _flValue = 0;
    private string? _szValue = null;
    private nint    _pValue  = nint.Zero;

    public static implicit operator KeyValuesVariantValueItem(int value)
        => new (value);

    public static implicit operator KeyValuesVariantValueItem(bool value)
        => new (value);

    public static implicit operator KeyValuesVariantValueItem(float value)
        => new (value);

    public static implicit operator KeyValuesVariantValueItem(string value)
        => new (value);

    public static implicit operator KeyValuesVariantValueItem(nint value)
        => new (value);

    public KeyValuesVariantValueItem(bool value)
    {
        Type    = EntityKeyValuesVariantType.Bool;
        _bValue = value;
    }

    public KeyValuesVariantValueItem(int value)
    {
        Type    = EntityKeyValuesVariantType.Int32;
        _iValue = value;
    }

    public KeyValuesVariantValueItem(float value)
    {
        Type     = EntityKeyValuesVariantType.Float;
        _flValue = value;
    }

    public KeyValuesVariantValueItem(nint value)
    {
        Type    = EntityKeyValuesVariantType.Pointer;
        _pValue = value;
    }

    public KeyValuesVariantValueItem(string value)
    {
        Type     = EntityKeyValuesVariantType.String;
        _szValue = value;
    }

    public bool BoolValue => Type is EntityKeyValuesVariantType.Bool ? _bValue : throw new TypeAccessException("Wrong Type");

    public int IntValue => Type is EntityKeyValuesVariantType.Int32 ? _iValue : throw new TypeAccessException("Wrong Type");

    public float FloatValue
        => Type is EntityKeyValuesVariantType.Float ? _flValue : throw new TypeAccessException("Wrong Type");

    public string StringValue
        => Type is EntityKeyValuesVariantType.String && _szValue is { } str ? str : throw new TypeAccessException("Wrong Type");

    public nint PointerValue
        => Type is EntityKeyValuesVariantType.Pointer ? _pValue : throw new TypeAccessException("Wrong Type");
}