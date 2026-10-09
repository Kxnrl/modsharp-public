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

#include "manager/AddonManager.h"

#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "sdkproxy.h"
#include "strtool.h"

#include "cstrike/interface/CDedicatedServerWorkshopManager.h"
#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/IFileSystem.h"
#include "cstrike/interface/IMemAlloc.h"
#include "cstrike/interface/INetChannel.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/IProtobufBinding.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CHostState.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/KeyValues.h"

#include <proto/networkbasetypes.pb.h>

#include <cctype>
#include <string>

extern void        AddonsResetClientCache(SteamId_t steamId);
extern std::string AddonsPrepareRefresh(SteamId_t steamId, bool resetCache);
extern void        AddonsCancelRefresh(SteamId_t steamId);
extern bool        AddonsUpdateAddon(uint64_t fileId);
extern void        AddonsSetOptions(double clientTimeout, double connectionTimeout, double cacheDuration, bool debug);

AddonManager g_AddonManager;

void AddonManager::Init()
{
    // seeds the addon list, the AddonManager module uses it instead of core.json "Addons"
    if (const auto pszValue = CommandLine()->ParamValue("-addons", nullptr))
    {
        for (auto token : StringSplit(pszValue, ","))
        {
            // "123, 456" and "123,456" both work
            std::erase_if(token, [](unsigned char c) { return std::isspace(c); });
            if (!StrIsNumber(token))
                continue;
            if (const auto id = strtoull(token.c_str(), nullptr, 10); id > 0)
                m_Addons.push_back(id);
        }
    }

    if (CommandLine()->HasParam("-dual_addon"))
        FatalError("-dual_addon has been removed, use -addons or core.json \"AddonManager\" with the AddonManager module instead");
}

void AddonManager::OnHostStateRequest(const CHostStateRequest* pRequest)
{
    DetectWorkshopMap(pRequest);

    m_ActiveAddons = m_Addons;

    m_bActive = !m_ActiveAddons.empty() || m_bClientQuery;
}

void AddonManager::DetectWorkshopMap(const CHostStateRequest* pRequest)
{
    m_bOfficialWorkshopMap = false;
    m_bWorkshopRequest     = false;
    m_WorkshopMap.clear();

    if (const auto kv = pRequest->m_pKV; kv != nullptr)
    {
        // default is 'ChangeLevel'
        if (std::string_view(kv->GetName()).starts_with("map_workshop"))
        {
            m_WorkshopMap      = kv->GetString("customgamemode", "");
            m_bWorkshopRequest = true;
        }
    }
    else if (const std::string addons = pRequest->m_Addons.Get();
             !pRequest->m_LevelName.IsEmpty() && pRequest->m_bChangeLevel && !addons.empty() && StrIsNumber(addons))
    {
        m_WorkshopMap      = addons;
        m_bWorkshopRequest = true;
    }

    // m_Addons can not be trusted here: changing from de_mirage to an official community map (e.g. cs_agency)
    // leaves the server addon in it instead of the map, so the client would miss the map's materials
    if (!pRequest->m_LevelName.IsEmpty()
        && g_pFullFileSystem->IsDirectory(pRequest->m_LevelName.Get(), "OFFICIAL_ADDONS")
        && g_pFullFileSystem->FileExists(FString("%s/%s_dir.vpk", pRequest->m_LevelName.Get(), pRequest->m_LevelName.Get()), "OFFICIAL_ADDONS"))
    {
        m_WorkshopMap          = pRequest->m_LevelName.Get();
        m_bOfficialWorkshopMap = true;
        m_bWorkshopRequest     = false;
    }
}

const std::vector<uint64_t>& AddonManager::GetAddons() const
{
    return m_Addons;
}

void AddonManager::SetAddons(std::vector<uint64_t> addons)
{
    std::erase(addons, 0);
    m_Addons = std::move(addons);
}

const std::vector<uint64_t>& AddonManager::GetActiveAddons() const
{
    return m_ActiveAddons;
}

bool AddonManager::IsActive() const
{
    return m_bActive;
}

void AddonManager::SetClientQueryEnabled(bool enabled)
{
    m_bClientQuery = enabled;
}

void AddonManager::ResetClientCache(SteamId_t steamId)
{
    AddonsResetClientCache(steamId);
}

