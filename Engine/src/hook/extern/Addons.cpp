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


#include "address.h"
#include "bridge/forwards/forward.h"
#include "global.h"
#include "manager/AddonManager.h"
#include "hook/installer.h"
#include "logging.h"
#include "manager/HookManager.h"
#include "module.h"
#include "sdkproxy.h"
#include "steamproxy.h"
#include "strtool.h"

#include "CoreCLR/NativeSpan.h"

#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/IFileSystem.h"
#include "cstrike/interface/INetChannel.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/IProtobufBinding.h"
#include "cstrike/interface/IServerGameClient.h"
#include "cstrike/type/CBufferString.h"
#include "cstrike/type/CHostState.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/CUtlString.h"

#include <proto/networkbasetypes.pb.h>
#include <steamworks/isteamugc.h>

#include <safetyhook.hpp>

#include <algorithm>
#include <deque>
#include <string>
#include <unordered_map>
#include <vector>

constexpr int    MAX_CLIENT_ADDONS = 64;
constexpr size_t ADDON_PATH_LENGTH = 260;

// tuned through IAddonManager.SetOptions
static double s_flClientTimeout     = 10;  // seconds allowed between reconnects for the next addon
static double s_flConnectionTimeout = 0;   // seconds allowed to accept the first addon, 0 disables
static double s_flCacheDuration     = 600; // keep downloaded addons per client, 0 forever, < 0 disabled
static bool   s_bDebug              = false;

struct AddonsClientInfo_t
{
    double                   lastActiveTime {};
    double                   connectionStartTime {};
    bool                     connecting {};
    std::vector<std::string> addons; // per-client addons from IAddonListener, refreshed on every ReplyConnection
    std::vector<std::string> downloadedAddons;
    std::string              currentPendingAddon;
};

struct AddonsDownload_t
{
    uint64_t fileId;
    bool     reloadMap; // reload once every reloadMap download finishes
    bool     remount;   // was mounted before an update, mount it again once done
};

static std::unordered_map<SteamId_t, AddonsClientInfo_t> s_ClientInfos;
static std::vector<std::string>                              s_MountedAddons;
static std::deque<AddonsDownload_t>                      s_DownloadQueue;
static std::vector<SteamId_t>                                s_TimedOutClients;
static std::string                                           s_CurrentWorkshopMap;
static bool                                                  s_bReloadBatchSucceeded = false;

static bool HasUGC()
{
    return g_pSteamApiProxy && g_pSteamApiProxy->GetSteamUGC();
}

static std::vector<std::string> GetServerAddons()
{
    std::vector<std::string> result;
    for (const auto id : g_AddonManager.GetActiveAddons())
        result.push_back(std::to_string(id));
    return result;
}

// workshop map + server addons + per-client addons (steamId == 0 omits the per-client layer)
static std::vector<std::string> GetClientAddons(SteamId_t steamId)
{
    std::vector<std::string> result;

    if (!s_CurrentWorkshopMap.empty())
        result.push_back(s_CurrentWorkshopMap);

    auto append_unique = [&](const std::vector<std::string>& src) {
        for (const auto& a : src)
            if (std::ranges::find(result, a) == result.end())
                result.push_back(a);
    };

    append_unique(GetServerAddons());

    if (steamId != 0)
    {
        if (const auto it = s_ClientInfos.find(steamId); it != s_ClientInfos.end())
            append_unique(it->second.addons);
    }

    return result;
}

static std::vector<std::string> GetRemainingAddons(SteamId_t steamId)
{
    auto        all  = GetClientAddons(steamId);
    const auto& info = s_ClientInfos[steamId];

    std::erase_if(all, [&](const std::string& addon) {
        return std::ranges::find(info.downloadedAddons, addon) != info.downloadedAddons.end();
    });
    return all;
}

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

