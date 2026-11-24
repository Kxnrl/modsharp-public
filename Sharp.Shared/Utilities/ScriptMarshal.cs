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
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Shared.Utilities;

public static unsafe class ScriptMarshal
{
    [ThreadStatic]
    private static nint _stringBuffer;

    [ThreadStatic]
    private static int _stringBufferSize;

    private const int InitialBufferSize = 1024;

    /// <summary>
    ///     Encodes <paramref name="value" /> as a null-terminated UTF-8 string in a reused thread-local unmanaged buffer.
    /// </summary>
    /// <remarks>
    ///     The returned pointer is overwritten by the next <see cref="Encode" /> or <see cref="EncodeString" /> call on the same thread.
    ///     Consume it immediately and never free or cache the pointer.
    /// </remarks>
    public static byte* Encode(string value)
    {
        // Grow the reused thread-local buffer to fit; never silently truncate a valid string.
        var needed = NativeString.GetByteCount(value) + 1;
        if (_stringBuffer == nint.Zero || needed > _stringBufferSize)
        {
            var size = Math.Max(needed, InitialBufferSize);

            // Allocate BEFORE freeing/committing: if Alloc throws (OOM), the old buffer and size stay valid
            // and consistent instead of leaving _stringBuffer dangling at an inflated _stringBufferSize
            // (which the next call would then write into as freed memory).
            var buffer = (nint) NativeMemory.Alloc((nuint) size);

            if (_stringBuffer != nint.Zero)
            {
                NativeMemory.Free((void*) _stringBuffer);
            }

            _stringBuffer     = buffer;
            _stringBufferSize = size;
        }

        NativeString.WriteString((byte*) _stringBuffer, _stringBufferSize, value);

        return (byte*) _stringBuffer;
    }

    /// <summary>
    ///     Creates a <see cref="ScriptValue" /> pointing to <paramref name="value" /> encoded in the reused thread-local buffer.
    /// </summary>
    /// <remarks>
    ///     The underlying pointer is invalidated by the next <see cref="Encode" /> or <see cref="EncodeString" /> call on the same thread;
    ///     consume it immediately.
    /// </remarks>
    public static ScriptValue EncodeString(string value)
        => new () { Type = ScriptValueType.String, String = Encode(value) };
}
