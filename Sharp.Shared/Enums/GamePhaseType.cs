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

namespace Sharp.Shared.Enums;

public enum GamePhase
{
    /// <summary>
    ///     热身
    /// </summary>
    WarmupRound,

    /// <summary>
    ///     不分上下半场的标准游戏 <br />
    ///     <remarks>多用于<b>mp_maxround</b>为0时的游戏</remarks>
    /// </summary>
    PlayingStandard,

    /// <summary>
    ///     上半场
    /// </summary>
    PlayingFirstHalf,

    /// <summary>
    ///     下半场
    /// </summary>
    PlayingSecondHalf,

    /// <summary>
    ///     半场休息时间
    /// </summary>
    Halftime,

    /// <summary>
    ///     游戏结束Intermission时间
    /// </summary>
    MatchEnded,
}