static void BuildAddonPath(const char* pszAddon, char* buf, size_t len, bool bLegacy)
{
    static CFixedBufferString<ADDON_PATH_LENGTH> s_WorkingDir;
    static bool                                  s_bWorkingDirResolved = false;
    if (!s_bWorkingDirResolved)
    {
        g_pFullFileSystem->GetSearchPath("EXECUTABLE_PATH", static_cast<GetSearchPathTypes_t>(0), &s_WorkingDir, 1);
        s_bWorkingDirResolved = true;
    }

    snprintf(buf, len, "%ssteamapps/workshop/content/730/%s/%s%s.vpk",
             s_WorkingDir.Get(), pszAddon, pszAddon, bLegacy ? "" : "_dir");
}

static bool IsDownloading(uint64_t fileId)
{
    return std::ranges::any_of(s_DownloadQueue, [&](const auto& e) { return e.fileId == fileId; });
}

static bool DownloadAddon(uint64_t fileId, bool reloadMap, bool remount)
{
    if (const auto it = std::ranges::find_if(s_DownloadQueue, [&](const auto& e) { return e.fileId == fileId; });
        it != s_DownloadQueue.end())
    {
        it->reloadMap |= reloadMap;
        it->remount |= remount;
        return true;
    }

    if (!g_pSteamApiProxy->DownloadItem(fileId, false))
    {
        LogInfo("[Addons] Failed to start download for %llu", fileId);
        return false;
    }

    s_DownloadQueue.push_back({fileId, reloadMap, remount});
    LogInfo("[Addons] Download started for %llu", fileId);
    return true;
}

static void PrintDownloadProgress()
{
    if (s_DownloadQueue.empty() || !HasUGC())
        return;

    const auto fileId = s_DownloadQueue.front().fileId;

    uint64_t downloaded = 0, total = 0;
    if (!g_pSteamApiProxy->GetItemDownloadInfo(fileId, &downloaded, &total) || total == 0)
        return;

    const double mbDownloaded = static_cast<double>(downloaded) / 1024.0 / 1024.0;
    const double mbTotal      = static_cast<double>(total) / 1024.0 / 1024.0;
    const double progress     = static_cast<double>(downloaded) * 100.0 / static_cast<double>(total);

    LogInfo("[Addons] Downloading %llu: %.2f/%.2f MB (%.2f%%)", fileId, mbDownloaded, mbTotal, progress);
}

static bool MountAddon(const char* pszAddon)
{
    // Already mounted by the engine as the current workshop map.
    if (s_CurrentWorkshopMap == pszAddon)
        return true;

    if (std::ranges::find(s_MountedAddons, pszAddon) != s_MountedAddons.end())
        return true;

    const auto fileId = strtoull(pszAddon, nullptr, 10);
    const auto state  = g_pSteamApiProxy->GetItemState(fileId);

    if (state & k_EItemStateLegacyItem)
    {
        LogInfo("[Addons] %s is a legacy item (Source 1), skipping", pszAddon);
        return false;
    }
    if (!(state & k_EItemStateInstalled))
    {
        LogInfo("[Addons] %s is not installed, queuing a download", pszAddon);
        DownloadAddon(fileId, true, false);
        return false;
    }

    char path[ADDON_PATH_LENGTH];
    BuildAddonPath(pszAddon, path, sizeof(path), false);
    if (!g_pFullFileSystem->FileExists(path))
    {
        BuildAddonPath(pszAddon, path, sizeof(path), true);
        if (!g_pFullFileSystem->FileExists(path))
        {
            LogInfo("[Addons] %s not found at %s", pszAddon, path);
            return false;
        }
    }
    else
    {
        BuildAddonPath(pszAddon, path, sizeof(path), true);
    }

    g_pFullFileSystem->AddSearchPath(path, "GAME", PATH_ADD_TO_HEAD, SEARCH_PATH_PRIORITY_VPK);
    s_MountedAddons.emplace_back(pszAddon);

    LogInfo("[Addons] Mounted addon %s -> %s", pszAddon, path);
    return true;
}

