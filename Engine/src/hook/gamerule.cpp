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

#include "CoreCLR/NativeSpan.h"
#include "bridge/forwards/forward.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "sdkproxy.h"

#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/IGameRules.h"
#include "cstrike/interface/IServerGameClient.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/CUtlMap.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/VProf.h"

#include <safetyhook.hpp>

static CConVarBaseData* ms_override_team_limit        = nullptr;
static CConVarBaseData* ms_fix_kick_cooldown          = nullptr;
static CConVarBaseData* sv_kick_players_with_cooldown = nullptr;

#pragma pack(push, 1)
struct CGcBanInformation_t
{
private:
    [[maybe_unused]] uint8_t m_padding0[4];

public:
    uint32_t m_uiReason;

private:
    [[maybe_unused]] uint8_t m_padding8[4];

public:
    double m_dblExpiration;

private:
    [[maybe_unused]] uint8_t m_padding14[8];

public:
    bool m_bHasCommunicationCooldown; // 0x1c

private:
    [[maybe_unused]] uint8_t m_padding1d[7];
};
#pragma pack(pop)
static_assert(sizeof(CGcBanInformation_t) == 0x24);

static CUtlMap<uint32_t, CGcBanInformation_t, int>* sm_mapGcBanInformation;

BeginMemberHookScope(CCSGameRules)
{
    DeclareMemberDetourHook(Constructor, void, (CCSGameRules * pGameRules))
    {
        gameRules = pGameRules;
        Constructor(pGameRules);
    }

    DeclareMemberDetourHook(TerminateRound, void, (CCSGameRules * pGameRules, float flDelay, uint32_t nReason, CUtlVector<TeamRewardInfo>* pRewardInfo))
    {
        if (CCSGameRules::IsBypassHook())
        {
            return TerminateRound(pGameRules, flDelay, nReason, pRewardInfo);
        }

        auto delay  = flDelay;
        auto reason = nReason;

        const auto nRewardSize = pRewardInfo ? pRewardInfo->Count() : 0;

        constexpr int CS_MAX_TEAM_REWARD_INFO = 8;

        TeamRewardInfo defaultArray[CS_MAX_TEAM_REWARD_INFO] = {};
        TeamRewardInfo changedArray[CS_MAX_TEAM_REWARD_INFO] = {};
        for (auto i = 0; i < CS_MAX_TEAM_REWARD_INFO && i < nRewardSize; ++i)
        {
            defaultArray[i] = pRewardInfo->Element(i);
            changedArray[i] = pRewardInfo->Element(i);
        }

        NativeFixedSpan defaultRewards(defaultArray, nRewardSize, CS_MAX_TEAM_REWARD_INFO);
        NativeFixedSpan changedRewards(changedArray, nRewardSize, CS_MAX_TEAM_REWARD_INFO);

        const auto action = forwards::OnTerminateRoundPre->Invoke(pGameRules, &delay, &reason, &changedRewards);

        if (action == EHookAction::SkipCallReturnOverride)
        {
            forwards::OnTerminateRoundPost->Invoke(pGameRules, flDelay, nReason, &defaultRewards, action);
            return;
        }

        if (action == EHookAction::ChangeParamReturnOverride || action == EHookAction::IgnoreParamReturnOverride)
        {
            FatalError("Not impl yet");
            return;
        }

        auto result = &defaultRewards;

        CUtlVector<TeamRewardInfo> tempRewardInfo;

        if (action == EHookAction::ChangeParamReturnDefault)
        {
            flDelay = delay;
            nReason = reason;

            auto* pTarget = pRewardInfo ? pRewardInfo : &tempRewardInfo;

            pTarget->RemoveAll();
            for (auto i = 0; i < changedRewards.m_nCount; ++i)
            {
                pTarget->AddToTail(changedRewards.m_pData[i]);
            }

            pRewardInfo = pTarget;
            result      = &changedRewards;
        }

        TerminateRound(pGameRules, flDelay, nReason, pRewardInfo);

        forwards::OnTerminateRoundPost->Invoke(pGameRules, flDelay, nReason, result, action);
    }

    DeclareMemberDetourHook(RestartRound, void, (CCSGameRules * pGameRules))
    {
        VPROF_MS_HOOK();

        g_pHookManager->Call_RoundRestart(HookType_Pre);
        forwards::OnRoundRestart->Invoke();

        RestartRound(pGameRules);

        g_pHookManager->Call_RoundRestart(HookType_Post);
        forwards::OnRoundRestarted->Invoke();
    }

    DeclareMemberDetourHook(WillTeamHaveRoomForPlayer, bool, (CCSGameRules * pGameRules, CCSPlayerController * pController, int32_t team))
    {
        if (ms_override_team_limit->GetValue<bool>())
            return true;

        return WillTeamHaveRoomForPlayer(pGameRules, pController, team);
    }

    DeclareMemberDetourHook(CreateEndMatchMapGroupVoteOptions, void, (CCSGameRules * pGameRules))
    {
        VPROF_MS_HOOK();

        CreateEndMatchMapGroupVoteOptions(pGameRules);

        forwards::OnMapVoteCreated->Invoke(pGameRules);
    }
}

