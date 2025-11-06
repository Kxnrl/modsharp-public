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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnitGenerator;

namespace Sharp.Shared.Units;

[UnitOf<ulong>(UnitGenerateOptions.ImplicitOperator)]
public partial struct NetworkReceiver : IEnumerable<PlayerSlot>
{
    private const ulong BaseMagic = 1UL;

    public NetworkReceiver(IEnumerable<PlayerSlot> players)
        => value = players.Aggregate<PlayerSlot, ulong>(0, (current, player) => current | (BaseMagic << player.AsPrimitive()));

    public NetworkReceiver(IReadOnlyCollection<PlayerSlot> players)
        => value = players.Aggregate<PlayerSlot, ulong>(0, (current, player) => current | (BaseMagic << player.AsPrimitive()));

    public bool IsEmpty()
        => value == 0;

    public bool IsFull()
        => value == ulong.MaxValue;

    public int Count()
        => BitOperations.PopCount(value);

    public bool HasClient(PlayerSlot slot)
        => (value & (BaseMagic << slot.AsPrimitive())) != 0;

    public NetworkReceiver Append(PlayerSlot slot)
        => new (value | (BaseMagic << slot.AsPrimitive()));

    public NetworkReceiver Remove(PlayerSlot slot)
        => new (value & ~(BaseMagic << slot.AsPrimitive()));

    public IEnumerable<PlayerSlot> GetClients()
    {
        for (PlayerSlot i = 0; i <= PlayerSlot.MaxPlayerSlot; i++)
        {
            if (HasClient(i))
            {
                yield return new PlayerSlot(i);
            }
        }
    }

    public PlayerSlot[] GetClientsArray()
        => Enumerable.Range(0, PlayerSlot.MaxPlayerCount.AsPrimitive())
                     .Select(i => new PlayerSlot((byte) i))
                     .Where(HasClient)
                     .ToArray();

    public string DestructureTransform()
        => $"{{ \"Value\": {value}, \"Receivers\": [{string.Join(',', GetClients())}] }}";

    public Enumerator GetEnumerator()
        => new (value);

    IEnumerator<PlayerSlot> IEnumerable<PlayerSlot>.GetEnumerator()
        => new Enumerator(value);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(value);

    public struct Enumerator : IEnumerator<PlayerSlot>, IEnumerator
    {
        private readonly ulong      _value;
        private          int        _currentIndex;
        private          PlayerSlot _current;

        public Enumerator(ulong value)
        {
            _value        = value;
            _currentIndex = -1;
            _current      = default;
        }

        public PlayerSlot  Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            var max = PlayerSlot.MaxPlayerSlot.AsPrimitive();

            while (++_currentIndex <= max)
            {
                var slot = new PlayerSlot((byte) _currentIndex);

                if ((_value & (BaseMagic << slot.AsPrimitive())) != 0)
                {
                    _current = slot;

                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _currentIndex = -1;
            _current      = default;
        }

        public void Dispose()
        {
        }
    }
}
