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
#include "gamefactory.h"
#include "global.h"
#include "logging.h"
#include "sdkproxy.h"
#include "strtool.h"
#include "vhook/call.h"

#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/ICvar.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/KeyValues.h"

#include "steamworks/steamclientpublic.h"

#include <cstdio>
#include <cstring>

extern void SendClientStringCommand(CServerSideClient* pClient, const char* pCommandString);


void IEngineServer::ChangeLevel(const char* map)
{
    // goes through the console command, so a name must not be able to append another command
    if (map == nullptr || *map == '\0' || std::strpbrk(map, ";\"\r\n") != nullptr)
    {
        WARN("IEngineServer::ChangeLevel: rejected map name \"%s\"", map ? map : "(null)");
        return;
    }

    ServerCommand(FString("changelevel \"%s\"\n", map));
}

bool IEngineServer::IsDedicatedServer()
{
    // the engine one caches "application mode is dedicated", which is what -dedicated selects
    return CommandLine()->HasParam("-dedicated");
}

const char* IEngineServer::GetPlayerNetworkIDString(PlayerSlot_t slot)
{
    // the engine one: "BOT", STEAM_ID_LAN / STEAM_ID_PENDING for a nil steamid, else SteamID3
    if (sv == nullptr || sv->GetState() < CNetworkGameServer::Active)
        return nullptr;

    const auto pClient = sv->GetClientSafety(slot);
    if (pClient == nullptr)
        return nullptr;

    if (pClient->IsFakeClient())
        return "BOT";

    static auto offset = g_pGameData->GetOffset("CServerSideClient::m_SteamId");
    const auto  steamId = CSteamID(static_cast<uint64>(*reinterpret_cast<const SteamId_t*>(reinterpret_cast<uintptr_t>(pClient) + offset)));

    if (steamId.ConvertToUint64() == 0)
    {
        // engine: server mode "no authentication", which sv_lan 1 selects
        static CConVarBaseData* pLanConVar = icvar->FindConVarIterator("sv_lan");
        return pLanConVar && pLanConVar->GetValue<bool>() ? "STEAM_ID_LAN" : "STEAM_ID_PENDING";
    }

    static char buffers[CS_MAX_PLAYERS][32];
    auto&       buffer = buffers[slot % CS_MAX_PLAYERS];

    // ponytail: only the individual-account forms; clients are never other account types
    if (steamId.GetEAccountType() == k_EAccountTypeIndividual && steamId.GetUnAccountInstance() == k_unSteamUserDefaultInstance)
        snprintf(buffer, sizeof(buffer), "[U:%u:%u]", steamId.GetEUniverse(), steamId.GetAccountID());
    else
        snprintf(buffer, sizeof(buffer), "[U:%u:%u:%u]", steamId.GetEUniverse(), steamId.GetAccountID(), steamId.GetUnAccountInstance());

    return buffer;
}

void IEngineServer::ServerCommand(const char* command)
{
    // the engine one is a thin wrapper of the input service: (target 4 = server, command, 0)
    static auto inputService = factory::engine->GetInterface<void*>("InputService_001");
    static auto offset       = g_pGameData->GetVFunctionIndex("IInputService::QueueCommand");
    AssertPtr(inputService);

    CALL_VIRTUAL(void, offset, inputService, 4, command, static_cast<void*>(nullptr));
}

void IEngineServer::ClientCommand(PlayerSlot_t slot, const char* command)
{
    // the engine one formats the command and sends it as a StringCmd, nothing else
    if (sv == nullptr || command == nullptr || *command == '\0')
        return;

    if (const auto pClient = sv->GetClientSafety(slot))
        SendClientStringCommand(pClient, command);
}

const char* IEngineServer::GetClientConVarValue(PlayerSlot_t slot, const char* name)
{
    // the engine one reads the client's userinfo KeyValues (it also records unknown names, unused here)
    if (sv == nullptr || name == nullptr || *name == '\0')
        return "";

    const auto pClient = sv->GetClientSafety(slot);
    if (pClient == nullptr)
        return "";

    const auto pConVars = pClient->GetConVars();
    return pConVars ? pConVars->GetString(name, "") : "";
}

const SteamId_t* IEngineServer::GetClientSteamId(PlayerSlot_t slot)
{
    // the engine one: &client->m_SteamId when it is a valid CSteamID, else nullptr
    if (sv == nullptr)
        return nullptr;

    const auto pClient = sv->GetClientSafety(slot);
    if (pClient == nullptr)
        return nullptr;

    static auto offset  = g_pGameData->GetOffset("CServerSideClient::m_SteamId");
    const auto  pSteamId = reinterpret_cast<const SteamId_t*>(reinterpret_cast<uintptr_t>(pClient) + offset);
    return CSteamID(static_cast<uint64>(*pSteamId)).IsValid() ? pSteamId : nullptr;
}

void IEngineServer::KickClient(PlayerSlot_t slot, const char* reason, int disconnectReason)
{
    // the engine one only checks the game server and forwards
    if (sv != nullptr)
        sv->KickClient(slot, reason, disconnectReason);
}
