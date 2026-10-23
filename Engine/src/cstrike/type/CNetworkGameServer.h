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

#ifndef CSTRIKE_INTERFACE_ISERVER_H
#define CSTRIKE_INTERFACE_ISERVER_H

#include "definitions.h"
#include "gamedata.h"
#include "global.h"

#include "cstrike/cstrike.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/CUtlVector.h"

class CServerSideClient;

class CNetworkGameServer : public CStrikeObject
{
public:
    enum State_t : int32_t
    {
        Dead,
        WaitingForGameSessionManifest,
        Loading,
        Active,
        Paused
    };

    State_t GetState() const
    {
        static auto offset = g_pGameData->GetOffset("CNetworkGameServer::m_ServerState");
        return GetFieldValue<State_t>(offset);
    }

    const char* GetMapName();

    const char* GetAddonName();
    void        SetAddonName(const char* pAddons);

    CServerSideClient* GetClient(PlayerSlot_t slot) const;
    CServerSideClient* GetClientSafety(PlayerSlot_t slot) const;
    CServerSideClient* GetClientByUserId(UserId_t userId) const;
    CServerSideClient* GetClientBySteamId(SteamId_t steamId) const;

    CUtlVector<CServerSideClient*>* GetClients() const;

    void PrintChannelTeam(HudPrint_t dest, CStrikeTeam_t team, const char* message) const;
    void PrintChannelAll(HudPrint_t dest, const char* message) const;

    void ForceFullUpdateAll() const;

    CGlobalVars* GetGlobalVars();
};

#endif
