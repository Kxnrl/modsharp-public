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
 * ============================================================================
 * Acknowledgements
 *
 * This module is a port / re-implementation of the multi-addon download flow
 * from MultiAddonManager (Source2ZE/MultiAddonManager) by xen, licensed under
 * GPL-3.0. The hook architecture (HostStateRequest / ReplyConnection /
 * SendNetMessage), per-client addon tracking (ClientAddonInfo_t), and the
 * SignonState manipulation logic all follow MAM's design.
 *
 *   https://github.com/Source2ZE/MultiAddonManager
 *   Copyright (C) 2024-2025 xen
 */

#include "hook/extern/ExtraAddon.h"

#include "gamedata.h"
#include "global.h"
#include "hook/extern/AddonHooks.h"
#include "hook/installer.h"
#include "hook/network.h"
#include "logging.h"
#include "manager/HookManager.h"
#include "sdkproxy.h"
#include "strtool.h"

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
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/KeyValues.h"

#include <proto/networkbasetypes.pb.h>

#include <safetyhook.hpp>

static CServerSideClient* GetClientByNetChannel(const INetChannel* pNetChan)
{
    if (!sv)
        return nullptr;

    const auto pClients = sv->GetClients();
    if (!pClients)
        return nullptr;

    for (int i = pClients->Count() - 1; i >= 0; i--)
    {
        const auto client = pClients->Element(i);
        if (client->GetNetChannel() == pNetChan)
            return client;
    }
    return nullptr;
}

static std::vector<std::string> GetFullAddonListAnonymous()
{
    return ExtraAddon::GetClientAddons(0);
}

static std::vector<std::string> GetRemainingAddons(SteamId_t steamId)
{
    auto        all  = ExtraAddon::GetClientAddons(steamId);
    const auto& info = ExtraAddon::GetClientInfo(steamId);

    std::erase_if(all, [&](const std::string& addon) {
        return std::ranges::find(info.downloadedAddons, addon) != info.downloadedAddons.end();
    });
    return all;
}

class ExtraAddonStrategy : public AddonHooks::IAddonStrategy
{
public:
    void OnHostStateRequestPre(void* /*a1*/, CHostStateRequest* pRequest) override
    {
        ExtraAddon::SetOfficialWorkshopMap(false);
        ExtraAddon::ClearCurrentWorkshopMap();

        if (!ExtraAddon::IsEnabled())
            return;

        if (ExtraAddon::GetDebug())
        {
            LogInfo("[ExtraAddon] HostStateRequest -> Addons=[%s] LevelName=[%s] ChangeLevel=%s",
                    pRequest->m_Addons.Get(), pRequest->m_LevelName.Get(), BooleanSTR(pRequest->m_bChangeLevel));
        }

        if (auto kv = pRequest->m_pKV; kv != nullptr)
        {
            const std::string kv_name = kv->GetName();
            if (kv_name.starts_with("map_workshop"))
            {
                ExtraAddon::SetCurrentWorkshopMap(kv->GetString("customgamemode", ""));
            }
        }
        else if (const std::string sOriginalAddons = pRequest->m_Addons.Get();
                 !pRequest->m_LevelName.IsEmpty() && pRequest->m_bChangeLevel
                 && !sOriginalAddons.empty() && StrIsNumber(sOriginalAddons))
        {
            ExtraAddon::SetCurrentWorkshopMap(sOriginalAddons);
        }

        if (!pRequest->m_LevelName.IsEmpty()
            && g_pFullFileSystem->IsDirectory(pRequest->m_LevelName.Get(), "OFFICIAL_ADDONS")
            && g_pFullFileSystem->FileExists(FString("%s/%s_dir.vpk", pRequest->m_LevelName.Get(), pRequest->m_LevelName.Get()), "OFFICIAL_ADDONS"))
        {
            ExtraAddon::SetCurrentWorkshopMap(pRequest->m_LevelName.Get());
            ExtraAddon::SetOfficialWorkshopMap(true);
        }

        const auto allAddons = GetFullAddonListAnonymous();
        pRequest->m_Addons   = StringJoin(allAddons, ",").c_str();

        if (ExtraAddon::GetDebug())
        {
            LogInfo("[ExtraAddon] HostStateRequest --> Addons=[%s] workshop_map=%s official=%s",
                    pRequest->m_Addons.Get(),
                    ExtraAddon::GetCurrentWorkshopMap().c_str(),
                    BooleanSTR(ExtraAddon::IsOfficialWorkshopMap()));
        }
    }

