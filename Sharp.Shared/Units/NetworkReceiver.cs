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

using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnitGenerator;

namespace Sharp.Shared.Units;

[UnitOf<ulong>(UnitGenerateOptions.ImplicitOperator)]
public partial struct NetworkReceiver : IEnumerable<PlayerSlot>
{
    private const ulong BaseMagic = 1UL;

    public NetworkReceiver(IEnumerable<PlayerSlot> players)
    {
        var v = 0UL;

        foreach (var player in players)
        {
            v |= BaseMagic << player.AsPrimitive();
        }

        value = v;
    }

    public NetworkReceiver(IReadOnlyCollection<PlayerSlot> players)
    {
        var v = 0UL;

        foreach (var player in players)
        {
            v |= BaseMagic << player.AsPrimitive();
        }

        value = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty()
        => value == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFull()
        => value == ulong.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count()
        => BitOperations.PopCount(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasClient(PlayerSlot slot)
        => (value & (BaseMagic << slot.AsPrimitive())) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NetworkReceiver Append(PlayerSlot slot)
        => new (value | (BaseMagic << slot.AsPrimitive()));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NetworkReceiver Remove(PlayerSlot slot)
        => new (value & ~(BaseMagic << slot.AsPrimitive()));

    public IEnumerable<PlayerSlot> GetClients()
    {
        var remaining = value;

        while (remaining != 0)
        {
            var bit = BitOperations.TrailingZeroCount(remaining);

            yield return new PlayerSlot((byte) bit);

            remaining &= remaining - 1; // clear lowest set bit
        }
    }

    public PlayerSlot[] GetClientsArray()
    {
        var count     = BitOperations.PopCount(value);
        var result    = new PlayerSlot[count];
        var remaining = value;
        var idx       = 0;

        while (remaining != 0)
        {
            var bit = BitOperations.TrailingZeroCount(remaining);
            result[idx++] =  new PlayerSlot((byte) bit);
            remaining     &= remaining - 1;
        }

        return result;
    }

    public string DestructureTransform()
        => $"{{ \"Value\": {value}, \"Receivers\": [{string.Join(',', GetClients())}] }}";

    public Enumerator GetEnumerator()
        => new (value);

    IEnumerator<PlayerSlot> IEnumerable<PlayerSlot>.GetEnumerator()
        => new Enumerator(value);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(value);

    public struct Enumerator : IEnumerator<PlayerSlot>
    {
        private ulong      _remaining;
        private PlayerSlot _current;

        public Enumerator(ulong value)
        {
            _remaining = value;
            _current   = default;
        }

        public PlayerSlot  Current => _current;
        object IEnumerator.Current => _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_remaining == 0)
            {
                return false;
            }

            var bit = BitOperations.TrailingZeroCount(_remaining);
            _current   =  new PlayerSlot((byte) bit);
            _remaining &= _remaining - 1;

            return true;
        }

        public void Reset()
        {
            throw new System.NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
