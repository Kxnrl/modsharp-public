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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

/// <summary>The value kind the encoder expects for the field being proxied.</summary>
public enum SendProxyValueKind
{
    Int    = 0,
    Float  = 1,
    Bool   = 2,
    Vector = 3,
    String = 4,
}

/// <summary>
///     Per-slot value one client receives for the proxied field. Mirrors the native SendProxyValue layout.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SendProxyValue
{
    private const int StrCap = 256;

    private int        _kind;
    private long       _i;
    private float      _f;
    private float      _x, _y, _z;
    private int        _strLen;
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

/// <summary>
///     Fills the per-recipient values for one (entity, field) in a tick. Set a value only for the clients that
///     should see a faked value with the method matching <see cref="Kind" />; clients you don't set receive the
///     real value. The callback runs once per tick per field, so iterate the clients you care about here.
///     <para><b>Valid only for the duration of the callback</b> — do not store it or use it afterwards.</para>
/// </summary>
public readonly unsafe struct SendProxyBatch
{
    // native FieldBatch layout: [int tick][int type][ulong hasMask][SendProxyValue values[64]]
    private const int MaxSlots     = 64;
    private const int HasMaskOffset = 8;
    private const int ValuesOffset  = 16;

    private readonly nint               _batch;
    private readonly SendProxyValueKind _kind;

    /// <summary>Framework use only — created by the manager around a native batch table.</summary>
    public SendProxyBatch(nint batch, SendProxyValueKind kind)
    {
        _batch = batch;
        _kind  = kind;
    }

    /// <summary>The value kind this field's encoder expects.</summary>
    public SendProxyValueKind Kind => _kind;

    private SendProxyValue* Mark(IGameClient client)
    {
        var slot = client.Slot.AsPrimitive();
        if (slot < 0 || slot >= MaxSlots)
        {
            return null;
        }

        *(ulong*) (_batch + HasMaskOffset) |= 1UL << slot;

        return (SendProxyValue*) (_batch + ValuesOffset + (nint) slot * sizeof(SendProxyValue));
    }

    /// <summary>Set an integer value for one client.</summary>
    public void SetFor(IGameClient client, long value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetInt(value);
        }
    }

    /// <summary>Set a boolean value for one client.</summary>
    public void SetFor(IGameClient client, bool value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetBool(value);
        }
    }

    /// <summary>Set a float value for one client.</summary>
    public void SetFor(IGameClient client, float value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetFloat(value);
        }
    }

    /// <summary>Set a vector/qangle value for one client.</summary>
    public void SetFor(IGameClient client, Vector value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetVector(value);
        }
    }

    /// <summary>Set a string value for one client (e.g. a per-viewer player name).</summary>
    public void SetFor(IGameClient client, string value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetString(value);
        }
    }
}

/// <summary>
///     Fires once per tick for a proxied (entity, field), before it is written to any client. Fill
///     <paramref name="batch" /> with the value each client should see (only for the clients you want to fake);
///     the real value is never changed on the server, and clients you don't set receive it unchanged.
///     <para>
///         <b>Runs on the main thread inside the per-client encode.</b> It must be fast and read-only: do not
///         remove/spawn entities, kick clients, send net messages, or otherwise mutate engine state from it.
///     </para>
/// </summary>
public delegate void SendProxyCallback(IBaseEntity entity, SendProxyBatch batch);

/// <summary>
///     Per-client networked-field (send-prop) value override. Register a field on an entity with a callback;
///     the callback decides, per recipient, what value that client sees for the field — the real server value
///     is never changed.
///     <para>Supported value kinds: <b>int, float, bool, qangle, string</b> (byte-array is not supported).</para>
///     <para>Remove your hooks in <c>Shutdown</c> with <see cref="Unhook" />/<see cref="UnhookEntity" />; a
///     module's hooks are also dropped automatically if it unloads. Currently <b>Linux only</b>.</para>
/// </summary>
public interface ISendProxyManager
{
    /// <summary>
    ///     Register <paramref name="callback" /> for a field on a single entity. It fires once per tick while
    ///     that field is networked. <paramref name="field" /> is the network field name (e.g. <c>m_iHealth</c>).
    /// </summary>
    void Hook(IBaseEntity entity, string field, SendProxyCallback callback);

    /// <summary>Remove a field hook from an entity.</summary>
    void Unhook(IBaseEntity entity, string field);

    /// <summary>Remove every field hook on an entity. Also done automatically when the entity is deleted.</summary>
    void UnhookEntity(IBaseEntity entity);
}
