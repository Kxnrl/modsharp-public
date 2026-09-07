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

#include "hook/extern/ExtraAddon.h"

#include "global.h"
#include "logging.h"
#include "manager/ConVarManager.h"
#include "sdkproxy.h"
#include "steamproxy.h"
#include "strtool.h"

#include "cstrike/interface/CDedicatedServerWorkshopManager.h"
#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/IFileSystem.h"
#include "cstrike/type/CBufferString.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"

#include <steamworks/isteamugc.h>

#include <algorithm>
#include <cstdio>
#include <deque>

namespace ExtraAddon
{

static std::vector<std::string>                              s_ExtraAddons;        // server-side mounted addons
static std::vector<std::string>                              s_MountedAddons;      // currently mounted via filesystem
static std::vector<std::string>                              s_GlobalClientAddons; // delivered to all clients
static std::unordered_map<SteamId_t, ExtraAddonClientInfo_t> s_ClientAddonInfos;
static std::string                                           s_CurrentWorkshopMap;
static bool                                                  s_IsOfficialWorkshopMap = false;

// ConVar handles read by the getters below.
static CConVarBaseData* s_pCvTimeoutSec              = nullptr;
static CConVarBaseData* s_pCvDebug                   = nullptr;
static CConVarBaseData* s_pCvCacheClientsWithAddons  = nullptr;
static CConVarBaseData* s_pCvCacheClientsDuration    = nullptr;
static CConVarBaseData* s_pCvBlockDisconnectMessages = nullptr;
static CConVarBaseData* s_pCvAddonMountDownload      = nullptr;

constexpr size_t kAddonPathBufSize = 260;

struct DownloadEntry_t
{
    uint64_t fileId;
    bool     important; // triggers ReloadMap when this and all other important downloads complete
    bool     remount;   // addon was mounted before this (re)download; remount it once done
};
static std::deque<DownloadEntry_t> s_DownloadQueue;

// ============================================================================

static std::vector<std::string> SplitCSV(const char* s)
{
    std::vector<std::string> out;
    if (!s || !*s)
        return out;

    auto v = StringSplit(s, ",");
    for (auto& token : v)
    {
        while (!token.empty() && token.front() == ' ') token.erase(token.begin());
        while (!token.empty() && token.back() == ' ') token.pop_back();
        if (!token.empty())
            out.push_back(std::move(token));
    }
    return out;
}

static void BuildAddonPath(const char* pszAddon, char* buf, size_t len, bool bLegacy)
{
    static CFixedBufferString<kAddonPathBufSize> s_WorkingDir;
    static bool                                  s_bWorkingDirResolved = false;
    if (!s_bWorkingDirResolved)
    {
        g_pFullFileSystem->GetSearchPath("EXECUTABLE_PATH", static_cast<GetSearchPathTypes_t>(0), &s_WorkingDir, 1);
        s_bWorkingDirResolved = true;
    }

    snprintf(buf, len, "%ssteamapps/workshop/content/730/%s/%s%s.vpk",
             s_WorkingDir.Get(), pszAddon, pszAddon, bLegacy ? "" : "_dir");
}

static bool IsDedicatedServer()
{
    return engine && engine->IsDedicatedServer();
}

// ============================================================================

float GetTimeoutSec()
{
    return s_pCvTimeoutSec ? s_pCvTimeoutSec->GetValue<float>() : 10.0f;
}

bool GetCacheClientsWithAddons()
{
    return s_pCvCacheClientsWithAddons && s_pCvCacheClientsWithAddons->GetValue<bool>();
}

float GetCacheClientsDuration()
{
    return s_pCvCacheClientsDuration ? s_pCvCacheClientsDuration->GetValue<float>() : 0.0f;
}

bool GetBlockDisconnectMessages()
{
    return s_pCvBlockDisconnectMessages && s_pCvBlockDisconnectMessages->GetValue<bool>();
}

bool GetDebug()
{
    return s_pCvDebug && s_pCvDebug->GetValue<bool>();
}

// ============================================================================

const std::string& GetCurrentWorkshopMap() { return s_CurrentWorkshopMap; }
void               SetCurrentWorkshopMap(const std::string& s) { s_CurrentWorkshopMap = s; }
void               ClearCurrentWorkshopMap() { s_CurrentWorkshopMap.clear(); }
bool               IsOfficialWorkshopMap() { return s_IsOfficialWorkshopMap; }
void               SetOfficialWorkshopMap(bool b) { s_IsOfficialWorkshopMap = b; }

// ============================================================================

bool IsAddonMounted(const char* pszAddon, bool checkWorkshopMap)
{
    if (!pszAddon || !*pszAddon)
        return false;

    if (std::ranges::find(s_MountedAddons, pszAddon) != s_MountedAddons.end())
        return true;

    if (checkWorkshopMap && s_CurrentWorkshopMap == pszAddon)
        return true;

    return false;
}

const std::vector<std::string>& GetMountedAddons() { return s_MountedAddons; }

static bool MountAddonFiles(const char* pszAddon, bool addToTail)
{
    if (std::ranges::find(s_MountedAddons, pszAddon) != s_MountedAddons.end())
        return true; // already mounted

    char path[kAddonPathBufSize];
    BuildAddonPath(pszAddon, path, sizeof(path), false);
    if (!g_pFullFileSystem->FileExists(path))
    {
        BuildAddonPath(pszAddon, path, sizeof(path), true);
        if (!g_pFullFileSystem->FileExists(path))
        {
            LogInfo("[ExtraAddon] MountAddon: %s not found at %s", pszAddon, path);
            return false;
        }
    }
    else
    {
        BuildAddonPath(pszAddon, path, sizeof(path), true);
    }

    g_pFullFileSystem->AddSearchPath(path, "GAME",
                                     addToTail ? PATH_ADD_TO_TAIL : PATH_ADD_TO_HEAD,
                                     SEARCH_PATH_PRIORITY_VPK);
    s_MountedAddons.emplace_back(pszAddon);

    LogInfo("[ExtraAddon] Mounted addon %s -> %s", pszAddon, path);
    return true;
}

bool MountAddon(const char* pszAddon, bool addToTail)
{
    if (!pszAddon || !*pszAddon)
        return false;

    // Already mounted by the engine as the current workshop map.
    if (s_CurrentWorkshopMap == pszAddon)
    {
        LogInfo("[ExtraAddon] MountAddon: %s is already mounted by the server (workshop map)", pszAddon);
        return false;
    }

    if (std::ranges::find(s_MountedAddons, pszAddon) != s_MountedAddons.end())
    {
        LogInfo("[ExtraAddon] MountAddon: %s is already mounted", pszAddon);
        return false;
    }

    if (g_pSteamApiProxy && g_pSteamApiProxy->GetSteamUGC())
    {
        const auto fileId = strtoull(pszAddon, nullptr, 10);
        const auto state  = g_pSteamApiProxy->GetItemState(fileId);

        if (state & k_EItemStateLegacyItem)
        {
            LogInfo("[ExtraAddon] MountAddon: %s is a legacy item (Source 1), skipping", pszAddon);
            return false;
        }
        if (!(state & k_EItemStateInstalled))
        {
            LogInfo("[ExtraAddon] MountAddon: %s is not installed, queuing a download", pszAddon);
            DownloadAddon(pszAddon, true, true);
            return false;
        }
        if (s_pCvAddonMountDownload && s_pCvAddonMountDownload->GetValue<bool>())
        {
            DownloadAddon(pszAddon, false, true);
        }
    }

    return MountAddonFiles(pszAddon, addToTail);
}

bool UnmountAddon(const char* pszAddon)
{
    if (!pszAddon || !*pszAddon)
        return false;

    char path[kAddonPathBufSize];
    BuildAddonPath(pszAddon, path, sizeof(path), true);

    if (!g_pFullFileSystem->RemoveSearchPath(path, "GAME"))
        return false;

    std::erase(s_MountedAddons, pszAddon);
    LogInfo("[ExtraAddon] UnmountAddon: removed search path %s", path);
    return true;
}

// ============================================================================

const std::vector<std::string>& GetServerAddons() { return s_ExtraAddons; }

void RefreshAddons(bool reloadMap)
{
    if (!g_pSteamApiProxy || !g_pSteamApiProxy->GetSteamUGC())
        return;

    LogInfo("[ExtraAddon] Refreshing addons: [%s]", StringJoin(s_ExtraAddons, ", ").c_str());

    // Unmount any currently-mounted addons first (in case the list changed).
    // Iterate a snapshot since UnmountAddon mutates s_MountedAddons.
    const auto mountedSnapshot = s_MountedAddons;
    for (const auto& addon : mountedSnapshot)
        UnmountAddon(addon.c_str());

    bool allMounted = true;
    for (const auto& addon : s_ExtraAddons)
    {
        if (!MountAddon(addon.c_str()))
            allMounted = false;
    }

    // Summary line so operators can see at-a-glance which addons are live.
    if (!s_ExtraAddons.empty())
    {
        std::vector<std::string> pending;
        for (const auto& addon : s_ExtraAddons)
        {
            if (std::ranges::find(s_MountedAddons, addon) == s_MountedAddons.end()
                && s_CurrentWorkshopMap != addon)
            {
                pending.push_back(addon);
            }
        }

        LogInfo("[ExtraAddon] Load complete -> mounted=%zu/%zu [%s]",
                s_MountedAddons.size(), s_ExtraAddons.size(),
                StringJoin(s_MountedAddons, ", ").c_str());

        if (!pending.empty())
        {
            LogInfo("[ExtraAddon] Pending (waiting for download): [%s]",
                    StringJoin(pending, ", ").c_str());
        }
    }

    if (allMounted && reloadMap)
        ReloadMap();
}

void ReloadMap()
{
    if (!sv)
        return;

    const char* mapName = sv->GetMapName();
    if (!mapName || !*mapName)
        return;

    char cmd[kAddonPathBufSize];
    if (s_CurrentWorkshopMap.empty() || g_pFullFileSystem->IsDirectory(s_CurrentWorkshopMap.c_str(), "OFFICIAL_ADDONS"))
        snprintf(cmd, sizeof(cmd), "changelevel %s", mapName);
    else
        snprintf(cmd, sizeof(cmd), "host_workshop_map %s", s_CurrentWorkshopMap.c_str());

    engine->ServerCommand(cmd);
}

bool AddAddon(const char* pszAddon, bool refresh)
{
    if (!pszAddon || !*pszAddon)
        return false;

    if (std::ranges::find(s_ExtraAddons, pszAddon) != s_ExtraAddons.end())
    {
        LogInfo("[ExtraAddon] AddAddon: %s is already in the list", pszAddon);
        return false;
    }

    LogInfo("[ExtraAddon] AddAddon: %s", pszAddon);
    s_ExtraAddons.emplace_back(pszAddon);

    if (refresh)
        RefreshAddons();
    return true;
}

bool RemoveAddon(const char* pszAddon, bool refresh)
{
    if (!pszAddon || !*pszAddon)
        return false;

    auto it = std::ranges::find(s_ExtraAddons, pszAddon);
    if (it == s_ExtraAddons.end())
    {
        LogInfo("[ExtraAddon] RemoveAddon: %s is not in the list", pszAddon);
        return false;
    }

    LogInfo("[ExtraAddon] RemoveAddon: %s", pszAddon);
    s_ExtraAddons.erase(it);

    if (refresh)
        RefreshAddons();
    return true;
}

void ClearAddons()
{
    s_ExtraAddons.clear();
    const auto mountedSnapshot = s_MountedAddons;
    for (const auto& addon : mountedSnapshot)
        UnmountAddon(addon.c_str());
}

// ============================================================================

const std::vector<std::string>& GetGlobalClientAddons() { return s_GlobalClientAddons; }

void AddClientAddon(const char* pszAddon, SteamId_t steamId, bool refresh)
{
    if (!pszAddon || !*pszAddon)
        return;

    if (steamId == 0)
    {
        if (std::ranges::find(s_GlobalClientAddons, pszAddon) == s_GlobalClientAddons.end())
            s_GlobalClientAddons.emplace_back(pszAddon);
    }
    else
    {
        auto& info = s_ClientAddonInfos[steamId];
        if (std::ranges::find(info.addonsToLoad, pszAddon) == info.addonsToLoad.end())
            info.addonsToLoad.emplace_back(pszAddon);
    }

    if (refresh)
        SendClientSignonRefresh(pszAddon, steamId);
}

void RemoveClientAddon(const char* pszAddon, SteamId_t steamId)
{
    if (!pszAddon || !*pszAddon)
        return;

    if (steamId == 0)
    {
        std::erase(s_GlobalClientAddons, pszAddon);
    }
    else
    {
        auto& info = s_ClientAddonInfos[steamId];
        std::erase(info.addonsToLoad, pszAddon);
    }
}

void ClearClientAddons(SteamId_t steamId)
{
    if (steamId == 0)
    {
        s_GlobalClientAddons.clear();
    }
    else
    {
        s_ClientAddonInfos[steamId].addonsToLoad.clear();
    }
}

std::vector<std::string> GetClientAddons(SteamId_t steamId)
{
    std::vector<std::string> result;

    if (!s_CurrentWorkshopMap.empty())
        result.push_back(s_CurrentWorkshopMap);

    auto append_unique = [&](const std::vector<std::string>& src) {
        for (const auto& a : src)
            if (std::ranges::find(result, a) == result.end())
                result.push_back(a);
    };

    append_unique(s_MountedAddons);
    append_unique(s_GlobalClientAddons);

    if (steamId != 0)
    {
        if (auto it = s_ClientAddonInfos.find(steamId); it != s_ClientAddonInfos.end())
            append_unique(it->second.addonsToLoad);
    }

    return result;
}

// ============================================================================

ExtraAddonClientInfo_t& GetClientInfo(SteamId_t steamId) { return s_ClientAddonInfos[steamId]; }
void                    PurgeClientInfo(SteamId_t steamId) { s_ClientAddonInfos.erase(steamId); }
void                    PurgeAllClientInfos() { s_ClientAddonInfos.clear(); }

bool IsEnabled()
{
    if (!s_ExtraAddons.empty() || !s_GlobalClientAddons.empty())
        return true;

    for (const auto& entry : s_ClientAddonInfos)
    {
        if (!entry.second.addonsToLoad.empty())
            return true;
    }
    return false;
}

// ============================================================================

bool DownloadAddon(const char* pszAddon, bool important, bool force)
{
    if (!g_pSteamApiProxy || !g_pSteamApiProxy->GetSteamUGC())
    {
        LogInfo("[ExtraAddon] DownloadAddon: SteamUGC not available");
        return false;
    }

    const auto fileId = strtoull(pszAddon, nullptr, 10);
    if (fileId == 0)
    {
        LogInfo("[ExtraAddon] DownloadAddon: invalid id %s", pszAddon);
        return false;
    }

    for (const auto& e : s_DownloadQueue)
        if (e.fileId == fileId)
        {
            LogInfo("[ExtraAddon] DownloadAddon: %llu already queued", fileId);
            return false;
        }

    const auto state = g_pSteamApiProxy->GetItemState(fileId);
    if (!force && (state & k_EItemStateInstalled))
    {
        LogInfo("[ExtraAddon] DownloadAddon: %llu already installed", fileId);
        return true;
    }

    // If forcing a (re)download of an addon that is currently mounted, the
    // engine holds an open handle on its .vpk
    // On Windows that handle locks the file, so Steam cannot replace it when committing the update
    // and the download fails with k_EResultLockingFailed (33)
    // Unmount first to release the lock, and remember to remount once the download finishes
    bool remount = false;
    if (force && IsAddonMounted(pszAddon))
    {
        LogInfo("[ExtraAddon] DownloadAddon: unmounting %llu before update to release the file lock", fileId);
        UnmountAddon(pszAddon);
        remount = true;
    }

    if (!g_pSteamApiProxy->DownloadItem(fileId, false))
    {
        LogInfo("[ExtraAddon] DownloadAddon: failed to start for %llu", fileId);
        if (remount)
            MountAddonFiles(pszAddon, false); // restore the mount we just removed
        return false;
    }

    s_DownloadQueue.push_back({fileId, important, remount});
    LogInfo("[ExtraAddon] Download started for %llu", fileId);
    return true;
}

void OnAddonDownloadCompleted(uint64_t fileId, int eResult)
{
    auto it = std::ranges::find_if(s_DownloadQueue, [&](const auto& e) { return e.fileId == fileId; });
    if (it == s_DownloadQueue.end())
        return; // not our download

    const bool wasImportant = it->important;
    const bool needRemount  = it->remount;
    s_DownloadQueue.erase(it);

    if (eResult == k_EResultOK)
        LogInfo("[ExtraAddon] Addon %llu downloaded", fileId);
    else
        LogInfo("[ExtraAddon] Addon %llu download failed (%d)", fileId, eResult);

    if (needRemount)
    {
        char idStr[32];
        snprintf(idStr, sizeof(idStr), "%llu", fileId);
        MountAddonFiles(idStr, false);
    }

    // When the last important download finishes, reload the map so the addon
    // takes effect.
    if (wasImportant)
    {
        const bool anyOther = std::ranges::any_of(s_DownloadQueue, [](const auto& e) { return e.important; });
        if (!anyOther)
        {
            LogInfo("[ExtraAddon] All important downloads complete, reloading map");
            ReloadMap();
        }
    }
}

void PrintDownloadProgress()
{
    if (s_DownloadQueue.empty())
        return;

    if (!g_pSteamApiProxy || !g_pSteamApiProxy->GetSteamUGC())
        return;

    const auto fileId = s_DownloadQueue.front().fileId;

    uint64_t downloaded = 0, total = 0;
    if (!g_pSteamApiProxy->GetItemDownloadInfo(fileId, &downloaded, &total) || total == 0)
        return;

    const double mbDownloaded = static_cast<double>(downloaded) / 1024.0 / 1024.0;
    const double mbTotal      = static_cast<double>(total) / 1024.0 / 1024.0;
    const double progress     = static_cast<double>(downloaded) * 100.0 / static_cast<double>(total);

    LogInfo("[ExtraAddon] Downloading %llu: %.2f/%.2f MB (%.2f%%)", fileId, mbDownloaded, mbTotal, progress);
}

// ============================================================================

static void OnExtraAddonsChanged(BaseConVar* /*ref*/, CSplitScreenSlot /*slot*/, CVValue_t* newValue, CVValue_t* /*oldValue*/)
{
    s_ExtraAddons = SplitCSV(newValue ? newValue->m_szValue : nullptr);
    RefreshAddons();
}

static void OnClientExtraAddonsChanged(BaseConVar* /*ref*/, CSplitScreenSlot /*slot*/, CVValue_t* newValue, CVValue_t* /*oldValue*/)
{
    s_GlobalClientAddons = SplitCSV(newValue ? newValue->m_szValue : nullptr);
}

// ============================================================================

static void OnAddAddonCommand(const CCommandContext& /*ctx*/, const CCommand& cmd)
{
    if (cmd.ArgC() < 2)
    {
        LogInfo("Usage: %s <workshop_id>", cmd.Arg(0));
        return;
    }
    AddAddon(cmd.Arg(1), true);
}

static void OnRemoveAddonCommand(const CCommandContext& /*ctx*/, const CCommand& cmd)
{
    if (cmd.ArgC() < 2)
    {
        LogInfo("Usage: %s <workshop_id>", cmd.Arg(0));
        return;
    }
    RemoveAddon(cmd.Arg(1), true);
}

static void OnAddClientAddonCommand(const CCommandContext& /*ctx*/, const CCommand& cmd)
{
    if (cmd.ArgC() < 2)
    {
        LogInfo("Usage: %s <workshop_id>", cmd.Arg(0));
        return;
    }
    AddClientAddon(cmd.Arg(1));
}

static void OnRemoveClientAddonCommand(const CCommandContext& /*ctx*/, const CCommand& cmd)
{
    if (cmd.ArgC() < 2)
    {
        LogInfo("Usage: %s <workshop_id>", cmd.Arg(0));
        return;
    }
    RemoveClientAddon(cmd.Arg(1));
}

static void OnDownloadAddonCommand(const CCommandContext& /*ctx*/, const CCommand& cmd)
{
    if (cmd.ArgC() < 2)
    {
        LogInfo("Usage: %s <workshop_id>", cmd.Arg(0));
        return;
    }
    DownloadAddon(cmd.Arg(1), false, true);
}

static void OnPrintSearchPathsCommand(const CCommandContext& /*ctx*/, const CCommand& /*cmd*/)
{
    g_pFullFileSystem->PrintSearchPaths();
}

void PrintSearchPaths()
{
    g_pFullFileSystem->PrintSearchPaths();
}

// ============================================================================

void Initialize()
{
    // Seed initial server addon list from -extra_addons CLI parameter.
    std::string initialList;
    if (CommandLine()->HasParam("-extra_addons"))
    {
        if (const auto pszValue = CommandLine()->ParamValue("-extra_addons", nullptr))
        {
            s_ExtraAddons = SplitCSV(pszValue);
            initialList   = StringJoin(s_ExtraAddons, ",");
            for (const auto& a : s_ExtraAddons)
                LogInfo("[ExtraAddon] Load extra addon = %s", a.c_str());
        }
    }

    // Register ConVars. The list ConVars (ms_extra_addons / ms_client_extra_addons)
    // are driven entirely by their change callbacks, so we don't need to keep
    // their handles around.
    g_ConVarManager.CreateConVar(
        "ms_extra_addons", initialList.c_str(),
        "Workshop IDs of extra server addons separated by commas. Updates trigger refresh.",
        FCVAR_RELEASE,
        OnExtraAddonsChanged);

    g_ConVarManager.CreateConVar(
        "ms_client_extra_addons", "",
        "Workshop IDs of extra addons applied to all clients (download-only), separated by commas.",
        FCVAR_RELEASE,
        OnClientExtraAddonsChanged);

    s_pCvTimeoutSec = g_ConVarManager.CreateConVar(
        "ms_extra_addons_timeout", 10.f, true, 1.f, true, 600.f,
        "Seconds to wait between client connects when downloading extra addons.",
        FCVAR_RELEASE);

    s_pCvDebug = g_ConVarManager.CreateConVar(
        "ms_extra_addons_debug", false,
        "Print verbose debug information for extra addon downloads.",
        FCVAR_RELEASE);

    s_pCvCacheClientsWithAddons = g_ConVarManager.CreateConVar(
        "ms_cache_clients_with_addons", false,
        "Cache the addon download list per client to avoid redundant downloads on reconnect.",
        FCVAR_RELEASE);

    s_pCvCacheClientsDuration = g_ConVarManager.CreateConVar(
        "ms_cache_clients_duration", 0.f, true, 0.f, false, 0.f,
        "How long (seconds) to cache a client's downloaded-addons list. 0 = forever.",
        FCVAR_RELEASE);

    s_pCvBlockDisconnectMessages = g_ConVarManager.CreateConVar(
        "ms_block_disconnect_messages", false,
        "Block 'loop shutdown' disconnect messages emitted during addon-driven reconnects.",
        FCVAR_RELEASE);

    s_pCvAddonMountDownload = g_ConVarManager.CreateConVar(
        "ms_addon_mount_download", false,
        "Always (re)download addons when mounting, even if installed.",
        FCVAR_RELEASE);

    // Register ConCommands.
    g_ConVarManager.CreateConsoleCommand("ms_add_addon", OnAddAddonCommand,
                                         "Add a workshop ID to the server extra addons list.", FCVAR_GAMEDLL);
    g_ConVarManager.CreateConsoleCommand("ms_remove_addon", OnRemoveAddonCommand,
                                         "Remove a workshop ID from the server extra addons list.", FCVAR_GAMEDLL);
    g_ConVarManager.CreateConsoleCommand("ms_add_client_addon", OnAddClientAddonCommand,
                                         "Add a workshop ID to the global client addon list.", FCVAR_GAMEDLL);
    g_ConVarManager.CreateConsoleCommand("ms_remove_client_addon", OnRemoveClientAddonCommand,
                                         "Remove a workshop ID from the global client addon list.", FCVAR_GAMEDLL);
    g_ConVarManager.CreateConsoleCommand("ms_download_addon", OnDownloadAddonCommand,
                                         "Manually queue a workshop addon download.", FCVAR_GAMEDLL);
    g_ConVarManager.CreateConsoleCommand("ms_print_searchpaths", OnPrintSearchPathsCommand,
                                         "Print all filesystem search paths.", FCVAR_GAMEDLL);
}

void OnStartupServer()
{
    if (!IsDedicatedServer())
        return;

    // Mount the configured server addons. Maps are mounted in the order the
    // engine reports them (workshop map first, then extras).
    RefreshAddons();
}

void OnSteamApiActivated()
{
    if (!IsDedicatedServer())
        return;

    if (s_ExtraAddons.empty())
        return;

    LogInfo("[ExtraAddon] Steam API activated, refreshing addons (with reload-on-complete)");
    RefreshAddons(true);
}

void Shutdown()
{
    ClearAddons();
    s_ClientAddonInfos.clear();
    s_DownloadQueue.clear();
    s_GlobalClientAddons.clear();
}

} // namespace ExtraAddon

extern "C" void ExtraAddon_OnSteamDownloadItemResult(uint64_t fileId, int eResult)
{
    ExtraAddon::OnAddonDownloadCompleted(fileId, eResult);
}
