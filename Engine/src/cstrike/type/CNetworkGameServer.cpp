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

#include "global.h"
#include "netmessage.h"
#include "vhook/call.h"

#include "cstrike/entity/PlayerController.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"

#include <algorithm>

const char* CNetworkGameServer::GetMapName()
{
    DeclareVFuncIndex(CNetworkGameServer, GetMapName, offset);
    return VCall_Manual(offset, const char*, this);
}

const char* CNetworkGameServer::GetAddonName()
{
    DeclareVFuncIndex(CNetworkGameServer, GetAddonName, offset);
    return VCall_Manual(offset, const char*, this);
}

void CNetworkGameServer::SetAddonName(const char* pAddons)
{
    static auto offset = g_pGameData->GetOffset("CNetworkGameServer::m_Addons");
    GetFieldPointer<CUtlString*>(offset)->Set(pAddons ? pAddons : "");
}

CUtlVector<CServerSideClient*>* CNetworkGameServer::GetClients() const
{
    static auto offset = g_pGameData->GetOffset("CNetworkGameServer::m_vecClients");
    return GetFieldPointer<CUtlVector<CServerSideClient*>*>(offset);
}

void CNetworkGameServer::PrintChannelTeam(HudPrint_t dest, CStrikeTeam_t team, const char* message) const
{
    const auto        clients    = GetClients();
    NetworkReceiver_t nReceivers = 0;
    int32_t           bitWide    = 0;

    for (int i = clients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = clients->Element(i);
        if (pClient == nullptr || !pClient->IsInGame() || pClient->IsFakeClient())
            continue;

        const auto pController = CBasePlayerController::FindBySlot(pClient->GetSlot());
        if (pController == nullptr || pController->GetTeam() != team)
            continue;

        nReceivers |= BASE_RECEIVER_MAGIC << pClient->GetSlot();

        bitWide = std::max(bitWide, i);
    }
    if (nReceivers > 0)
        bitWide++;
    UTIL_TextMsgFilter(bitWide, nReceivers, dest, message);
}

void CNetworkGameServer::PrintChannelAll(HudPrint_t dest, const char* message) const
{
    const auto clients = GetClients();

    NetworkReceiver_t nReceivers = 0;
    int32_t           bitWide    = 0;

    for (int i = clients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = clients->Element(i);
        if (pClient == nullptr || !pClient->IsInGame() || pClient->IsFakeClient())
            continue;

        nReceivers |= BASE_RECEIVER_MAGIC << pClient->GetSlot();

        bitWide = std::max(bitWide, i);
    }
    if (nReceivers > 0)
        bitWide++;
    UTIL_TextMsgFilter(bitWide, nReceivers, dest, message);
}

void CNetworkGameServer::ForceFullUpdateAll() const
{
    const auto clients = GetClients();

    for (int i = clients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = clients->Element(i);
        if (pClient == nullptr || pClient->IsFakeClient() || pClient->GetSignonState() < CServerSideClient::SIGNONSTATE_NEW)
            continue;

        pClient->ForceFullUpdate();
    }
}

CGlobalVars* CNetworkGameServer::GetGlobalVars()
{
    DeclareVFuncIndex(CNetworkGameServer, GetGlobalVars, offset);
    return VCall_Manual(offset, CGlobalVars*, this);
}

CServerSideClient* CNetworkGameServer::GetClient(PlayerSlot_t slot) const
{
    AssertBool((slot < CS_MAX_PLAYERS));
    const auto clients = GetClients();
    return clients->Element(slot);
}

CServerSideClient* CNetworkGameServer::GetClientSafety(PlayerSlot_t slot) const
{
    AssertBool((slot < CS_MAX_PLAYERS));
    const auto clients = GetClients();
    if (slot >= clients->Count())
        return nullptr;
    return clients->Element(slot);
}

CServerSideClient* CNetworkGameServer::GetClientByUserId(UserId_t userId) const
{
    const auto clients = GetClients();

    for (int i = clients->Count() - 1; i >= 0; i--)
    {
        if (const auto client = clients->Element(i); client->GetUserId() == userId)
            return client;
    }

    return nullptr;
}

CServerSideClient* CNetworkGameServer::GetClientBySteamId(SteamId_t steamId) const
{
    const auto clients = GetClients();

    for (int i = clients->Count() - 1; i >= 0; i--)
    {
        if (const auto client = clients->Element(i); client->GetSteamId() == steamId)
            return client;
    }

    return nullptr;
}
