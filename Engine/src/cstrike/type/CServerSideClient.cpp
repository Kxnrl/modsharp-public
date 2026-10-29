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

#include "gamedata.h"
#include "netmessage.h"
#include "sdkproxy.h"
#include "vhook/call.h"

#include "cstrike/entity/PlayerController.h"
#include "cstrike/interface/IGameRules.h"
#include "cstrike/interface/INetChannel.h"
#include "cstrike/interface/IRecipientFilter.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/CServerSideClient.h"

void CServerSideClient::ConsolePrint(const char* message) const
{
    if (GetSignonState() != SIGNONSTATE_FULL)
        return;

    UTIL_PrintSingle(GetSlot(), message);
}

void CServerSideClient::SetName(const char* pszName)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CServerSideClient::SetName");
    CALL_VIRTUAL(void, offset, this, pszName);
    UpdateUserSettings();
}

void CServerSideClient::UpdateUserSettings()
{
    static auto offset = g_pGameData->GetVFunctionIndex("CServerSideClient::UpdateUserSettings");
    CALL_VIRTUAL(void, offset, this);
}

bool CServerSideClient::SayChatMessage(bool teamOnly, const char* message) const
{
    const auto nSlot             = GetSlot();
    const auto pSenderController = reinterpret_cast<CCSPlayerController*>(CBasePlayerController::FindBySlot(nSlot));
    if (pSenderController == nullptr)
        return false;

    AssertPtr(gameRules);

    const auto clients = sv->GetClients();

    CBroadcastRecipientFilter filter(true);

    for (int i = clients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = clients->Element(i);
        if (pClient == nullptr || !pClient->IsInGame() || pClient->IsFakeClient() || pClient->GetSlot() == nSlot)
            continue;

        const auto pReceivedController = reinterpret_cast<CCSPlayerController*>(CBasePlayerController::FindBySlot(pClient->GetSlot()));
        if (pReceivedController == nullptr)
            continue;

        if (!gameRules->PlayerCanHearChat(pReceivedController, pSenderController, teamOnly))
            continue;

        if (!pReceivedController->CanHearAndReadChatFrom(pSenderController))
            continue;

        filter.AddRecipient(pClient->GetSlot());
    }

    filter.AddRecipient(nSlot);
    UTIL_SayText2Filter(&filter, true, pSenderController->GetEntityIndex(), message);

    return true;
}

void CServerSideClient::SetLoadedSpawnGroups(const CUtlVector<SpawnGroup_t>& groups) const
{
    static auto offset = g_pGameData->GetOffset("CServerSideClient::m_vecLoadedSpawnGroups");

    auto& vec = *reinterpret_cast<CUtlVector<SpawnGroup_t>*>(reinterpret_cast<uintptr_t>(this) + offset);

    if (vec.Count() == groups.Count())
        return;

    vec = groups;
}

/* NetChan */
[[nodiscard]] const char* CServerSideClient::GetNetWorkAddress() const
{
    if (const auto net = GetNetChannel())
        return net->GetNetWorkAddress();
    return nullptr;
}

[[nodiscard]] float CServerSideClient::GetTimeConnected() const
{
    if (const auto net = GetNetChannel())
        return net->GetTimeConnected();

    if (IsFakeClient())
        return static_cast<float>(Plat_FloatTime());

    return 0.0f;
}