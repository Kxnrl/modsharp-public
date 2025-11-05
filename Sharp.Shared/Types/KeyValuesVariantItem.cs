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
using System.Runtime.InteropServices;
using Sharp.Shared.Utilities;

namespace Sharp.Shared.Types;

internal enum KeyValuesVariantValueItemType : uint
{
    Bool,
    Int32,
    Float,
    String,
    Pointer,
}

[StructLayout(LayoutKind.Explicit, Size = 264)]
public unsafe struct KeyValuesVariantValueItem
{
    [FieldOffset(0)]
    private KeyValuesVariantValueItemType _type;

    [FieldOffset(8)]
    private byte _bValue; // bool

    [FieldOffset(8)]
    private int _i32Value;

    [FieldOffset(8)]
    private float _flValue;

    [FieldOffset(8)]
    private fixed byte _szValue[KeyValuesVariantItem.MaxStringLength];

    [FieldOffset(8)]
    private nint _pValue;

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
        _type   = KeyValuesVariantValueItemType.Bool;
        _bValue = (byte) (value ? 1 : 0);
    }

    public KeyValuesVariantValueItem(int value)
    {
        _type     = KeyValuesVariantValueItemType.Int32;
        _i32Value = value;
    }

    public KeyValuesVariantValueItem(float value)
    {
        _type    = KeyValuesVariantValueItemType.Float;
        _flValue = value;
    }

    public KeyValuesVariantValueItem(nint value)
    {
        _type   = KeyValuesVariantValueItemType.Pointer;
        _pValue = value;
    }

    public KeyValuesVariantValueItem(string value)
    {
        _type = KeyValuesVariantValueItemType.String;

        fixed (byte* ptr = _szValue)
        {
            var bytes = new Span<byte>(ptr, KeyValuesVariantItem.MaxStringLength);

            bytes.WriteStringUtf8(value);
        }
    }
}

public unsafe struct KeyValuesVariantItem
{
    public const int MaxStringLength = 256;

    public fixed byte Key[MaxStringLength];

    public KeyValuesVariantValueItem Value;

    public KeyValuesVariantItem(string key, KeyValuesVariantValueItem val)
    {
        fixed (byte* ptr = Key)
        {
            var bytes = new Span<byte>(ptr, MaxStringLength);

            bytes.WriteStringUtf8(key);
        }

        Value = val;
    }

    public KeyValuesVariantItem(string key, ref KeyValuesVariantValueItem val)
    {
        fixed (byte* ptr = Key)
        {
            var bytes = new Span<byte>(ptr, MaxStringLength);

            bytes.WriteStringUtf8(key);
        }

        Value = val;
    }
}
