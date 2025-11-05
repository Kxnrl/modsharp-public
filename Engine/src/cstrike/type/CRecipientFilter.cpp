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

#include "global.h"
#include "logging.h"

#include "CoreCLR/RuntimeRecipientFilter.h"

#include "cstrike/consts.h"
#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/IRecipientFilter.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/CServerSideClient.h"

#include <algorithm>

void CBroadcastRecipientFilter::AddTeamPlayer(const CStrikeTeam_t team)
{
    const auto pClients = sv->GetClients();
    for (int i = pClients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = pClients->Element(i);
        if (!pClient->IsConnected() || pClient->IsFakeClient())
            continue;

        const auto pController = CBasePlayerController::FindBySlot(pClient->GetSlot());
        if (pController && pController->GetTeam() == team)
            AddRecipient(pClient->GetSlot());
    }
}

void CBroadcastRecipientFilter::AddAllPlayer()
{
    const auto pClients = sv->GetClients();
    for (int i = pClients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = pClients->Element(i);
        if (!pClient->IsConnected() || pClient->IsFakeClient())
            continue;

        AddRecipient(pClient->GetSlot());
    }
}

bool ParseNetworkReceivers(const RuntimeRecipientFilter* pFilter, int32_t* bits, NetworkReceiver_t* receivers)
{
    *receivers = 0;
    *bits      = 0;

    if (pFilter->Type == RuntimeRecipientFilterType::All)
    {
        const auto        clients    = sv->GetClients();
        NetworkReceiver_t nReceivers = 0;
        int32_t           bitWide    = 0;

        for (int i = clients->Count() - 1; i >= 0; i--)
        {
            const auto pClient = clients->Element(i);
            if (pClient == nullptr || !pClient->IsConnected() || pClient->IsFakeClient())
                continue;

            nReceivers |= BASE_RECEIVER_MAGIC << pClient->GetSlot();

            bitWide = std::max(bitWide, i);
        }
        if (nReceivers > 0)
            bitWide++;

        *receivers = nReceivers;
        *bits      = bitWide;

        return bitWide > 0;
    }

    if (pFilter->Type == RuntimeRecipientFilterType::Team)
    {
        const auto        clients    = sv->GetClients();
        NetworkReceiver_t nReceivers = 0;
        int32_t           bitWide    = 0;

        for (int i = clients->Count() - 1; i >= 0; i--)
        {
            const auto pClient = clients->Element(i);
            if (pClient == nullptr || !pClient->IsConnected() || pClient->IsFakeClient())
                continue;

            const auto pController = CBasePlayerController::FindBySlot(pClient->GetSlot());
            if (pController == nullptr || pController->GetTeam() != pFilter->Team)
                continue;

            nReceivers |= BASE_RECEIVER_MAGIC << pClient->GetSlot();

            bitWide = std::max(bitWide, i);
        }
        if (nReceivers > 0)
            bitWide++;

        *receivers = nReceivers;
        *bits      = bitWide;

        return bitWide > 0;
    }

    if (pFilter->Type == RuntimeRecipientFilterType::Players)
    {
        int32_t bitWide = 0;

        for (auto i = CS_MAX_PLAYERS - 1; i >= 0; i--)
        {
            if ((pFilter->Receivers & (BASE_RECEIVER_MAGIC << i)) != 0)
            {
                bitWide = i + 1;
                break;
            }
        }

        if (bitWide > 0)
        {
            *receivers = pFilter->Receivers;
            *bits      = bitWide;
            return true;
        }

        return false;
    }

    if (pFilter->Type == RuntimeRecipientFilterType::Single)
    {
        if (pFilter->Receivers >= CS_MAX_PLAYERS)
            return false;

        const auto pClient = sv->GetClientSafety(static_cast<PlayerSlot_t>(pFilter->Receivers));
        if (!pClient || !pClient->IsConnected() || pClient->IsFakeClient())
            return false;

        *receivers = BASE_RECEIVER_MAGIC << pFilter->Receivers;
        *bits      = static_cast<int32_t>(pFilter->Receivers) + 1;

        return true;
    }

    FatalError("Unknown recipient filter type %d", pFilter->Type);

    return false;
}