bool AddonManager::UpdateAddon(uint64_t fileId)
{
    return AddonsUpdateAddon(fileId);
}

void AddonManager::SetOptions(double clientTimeout, double connectionTimeout, double cacheDuration, bool debug)
{
    AddonsSetOptions(clientTimeout, connectionTimeout, cacheDuration, debug);
}

void AddonManager::ReloadMap()
{
    if (!sv || !engine)
        return;

    const char* mapName = sv->GetMapName();
    if (!mapName || !*mapName)
        return;

    const auto& workshopMap = GetWorkshopMap();

    // official community maps are tracked by name and load through a plain changelevel
    if (workshopMap.empty() || !StrIsNumber(workshopMap))
    {
        engine->ServerCommand(FString("changelevel %s", mapName));
        return;
    }

    // ds_workshop_changelevel skips the update check but only reaches maps the server already has,
    // host_workshop_map works for any workshop map
    const auto fileId = strtoull(workshopMap.c_str(), nullptr, 10);

    CUtlVector<WorkshopMap_t> maps;
    g_pServerWorkshopManager->ListWorkshopMaps(&maps);
    for (int i = 0; i < maps.Count(); i++)
    {
        if (maps[i].m_nPublishFileId == fileId && maps[i].m_pName && *maps[i].m_pName)
        {
            engine->ServerCommand(FString("ds_workshop_changelevel %s", maps[i].m_pName));
            return;
        }
    }

    engine->ServerCommand(FString("host_workshop_map %s", workshopMap.c_str()));
}

const std::string& AddonManager::GetWorkshopMap() const
{
    return m_WorkshopMap;
}

bool AddonManager::IsOfficialWorkshopMap() const
{
    return m_bOfficialWorkshopMap;
}

bool AddonManager::IsWorkshopRequest() const
{
    return m_bWorkshopRequest;
}

bool AddonManager::RefreshClient(SteamId_t steamId, bool resetCache)
{
    if (!sv || !gpGlobals || !engine || !g_pNetworkMessages || steamId == 0)
        return false;

    const auto pClients = sv->GetClients();
    if (!pClients)
        return false;

    CServerSideClient* pTarget = nullptr;
    for (int i = 0; i < pClients->Count(); i++)
    {
        const auto pClient = pClients->Element(i);
        if (pClient && !pClient->IsFakeClient() && pClient->GetSteamId() == steamId)
        {
            pTarget = pClient;
            break;
        }
    }

    // a client already at SIGNONSTATE_CHANGELEVEL gets kicked by "Received signon X when at Y"
    if (!pTarget || !pTarget->IsInGame() || !pTarget->GetNetChannel())
        return false;

    if (!m_bActive)
        return false;

    const auto addon = AddonsPrepareRefresh(steamId, resetCache);
    if (addon.empty())
        return false;

    const auto pNetMsg = g_pNetworkMessages->FindNetworkMessagePartial("SignonState");
    if (!pNetMsg)
        return false;

    using SendFn_t = bool (*)(INetChannel*, CNetMessage*, NetChannelBufType_t);
    static auto pSendCall = g_pGameData->GetAddress<SendFn_t>("INetChannel::SendNetMessage");
    if (!pSendCall)
        return false;

    const auto pData   = pNetMsg->AllocateMessage();
    const auto pSignon = pData->ToPB<CNETMsg_SignonState>();
    pSignon->set_spawn_count(gpGlobals->nServerCount);
    pSignon->set_signon_state(SIGNONSTATE_CHANGELEVEL);
    pSignon->set_addons(addon);
    pSignon->set_num_server_players(pClients->Count());
    for (int i = 0; i < pClients->Count(); i++)
    {
        const auto pClient = pClients->Element(i);
        if (!pClient)
            continue;
        if (const auto netId = engine->GetPlayerNetworkIDString(pClient->GetSlot()))
            pSignon->add_players_networkids(netId);
    }

    // goes through our SendNetMessage detour, so the pending addon gets recorded
    const auto sent = pSendCall(pTarget->GetNetChannel(), pData, BUF_RELIABLE);
    g_pMemAlloc->Free(pData);

    if (!sent)
    {
        // the detour already recorded the pending addon, a quick reconnect would mark it downloaded
        AddonsCancelRefresh(steamId);
        return false;
    }

    LOG("RefreshClient -> %llu addon=%s", steamId, addon.c_str());
    return true;
}
