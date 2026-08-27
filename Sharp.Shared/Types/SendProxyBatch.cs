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

using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Shared.Types;

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

    /// <summary>
    ///     Framework use only — created by the manager around a native batch table.
    /// </summary>
    public SendProxyBatch(nint batch, SendProxyValueKind kind)
    {
        _batch = batch;
        _kind  = kind;
    }

    /// <summary>
    ///     The value kind this field's encoder expects.
    /// </summary>
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

    /// <summary>
    ///     Set an integer value for one client.
    /// </summary>
    public void SetFor(IGameClient client, long value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetInt(value);
        }
    }

    /// <summary>
    ///     Set a boolean value for one client.
    /// </summary>
    public void SetFor(IGameClient client, bool value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetBool(value);
        }
    }

    /// <summary>
    ///     Set a float value for one client.
    /// </summary>
    public void SetFor(IGameClient client, float value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetFloat(value);
        }
    }

    /// <summary>
    ///     Set a vector/qangle value for one client.
    /// </summary>
    public void SetFor(IGameClient client, Vector value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetVector(value);
        }
    }

    /// <summary>
    ///     Set a string value for one client (e.g. a per-viewer player name).
    /// </summary>
    public void SetFor(IGameClient client, string value)
    {
        var v = Mark(client);
        if (v != null)
        {
            v->SetString(value);
        }
    }
}
