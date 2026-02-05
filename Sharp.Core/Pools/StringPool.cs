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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp.Core.Pools;

internal class StringPool
{
    private static StringPool? _instance;

    public static StringPool Instance
        => _instance ?? throw new InvalidOperationException("You forgot to initialize string pool");

    private readonly Dictionary<string, CString> _pool;

    public static void Initialize(int capacity)
    {
        if (_instance is not null)
        {
            throw new InvalidOperationException("String pool is already initialized");
        }

        _instance = new StringPool(capacity);
    }

    public static void Shutdown()
    {
        if (_instance is null)
        {
            throw new InvalidOperationException("String pool is null");
        }

        unsafe
        {
            foreach (var (_, v) in _instance._pool)
            {
                NativeMemory.Free(v.Data);
            }
        }

        _instance = null;
    }

    private StringPool(int capacity)
        => _pool = new Dictionary<string, CString>(capacity);

    public unsafe byte* AllocPooledString(string str)
    {
        if (_pool.TryGetValue(str, out var pooled))
        {
            return pooled.Data;
        }

        var count = Encoding.UTF8.GetByteCount(str);

        var buffer  = (byte*) NativeMemory.Alloc((nuint) (count + 1));
        var dst     = new Span<byte>(buffer, count);
        var written = Encoding.UTF8.GetBytes(str.AsSpan(), dst);
        buffer[written] = 0;

        _pool[str] = new CString(buffer);

        return buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly unsafe struct CString
    {
        public readonly byte* Data;

        public CString(byte* data)
            => Data = data;

        public CString(nint data)
            => Data = (byte*) data;
    }
}
