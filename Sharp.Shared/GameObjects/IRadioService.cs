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

using Sharp.Shared.Attributes;
using Sharp.Shared.CStrike;

namespace Sharp.Shared.GameObjects;

/// <summary>
///     RadioService is CCSPlayerPawn only
/// </summary>
[NetClass("CCSPlayer_RadioServices")]
public interface IRadioService : IPlayerPawnComponent
{
    /// <summary>
    ///     m_flGotHostageTalkTimer
    /// </summary>
    float GotHostageTalkTimer { get; set; }

    /// <summary>
    ///     m_flDefusingTalkTimer
    /// </summary>
    float DefusingTalkTimer { get; set; }

    /// <summary>
    ///     m_flC4PlantTalkTimer
    /// </summary>
    float C4PlantTalkTimer { get; set; }

    /// <summary>
    ///     m_bIgnoreRadio
    /// </summary>
    bool IgnoreRadio { get; set; }

    /// <summary>
    ///     m_flRadioTokenSlots
    /// </summary>
    ISchemaArray<float> GetRadioTokenSlots();
}
