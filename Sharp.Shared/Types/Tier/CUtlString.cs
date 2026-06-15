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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using Sharp.Shared.Utilities;

namespace Sharp.Shared.Types.Tier;

// when use in schema generator, it will be auto convert into string
[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
public unsafe struct CUtlString : IDisposable
{
    private byte* _pString;

    public CUtlString(string str)
    {
        SetString(str);
    }

    public CUtlString(byte* str)
        => _pString = str;

    public static implicit operator CUtlString(byte* pString)
        => new (pString);

    public string Get()
        => NativeString.ReadString(_pString);

    public ReadOnlySpan<byte> AsReadOnlySpan()
        => _pString == null ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(_pString, Length());

    public Span<byte> AsSpan()
        => _pString == null ? Span<byte>.Empty : new Span<byte>(_pString, Length());

    public void Dispose()
    {
        UtlString_Purge(Self());
        _pString = null;
    }

    public void SetString(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            UtlString_SetDirect(Self(), null, 0);

            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(str) + 1;

        const int stackAllocThreshold = 256;

        var allocateOnHeap = byteCount > stackAllocThreshold;

        var utf8Bytes = stackalloc byte[stackAllocThreshold];

        if (allocateOnHeap)
        {
            utf8Bytes = (byte*) NativeMemory.Alloc((nuint) byteCount);
        }

        try
        {
            var span = new Span<byte>(utf8Bytes, byteCount);

            Utf8.FromUtf16(str, span, out _, out var bytesWritten);

            UtlString_SetDirect(Self(), utf8Bytes, bytesWritten);
        }
        finally
        {
            if (allocateOnHeap)
            {
                NativeMemory.Free(utf8Bytes);
            }
        }
    }

    private void* Self()
        => Unsafe.AsPointer(ref this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Length()
    {
        if (_pString == null)
        {
            return 0;
        }

        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(_pString).Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty()
        => _pString == null || _pString[0] == 0;

    [DllImport("modsharp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UtlString_Set(void* pThis, byte* str);

    [DllImport("modsharp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UtlString_SetDirect(void* pThis, byte* str, int length);

    [DllImport("modsharp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UtlString_Purge(void* pThis);
}
