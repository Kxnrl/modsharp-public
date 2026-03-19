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

#include "address.h"

#include "cstrike/interface/IGameRules.h"

static bool s_bBypassTerminateRoundHook;

bool IGameRules::PlayerCanHearChat(CCSPlayerController* pReceiverController, CCSPlayerController* pSenderController, bool bTeamOnly)
{
    return address::server::CCSGameRules_PlayerCanHearChat(this, pReceiverController, pSenderController, bTeamOnly);
}

void IGameRules::TerminateRound(float flDelay, uint32_t iReason, bool bypassHook, TeamRewardInfo* reward_info, uint32_t reward_size)
{
    s_bBypassTerminateRoundHook = bypassHook;
    CUtlVector<TeamRewardInfo> rewards(reward_info, reward_size, reward_size);

    address::server::CCSGameRules_TerminateRound(this, flDelay, iReason, &rewards);
    s_bBypassTerminateRoundHook = false;
}

bool IGameRules::IsBypassHook()
{
    return s_bBypassTerminateRoundHook;
}