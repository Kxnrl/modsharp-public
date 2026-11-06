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

#ifndef MS_MANAGER_ADDON_H
#define MS_MANAGER_ADDON_H

#include "definitions.h"

#include <cstdint>
#include <string>
#include <vector>

struct CHostStateRequest;

class AddonManager
{
public:
    // Seed the addon list from -addons, reject the removed -dual_addon.
    void Init();

    // Latch the addon list and the workshop map for the requested map.
    void OnHostStateRequest(const CHostStateRequest* pRequest);

    // Addon list applied on the next map change, seeded from the -addons command line parameter.
    const std::vector<uint64_t>& GetAddons() const;
    void                         SetAddons(std::vector<uint64_t> addons);

    // Addon list latched at the last HostStateRequest, active when it has any addon or client queries are enabled.
    const std::vector<uint64_t>& GetActiveAddons() const;
    bool                         IsActive() const;

    // Whether managed listeners want to be queried for per-client addons (activates even without server addons).
    void SetClientQueryEnabled(bool enabled);

    void ResetClientCache(SteamId_t steamId);

    // Resend the addons to an in-game client (it will reconnect).
    // resetCache resends every addon, otherwise only the ones it does not have yet.
    bool RefreshClient(SteamId_t steamId, bool resetCache);

    // Force a workshop download (update), remounting the addon if it was mounted.
    // reloadMap reloads once every download that asked for it finished, unless all of them failed.
    bool UpdateAddon(uint64_t fileId, bool reloadMap);

    void SetOptions(double clientTimeout, double connectionTimeout, double cacheDuration, bool debug);

    // Workshop map (file id, or name for official community maps) latched at the last HostStateRequest, empty when unknown.
    const std::string& GetWorkshopMap() const;
    bool               IsOfficialWorkshopMap() const;

    // Reload the current map, through host_workshop_map when it is a workshop map.
    void ReloadMap();

private:
    void DetectWorkshopMap(const CHostStateRequest* pRequest);

    bool                  m_bClientQuery         = false;
    bool                  m_bOfficialWorkshopMap = false;
    bool                  m_bActive              = false;
    std::string           m_WorkshopMap;
    std::vector<uint64_t> m_Addons;
    std::vector<uint64_t> m_ActiveAddons;
};

extern AddonManager g_AddonManager;

#endif
