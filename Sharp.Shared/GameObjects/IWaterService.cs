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
using Sharp.Shared.Types;

namespace Sharp.Shared.GameObjects;

/// <summary>
///     WaterService is CCSPlayerPawn only
/// </summary>
[NetClass("CCSPlayer_WaterServices")]
public interface IWaterService : IPlayerPawnComponent
{
    /// <summary>
    ///     m_NextDrownDamageTime
    /// </summary>
    float NextDrownDamageTime { get; set; }

    /// <summary>
    ///     m_nDrownDmgRate
    /// </summary>
    int DrownDmgRate { get; set; }

    /// <summary>
    ///     m_AirFinishedTime
    /// </summary>
    float AirFinishedTime { get; set; }

    /// <summary>
    ///     m_flWaterJumpTime
    /// </summary>
    float WaterJumpTime { get; set; }

    /// <summary>
    ///     m_vecWaterJumpVel
    /// </summary>
    Vector WaterJumpVel { get; set; }

    /// <summary>
    ///     m_flSwimSoundTime
    /// </summary>
    float SwimSoundTime { get; set; }
}
