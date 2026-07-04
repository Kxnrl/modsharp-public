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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

/// <summary>
///     Per-client networked-field (send-prop) value override. Register a field on an entity with a callback;
///     the callback decides, per recipient, what value that client sees for the field — the real server value
///     is never changed.
///     <para>Supported value kinds: <b>int, float, bool, qangle, string</b> (byte-array is not supported).</para>
///     <para>Remove your hooks in <c>Shutdown</c> with <see cref="Unhook" />/<see cref="UnhookEntity" />; a
///     module's hooks are also dropped automatically if it unloads.</para>
/// </summary>
public interface ISendProxyManager
{
    /// <summary>
    ///     Fires once per tick for a proxied (entity, field), before it is written to any client. Fill
    ///     <paramref name="batch" /> with the value each client should see (only for the clients you want to fake);
    ///     the real value is never changed on the server, and clients you don't set receive it unchanged.
    ///     <para>
    ///         <b>Runs on the main thread inside the per-client encode.</b> It must be fast and read-only: do not
    ///         remove/spawn entities, kick clients, send net messages, or otherwise mutate engine state from it.
    ///     </para>
    /// </summary>
    public delegate void DelegateSendProxyBatch(IBaseEntity entity, SendProxyBatch batch);

    /// <summary>
    ///     Register <paramref name="callback" /> for a field on a single entity. It fires once per tick while
    ///     that field is networked. <paramref name="field" /> is the network field name (e.g. <c>m_iHealth</c>).
    ///     <para>The callback only fires when the entity is actually encoded into a client's delta. Entities that
    ///     rarely change (e.g. <c>prop_dynamic</c>) are not re-encoded until a value changes, so a passive hook on
    ///     one may never fire — proxy fields that update every tick (pawns, etc.), or mark the field dirty each
    ///     tick yourself to force the entity into the delta.</para>
    /// </summary>
    void Hook(IBaseEntity entity, string field, DelegateSendProxyBatch callback);

    /// <summary>Remove a field hook from an entity.</summary>
    void Unhook(IBaseEntity entity, string field);

    /// <summary>Remove every field hook on an entity. Also done automatically when the entity is deleted.</summary>
    void UnhookEntity(IBaseEntity entity);
}
