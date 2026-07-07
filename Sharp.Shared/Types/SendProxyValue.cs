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
using System.Text;

namespace Sharp.Shared.Types;

/// <summary>
///     Per-slot value one client receives for the proxied field. Mirrors the native SendProxyValue layout:
///     a union of the payloads — the batch-level kind decides which member the encoder reads.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 264)]
internal unsafe struct SendProxyValue
{
    private const int StrCap = 256;

    [FieldOffset(0)]
    private long _i;

    [FieldOffset(0)]
    private float _f;

    [FieldOffset(0)]
    private float _x;

    [FieldOffset(4)]
    private float _y;

    [FieldOffset(8)]
    private float _z;

    [FieldOffset(0)]
    private int _strLen;

    [FieldOffset(4)]
    private fixed byte _str[StrCap];

    public void SetInt(long value)    => _i = value;
    public void SetBool(bool value)   => _i = value ? 1 : 0;
    public void SetFloat(float value) => _f = value;

    public void SetVector(Vector value)
    {
        _x = value.X;
        _y = value.Y;
        _z = value.Z;
    }

    public void SetString(string value)
    {
        value ??= string.Empty;

        if (Encoding.UTF8.GetByteCount(value) > StrCap - 1)
        {
            value = value[..Math.Min(value.Length, (StrCap - 1) / 4)];
        }

        fixed (byte* p = _str)
        {
            var written = Encoding.UTF8.GetBytes(value, new Span<byte>(p, StrCap - 1));
            p[written] = 0;
            _strLen    = written;
        }
    }
}
