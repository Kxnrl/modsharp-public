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
///     The value a <see cref="SendProxyCallback" /> writes back. <see cref="Kind" /> is pre-set to what the
///     field's encoder expects; the value itself defaults to 0/empty (the real value is not provided), so set it
///     with the method matching <see cref="Kind" /> before returning <c>true</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct SendProxyValue
{
    private const int StrCap = 256;

    private int         _kind;
    private long        _i;
    private float       _f;
    private float       _x, _y, _z;
    private int         _strLen;
    private fixed byte  _str[StrCap];

    /// <summary>The value kind the encoder expects for this field.</summary>
    public readonly SendProxyValueKind Kind => (SendProxyValueKind) _kind;

    public void SetInt(long value)    => _i = value;
    public void SetBool(bool value)   => _i = value ? 1 : 0;
    public void SetFloat(float value) => _f = value;

    public void SetVector(Vector value)
    {
        _x = value.X;
        _y = value.Y;
        _z = value.Z;
    }

    /// <summary>Set a string value (for name/string fields). Truncated if longer than 255 UTF-8 bytes.</summary>
    public void SetString(string value)
    {
        value ??= string.Empty;

        // Cap so the UTF-8 bytes fit the 255-byte buffer (UTF-8 is at most 4 bytes/char).
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
///     Invoked on the main thread while a networked field is written to a specific client, so the value that
///     client receives can differ from the real server value (and from what other clients receive). Set the
///     value via <paramref name="value" /> and return <c>true</c> to override; return <c>false</c> to send the
///     real value.
///     <para>
///         <b>The callback runs deep inside the per-client encode.</b> It must be fast and read-only: do not
///         remove/spawn entities, kick the client, send net messages, or otherwise mutate engine state from it.
///     </para>
/// </summary>
public delegate bool SendProxyCallback(IGameClient client, IBaseEntity entity, ref SendProxyValue value);

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
    ///     Register <paramref name="callback" /> for a field on a single entity. The callback fires per
    ///     recipient during the per-client encode. <paramref name="field" /> is the network field name
    ///     (e.g. <c>m_iHealth</c>).
    /// </summary>
    void Hook(IBaseEntity entity, string field, SendProxyCallback callback);

    /// <summary>Remove a field hook from an entity.</summary>
    void Unhook(IBaseEntity entity, string field);

    /// <summary>Remove every field hook on an entity. Also done automatically when the entity is deleted.</summary>
    void UnhookEntity(IBaseEntity entity);
}
