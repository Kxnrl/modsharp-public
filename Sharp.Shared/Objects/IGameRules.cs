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

using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Objects;

public interface IGameRules : ISchemaObject
{
    /// <summary>
    ///     当前CT阵营人数
    /// </summary>
    int NumCT { get; }

    /// <summary>
    ///     当前TE阵营人数
    /// </summary>
    int NumTE { get; }

    /// <summary>
    ///     CT阵营剩余暂停
    /// </summary>
    int CTTimeOuts { get; }

    /// <summary>
    ///     TE阵营剩余暂停
    /// </summary>
    int TETimeOuts { get; }

    /// <summary>
    ///     TE阵营最大允许的人数
    /// </summary>
    int MaxNumTEs { get; set; }

    /// <summary>
    ///     CT阵营允许的最大人数
    /// </summary>
    int MaxNumCTs { get; set; }

    /// <summary>
    ///     在回合交替时交换队伍
    /// </summary>
    bool SwitchingTeamsAtRoundReset { get; set; }

    /// <summary>
    ///     不发消息的交换队伍
    /// </summary>
    bool ForceTeamChangeSilent { get; set; }

    /// <summary>
    ///     游戏进程
    /// </summary>
    GamePhase GamePhase { get; }

    /// <summary>
    ///     冻结阶段
    /// </summary>
    bool IsFreezePeriod { get; }

    /// <summary>
    ///     热身阶段
    /// </summary>
    bool IsWarmupPeriod { get; }

    /// <summary>
    ///     游戏暂停
    /// </summary>
    bool IsGamePaused { get; }

    /// <summary>
    ///     Team Intro
    /// </summary>
    bool IsTeamIntroPeriod { get; }

    /// <summary>
    ///     回合时间
    /// </summary>
    int RoundTime { get; set; }

    /// <summary>
    ///     游戏开始时间
    /// </summary>
    float GameStartTime { get; set; }

    /// <summary>
    ///     回合开始时间 (基于CurTime)
    /// </summary>
    float RoundStartTime { get; set; }

    /// <summary>
    ///     比赛是否暂停
    /// </summary>
    bool MatchWaitingForResume { get; set; }

    /// <summary>
    ///     回合重置时间 (基于CurTime)
    /// </summary>
    float RestartRoundTime { get; }

    /// <summary>
    ///     已经游玩的回合数
    /// </summary>
    int TotalRoundsPlayed { get; }

    /// <summary>
    ///     比赛开始
    /// </summary>
    bool IsMatchStarted { get; }

    /// <summary>
    ///     选择队伍时隐藏提示
    /// </summary>
    bool IsForceTeamChangeSilent { get; }

    /// <summary>
    ///     官方服务器?
    /// </summary>
    bool IsValveDS { get; set; }

    /// <summary>
    ///     m_bGameRestart
    /// </summary>
    bool IsGameRestart { get; set; }

    /// <summary>
    ///     重启游戏
    /// </summary>
    void RestartGame();

    /// <summary>
    ///     当前Phase已经游玩的回合数
    /// </summary>
    int RoundsPlayedThisPhase { get; set; }

    /// <summary>
    ///     是否已经播放过开场队伍POSE
    /// </summary>
    bool PlayedTeamIntroVO { get; set; }

    /// <summary>
    ///     回合结束原因
    /// </summary>
    RoundEndReason RoundWinReason { get; }

    /// <summary>
    ///     回合胜利者
    /// </summary>
    CStrikeTeam RoundWinStatus { get; }

    /// <summary>
    ///     结束回合
    /// </summary>
    void TerminateRound(float delay, RoundEndReason reason, bool bypassHook = false, TeamRewardInfo[]? info = null);

    /// <summary>
    ///     获取回合剩余时间
    /// </summary>
    float GetRoundRemainingTime();

    /// <summary>
    ///     获取回合进行的时间
    /// </summary>
    float GetRoundElapsedTime();

    /// <summary>
    ///     获取地图剩余时间<br />
    ///     <remarks>
    ///         &lt; 0 = * No Time Limit *<br />
    ///         = 0 =* Last Round *<br />
    ///         &gt; 0 = Time Remaining
    ///     </remarks>
    /// </summary>
    float GetMapRemainingTime();

    /// <summary>
    ///     m_nEndMatchMapGroupVoteTypes
    /// </summary>
    /// <returns></returns>
    ISchemaArray<int> GetEndMatchMapGroupVoteTypes();

    /// <summary>
    ///     m_nEndMatchMapGroupVoteOptions
    /// </summary>
    /// <returns></returns>
    ISchemaArray<int> GetEndMatchMapGroupVoteOptions();
}