    void OnSignonStateNetMessagePre(INetChannel* pNetChannel, CNetMessagePB<CNETMsg_SignonState>* pData) override
    {
        if (!ExtraAddon::IsEnabled())
            return;

        const auto pClient = GetClientByNetChannel(pNetChannel);
        if (!pClient || pClient->IsFakeClient())
            return;

        const auto steamId = pClient->GetSteamId();
        if (steamId == 0)
            return;

        auto& info          = ExtraAddon::GetClientInfo(steamId);
        info.lastActiveTime = Plat_FloatTime();

        if (ExtraAddon::GetDebug())
        {
            LogInfo("[ExtraAddon] SendNetMessage -> Steam=%llu State=%d Addons=[%s]",
                    static_cast<unsigned long long>(steamId), pData->signon_state(), pData->addons().c_str());
        }

        if (pData->signon_state() == SIGNONSTATE_CHANGELEVEL)
        {
            if (const auto addonsStr = pData->addons(); addonsStr.find(',') != std::string::npos)
            {
                if (auto vecAddons = StringSplit(addonsStr.c_str(), ","); !vecAddons.empty() && !ExtraAddon::IsOfficialWorkshopMap())
                {
                    pData->set_addons(vecAddons[0]);
                    info.currentPendingAddon = vecAddons[0];
                }
            }
            else if (!pData->addons().empty())
            {
                info.currentPendingAddon = pData->addons();
            }
            return;
        }

        const auto remaining = GetRemainingAddons(steamId);
        if (remaining.empty())
            return;

        info.currentPendingAddon = remaining[0];
        pData->set_addons(remaining[0]);
        pData->set_signon_state(SIGNONSTATE_CHANGELEVEL);
    }
};

static ExtraAddonStrategy s_ExtraAddonStrategy;

BeginStaticHookScope(ReplyConnection)
{
    DeclareStaticDetourHook(ReplyConnection, void, (CNetworkGameServer* pServer, CServerSideClient* pClient))
    {
        if (!ExtraAddon::IsEnabled())
            return ReplyConnection(pServer, pClient);

        if (pClient->IsFakeClient())
            return ReplyConnection(pServer, pClient);

        const auto steamId = pClient->GetSteamId();
        if (steamId == 0)
            return ReplyConnection(pServer, pClient);

        auto& info          = ExtraAddon::GetClientInfo(steamId);
        info.lastActiveTime = Plat_FloatTime();

        const auto allAddons = ExtraAddon::GetClientAddons(steamId);
        if (allAddons.empty())
            return ReplyConnection(pServer, pClient);

        std::string nextAddon;
        for (const auto& addon : allAddons)
        {
            if (std::ranges::find(info.downloadedAddons, addon) == info.downloadedAddons.end())
            {
                nextAddon = addon;
                break;
            }
        }
        if (!nextAddon.empty())
            info.currentPendingAddon = nextAddon;

        std::vector<std::string> clientAddons;
        for (const auto& addon : allAddons)
        {
            if (std::ranges::find(info.downloadedAddons, addon) != info.downloadedAddons.end()
                || addon == info.currentPendingAddon)
            {
                clientAddons.push_back(addon);
            }
        }

        if (clientAddons.empty())
            return ReplyConnection(pServer, pClient);

        const std::string originalAddons = pServer->GetAddonName() ? pServer->GetAddonName() : "";

        pServer->SetAddonName(StringJoin(clientAddons, ",").c_str());

        if (ExtraAddon::GetDebug())
        {
            LogInfo("[ExtraAddon] ReplyConnection -> Steam=%llu Addons=[%s] (original=[%s])",
                    static_cast<unsigned long long>(steamId), pServer->GetAddonName(), originalAddons.c_str());
        }

        ReplyConnection(pServer, pClient);

        pServer->SetAddonName(originalAddons.c_str());
    }
}

