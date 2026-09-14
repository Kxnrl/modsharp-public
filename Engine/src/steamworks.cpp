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

#include "steamworks.h"
#include "bridge/forwards/forward.h"
#include "global.h"
#include "logging.h"
#include "sdkproxy.h"
#include "steamproxy.h"

#include "cstrike/interface/ICommandLine.h"

#include <steamworks/steam_api.h>
#include <steamworks/steam_gameserver.h>

#include <cstdlib>

// #define DOWNLOAD_DUAL_ADDON_UGC

class CallbackListener
{
public:
    CallbackListener() :
        m_CallbackSteamConnected(this, &CallbackListener::OnSteamServersConnected),
        m_CallbackSteamConnectFailure(this, &CallbackListener::OnSteamServersConnectFailure),
        m_CallbackSteamDisconnected(this, &CallbackListener::OnSteamServersDisconnected),
        m_CallbackGroupStatus(this, &CallbackListener::OnGroupStatusResult),
        m_CallbackDownloadItemResult(this, &CallbackListener::OnDownloadItemResult),
        m_CallbackItemInstalled(this, &CallbackListener::OnItemInstalled)
    {
    }

    ~CallbackListener()
    {
    }

    STEAM_GAMESERVER_CALLBACK(CallbackListener, OnSteamServersConnected, SteamServersConnected_t, m_CallbackSteamConnected);
    STEAM_GAMESERVER_CALLBACK(CallbackListener, OnSteamServersConnectFailure, SteamServerConnectFailure_t, m_CallbackSteamConnectFailure);
    STEAM_GAMESERVER_CALLBACK(CallbackListener, OnSteamServersDisconnected, SteamServersDisconnected_t, m_CallbackSteamDisconnected);
    STEAM_GAMESERVER_CALLBACK(CallbackListener, OnGroupStatusResult, GSClientGroupStatus_t, m_CallbackGroupStatus);
    STEAM_GAMESERVER_CALLBACK(CallbackListener, OnDownloadItemResult, DownloadItemResult_t, m_CallbackDownloadItemResult);
    STEAM_GAMESERVER_CALLBACK(CallbackListener, OnItemInstalled, ItemInstalled_t, m_CallbackItemInstalled);
};

static CSteamGameServerAPIContext g_SteamGameServerAPIContext;
static ISteamApiProxy             g_SteamApiProxy;
static CallbackListener*          g_pCallbackListener   = nullptr;
ISteamApiProxy*                   g_pSteamApiProxy      = &g_SteamApiProxy;
uint64_t                          g_unDualAddonId       = 0;
bool                              g_bDualAddonAvailable = false;

extern "C" void ExtraAddon_OnSteamDownloadItemResult(uint64_t fileId, int eResult);

void InitApiContext()
{
    static bool initDualAddonId = false;
    if (!initDualAddonId && CommandLine()->HasParam("-dual_addon"))
    {
        if (const auto pszValue = CommandLine()->ParamValue("-dual_addon", nullptr))
        {
            if (uint64_t ugcId; (ugcId = strtoull(pszValue, nullptr, 10)) > 0)
            {
                g_unDualAddonId = ugcId;
                LOG("Load dual addon = %llu", ugcId);
            }
        }

        initDualAddonId = true;
    }

    g_SteamGameServerAPIContext.Init();

    g_pCallbackListener = new CallbackListener;

    AssertPtr(g_pCallbackListener);

    g_pSteamApiProxy->SetSteamServer(g_SteamGameServerAPIContext.SteamGameServer());
    g_pSteamApiProxy->SetSteamUGC(g_SteamGameServerAPIContext.SteamUGC());
}

void DestroyApiContext()
{
    delete g_pCallbackListener;
    g_pCallbackListener = nullptr;
    g_SteamApiProxy.SetSteamServer(nullptr);
    g_SteamApiProxy.SetSteamUGC(nullptr);
    g_SteamGameServerAPIContext.Clear();
}

uint64_t GetDualAddonId()
{
#ifdef DOWNLOAD_DUAL_ADDON_UGC
    return g_unDualAddonId > 0 && g_bDualAddonAvailable ? g_unDualAddonId : 0;
#else
    return g_unDualAddonId;
#endif
}

bool SetDualAddonId(uint64_t publishId)
{
    if (!CommandLine()->HasParam("-dual_addon"))
        return false;

    const auto old  = g_unDualAddonId;
    g_unDualAddonId = publishId;

    if (old != publishId)
        LOG("Dual AddonId changed to %llu from %llu", publishId, old);

    return true;
}

void CallbackListener::OnGroupStatusResult(GSClientGroupStatus_t* pParam)
{
    const auto steamId = static_cast<SteamId_t>(pParam->m_SteamIDUser.ConvertToUint64());
    const auto groupId = static_cast<uint64_t>(pParam->m_SteamIDGroup.ConvertToUint64());
    const auto member  = pParam->m_bMember;
    const auto officer = pParam->m_bOfficer;

    forwards::OnGroupStatusResult->Invoke(steamId, groupId, member, officer);
}

void CallbackListener::OnSteamServersConnected(SteamServersConnected_t* pParam)
{
    forwards::OnSteamServersConnected->Invoke();

#ifdef DOWNLOAD_DUAL_ADDON_UGC
    if (g_unDualAddonId > 0)
    {
        if (g_SteamGameServerAPIContext.SteamUGC()->DownloadItem(static_cast<PublishedFileId_t>(g_unDualAddonId), true))
            LOG("Downloading dual addon %llu", g_unDualAddonId);
        else
            WARN("Failed to download dual addon %llu", g_unDualAddonId);
    }
#endif
}

void CallbackListener::OnSteamServersDisconnected(SteamServersDisconnected_t* pParam)
{
    forwards::OnSteamServersDisconnected->Invoke(pParam->m_eResult);
}

void CallbackListener::OnSteamServersConnectFailure(SteamServerConnectFailure_t* pParam)
{
    forwards::OnSteamServersConnectFailure->Invoke(pParam->m_eResult, pParam->m_bStillRetrying);
}

void CallbackListener::OnDownloadItemResult(DownloadItemResult_t* pParam)
{
    const auto id = static_cast<uint64_t>(pParam->m_nPublishedFileId);

#ifdef DOWNLOAD_DUAL_ADDON_UGC
    if (id == g_unDualAddonId)
    {
        if (pParam->m_eResult == k_EResultOK)
        {
            LogInfo("Downloaded dual addon %llu", g_unDualAddonId);
            g_bDualAddonAvailable = true;
        }
        else
        {
            LogError("Failed to download dual addon %llu", g_unDualAddonId);
        }
    }
    else
    {
        LogInfo("Downloaded addon %llu", id);
    }
#endif

    ExtraAddon_OnSteamDownloadItemResult(id, pParam->m_eResult);

    forwards::OnDownloadItemResult->Invoke(id, pParam->m_eResult);
}

void CallbackListener::OnItemInstalled(ItemInstalled_t* pParam)
{
    const auto id = static_cast<uint64_t>(pParam->m_nPublishedFileId);

#ifdef DOWNLOAD_DUAL_ADDON_UGC
    if (id == g_unDualAddonId)
    {
        LogInfo("OnItemInstalled -> dual addon %llu\n", g_unDualAddonId);
        g_bDualAddonAvailable = true;
    }
    else
    {
        LogInfo("OnItemInstalled addon %llu", id);
    }
#endif

    forwards::OnItemInstalled->Invoke(id);
}