static bool UnmountAddon(const std::string& addon)
{
    const auto it = std::ranges::find(s_MountedAddons, addon);
    if (it == s_MountedAddons.end())
        return false;

    char path[ADDON_PATH_LENGTH];
    BuildAddonPath(addon.c_str(), path, sizeof(path), true);
    g_pFullFileSystem->RemoveSearchPath(path, "GAME");
    s_MountedAddons.erase(it);
    return true;
}

static void UnmountAllAddons()
{
    for (const auto& addon : s_MountedAddons)
    {
        char path[ADDON_PATH_LENGTH];
        BuildAddonPath(addon.c_str(), path, sizeof(path), true);
        g_pFullFileSystem->RemoveSearchPath(path, "GAME");
    }
    s_MountedAddons.clear();
}

static void RefreshAddons(bool reloadMap)
{
    UnmountAllAddons();

    if (!g_AddonManager.IsActive() || !HasUGC())
        return;

    const auto addons     = GetServerAddons();
    bool       allMounted = true;
    for (const auto& addon : addons)
    {
        if (!MountAddon(addon.c_str()))
            allMounted = false;
    }

    LogInfo("[Addons] Load complete -> mounted=%zu/%zu [%s]",
            s_MountedAddons.size(), addons.size(), StringJoin(s_MountedAddons, ", ").c_str());

    if (allMounted && reloadMap)
        g_AddonManager.ReloadMap();
}

void AddonsOnDownloadItemResult(uint64_t fileId, int eResult)
{
    const auto it = std::ranges::find_if(s_DownloadQueue, [&](const auto& e) { return e.fileId == fileId; });
    if (it == s_DownloadQueue.end())
        return; // not our download

    const auto entry = *it;
    s_DownloadQueue.erase(it);

    if (eResult == k_EResultOK)
    {
        LogInfo("[Addons] Addon %llu downloaded", fileId);
        if (entry.reloadMap)
            s_bReloadBatchSucceeded = true;
    }
    else
        LogInfo("[Addons] Addon %llu download failed (%d)", fileId, eResult);

    if (entry.remount)
        MountAddon(std::to_string(fileId).c_str());

    // reload once every queued download has finished so the new addons get mounted
    if (entry.reloadMap && std::ranges::none_of(s_DownloadQueue, [](const auto& e) { return e.reloadMap; }))
    {
        // a failed addon is queued again on the next map load, so reloading on an all-failed batch loops forever
        if (!s_bReloadBatchSucceeded)
        {
            LogInfo("[Addons] All downloads failed, skipping map reload");
            return;
        }

        s_bReloadBatchSucceeded = false;
        LogInfo("[Addons] All downloads complete, reloading map");
        g_AddonManager.ReloadMap();
    }
}

bool AddonsUpdateAddon(uint64_t fileId)
{
    if (!HasUGC() || fileId == 0)
        return false;

    if (IsDownloading(fileId))
        return true;

    // the engine keeps a mounted vpk open, on Windows that locks it and the update fails
    // with k_EResultLockingFailed, so unmount first and mount again once the download finishes
    const auto addon   = std::to_string(fileId);
    const auto remount = UnmountAddon(addon);

    if (!DownloadAddon(fileId, false, remount))
    {
        if (remount)
            MountAddon(addon.c_str());
        return false;
    }

    return true;
}

void AddonsSetOptions(double clientTimeout, double connectionTimeout, double cacheDuration, bool debug)
{
    s_flClientTimeout     = clientTimeout;
    s_flConnectionTimeout = connectionTimeout;
    s_flCacheDuration     = cacheDuration;
    s_bDebug              = debug;
}

void AddonsOnSteamApiActivated()
{
    if (!g_AddonManager.IsActive() || !engine || !engine->IsDedicatedServer())
        return;

    LogInfo("[Addons] Steam API activated, refreshing addons");
    RefreshAddons(true);
}