BeginStaticHookScope(ScriptGetAddon)
{
    DeclareStaticDetourHook(ScriptGetAddon, uint64_t, ())
    {
        if (!ExtraAddon::IsEnabled())
            return ScriptGetAddon();

        const auto& workshopMap = ExtraAddon::GetCurrentWorkshopMap();
        if (workshopMap.empty())
            return ScriptGetAddon();

        const auto id = strtoull(workshopMap.c_str(), nullptr, 10);
        if (id == 0)
            return ScriptGetAddon();

        return id;
    }
}

static void OnClientConnectPre(PlayerSlot_t slot, const char* name, SteamId_t steamId, bool bot)
{
    if (bot)
        return;

    if (!ExtraAddon::IsEnabled())
        return;

    const auto pClient = sv->GetClientSafety(slot);
    if (!pClient)
    {
        LogInfo("[ExtraAddon] OnClientConnectPre -> %s <%llu> with nullptr",
                name, static_cast<unsigned long long>(steamId));
        return;
    }

    AssertBool(pClient->GetSteamId() == steamId);

    auto& info = ExtraAddon::GetClientInfo(steamId);

    if (ExtraAddon::GetCacheClientsWithAddons())
    {
        const auto cacheDur = ExtraAddon::GetCacheClientsDuration();
        if (cacheDur > 0.0f && (Plat_FloatTime() - info.lastActiveTime) > cacheDur)
        {
            if (ExtraAddon::GetDebug())
                LogInfo("[ExtraAddon] OnClientConnectPre -> %llu cache expired, clearing",
                        static_cast<unsigned long long>(steamId));
            info.currentPendingAddon.clear();
            info.downloadedAddons.clear();
        }
    }

    if (!info.currentPendingAddon.empty())
    {
        if ((Plat_FloatTime() - info.lastActiveTime) < ExtraAddon::GetTimeoutSec())
        {
            if (std::ranges::find(info.downloadedAddons, info.currentPendingAddon) == info.downloadedAddons.end())
                info.downloadedAddons.push_back(info.currentPendingAddon);
        }
        info.currentPendingAddon.clear();
    }

    info.lastActiveTime = Plat_FloatTime();
}

static void OnClientDisconnectPost(PlayerSlot_t /*slot*/, int32_t /*reason*/, const char* /*name*/, SteamId_t steamId)
{
    if (steamId == 0)
        return;
    ExtraAddon::GetClientInfo(steamId).lastActiveTime = Plat_FloatTime();
}

static void OnClientActivatePost(PlayerSlot_t /*slot*/, const char* /*name*/, SteamId_t steamId)
{
    if (steamId == 0)
        return;

    auto& info = ExtraAddon::GetClientInfo(steamId);
    info.currentPendingAddon.clear();
    // When caching is on, keep the downloadedAddons list so subsequent
    // reconnects skip the redownload flow.
    if (!ExtraAddon::GetCacheClientsWithAddons())
        info.downloadedAddons.clear();
}

static void OnGameFrame(bool /*sim*/, bool /*first*/, bool /*last*/)
{
    if (!sv)
        return;

    static double s_flNextUpdate = 0;
    const auto    flTime         = Plat_FloatTime();
    if (flTime <= s_flNextUpdate)
        return;
    s_flNextUpdate = flTime + 1.0;

    const auto pClients = sv->GetClients();
    for (auto i = pClients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = pClients->Element(i);
        if (!pClient || !pClient->IsInGame() || pClient->IsFakeClient())
            continue;

        const auto steamId = pClient->GetSteamId();
        if (steamId == 0)
            continue;

        ExtraAddon::GetClientInfo(steamId).lastActiveTime = flTime;
    }

    ExtraAddon::PrintDownloadProgress();
}

static void OnServerInitPost()
{
    ExtraAddon::OnStartupServer();
}

