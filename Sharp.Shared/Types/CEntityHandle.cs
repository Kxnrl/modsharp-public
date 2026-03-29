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

    public CEntityHandle(int index, int serial)
        => _value = ((uint) index & 0x7FFF) | (((uint) serial & 0x1FFFF) << 15);

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

    /// <summary>
    ///     Compresses the handle into a 24-bit integer (Index + Truncated Serial).
    ///     Used for network transmission (Dispatch effect, etc.)
    /// </summary>
    public uint GetPackedValue()
    {
        if (_value == uint.MaxValue)
        {
            return 0xFFFFFF;
        }

        return (_value & 0x7FFF) | (((_value >> 15) & 0x3FF) << 14);
    }

    /// <summary>
    ///     Reconstructs a CEntityHandle from a compressed 24-bit integer.
    ///     Typically used when reading handles from network packets.
    /// </summary>
    /// <param name="packed">The 24-bit packed value.</param>
    public static CEntityHandle<T> FromPackedValue(uint packed)
    {
        if (packed == 0xFFFFFF)
        {
            return new CEntityHandle<T>(uint.MaxValue);
        }

        // extract index from lower 14 bits
        var index = packed & 0x3FFF;

        // extract serial from bit 14
        var serial = (packed >> 14) & 0x3FF;

        return new CEntityHandle<T>(index | (serial << 15));
    }
}
