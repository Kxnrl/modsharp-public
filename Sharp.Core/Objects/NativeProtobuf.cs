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
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Shared.Objects;

namespace Sharp.Core.Objects;

internal partial class NativeProtobuf : NativeObject, INativeProtobuf
{
    private List<NativeProtobuf>? _chain;

    protected NativeProtobuf(nint pointer) : base(pointer)
    {
    }

    protected override void OnDisposed()
    {
        base.OnDisposed();

        if (_chain is { } chain)
        {
            foreach (var c in chain)
            {
                c.MarkAsDisposed();
            }
        }
    }

    public string GetName()
    {
        CheckDisposed();

        var ptr = Net.GetNetMessageName(_this);

        if (ptr == nint.Zero)
        {
            throw new InvalidOperationException("Missing Protobuf Name");
        }

        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    public ulong GetSize()
    {
        CheckDisposed();

        return Net.GetNetMessageSize(_this);
    }

    public bool HasField(string field)
    {
        CheckDisposed();

        return Net.NetMessageHasField(_this, field);
    }

    public int GetRepeatedFieldCount(string field)
    {
        CheckDisposed();

        return Net.NetMessageGetRepeatedCount(_this, field);
    }

    public int? ReadInt32(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageInt32(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public uint? ReadUInt32(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageUInt32(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public long? ReadInt64(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageInt64(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public ulong? ReadUInt64(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageUInt64(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public bool? ReadBool(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageBool(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public int? ReadEnum(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageEnum(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public float? ReadFloat(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageFloat(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public double? ReadDouble(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.ReadNetMessageDouble(_this, field, out var value, repeatedIndex) ? value : null;
    }

    public string? ReadString(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        var ptr = Net.ReadNetMessageString(_this, field, repeatedIndex);

        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    public byte[]? ReadBytes(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        if (!Net.ReadNetMessageBytes(_this, field, out var data, out var size, repeatedIndex))
        {
            return null;
        }

        var bytes = new byte[size];
        Marshal.Copy(data, bytes, 0, size);

        return bytes;
    }

    public bool SetInt32(string field, int value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageInt32(_this, field, value, repeatedIndex);
    }

    public bool SetUInt32(string field, uint value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageUInt32(_this, field, value, repeatedIndex);
    }

    public bool SetInt64(string field, long value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageInt64(_this, field, value, repeatedIndex);
    }

    public bool SetUInt64(string field, ulong value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageUInt64(_this, field, value, repeatedIndex);
    }

    public bool SetBool(string field, bool value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageBool(_this, field, value, repeatedIndex);
    }

    public bool SetEnum(string field, int value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageEnum(_this, field, value, repeatedIndex);
    }

    public bool SetFloat(string field, float value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageFloat(_this, field, value, repeatedIndex);
    }

    public bool SetDouble(string field, double value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageDouble(_this, field, value, repeatedIndex);
    }

    public bool SetString(string field, string value, int repeatedIndex = -1)
    {
        CheckDisposed();

        return Net.SetNetMessageString(_this, field, value, repeatedIndex);
    }

    public unsafe bool SetBytes(string field, byte[] value, int repeatedIndex = -1)
    {
        CheckDisposed();

        fixed (byte* p = value)
        {
            return Net.SetNetMessageBytes(_this, field, p, value.Length, repeatedIndex);
        }
    }

    public int AddInt32(string field, int value)
    {
        CheckDisposed();

        return Net.AddNetMessageInt32(_this, field, value);
    }

    public int AddUInt32(string field, uint value)
    {
        CheckDisposed();

        return Net.AddNetMessageUInt32(_this, field, value);
    }

    public int AddInt64(string field, long value)
    {
        CheckDisposed();

        return Net.AddNetMessageInt64(_this, field, value);
    }

    public int AddUInt64(string field, ulong value)
    {
        CheckDisposed();

        return Net.AddNetMessageUInt64(_this, field, value);
    }

    public int AddBool(string field, bool value)
    {
        CheckDisposed();

        return Net.AddNetMessageBool(_this, field, value);
    }

    public int AddEnum(string field, int value)
    {
        CheckDisposed();

        return Net.AddNetMessageEnum(_this, field, value);
    }

    public int AddFloat(string field, float value)
    {
        CheckDisposed();

        return Net.AddNetMessageFloat(_this, field, value);
    }

    public int AddDouble(string field, double value)
    {
        CheckDisposed();

        return Net.AddNetMessageDouble(_this, field, value);
    }

    public int AddString(string field, string value)
    {
        CheckDisposed();

        return Net.AddNetMessageString(_this, field, value);
    }

    public unsafe int AddBytes(string field, byte[] value)
    {
        CheckDisposed();

        fixed (byte* p = value)
        {
            return Net.AddNetMessageBytes(_this, field, p, value.Length);
        }
    }

    public INativeProtobuf? ReadMessage(string field, int repeatedIndex = -1)
    {
        CheckDisposed();

        var ptr = Net.ReadNetMessageMessage(_this, field, repeatedIndex);

        if (ptr == nint.Zero)
        {
            return null;
        }

        var msg = new NativeProtobuf(ptr);

        _chain ??= [];
        _chain.Add(msg);

        return msg;
    }

    public INativeProtobuf? AddMessage(string field)
    {
        CheckDisposed();

        var ptr = Net.AddNetMessageRepeated(_this, field);

        if (ptr == nint.Zero)
        {
            return null;
        }

        var msg = new NativeProtobuf(ptr);

        _chain ??= [];
        _chain.Add(msg);

        return msg;
    }

    public bool RemoveRepeatedFieldValue(string field, int repeatedIndex)
    {
        CheckDisposed();

        return Net.RemoveNetMessageRepeated(_this, field, repeatedIndex);
    }

    public unsafe bool CopyFromOtherMessage(IMessage message)
    {
        CheckDisposed();

        var data = message.ToByteArray();

        fixed (byte* p = data)
        {
            return Net.CopyFromOtherMessage(_this, p, data.Length);
        }
    }

    public unsafe T Deserialize<T>() where T : class, IMessage<T>, IMessage, new()
    {
        CheckDisposed();

        var name = GetName();
        var msg  = new T();

        if (!msg.Descriptor.Name.Equals(name))
        {
            throw new InvalidOperationException($"Message type mismatch, actual is {name}, but pass {msg.Descriptor.Name}");
        }

        var size = (int) GetSize();

        if (size is 0)
        {
            return msg;
        }

        if (size > 256)
        {
            // fallback big message to ArrayPool
            DeserializeFromPool(size, msg);
        }
        else
        {
            var buffer = stackalloc byte[size];
            var span   = new ReadOnlySpan<byte>(buffer, size);
            Net.SerializeNetMessage(_this, size, buffer);

            msg.MergeFrom(span);
        }

        return msg;
    }

    private unsafe void DeserializeFromPool<T>(int size, T msg) where T : class, IMessage<T>, IMessage, new()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            var span = buffer.AsSpan(0, size);

            fixed (byte* p = span)
            {
                Net.SerializeNetMessage(_this, size, p);
            }

            msg.MergeFrom(span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