namespace ExtraAddon
{
void SendClientSignonRefresh(const char* pszAddon, SteamId_t steamId)
{
    if (!sv || !pszAddon || !*pszAddon || !gpGlobals || !engine)
        return;

    if (!g_pNetworkMessages)
        return;

    const auto pNetMsg = g_pNetworkMessages->FindNetworkMessagePartial("SignonState");
    if (!pNetMsg)
        return;

    auto       pData   = pNetMsg->AllocateMessage();
    const auto pSignon = pData->ToPB<CNETMsg_SignonState>();
    pSignon->set_spawn_count(gpGlobals->nServerCount);
    pSignon->set_signon_state(SIGNONSTATE_CHANGELEVEL);
    pSignon->set_addons(pszAddon);

    const auto pClients = sv->GetClients();
    if (!pClients)
    {
        g_pMemAlloc->Free(pData);
        return;
    }

    pSignon->set_num_server_players(pClients->Count());
    for (int i = 0; i < pClients->Count(); ++i)
    {
        const auto pClient = pClients->Element(i);
        if (!pClient)
            continue;
        if (const auto netId = engine->GetPlayerNetworkIDString(pClient->GetSlot()))
            pSignon->add_players_networkids(netId);
    }

    using SendFn_t = bool (*)(INetChannel*, CNetMessage*, NetChannelBufType_t);
    static auto pSendCall = g_pGameData->GetAddress<SendFn_t>("INetChannel::SendNetMessage");
    if (!pSendCall)
    {
        g_pMemAlloc->Free(pData);
        return;
    }

    int nSent = 0;
    for (int i = 0; i < pClients->Count(); ++i)
    {
        const auto pClient = pClients->Element(i);
        if (!pClient || pClient->IsFakeClient())
            continue;

        const auto sid = pClient->GetSteamId();
        if (sid == 0)
            continue;
        if (steamId != 0 && sid != steamId)
            continue;

        // Skip clients that are already mid-changelevel (sending another
        // SIGNONSTATE_CHANGELEVEL while they're at one disconnects them with
        // "Received signon X when at Y").
        if (pClient->GetSignonState() == CServerSideClient::SIGNONSTATE_CHANGELEVEL)
            continue;

        // Skip if the client is already in the addon-download flow — they
        // will receive the new addon naturally after the current pending one.
        auto& info = ExtraAddon::GetClientInfo(sid);
        if (!info.currentPendingAddon.empty())
            continue;

        // Skip if the client has nothing left to download anyway.
        auto remaining = ExtraAddon::GetClientAddons(sid);
        std::erase_if(remaining, [&](const std::string& a) {
            return std::ranges::find(info.downloadedAddons, a) != info.downloadedAddons.end();
        });
        if (remaining.empty())
            continue;

        const auto pChan = pClient->GetNetChannel();
        if (!pChan)
            continue;

        // The active SendNetMessage hook will observe this CHANGELEVEL signon
        // and update info.currentPendingAddon to pszAddon, so we don't need
        // to set it ourselves.
        pSendCall(pChan, pData, BUF_RELIABLE);
        ++nSent;

        if (steamId != 0)
            break;
    }

    g_pMemAlloc->Free(pData);

    if (nSent > 0)
    {
        LogInfo("[ExtraAddon] SendClientSignonRefresh -> %d client(s) notified for addon %s",
                nSent, pszAddon);
    }
}
} // namespace ExtraAddon

void InstallExtraAddonHooks()
{
    ExtraAddon::Initialize();

    g_pHookManager->Hook_ClientConnect(HookType_Pre, OnClientConnectPre);
    g_pHookManager->Hook_ClientDisconnect(HookType_Post, OnClientDisconnectPost);
    g_pHookManager->Hook_ClientActivate(HookType_Post, OnClientActivatePost);
    g_pHookManager->Hook_GameFrame(HookType_Post, OnGameFrame);
    g_pHookManager->Hook_ServerInit(HookType_Post, OnServerInitPost);

    SHOOK(ReplyConnection);
    SHOOK(ScriptGetAddon);

    AddonHooks::Install(&s_ExtraAddonStrategy);
}
