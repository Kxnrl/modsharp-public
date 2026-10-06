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
using System.Runtime.InteropServices;
using Sharp.Shared.Utilities;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit)]
public struct ConVarVariantValue
{
    [FieldOffset(0)]
    private byte _bValue; // bool

    [FieldOffset(0)]
    private Int16 _i16Value;

    [FieldOffset(0)]
    private UInt16 _u16Value;

    [FieldOffset(0)]
    private Int32 _i32Value;

    [FieldOffset(0)]
    private UInt32 _u32Value;

    [FieldOffset(0)]
    private Int64 _i64Value;

    [FieldOffset(0)]
    private UInt64 _u64Value;

    [FieldOffset(0)]
    private float _flValue;

    [FieldOffset(0)]
    private double _dbValue;

    [FieldOffset(0)]
    private nint _szValue; // const char*

    [FieldOffset(0)]
    private Color32 _clrValue;

    [FieldOffset(0)]
    private Vector2D _vec2Value;

    [FieldOffset(0)]
    private Vector _vec3Value;

    [FieldOffset(0)]
    private Vector4D _vec4Value;

    public ConVarVariantValue(bool value)
        => _bValue = (byte) (value ? 1 : 0);

    public ConVarVariantValue(short value)
        => _i16Value = value;

    public ConVarVariantValue(ushort value)
        => _u16Value = value;

    public ConVarVariantValue(int value)
        => _i32Value = value;

    public ConVarVariantValue(uint value)
        => _u32Value = value;

    public ConVarVariantValue(long value)
        => _i64Value = value;

    public ConVarVariantValue(ulong value)
        => _u64Value = value;

    public ConVarVariantValue(float value)
        => _flValue = value;

    public ConVarVariantValue(double value)
        => _dbValue = value;

    public ConVarVariantValue(nint value)
        => _szValue = value;

    public ConVarVariantValue(Color value)
        => _clrValue = new Color32(value.R, value.G, value.B, 255);

    public ConVarVariantValue(Color32 value)
        => _clrValue = value;

    public ConVarVariantValue(Vector2D value)
        => _vec2Value = value;

    public ConVarVariantValue(Vector value)
        => _vec3Value = value;

    public ConVarVariantValue(Vector4D value)
        => _vec4Value = value;

    public bool AsBool
    {
        get => _bValue != 0;
        set => _bValue = (byte) (value ? 1 : 0);
    }

    public short AsInt16
    {
        get => _i16Value;
        set => _i16Value = value;
    }

    public ushort AsUInt16
    {
        get => _u16Value;
        set => _u16Value = value;
    }

    public int AsInt32
    {
        get => _i32Value;
        set => _i32Value = value;
    }

    public uint AsUInt32
    {
        get => _u32Value;
        set => _u32Value = value;
    }

    public long AsInt64
    {
        get => _i64Value;
        set => _i64Value = value;
    }

    public ulong AsUInt64
    {
        get => _u64Value;
        set => _u64Value = value;
    }

    public float AsFloat
    {
        get => _flValue;
        set => _flValue = value;
    }

    public double AsDouble
    {
        get => _dbValue;
        set => _dbValue = value;
    }

    public string AsString => _szValue.ReadStringUtf8(0);

    public Color AsColor
    {
        get => new (_clrValue.R, _clrValue.G, _clrValue.B);
        set => _clrValue = new Color32(value.R, value.G, value.B, 255);
    }

    public Color32 AsColor32
    {
        get => _clrValue;
        set => _clrValue = value;
    }

    public Vector2D AsVector2D
    {
        get => _vec2Value;
        set => _vec2Value = value;
    }

    public Vector AsVector3D
    {
        get => _vec3Value;
        set => _vec3Value = value;
    }

    public Vector4D AsVector4D
    {
        get => _vec4Value;
        set => _vec4Value = value;
    }

    public static implicit operator ConVarVariantValue(bool value)
        => new (value);

    public static implicit operator ConVarVariantValue(short value)
        => new (value);

    public static implicit operator ConVarVariantValue(ushort value)
        => new (value);

    public static implicit operator ConVarVariantValue(int value)
        => new (value);

    public static implicit operator ConVarVariantValue(uint value)
        => new (value);

    public static implicit operator ConVarVariantValue(long value)
        => new (value);

    public static implicit operator ConVarVariantValue(ulong value)
        => new (value);

    public static implicit operator ConVarVariantValue(float value)
        => new (value);

    public static implicit operator ConVarVariantValue(double value)
        => new (value);
}
