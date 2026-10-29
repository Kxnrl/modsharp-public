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

#ifndef CSTRIKE_INTERFACE_ICLIENT_H
#define CSTRIKE_INTERFACE_ICLIENT_H

#include "gamedata.h"
#include "global.h"

#include "cstrike/cstrike.h"

#include <cstdint>

#define INTERFACEVERSION_ENGINECLIENT "Source2EngineToClient001"

class KeyValues;
class INetChannel;

class CServerSideClient : public CStrikeObject
{
public:
    enum SIGNONSTATE
    {
        SIGNONSTATE_NONE        = 0, // no state yet; about to connect
        SIGNONSTATE_CHALLENGE   = 1, // client challenging server; all OOB packets
        SIGNONSTATE_CONNECTED   = 2, // client is connected to server; netchans ready
        SIGNONSTATE_NEW         = 3, // just got serverinfo and string tables
        SIGNONSTATE_PRESPAWN    = 4, // received signon buffers
        SIGNONSTATE_SPAWN       = 5, // ready to receive entity packets
        SIGNONSTATE_FULL        = 6, // we are fully connected; first non-delta packet received
        SIGNONSTATE_CHANGELEVEL = 7, // server is changing level; please wait
    };

    [[nodiscard]] SIGNONSTATE GetSignonState() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_SignonState");
        return GetFieldValue<SIGNONSTATE>(offset);
    }
    void SetSignonState(const SIGNONSTATE value) const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_SignonState");
        SetFieldValue(offset, value);
    }
    [[nodiscard]] UserId_t GetUserId() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_UserId");
        return GetFieldValue<UserId_t>(offset);
    }
    [[nodiscard]] PlayerSlot_t GetSlot() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_Slot");
        return GetFieldValue<PlayerSlot_t>(offset);
    }
    [[nodiscard]] EntityIndex_t GetControllerEntityIndex() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_ControllerEntityIndex");
        return GetFieldValue<EntityIndex_t>(offset);
    }
    [[nodiscard]] SteamId_t GetSteamId() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_SteamId");
        return IsFakeClient() ? 0 : GetFieldValue<SteamId_t>(offset);
    }
    [[nodiscard]] uint32_t GetAccountId() const
    {
        return GetSteamId() & 0xFFFFFFFF;
    }
    [[nodiscard]] const char* GetName() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_Name");
        return GetFieldValue<char*>(offset);
    }
    [[nodiscard]] bool IsAuthenticated() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_FullyAuthenticated");
        return IsFakeClient() ? true : GetFieldValue<bool>(offset);
    }
    [[nodiscard]] bool IsFakeClient() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_FakeClient");
        return GetFieldValue<bool>(offset);
    }
    [[nodiscard]] INetChannel* GetNetChannel() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_NetChannel");
        return GetFieldValue<INetChannel*>(offset);
    }
    [[nodiscard]] bool       IsInGame() const { return GetSignonState() == SIGNONSTATE_FULL; }
    [[nodiscard]] bool       IsConnected() const { return GetSignonState() >= SIGNONSTATE_CONNECTED; }
    [[nodiscard]] KeyValues* GetConVars() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_ConVars");
        return GetFieldValue<KeyValues*>(offset);
    }
    [[nodiscard]] int32_t GetDeltaTick() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_nDeltaTick");
        return GetFieldValue<int32_t>(offset);
    }

    [[nodiscard]] bool IsHltv() const
    {
        static auto offset = g_pGameData->GetOffset("CServerSideClient::m_IsHLTV");
        return GetFieldValue<bool>(offset);
    }
    void ConsolePrint(const char* message) const;
    void SetName(const char* name);
    void UpdateUserSettings();
    bool SayChatMessage(bool teamOnly, const char* message) const;

    void ForceFullUpdate() const
    {
        static auto       offset = g_pGameData->GetOffset("CServerSideClient::m_nDeltaTick");
        constexpr int32_t value  = -1;
        SetFieldValue(offset, value);
    }

    void SetLoadedSpawnGroups(const CUtlVector<SpawnGroup_t>& groups) const;

    /* NetChan */
    [[nodiscard]] const char* GetNetWorkAddress() const;
    [[nodiscard]] float       GetTimeConnected() const;
};

#endif
