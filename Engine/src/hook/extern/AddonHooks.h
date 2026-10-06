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

#ifndef MS_HOOK_EXTERN_ADDONHOOKS_H
#define MS_HOOK_EXTERN_ADDONHOOKS_H

#include "definitions.h"

#include <cstdint>
#include <string>
#include <vector>

struct CHostStateRequest;
class INetChannel;
class CNETMsg_SignonState;
template <typename T>
class CNetMessagePB;

namespace AddonHooks
{
enum class Mode
{
    None,
    Dual,  // exactly 1 addon: DualMountAddon flow
    Multi, // 2+ addons or per-client addons: MultiAddon flow
};

class IAddonStrategy
{
public:
    virtual ~IAddonStrategy() = default;

    // Pre-call: opportunity to mutate pRequest before HostStateRequest runs.
    virtual void OnHostStateRequestPre(void* a1, CHostStateRequest* pRequest) = 0;

    // Pre-send: opportunity to mutate the SignonState message before the engine sends it.
    // Only called when m_MessageId == NET_MESSAGE_ID_SIGNON and the bypass flag is off.
    virtual void OnSignonStateNetMessagePre(INetChannel* pNetChannel, CNetMessagePB<CNETMsg_SignonState>* pData) = 0;
};

// Addon list applied on the next map change, seeded from the deprecated -dual_addon command line parameter.
const std::vector<uint64_t>& GetAddons();
void                         SetAddons(std::vector<uint64_t> addons);

// Addon list and mode latched at the last HostStateRequest.
const std::vector<uint64_t>& GetActiveAddons();
Mode                         GetMode();
uint64_t                     GetDualAddonId();

// Whether managed listeners want to be queried for per-client addons (forces Multi mode).
void SetClientQueryEnabled(bool enabled);

void ResetClientCache(SteamId_t steamId);

// Resend the addons to an in-game client (it will reconnect).
// resetCache resends every addon, otherwise only the ones it does not have yet.
bool RefreshClient(SteamId_t steamId, bool resetCache);

// Force a workshop download (update), remounting the addon if it was mounted.
bool UpdateAddon(uint64_t fileId);

void SetOptions(double clientTimeout, double connectionTimeout, double cacheDuration, bool debug);

// Workshop map (file id, or name for official community maps) latched at the last HostStateRequest, empty when unknown.
const std::string& GetWorkshopMap();
bool               IsOfficialWorkshopMap();
// The last HostStateRequest asked for a (non official) workshop map.
bool IsWorkshopRequest();

// Reload the current map, through host_workshop_map when it is a workshop map.
void ReloadMap();
} // namespace AddonHooks

void InstallAddonHooks();

#endif
