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
using Sharp.Shared.Types;

namespace Sharp.Shared.Utilities;

public static class Utils
{
    public static unsafe string ReadString(sbyte* ptr)
        => Marshal.PtrToStringUTF8((nint) ptr) ?? string.Empty;

    public static unsafe string ReadString(byte* ptr)
        => Marshal.PtrToStringUTF8((nint) ptr) ?? string.Empty;

    public static unsafe void WriteString(sbyte* ptr, int size, string value)
    {
        var bytes        = Encoding.Default.GetBytes(value).AsSpan();
        var realSize     = size - 1;
        var lengthToCopy = bytes.Length >= realSize ? realSize : bytes.Length;
        var targetSpan   = new Span<byte>(ptr, size);
        bytes[..lengthToCopy].CopyTo(targetSpan);

        targetSpan[lengthToCopy] = 0;
    }

    public static unsafe string ReadStringUtf8(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return ReadString((sbyte*) targetAddress.ToPointer());
    }

    public static unsafe void WriteStringUtf8(this nint ptr, int offset, string value, int maxlen)
    {
        var targetAddress = nint.Add(ptr, offset);

        WriteStringUtf8((char*) targetAddress.ToPointer(), maxlen, value);
    }

    public static unsafe void WriteStringUtf8(char* ptr, int size, string value)
    {
        var chars = Encoding.UTF8.GetBytes(value);

#if USE_MARSHARL
        Marshal.Copy(chars, 0, (IntPtr) ptr, chars.Length > size ? size : chars.Length);
#else
        var realSize     = size - 1;
        var lengthToCopy = chars.Length >= realSize ? realSize : chars.Length;

        fixed (byte* sourcePtr = chars)
        {
            Buffer.MemoryCopy(sourcePtr, (byte*) ptr, size, lengthToCopy);
            ((byte*) ptr)[lengthToCopy] = 0;
        }
#endif
    }

    public static unsafe float GetFloat(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(float*) targetAddress.ToPointer();
    }

    public static unsafe void WriteFloat(this nint ptr, int offset, float value)
    {
        var targetAddress = (float*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static unsafe double GetDouble(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(double*) targetAddress.ToPointer();
    }

    public static unsafe void WriteDouble(this nint ptr, int offset, double value)
    {
        var targetAddress = (double*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static unsafe long GetInt64(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(long*) targetAddress.ToPointer();
    }

    public static unsafe void WriteInt64(this nint ptr, int offset, long value)
    {
        var targetAddress = (long*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static unsafe ulong GetUInt64(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(ulong*) targetAddress.ToPointer();
    }

    public static unsafe void WriteUInt64(this nint ptr, int offset, ulong value)
    {
        var targetAddress = (ulong*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static unsafe int GetInt32(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(int*) targetAddress.ToPointer();
    }

    public static unsafe void WriteInt32(this nint ptr, int offset, int value)
    {
        var targetAddress = (int*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static unsafe uint GetUInt32(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(uint*) targetAddress.ToPointer();
    }

    public static unsafe void WriteUInt32(this nint ptr, int offset, uint value)
    {
        var targetAddress = (uint*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static unsafe short GetInt16(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(short*) targetAddress.ToPointer();
    }

    public static unsafe void WriteInt16(this nint ptr, int offset, short value)
    {
        var targetAddress = (short*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static unsafe ushort GetUInt16(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(ushort*) targetAddress.ToPointer();
    }

    public static unsafe void WriteUInt16(this nint ptr, int offset, ushort value)
    {
        var targetAddress = (ushort*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static bool GetBool(this nint ptr, int offset)
        => ptr.GetByte(offset) != 0;

    public static void WriteBool(this nint ptr, int offset, bool value)
        => ptr.WriteByte(offset, (byte) (value ? 1 : 0));

    public static unsafe byte GetByte(this nint ptr, int offset)
    {
        var targetAddress = nint.Add(ptr, offset);

        return *(byte*) targetAddress.ToPointer();
    }

    public static unsafe void WriteByte(this nint ptr, int offset, byte value)
    {
        var targetAddress = (byte*) nint.Add(ptr, offset).ToPointer();
        *targetAddress = value;
    }

    public static string GetString(this nint ptr, int offset)
        => Marshal.PtrToStringUTF8(nint.Add(ptr, offset)) ?? string.Empty;

    public static unsafe string GetPtrString(this nint ptr, int offset)
    {
        var strPtr = *(nint*) nint.Add(ptr, offset).ToPointer();

        return Marshal.PtrToStringUTF8(strPtr) ?? string.Empty;
    }

    public static unsafe string GetObjectVarString(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();
        var varPtr = *(byte**) (objPtr + varOffset);

        return Marshal.PtrToStringUTF8((nint) varPtr) ?? string.Empty;
    }

    public static unsafe bool GetObjectVarBool(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetBool(varOffset);
    }

    public static unsafe float GetObjectVarFloat(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetFloat(varOffset);
    }

    public static unsafe double GetObjectVarDouble(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetDouble(varOffset);
    }

    public static unsafe short GetObjectVarInt16(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetInt16(varOffset);
    }

    public static unsafe ushort GetObjectVarUInt16(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetUInt16(varOffset);
    }

    public static unsafe int GetObjectVarInt32(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetInt32(varOffset);
    }

    public static unsafe uint GetObjectVarUInt32(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetUInt32(varOffset);
    }

    public static unsafe long GetObjectVarInt64(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetInt64(varOffset);
    }

    public static unsafe ulong GetObjectVarUInt64(this nint @this, int objectOffset, int varOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr.GetUInt64(varOffset);
    }

    public static unsafe nint GetObjectPtr(this nint @this, int objectOffset)
    {
        var objPtr = *(nint*) (@this + objectOffset).ToPointer();

        return objPtr;
    }

    public static unsafe nint GetVirtualMethod(this nint @this, int offset)
    {
        var pVTable = *(void***) @this;

        var method = pVTable[offset];

        return new nint(method);
    }

    public static unsafe void WritePtr(this nint @this, int offset, nint ptr)
    {
        var targetAddress = (void**) nint.Add(@this, offset).ToPointer();
        *targetAddress = ptr.ToPointer();
    }

    public static unsafe Vector GetVector(this nint ptr, int offset)
        => *(Vector*) IntPtr.Add(ptr, offset);

    public static unsafe void WriteVector(this nint ptr, int offset, Vector vector)
        => *(Vector*) IntPtr.Add(ptr, offset) = vector;

    public static unsafe Color GetColor(this nint ptr, int offset)
        => *(Color*) IntPtr.Add(ptr, offset);

    public static unsafe void SetColor(this nint ptr, int offset, Color value)
        => *(Color*) IntPtr.Add(ptr, offset) = value;
}
