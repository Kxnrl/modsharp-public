/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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

using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

public interface ISoundManager
{
    /// <summary>
    ///     Gets the duration of a SoundEvent
    /// </summary>
    float GetSoundDuration(string soundEvent);

    /// <summary>
    ///     Starts a SoundEvent
    /// </summary>
    SoundOpEventGuid StartSoundEvent(string sound,
        IBaseEntity?                        entity = null,
        float?                              volume = null,
        RecipientFilter                     filter = default);

    /// <summary>
    ///     Stops a SoundEvent
    /// </summary>
    void StopSoundEvent(SoundOpEventGuid guid);

    /// <summary>
    ///     Stops a SoundEvent
    /// </summary>
    void StopSoundEvent(SoundOpEventGuid guid, RecipientFilter filter);

    /// <summary>
    ///     Sets a SoundEvent parameter
    /// </summary>
    bool SetSoundEventParam(SoundOpEventGuid guid, string param, float value, RecipientFilter filter = default);

    /// <summary>
    ///     Sets a SoundEvent parameter
    /// </summary>
    bool SetSoundEventParam(SoundOpEventGuid guid, string param, Vector value, RecipientFilter filter = default);

    /// <summary>
    ///     Sets a SoundEvent parameter
    /// </summary>
    bool SetSoundEventParam(SoundOpEventGuid guid, string param, int value, RecipientFilter filter = default);

    /// <summary>
    ///     Sets a SoundEvent parameter
    /// </summary>
    bool SetSoundEventParam(SoundOpEventGuid guid, string param, uint value, RecipientFilter filter = default);

    /// <summary>
    ///     Sets a SoundEvent parameter
    /// </summary>
    bool SetSoundEventParam(SoundOpEventGuid guid, string param, ulong value, RecipientFilter filter = default);

    /// <summary>
    ///     Checks if a SoundEvent is valid
    /// </summary>
    bool IsSoundEventValid(string soundEvent);
}
