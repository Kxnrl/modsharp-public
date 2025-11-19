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

using Sharp.Shared.Units;

namespace Sharp.Shared.HookParams;

public interface IEmitSoundHookParams : IFunctionParams
{
    EntityIndex     EntityIndex { get; }
    string          SoundName   { get; }
    float           Volume      { get; }
    NetworkReceiver Receivers   { get; }
    bool            Changed     { get; }

    void SetSoundName(string soundName);

    void SetVolume(float volume);

    int GetReceiverCount();

    bool HasReceiver(PlayerSlot slot);

    void AddReceiver(PlayerSlot slot);

    void RemoveReceiver(PlayerSlot slot);

    void UpdateReceiver(NetworkReceiver newReceivers);
}
