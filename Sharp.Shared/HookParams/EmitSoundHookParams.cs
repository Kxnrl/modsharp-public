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

using System;
using Sharp.Shared.Enums;
using Sharp.Shared.Units;

namespace Sharp.Shared.HookParams;

public interface IEmitSoundHookParams : IFunctionParams
{
    EntityIndex     EntityIndex { get; }
    string          SoundName   { get; }
    float           Volume      { get; }
    NetworkReceiver Receivers   { get; }
    bool            Changed     { get; }

    [Obsolete("Valve removed channel at 1.41.1.3, we will remove it in 2.1")]
    SoundChannel Channel { get; }

    void SetSoundName(string soundName);

    [Obsolete("Valve removed channel at 1.41.1.3, we will remove it in 2.1")]
    void SetChannel(SoundChannel channel);

    void SetVolume(float volume);

    int GetReceiverCount();

    bool HasReceiver(PlayerSlot slot);

    void AddReceiver(PlayerSlot slot);

    void RemoveReceiver(PlayerSlot slot);

    void UpdateReceiver(NetworkReceiver newReceivers);
}