void AddonsResetClientCache(SteamId_t steamId)
{
    if (steamId == 0)
        s_ClientInfos.clear();
    else
        s_ClientInfos.erase(steamId);
}

void AddonsCancelRefresh(SteamId_t steamId)
{
    if (const auto it = s_ClientInfos.find(steamId); it != s_ClientInfos.end())
        it->second.currentPendingAddon.clear();
}

static void QueryClientAddons(SteamId_t steamId, AddonsClientInfo_t& info)
{
    uint64_t        buffer[MAX_CLIENT_ADDONS];
    NativeFixedSpan span(buffer, 0, MAX_CLIENT_ADDONS);
    forwards::OnClientQueryAddons->Invoke(steamId, &span);

    info.addons.clear();
    for (int i = 0; i < std::min(span.m_nCount, MAX_CLIENT_ADDONS); i++)
    {
        if (buffer[i] > 0)
            info.addons.push_back(std::to_string(buffer[i]));
    }
}

std::string AddonsPrepareRefresh(SteamId_t steamId, bool resetCache)
{
    auto& info = s_ClientInfos[steamId];

    if (resetCache)
    {
        info.currentPendingAddon.clear();
        info.downloadedAddons.clear();

        // the client is in game, so it already has the map
        if (!s_CurrentWorkshopMap.empty())
            info.downloadedAddons.push_back(s_CurrentWorkshopMap);
    }
    else if (!info.currentPendingAddon.empty())
    {
        // still downloading, it receives the rest after the pending one anyway
        return {};
    }

    // pick up addons the listeners added since the client connected
    QueryClientAddons(steamId, info);

    const auto remaining = GetRemainingAddons(steamId);
    return remaining.empty() ? std::string() : remaining[0];
}

void AddonsOnHostStateRequestPre(CHostStateRequest* pRequest)
{
    s_CurrentWorkshopMap.clear();

    // without a cache clients go through the download flow again on the next map,
    // the history is kept while they stay on this one so a refresh only sends the new addons
    if (s_flCacheDuration < 0)
    {
        for (auto& [steamId, info] : s_ClientInfos)
            info.downloadedAddons.clear();
    }

    if (!g_AddonManager.IsActive())
        return;

    if (s_bDebug)
    {
        LOG("HostStateRequest -> Addons=[%s] LevelName=[%s] ChangeLevel=%s",
            pRequest->m_Addons.Get(), pRequest->m_LevelName.Get(), BooleanSTR(pRequest->m_bChangeLevel));
    }

    s_CurrentWorkshopMap = g_AddonManager.GetWorkshopMap();

    pRequest->m_Addons = StringJoin(GetClientAddons(0), ",").c_str();

    LOG("HostStateRequest --> Addons=[%s] workshop_map=%s official=%s",
        pRequest->m_Addons.Get(), s_CurrentWorkshopMap.c_str(), BooleanSTR(g_AddonManager.IsOfficialWorkshopMap()));
}

