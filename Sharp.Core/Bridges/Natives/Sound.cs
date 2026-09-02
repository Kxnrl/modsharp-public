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

using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Sound
{
    public static partial float GetSoundDuration(string sound);

    public static partial long StartSoundEvent(nint entity, string sound, float?* volume, RecipientFilter* filter);

    public static partial void StopSoundEvent(SoundOpEventGuid* guid);

    public static partial void StopSoundEventFilter(SoundOpEventGuid* guid, RecipientFilter* filter);

    public static partial bool SetSoundEventParamFloat(long guid, string param, float value, RecipientFilter* filter);

    public static partial bool SetSoundEventParamFloatHash(long guid, string param, float value, RecipientFilter* filter);

    public static partial bool SetSoundEventParamVector(long guid, string param, Vector* value, RecipientFilter* filter);

    public static partial bool SetSoundEventParamInt32(long guid, string param, int value, RecipientFilter* filter);

    public static partial bool SetSoundEventParamUInt32(long guid, string param, uint value, RecipientFilter* filter);

    public static partial bool SetSoundEventParamUInt64(long guid, string param, ulong value, RecipientFilter* filter);

    public static partial bool IsSoundEventValid(string soundEvent);
}
