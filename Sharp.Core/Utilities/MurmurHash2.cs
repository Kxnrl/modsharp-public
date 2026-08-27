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
using System.Runtime.CompilerServices;
using System.Text;

namespace Sharp.Core.Utilities;

// 32-bit MurmurHash2, byte-for-byte identical to the native MurmurHash2 in Engine/src/murmurhash.h so a value
// hashed here matches one hashed there (used to route SendProxy callbacks by field-path hash across the boundary).
internal static class MurmurHash2
{
    public const uint Seed = 0x31415926;

    private const uint M = 0x5bd1e995;
    private const int  R = 24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(ReadOnlySpan<byte> source, uint seed)
    {
        var len = source.Length;

        unchecked
        {
            var h = seed ^ (uint) len;
            var i = 0;

            while (len >= 4)
            {
                var k = source[i] | ((uint) source[i + 1] << 8) | ((uint) source[i + 2] << 16) | ((uint) source[i + 3] << 24);

                k *= M;
                k ^= k >> R;
                k *= M;
                h *= M;
                h ^= k;

                i   += 4;
                len -= 4;
            }

            switch (len)
            {
                case 3:
                    h ^= (uint) source[i + 2] << 16;
                    goto case 2;
                case 2:
                    h ^= (uint) source[i + 1] << 8;
                    goto case 1;
                case 1:
                    h ^= source[i];
                    h *= M;
                    break;
            }

            h ^= h >> 13;
            h *= M;
            h ^= h >> 15;

            return h;
        }
    }

    public static uint Compute(string source)
        => Compute(Encoding.UTF8.GetBytes(source), Seed);
}