BeginStaticHookScope(HandleGCBanInfo)
{
    constexpr int32_t KickModeConditionally = 1;
    constexpr int32_t KickModeEveryCooldown = 2;

    static void CheckClient(const CServerSideClient* pClient, const int32_t kickMode, const double flTime)
    {
        const auto playerAccountId = pClient->GetAccountId();

        for (int it = 0; it < sm_mapGcBanInformation->MaxElement(); ++it)
        {
            if (!sm_mapGcBanInformation->IsValidIndex(it))
                continue;

            const auto accountId = sm_mapGcBanInformation->Key(it);

            if (accountId != playerAccountId)
                continue;

            auto& value = sm_mapGcBanInformation->Element(it);

            const auto reason     = value.m_uiReason;
            const auto expireTime = value.m_dblExpiration;

            if (reason == 12 || reason == 13 || reason == 18)
                break;

            if (flTime > expireTime)
            {
                sm_mapGcBanInformation->RemoveAt(it);
                break;
            }

            auto shouldKick = true;

            if (kickMode == KickModeConditionally)
            {
                switch (reason)
                {
                case 8:
                case 10:
                case 11:
                case 14:
                case 15:
                case 19:
                case 20:
                    break;
                default:
                    shouldKick = reason - 22 <= 1;

                    break;
                }
            }

            if (!shouldKick)
                break;

            auto kickReason = IServerGameClient::ENetworkDisconnectionReason::NETWORK_DISCONNECT_KICKED_COMPETITIVECOOLDOWN;

            switch (reason)
            {
            case 8:
            case 14:
                kickReason = IServerGameClient::ENetworkDisconnectionReason::NETWORK_DISCONNECT_KICKED_UNTRUSTEDACCOUNT;
                break;

            case 10:
            case 11:
            case 19:
                kickReason = IServerGameClient::ENetworkDisconnectionReason::NETWORK_DISCONNECT_KICKED_CONVICTEDACCOUNT;
                break;
            case 15:
                kickReason = IServerGameClient::ENetworkDisconnectionReason::NETWORK_DISCONNECT_KICKED_NOSTEAMTICKET;
                break;
            default:
                break;
            }

            FLOG("Kick %s<%llu> due to GC<%u>", pClient->GetName(), pClient->GetSteamId(), reason);
            engine->KickClient(pClient->GetSlot(), "sv_kick_players_with_cooldown", kickReason);
            break;
        }
    }

    DeclareStaticDetourHook(HandleGCBanInfo, void, ())
    {
        if (!ms_fix_kick_cooldown->GetValue<bool>())
            return HandleGCBanInfo();

        const auto kickMode = sv_kick_players_with_cooldown->GetValue<int>();
        if (kickMode == 0)
            return;

        if (sm_mapGcBanInformation->Count() == 0)
            return;

        const auto flTime   = Plat_FloatTime();
        const auto pClients = sv->GetClients();

        for (auto i = pClients->Count() - 1; i >= 0; i--)
        {
            const auto pClient = pClients->Element(i);

            if (!pClient->IsConnected() || pClient->IsFakeClient())
                continue;

            CheckClient(pClient, kickMode, flTime);
        }
    }
}

void InstallGameRulesHooks()
{
    InstallMemberDetourAutoSig(CCSGameRules, Constructor);
    InstallMemberDetourAutoSig(CCSGameRules, RestartRound);
    InstallMemberDetourAutoSig(CCSGameRules, WillTeamHaveRoomForPlayer);
    InstallMemberDetourAutoSig(CCSGameRules, CreateEndMatchMapGroupVoteOptions);
    InstallMemberDetourAutoSig(CCSGameRules, TerminateRound);

    InstallStaticDetourAutoSig(HandleGCBanInfo);

    sm_mapGcBanInformation = g_pGameData->GetAddress<CUtlMap<uint32_t, CGcBanInformation_t, int>*>("CCSGameRules::sm_mapGcBanInformation");

    sv_kick_players_with_cooldown = icvar->FindConVarIterator("sv_kick_players_with_cooldown");

    ms_override_team_limit = g_ConVarManager.CreateConVar("ms_override_team_limit", false, "Override Team limit.", FCVAR_RELEASE);
    ms_fix_kick_cooldown   = g_ConVarManager.CreateConVar("ms_fix_kick_cooldown", true, "Fix GC cooldown reject connection.", FCVAR_RELEASE);
}

// RoundEnd 在热身时间结束时或Intermission后不触发
// 所以获取RoundEnd最保险为RestartRound

// The following round-related functions are called as follows:
//
// At Match Start:
//		PreRestartRound() -> RestartRound() -> PostRestartRound()
//
// During Subsequent Round Gameplay:
//		RoundWin() is called at the point when the winner of the round has been determined - prior to free-play commencing
//		PreRestartRound() is called with 1 second remaining prior to the round officially ending (This is after a round
//						  winner has been chosen and players are allowed to continue playing)
//		RoundEnd() is then called when the round has completely ended
//		RestartRound() is then called immediately after RoundEnd()
//		PostRestartRound() is called immediately after RestartRound() has completed