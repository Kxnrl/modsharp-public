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
using Sharp.Core.Pools;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Core.Types;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly unsafe struct EntityKeyValuesVariantValue
{
    [FieldOffset(0)]
    private readonly EntityKeyValuesVariantType _type;

    [FieldOffset(8)]
    private readonly bool _bValue; // bool

    [FieldOffset(8)]
    private readonly int _iValue;

    [FieldOffset(8)]
    private readonly float _flValue;

    [FieldOffset(8)]
    private readonly byte* _szValue;

    [FieldOffset(8)]
    private readonly nint _pValue;

    public static implicit operator EntityKeyValuesVariantValue(KeyValuesVariantValueItem item)
        => item.Type switch
        {
            EntityKeyValuesVariantType.Bool    => new EntityKeyValuesVariantValue(item.BoolValue),
            EntityKeyValuesVariantType.Int32   => new EntityKeyValuesVariantValue(item.IntValue),
            EntityKeyValuesVariantType.Float   => new EntityKeyValuesVariantValue(item.FloatValue),
            EntityKeyValuesVariantType.String  => new EntityKeyValuesVariantValue(item.StringValue),
            EntityKeyValuesVariantType.Pointer => new EntityKeyValuesVariantValue(item.PointerValue),
            _                                  => throw new NotSupportedException("Missing type mapping"),
        };

    private EntityKeyValuesVariantValue(bool v)
    {
        _type   = EntityKeyValuesVariantType.Bool;
        _bValue = v;
    }

    private EntityKeyValuesVariantValue(int v)
    {
        _type   = EntityKeyValuesVariantType.Int32;
        _iValue = v;
    }

    private EntityKeyValuesVariantValue(float v)
    {
        _type    = EntityKeyValuesVariantType.Float;
        _flValue = v;
    }

    private EntityKeyValuesVariantValue(string v)
    {
        _type    = EntityKeyValuesVariantType.String;
        _szValue = StringPool.Instance.AllocPooledString(v);
    }

    private EntityKeyValuesVariantValue(nint v)
    {
        _type   = EntityKeyValuesVariantType.Pointer;
        _pValue = v;
    }
}
