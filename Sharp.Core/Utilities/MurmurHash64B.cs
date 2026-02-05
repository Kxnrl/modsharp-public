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
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp.Core.Utilities;

internal static class MurmurHash64B
{
    private const uint M = 0x5bd1e995;
    private const int  R = 24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Compute(ReadOnlySpan<byte> source, uint seed = 0)
    {
        var length = source.Length;

        unchecked
        {
            var  h1 = seed ^ (uint) length;
            uint h2 = 0;

            ref var current = ref MemoryMarshal.GetReference(source);

            var remainder = length;

            while (remainder >= 8)
            {
                var k1 = BinaryPrimitives.ReadUInt32LittleEndian(MemoryMarshal.CreateReadOnlySpan(ref current, 4));

                current = ref Unsafe.Add(ref current, 4);

                k1 *= M;
                k1 ^= k1 >> R;
                k1 *= M;
                h1 *= M;
                h1 ^= k1;

                var k2 = BinaryPrimitives.ReadUInt32LittleEndian(MemoryMarshal.CreateReadOnlySpan(ref current, 4));

                current = ref Unsafe.Add(ref current, 4);

                k2 *= M;
                k2 ^= k2 >> R;
                k2 *= M;
                h2 *= M;
                h2 ^= k2;

                remainder -= 8;
            }

            if (remainder >= 4)
            {
                var k1 = BinaryPrimitives.ReadUInt32LittleEndian(MemoryMarshal.CreateReadOnlySpan(ref current, 4));

                current = ref Unsafe.Add(ref current, 4);

                k1 *= M;
                k1 ^= k1 >> R;
                k1 *= M;
                h1 *= M;
                h1 ^= k1;

                remainder -= 4;
            }

            if (remainder > 0)
            {
                switch (remainder)
                {
                    case 3:
                        h2 ^= (uint) Unsafe.Add(ref current, 2) << 16;
                        goto case 2;
                    case 2:
                        h2 ^= (uint) Unsafe.Add(ref current, 1) << 8;
                        goto case 1;
                    case 1:
                        h2 ^= current;
                        h2 *= M;

                        break;
                }
            }

            h1 ^= h2 >> 18;
            h1 *= M;
            h2 ^= h1 >> 22;
            h2 *= M;
            h1 ^= h2 >> 17;
            h1 *= M;
            h2 ^= h1 >> 19;
            h2 *= M;

            return ((ulong) h1 << 32) | h2;
        }
    }

    public static ulong Compute(string source, uint seed = 0)
    {
        if (string.IsNullOrEmpty(source))
        {
            return Compute(ReadOnlySpan<byte>.Empty, seed);
        }

        var maxBytes = Encoding.UTF8.GetMaxByteCount(source.Length);

        if (maxBytes <= 256)
        {
            Span<byte> buffer       = stackalloc byte[maxBytes];
            var        bytesWritten = Encoding.UTF8.GetBytes(source.AsSpan(), buffer);

            return Compute(buffer[..bytesWritten], seed);
        }

        var poolArray = ArrayPool<byte>.Shared.Rent(maxBytes);

        try
        {
            var bytesWritten = Encoding.UTF8.GetBytes(source.AsSpan(), poolArray);

            return Compute(new ReadOnlySpan<byte>(poolArray, 0, bytesWritten), seed);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(poolArray);
        }
    }
}
