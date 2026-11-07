/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
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
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Core.Objects;

internal partial class GameRules : SchemaObject, IGameRules
{
    private readonly GlobalVars _globalVars;
    private readonly ConVar?    _cvarTimeLimit;

    private GameRules(nint ptr) : base(ptr)
    {
        _globalVars = GlobalVars.Create(Bridges.Natives.Core.GetGlobals())
                      ?? throw new EntryPointNotFoundException("Failed to get global variables");

        _cvarTimeLimit = ConVar.Create(Cvar.FindConVar("mp_timelimit", false));
    }

    public void RestartGame()
    {
        IsWarmupPeriod   = false;
        IsMatchStarted   = false;
        RestartRoundTime = 1f;
        IsCompleteReset  = true;
        IsGameRestart    = true;
    }

    public unsafe void TerminateRound(float delay,
        RoundEndReason                      reason,
        bool                                bypassHook = false,
        TeamRewardInfo[]?                   info       = null)
    {
        CheckDisposed();

        if (info is null)
        {
            Game.TerminateRound(delay, reason, bypassHook, null, 0);
        }
        else
        {
            fixed (TeamRewardInfo* pInfo = info)
            {
                Game.TerminateRound(delay, reason, bypassHook, pInfo, info.Length);
            }
        }
    }

    public float GetRoundRemainingTime()
        => (RoundStartTime + RoundTime) - _globalVars.CurTime;

    public float GetRoundElapsedTime()
        => _globalVars.CurTime - RoundStartTime;

    public float GetMapRemainingTime()
    {
        if (_cvarTimeLimit == null)
        {
            return -1;
        }

        var flTimeLimit = _cvarTimeLimit.GetFloat();

        if (flTimeLimit <= 0.0f)
        {
            return -1;
        }

        var flTimeLeft = (GameStartTime + (flTimeLimit * 60.0f)) - _globalVars.CurTime;

        return float.Max(flTimeLeft, 0.0f);
    }

#region Schemas

    [NativeSchemaField("CCSGameRules", "m_iMaxNumTerrorists", typeof(int), Validate = true)]
    private partial SchemaField GetMaxNumTEsField();

    [NativeSchemaField("CCSGameRules", "m_iMaxNumCTs", typeof(int), Validate = true)]
    private partial SchemaField GetMaxNumCTsField();

    [NativeSchemaField("CCSGameRules", "m_bSwitchingTeamsAtRoundReset", typeof(bool), Validate = true)]
    private partial SchemaField GetSwitchingTeamsAtRoundResetField();

    [NativeSchemaField("CCSGameRules", "m_bForceTeamChangeSilent", typeof(bool), Validate = true)]
    private partial SchemaField GetForceTeamChangeSilentField();

    [NativeSchemaField("CCSGameRules", "m_iNumCT", typeof(int), Validate = true)]
    private partial SchemaField GetNumCTField();

    [NativeSchemaField("CCSGameRules", "m_iNumTerrorist", typeof(int), Validate = true)]
    private partial SchemaField GetNumTEField();

    [NativeSchemaField("CCSGameRules", "m_nCTTimeOuts", typeof(int), Validate = true)]
    private partial SchemaField GetCTTimeOutsField();

    [NativeSchemaField("CCSGameRules", "m_nTerroristTimeOuts", typeof(int), Validate = true)]
    private partial SchemaField GetTETimeOutsField();

    [NativeSchemaField("CCSGameRules", "m_bFreezePeriod", typeof(bool), Validate = true)]
    private partial SchemaField GetIsFreezePeriodField();

    [NativeSchemaField("CCSGameRules", "m_bWarmupPeriod", typeof(bool), Validate = true)]
    private partial SchemaField GetIsWarmupPeriodField();

    [NativeSchemaField("CGameRules", "m_bGamePaused", typeof(bool), Validate = true)]
    private partial SchemaField GetIsGamePausedField();

    [NativeSchemaField("CCSGameRules", "m_bTeamIntroPeriod", typeof(bool), Validate = true)]
    private partial SchemaField GetIsTeamIntroPeriodField();

    [NativeSchemaField("CCSGameRules", "m_gamePhase", typeof(GamePhase), Validate = true)]
    private partial SchemaField GetGamePhaseField();

    [NativeSchemaField("CCSGameRules", "m_iRoundTime", typeof(int), Validate = true)]
    private partial SchemaField GetRoundTimeField();

    [NativeSchemaField("CCSGameRules", "m_totalRoundsPlayed", typeof(int), Validate = true)]
    private partial SchemaField GetTotalRoundsPlayedField();

    [NativeSchemaField("CCSGameRules", "m_flGameStartTime", typeof(float), Validate = true)]
    private partial SchemaField GetGameStartTimeField();

    [NativeSchemaField("CCSGameRules", "m_fRoundStartTime", typeof(float), Validate = true)]
    private partial SchemaField GetRoundStartTimeField();

    [NativeSchemaField("CCSGameRules", "m_flRestartRoundTime", typeof(float), Validate = true)]
    private partial SchemaField GetRestartRoundTimeField();

    [NativeSchemaField("CCSGameRules", "m_bMatchWaitingForResume", typeof(bool), Validate = true)]
    private partial SchemaField GetMatchWaitingForResumeField();

    [NativeSchemaField("CCSGameRules", "m_bHasMatchStarted", typeof(bool), Validate = true)]
    private partial SchemaField GetIsMatchStartedField();

    [NativeSchemaField("CCSGameRules", "m_bForceTeamChangeSilent", typeof(bool), Validate = true)]
    private partial SchemaField GetIsForceTeamChangeSilentField();

    [NativeSchemaField("CCSGameRules", "m_bIsValveDS", typeof(bool), Validate = true)]
    private partial SchemaField GetIsValveDSField();

    [NativeSchemaField("CCSGameRules", "m_bPlayedTeamIntroVO", typeof(bool), Validate = true)]
    private partial SchemaField GetPlayedTeamIntroVOField();

    [NativeSchemaField("CCSGameRules", "m_nRoundsPlayedThisPhase", typeof(int), Validate = true)]
    private partial SchemaField GetRoundsPlayedThisPhaseField();

    [NativeSchemaField("CCSGameRules", "m_eRoundWinReason", typeof(RoundEndReason), Validate = true)]
    private partial SchemaField GetRoundWinReasonField();

    [NativeSchemaField("CCSGameRules", "m_iRoundWinStatus", typeof(CStrikeTeam), Validate = true)]
    private partial SchemaField GetRoundWinStatusField();

    [NativeSchemaField("CCSGameRules", "m_bGameRestart", typeof(bool), Validate = true)]
    private partial SchemaField GetIsGameRestartField();

    [NativeSchemaField("CCSGameRules", "m_bCompleteReset", typeof(bool), Validate = true)]
    private partial SchemaField GetIsCompleteResetField();

    [NativeSchemaField("CCSGameRules", "m_bMapHasRescueZone", typeof(bool), Validate = true)]
    private partial SchemaField GetMapHasRescueZoneField();

    [NativeSchemaField("CCSGameRules", "m_nEndMatchMapGroupVoteTypes", typeof(int[]), Validate = true)]
    private partial SchemaField GetEndMatchMapGroupVoteTypesField();

    [NativeSchemaField("CCSGameRules", "m_nEndMatchMapGroupVoteOptions", typeof(int[]), Validate = true)]
    private partial SchemaField GetEndMatchMapGroupVoteOptionsField();

#endregion

    public override string GetSchemaClassname()
        => "CCSGameRules";
}
