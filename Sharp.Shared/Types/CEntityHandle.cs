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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Units;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly struct CEntityHandle<T> :
    IEquatable<uint>,
    IEquatable<CEntityHandle<T>> where T : class, IBaseEntity
{
    private readonly uint _value;

    public CEntityHandle(uint value = uint.MaxValue)
        => _value = value;

    public bool IsValid()
        => _value != uint.MaxValue;

    public EntityIndex GetEntryIndex()
    {
        if (IsValid())
        {
            return (int) (_value & 0x7FFF);
        }

        return -1;
    }

    public int GetSerialNum()
        => (int) ((_value >> 15) & 0x1FFFF);

    public uint GetValue()
        => _value;

    public int GetEHandleInt()
        => unchecked((int) _value);

    
    /// <summary>
    ///     Casts the handle to a different entity type. 
    ///     (Required because generic structs do not support implicit variance).
    /// </summary>
    public CEntityHandle<TOut> As<TOut>() where TOut : class, IBaseEntity
        => new (_value);

    public static implicit operator CEntityHandle<T>(uint e)
        => new (e);

    public static implicit operator uint(CEntityHandle<T> e)
        => e._value;

    public static bool operator ==(CEntityHandle<T> left, CEntityHandle<T> right)
        => left.GetValue() == right.GetValue();

    public static bool operator !=(CEntityHandle<T> left, CEntityHandle<T> right)
        => !(left == right);

    // ReSharper disable once StaticMemberInGenericType
    private static readonly Type ThisType = typeof(CEntityHandle<>);

    public bool Equals(uint other)
        => _value == other;

    public override bool Equals(object? obj)
    {
        if (obj is IEquatable<uint> e)
        {
            return e.Equals(_value);
        }

        return false;
    }

    public override int GetHashCode()
        => _value.GetHashCode();

    public bool Equals(CEntityHandle<T> other)
        => _value == other._value;
}
