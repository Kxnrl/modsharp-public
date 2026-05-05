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

#ifndef MS_HOOK_EXTERN_EXTRAADDON_H
#define MS_HOOK_EXTERN_EXTRAADDON_H

#include "definitions.h"

#include <cstdint>
#include <string>
#include <unordered_map>
#include <vector>

struct ExtraAddonClientInfo_t
{
    double                   lastActiveTime {};
    std::vector<std::string> addonsToLoad;
    std::vector<std::string> downloadedAddons;
    std::string              currentPendingAddon;
};

namespace ExtraAddon
{
void Initialize();
void Shutdown();
void OnStartupServer();
void OnSteamApiActivated();
bool IsEnabled();

float GetTimeoutSec();
bool  GetCacheClientsWithAddons();
float GetCacheClientsDuration();
bool  GetBlockDisconnectMessages();
bool  GetDebug();

const std::string& GetCurrentWorkshopMap();
void               SetCurrentWorkshopMap(const std::string& s);
void               ClearCurrentWorkshopMap();
bool               IsOfficialWorkshopMap();
void               SetOfficialWorkshopMap(bool b);

const std::vector<std::string>& GetServerAddons();
bool                            AddAddon(const char* pszAddon, bool refresh);
bool                            RemoveAddon(const char* pszAddon, bool refresh);
void                            ClearAddons();
void                            RefreshAddons(bool reloadMap = false);
void                            ReloadMap();

const std::vector<std::string>& GetMountedAddons();
bool                            MountAddon(const char* pszAddon, bool addToTail = false);
bool                            UnmountAddon(const char* pszAddon);
bool                            IsAddonMounted(const char* pszAddon, bool checkWorkshopMap = false);

const std::vector<std::string>& GetGlobalClientAddons();
void                            AddClientAddon(const char* pszAddon, SteamId_t steamId = 0, bool refresh = false);
void                            RemoveClientAddon(const char* pszAddon, SteamId_t steamId = 0);
void                            ClearClientAddons(SteamId_t steamId = 0);
std::vector<std::string> GetClientAddons(SteamId_t steamId = 0);

void SendClientSignonRefresh(const char* pszAddon, SteamId_t steamId = 0);

ExtraAddonClientInfo_t& GetClientInfo(SteamId_t steamId);
void                    PurgeClientInfo(SteamId_t steamId);
void                    PurgeAllClientInfos();

bool DownloadAddon(const char* pszAddon, bool important = false, bool force = false);
void OnAddonDownloadCompleted(uint64_t fileId, int eResult);
void PrintDownloadProgress();

void PrintSearchPaths();
} // namespace ExtraAddon

void InstallExtraAddonHooks();

#endif
