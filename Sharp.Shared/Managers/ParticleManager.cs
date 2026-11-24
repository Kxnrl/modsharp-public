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

public interface IParticleManager
{
    /// <summary>
    ///     Dispatch particle effect (<c>.vpcf</c>) at a world position.
    /// </summary>
    /// <remarks>
    ///     Must be called after <see cref="Listeners.IGameListener.OnServerActivate" /> (before that, the particle effect index is uninitialized and sent as <c>0</c>).
    ///     The <paramref name="particle" /> path is normalized (<c>\</c> to <c>/</c>, lowercased ASCII, truncated to 199 UTF-8 bytes; empty paths or paths starting with <c>/</c> resolve to resource ID <c>0</c>).
    ///     Sent via <c>CMsgTEEffectDispatch</c> bypassing <see cref="IHookManager.PostEventAbstract" />.
    /// </remarks>
    void DispatchParticleEffect(string particle, Vector origin, Vector angles, RecipientFilter filter = default);

    /// <summary>
    ///     Dispatch particle effect (<c>.vpcf</c>) associated with an entity.
    /// </summary>
    /// <remarks>
    ///     Must be called after <see cref="Listeners.IGameListener.OnServerActivate" />.
    ///     See <see cref="DispatchParticleEffect(string, Vector, Vector, RecipientFilter)" /> for path normalization and delivery notes.
    /// </remarks>
    void DispatchParticleEffect(string particle,
        IBaseEntity                    entity,
        Vector                         origin,
        Vector                         angles,
        bool                           resetEntity = false,
        RecipientFilter                filter      = default);

    /// <summary>
    ///     Dispatch particle effect (<c>.vpcf</c>) attached to an entity.
    /// </summary>
    /// <remarks>
    ///     Must be called after <see cref="Listeners.IGameListener.OnServerActivate" />.
    ///     See <see cref="DispatchParticleEffect(string, Vector, Vector, RecipientFilter)" /> for path normalization and delivery notes.
    /// </remarks>
    void DispatchParticleEffect(string particle,
        ParticleAttachmentType         attachType,
        IBaseEntity                    entity,
        byte                           attachmentIndex = 0,
        bool                           resetEntity     = false,
        RecipientFilter                filter          = default);

    /// <summary>
    ///     Dispatch named effects (<c>csshocked</c>, <c>cstaserrope</c>, <c>gunshotsplash</c>, <c>csblood</c>, etc.).
    /// </summary>
    /// <remarks>
    ///     Must be called after <see cref="Listeners.IGameListener.OnServerActivate" />; calling earlier throws <see cref="System.NullReferenceException" />
    ///     because the <c>EffectDispatch</c> string table is not yet initialized.
    ///     Sent via <c>CMsgTEEffectDispatch</c> bypassing <see cref="IHookManager.PostEventAbstract" />.
    /// </remarks>
    void DispatchEffect(string effectName,
        IBaseEntity            entity,
        Vector                 origin,
        RecipientFilter        filter = default);
}
