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

namespace Sharp.Shared.GameEntities;

[NetClass("CCSTeam")]
public interface IBaseTeam : IBaseEntity
{
    /// <summary>
    ///     Score
    /// </summary>
    int Score { get; set; }

    /// <summary>
    /// Score of FirstHalf
    /// </summary>
    int ScoreFirstHalf { get; set; }

    /// <summary>
    /// Score of SecondHalf
    /// </summary>
    int ScoreSecondHalf { get; set; }

    /// <summary>
    /// Score of Overtime
    /// </summary>
    int ScoreOvertime { get; set; }

    /// <summary>
    /// m_iLastUpdateSentAt
    /// </summary>
    int LastUpdateSentAt { get; set; }

    /// <summary>
    /// m_flNextResourceTime
    /// </summary>
    float NextResourceTime { get; set; }
}
