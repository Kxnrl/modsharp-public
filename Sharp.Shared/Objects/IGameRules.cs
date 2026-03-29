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
    ///     Current number of CT players
    /// </summary>
    int NumCT { get; }

    /// <summary>
    ///     Current number of terrorist players
    /// </summary>
    int NumTE { get; }

    /// <summary>
    ///     Remaining timeouts for CT team
    /// </summary>
    int CTTimeOuts { get; }

    /// <summary>
    ///     Remaining timeouts for terrorist team
    /// </summary>
    int TETimeOuts { get; }

    /// <summary>
    ///     Maximum allowed terrorist players
    /// </summary>
    int MaxNumTEs { get; set; }

    /// <summary>
    ///     Maximum allowed CT players
    /// </summary>
    int MaxNumCTs { get; set; }

    /// <summary>
    ///     Switch teams at round reset
    /// </summary>
    bool SwitchingTeamsAtRoundReset { get; set; }

    /// <summary>
    ///     Force team change without message
    /// </summary>
    bool ForceTeamChangeSilent { get; set; }

    /// <summary>
    ///     Current game phase
    /// </summary>
    GamePhase GamePhase { get; }

    /// <summary>
    ///     Whether in freeze period
    /// </summary>
    bool IsFreezePeriod { get; }

    /// <summary>
    ///     Whether in warmup period
    /// </summary>
    bool IsWarmupPeriod { get; }

    /// <summary>
    ///     Whether game is paused
    /// </summary>
    bool IsGamePaused { get; }

    /// <summary>
    ///     Whether in team intro period
    /// </summary>
    bool IsTeamIntroPeriod { get; }

    /// <summary>
    ///     Round time in seconds
    /// </summary>
    int RoundTime { get; set; }

    /// <summary>
    ///     Game start time
    /// </summary>
    float GameStartTime { get; set; }

    /// <summary>
    ///     Round start time (based on CurTime)
    /// </summary>
    float RoundStartTime { get; set; }

    /// <summary>
    ///     Whether match is waiting for resume
    /// </summary>
    bool MatchWaitingForResume { get; set; }

    /// <summary>
    ///     Round restart time (based on CurTime)
    /// </summary>
    float RestartRoundTime { get; }

    /// <summary>
    ///     Total rounds played
    /// </summary>
    int TotalRoundsPlayed { get; }

    /// <summary>
    ///     Whether match has started
    /// </summary>
    bool IsMatchStarted { get; }

    /// <summary>
    ///     Hide team selection messages
    /// </summary>
    bool IsForceTeamChangeSilent { get; }

    /// <summary>
    ///     Whether this is a Valve dedicated server
    /// </summary>
    bool IsValveDS { get; set; }

    /// <summary>
    ///     Game restart flag (m_bGameRestart)
    /// </summary>
    bool IsGameRestart { get; set; }

    /// <summary>
    ///     Restart the game
    /// </summary>
    void RestartGame();

    /// <summary>
    ///     Rounds played in current phase
    /// </summary>
    int RoundsPlayedThisPhase { get; set; }

    /// <summary>
    ///     Whether team intro voice-over has been played
    /// </summary>
    bool PlayedTeamIntroVO { get; set; }

    /// <summary>
    ///     Reason for round end
    /// </summary>
    RoundEndReason RoundWinReason { get; }

    /// <summary>
    ///     Team that won the round
    /// </summary>
    CStrikeTeam RoundWinStatus { get; }

    /// <summary>
    ///     End the current round
    /// </summary>
    void TerminateRound(float delay, RoundEndReason reason, bool bypassHook = false, TeamRewardInfo[]? info = null);

    /// <summary>
    ///     Get remaining time in current round
    /// </summary>
    float GetRoundRemainingTime();

    /// <summary>
    ///     Get elapsed time in current round
    /// </summary>
    float GetRoundElapsedTime();

    /// <summary>
    ///     Get remaining time on map<br />
    ///     <remarks>
    ///         &lt; 0 = No Time Limit<br />
    ///         = 0 = Last Round<br />
    ///         &gt; 0 = Time Remaining
    ///     </remarks>
    /// </summary>
    float GetMapRemainingTime();

    /// <summary>
    ///     End match map group vote types (m_nEndMatchMapGroupVoteTypes)
    /// </summary>
    /// <returns></returns>
    ISchemaArray<int> GetEndMatchMapGroupVoteTypes();

    /// <summary>
    ///     End match map group vote options (m_nEndMatchMapGroupVoteOptions)
    /// </summary>
    /// <returns></returns>
    ISchemaArray<int> GetEndMatchMapGroupVoteOptions();
}