void AddonsOnSignonStateNetMessagePre(INetChannel* pNetChannel, CNetMessagePB<CNETMsg_SignonState>* pData)
{
    const auto pClient = GetClientByNetChannel(pNetChannel);
    if (!pClient || pClient->IsFakeClient())
        return;

    const auto steamId = pClient->GetSteamId();
    if (steamId == 0)
        return;

    auto& info          = s_ClientInfos[steamId];
    info.lastActiveTime = Plat_FloatTime();

    if (s_bDebug)
        LOG("SignonState -> Steam=%llu State=%d Addons=[%s]", steamId, pData->signon_state(), pData->addons().c_str());

    if (pData->signon_state() == SIGNONSTATE_CHANGELEVEL)
    {
        // HACK Valve 24/7/27: sending 2+ ids on a native changelevel stalls the client
        if (const auto addonsStr = pData->addons(); addonsStr.find(',') != std::string::npos)
        {
            if (auto vecAddons = StringSplit(addonsStr.c_str(), ","); !vecAddons.empty())
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

BeginStaticHookScope(ReplyConnection)
{
    DeclareStaticDetourHook(ReplyConnection, void, (CNetworkGameServer * pServer, CServerSideClient * pClient))
    {
        if (!g_AddonManager.IsActive() || pClient->IsFakeClient())
            return ReplyConnection(pServer, pClient);

        const auto steamId = pClient->GetSteamId();
        if (steamId == 0)
            return ReplyConnection(pServer, pClient);

        auto&      info = s_ClientInfos[steamId];
        const auto now  = Plat_FloatTime();

        if (s_flCacheDuration > 0 && (now - info.lastActiveTime) > s_flCacheDuration)
        {
            if (s_bDebug)
                LOG("ReplyConnection -> %llu has not connected for a while, clearing the cache", steamId);

            info.currentPendingAddon.clear();
            info.downloadedAddons.clear();
        }
        info.lastActiveTime = now;

        QueryClientAddons(steamId, info);

        const auto allAddons = GetClientAddons(steamId);
        if (allAddons.empty())
        {
            info.currentPendingAddon.clear();
            return ReplyConnection(pServer, pClient);
        }

        if (!info.connecting)
        {
            info.connecting          = true;
            info.connectionStartTime = now;
        }
        else if (s_flConnectionTimeout > 0 && (now - info.connectionStartTime) > s_flConnectionTimeout)
        {
            // kicking right now crashes on Windows, defer to the next frame
            s_TimedOutClients.push_back(steamId);
            return;
        }

        for (const auto& addon : allAddons)
        {
            if (std::ranges::find(info.downloadedAddons, addon) == info.downloadedAddons.end())
            {
                info.currentPendingAddon = addon;
                break;
            }
        }

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

        if (s_bDebug)
            LOG("ReplyConnection -> Steam=%llu Addons=[%s] (original=[%s])", steamId, pServer->GetAddonName(), originalAddons.c_str());

        ReplyConnection(pServer, pClient);

        pServer->SetAddonName(originalAddons.c_str());
    }
}

BeginStaticHookScope(EngineClientDisconnect)
{
    // the engine-side disconnect every drop goes through. IServerGameClients::ClientDisconnect only runs once the client got past SIGNONSTATE_CONNECTED
    // so a client leaving right after ReplyConnection (to download the pending addon, cancel, crash...)
    // never reaches OnClientDisconnectPost and would keep a stale connectionStartTime
    DeclareStaticDetourHook(EngineClientDisconnect, void, (CServerSideClient * pClient, void* pInfo))
    {
        if (g_AddonManager.IsActive() && !pClient->IsFakeClient())
        {
            if (const auto it = s_ClientInfos.find(pClient->GetSteamId()); it != s_ClientInfos.end())
                it->second.connecting = false;
        }

        EngineClientDisconnect(pClient, pInfo);
    }
}

BeginStaticHookScope(ScriptGetAddon)
{
    // level resource loading takes the first id of the addon list as the map's addon, keep it the workshop map
    DeclareStaticDetourHook(ScriptGetAddon, uint64_t, ())
    {
        if (!g_AddonManager.IsActive() || s_CurrentWorkshopMap.empty())
            return ScriptGetAddon();

        const auto id = strtoull(s_CurrentWorkshopMap.c_str(), nullptr, 10);
        return id > 0 ? id : ScriptGetAddon();
    }
}

static void OnClientConnectPre(PlayerSlot_t /*slot*/, const char* /*name*/, SteamId_t steamId, bool bot)
{
    if (bot || !g_AddonManager.IsActive())
        return;

    auto&      info = s_ClientInfos[steamId];
    const auto now  = Plat_FloatTime();

    info.connecting = false;

    // reconnected within the timeout -> the pending addon was downloaded
    if (!info.currentPendingAddon.empty())
    {
        if ((now - info.lastActiveTime) < s_flClientTimeout)
        {
            if (std::ranges::find(info.downloadedAddons, info.currentPendingAddon) == info.downloadedAddons.end())
                info.downloadedAddons.push_back(info.currentPendingAddon);

            if (s_bDebug)
                LOG("ClientConnect -> %llu connected within the interval with the pending addon %s", steamId, info.currentPendingAddon.c_str());
        }
        else if (s_bDebug)
        {
            LOG("ClientConnect -> %llu reconnected after the timeout, %s is not marked as downloaded", steamId, info.currentPendingAddon.c_str());
        }
        info.currentPendingAddon.clear();
    }

    info.lastActiveTime = now;
}

static void OnClientDisconnectPost(PlayerSlot_t /*slot*/, int32_t /*reason*/, const char* /*name*/, SteamId_t steamId)
{
    if (steamId == 0 || !g_AddonManager.IsActive())
        return;

    auto& info          = s_ClientInfos[steamId];
    info.lastActiveTime = Plat_FloatTime();
    info.connecting     = false;

    // without a cache a client that really leaves goes through the download flow again when it rejoins,
    // one leaving to download its pending addon (refresh, the next addon of the flow) keeps the history
    if (s_flCacheDuration < 0 && info.currentPendingAddon.empty())
        info.downloadedAddons.clear();
}

static void OnClientActivatePost(PlayerSlot_t /*slot*/, const char* /*name*/, SteamId_t steamId)
{
    if (steamId == 0 || !g_AddonManager.IsActive())
        return;

    s_ClientInfos[steamId].currentPendingAddon.clear();
}

static void OnGameFrame(bool /*sim*/, bool /*first*/, bool /*last*/)
{
    if (!sv || !g_AddonManager.IsActive())
        return;

    if (!s_TimedOutClients.empty())
    {
        const auto pClients = sv->GetClients();
        for (auto i = pClients->Count() - 1; i >= 0; i--)
        {
            const auto pClient = pClients->Element(i);
            if (!pClient || std::ranges::find(s_TimedOutClients, pClient->GetSteamId()) == s_TimedOutClients.end())
                continue;

            engine->KickClient(pClient->GetSlot(), "Required Workshop addon download was not accepted in time", IServerGameClient::NETWORK_DISCONNECT_TIMEDOUT);
            s_ClientInfos[pClient->GetSteamId()].connecting = false;
        }
        s_TimedOutClients.clear();
    }

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

        if (const auto steamId = pClient->GetSteamId(); steamId != 0)
            s_ClientInfos[steamId].lastActiveTime = flTime;
    }

    PrintDownloadProgress();
}

static void OnServerInitPost()
{
    // kicks deferred on a map without addons never ran, do not apply them to this one
    s_TimedOutClients.clear();

    if (engine && engine->IsDedicatedServer())
        RefreshAddons(false);
}

void InstallAddonsHooks()
{
    g_pHookManager->Hook_ClientConnect(HookType_Pre, OnClientConnectPre);
    g_pHookManager->Hook_ClientDisconnect(HookType_Post, OnClientDisconnectPost);
    g_pHookManager->Hook_ClientActivate(HookType_Post, OnClientActivatePost);
    g_pHookManager->Hook_GameFrame(HookType_Post, OnGameFrame);
    g_pHookManager->Hook_ServerInit(HookType_Post, OnServerInitPost);

    SHOOK(ReplyConnection);
    SHOOK(ScriptGetAddon);

    if (const auto address = modules::engine->FindFunctionFromStringRef("Disconnect client '%s' from server: %s\n"); address.IsValid())
        SHOOK(EngineClientDisconnect, {.address = address});
    else
        WARN("[Addons] Failed to find the engine client disconnect, stale connection timeouts may kick reconnecting clients");
}
