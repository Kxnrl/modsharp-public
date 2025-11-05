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
using Sharp.Shared.Enums;
using Sharp.Shared.Utilities;

namespace Sharp.Shared.Types;

// Explicit 因为强制编辑为union之后, 就不能简单的复制了
// 直接复制会导致内存布局错误, 从而导致数据错误
[StructLayout(LayoutKind.Explicit)]
public ref struct EntityVariant
{
    [FieldOffset(0)]
    private byte _bValue; // bool

    [FieldOffset(0)]
    private int _i32Value;

    [FieldOffset(0)]
    private float _flValue;

    [FieldOffset(0)]
    private nint _szValue; // const char*

    [FieldOffset(0)]
    private nint _pValue;

    [FieldOffset(8)]
    private VariantFieldType _type;

    public readonly VariantFieldType Type => _type;

    public readonly string AsString
        => _type switch
        {
            VariantFieldType.CString => _szValue.ReadStringUtf8(0),
            VariantFieldType.String  => ReadPointerString(),
            _                        => throw new InvalidCastException(),
        };

    public readonly int   AsInt32 => _type is VariantFieldType.Int32 ? _i32Value : throw new InvalidCastException();
    public readonly float AsFloat => _type is VariantFieldType.Float32 ? _flValue : throw new InvalidCastException();
    public readonly bool  AsBool  => _type is VariantFieldType.Boolean ? _bValue != 0 : throw new InvalidCastException();

    private readonly unsafe string ReadPointerString()
    {
        if (_pValue == nint.Zero)
        {
            return string.Empty;
        }

        var ptr = *(sbyte**) _pValue;

        return Utils.ReadString(ptr);
    }

    public override string ToString()
        => _type switch
        {
            VariantFieldType.CString => AsString,
            VariantFieldType.String  => AsString,
            VariantFieldType.Int32   => AsInt32.ToString(),
            VariantFieldType.Float32 => $"{AsFloat:F2}",
            VariantFieldType.Boolean => AsBool.ToString(),
            _                        => throw new InvalidCastException(),
        };

    /// <summary>
    ///     有VScript调用的IO中, Type String但是使用的是CString
    /// </summary>
    /// <returns></returns>
    public readonly string AutoCastString()
        => _type is VariantFieldType.CString or VariantFieldType.String ? _szValue.ReadStringUtf8(0) : string.Empty;
}
