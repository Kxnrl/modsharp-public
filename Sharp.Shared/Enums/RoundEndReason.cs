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

namespace Sharp.Shared.Enums;

public enum RoundEndReason : uint
{
    /// <summary>
    ///     Game is still in progress
    /// </summary>
    RoundEndReasonStillInProgress = 0,

    /// <summary>
    ///     Bomb exploded
    /// </summary>
    TargetBombed = 1,

    /// <summary>
    ///     VIP escaped
    /// </summary>
    [Obsolete("Valve removed it in 1.4.1.8, will be removed in 2.2", true)]
    VipEscaped = 2,

    /// <summary>
    ///     VIP was killed
    /// </summary>
    [Obsolete("Valve removed it in 1.4.1.8, will be removed in 2.2", true)]
    VipAssassinated = 3,

    /// <summary>
    ///     Terrorists escaped
    /// </summary>
    TerroristsEscaped = 4,

    /// <summary>
    ///     CTs prevented terrorist escape
    /// </summary>
    CTsPreventEscape = 5,

    /// <summary>
    ///     EscapingTerroristsNeutralized
    /// </summary>
    EscapingTerroristsNeutralized = 6,

    /// <summary>
    ///     Bomb defused
    /// </summary>
    BombDefused = 7,

    /// <summary>
    ///     CTs win by default
    /// </summary>
    CTsWin = 8,

    /// <summary>
    ///     Terrorists win by default
    /// </summary>
    TerroristsWin = 9,

    /// <summary>
    ///     Round draw
    /// </summary>
    RoundDraw = 10,

    /// <summary>
    ///     All hostages rescued
    /// </summary>
    AllHostagesRescued = 11,

    /// <summary>
    ///     Bomb site not destroyed
    /// </summary>
    TargetSaved = 12,

    /// <summary>
    ///     Hostages not rescued
    /// </summary>
    HostagesNotRescued = 13,

    /// <summary>
    ///     Terrorists did not escape
    /// </summary>
    TerroristsNotEscaped = 14,

    /// <summary>
    ///     VIP did not escape
    /// </summary>
    [Obsolete("Valve removed it in 1.4.1.8, will be removed in 2.2", true)]
    VipNotEscaped = 15,

    /// <summary>
    ///     Game restarting
    /// </summary>
    GameCommencing = 16,

    /// <summary>
    ///     Terrorists surrendered
    /// </summary>
    TerroristsSurrender = 17,

    /// <summary>
    ///     CTs surrendered
    /// </summary>
    CTsSurrender = 18,

    /// <summary>
    ///     Terrorists planted the bomb
    /// </summary>
    TerroristsPlanted = 19,

    /// <summary>
    ///     CTs reached the hostage
    /// </summary>
    CTsReachedHostage = 20,

    /// <summary>
    ///     Survival win
    /// </summary>
    SurvivalWin = 21,

    /// <summary>
    ///     Survival draw
    /// </summary>
    SurvivalDraw = 22,
}
