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
    ///     游戏还在进行
    /// </summary>
    RoundEndReasonStillInProgress = 0,

    /// <summary>
    ///     包炸了
    /// </summary>
    TargetBombed = 1,

    /// <summary>
    ///     Vip润了
    /// </summary>
    [Obsolete("Valve removed it in 1.4.1.8, will be removed in 2.2", true)]
    VipEscaped = 2,

    /// <summary>
    ///     Vip嗝屁了
    /// </summary>
    [Obsolete("Valve removed it in 1.4.1.8, will be removed in 2.2", true)]
    VipAssassinated = 3,

    /// <summary>
    ///     TE跑路了
    /// </summary>
    TerroristsEscaped = 4,

    /// <summary>
    ///     CT阻止TE跑路
    /// </summary>
    CTsPreventEscape = 5,

    /// <summary>
    ///     EscapingTerroristsNeutralized
    /// </summary>
    EscapingTerroristsNeutralized = 6,

    /// <summary>
    ///     包拆了
    /// </summary>
    BombDefused = 7,

    /// <summary>
    ///     CT默认胜利
    /// </summary>
    CTsWin = 8,

    /// <summary>
    ///     TE默认胜利
    /// </summary>
    TerroristsWin = 9,

    /// <summary>
    ///     平局
    /// </summary>
    RoundDraw = 10,

    /// <summary>
    ///     人质跑路了额
    /// </summary>
    AllHostagesRescued = 11,

    /// <summary>
    ///     包点没炸
    /// </summary>
    TargetSaved = 12,

    /// <summary>
    ///     人质没救出来
    /// </summary>
    HostagesNotRescued = 13,

    /// <summary>
    ///     TE没跑掉
    /// </summary>
    TerroristsNotEscaped = 14,

    /// <summary>
    ///     Vip没跑掉
    /// </summary>
    [Obsolete("Valve removed it in 1.4.1.8, will be removed in 2.2", true)]
    VipNotEscaped = 15,

    /// <summary>
    ///     游戏重启
    /// </summary>
    GameCommencing = 16,

    /// <summary>
    ///     TE投降了
    /// </summary>
    TerroristsSurrender = 17,

    /// <summary>
    ///     CT投降了
    /// </summary>
    CTsSurrender = 18,

    /// <summary>
    ///     TE下包了
    /// </summary>
    TerroristsPlanted = 19,

    /// <summary>
    ///     CT救出人质了
    /// </summary>
    CTsReachedHostage = 20,

    /// <summary>
    ///     存活胜利
    /// </summary>
    SurvivalWin = 21,

    /// <summary>
    ///     存活平局
    /// </summary>
    SurvivalDraw = 22,
}
