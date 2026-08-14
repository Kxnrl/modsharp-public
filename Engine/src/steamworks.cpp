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

#include <fstream>
#include <json.hpp>
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

// Resolves the dual-addon id once, from the command line first and configs/core.json second.
//
// The command line still wins, so nothing changes for an existing server. The file exists because
// many hosts do not expose startup arguments to the person renting the server — a control panel with
// a file manager and a restart button is common — and -dual_addon is otherwise the one setting they
// cannot reach.
//
// It cannot be a convar: FixFileSystem() needs this during filesystem setup, long before any cfg
// executes. core.json is read directly here for the same reason — the managed configuration is not
// available until CoreCLR starts, which is later still.
//
// The result is cached, so the file is read at most once per process.
uint64_t ResolveConfiguredDualAddonId()
{
    // Function-local static: initialised exactly once, thread-safe by the standard, and the file is
    // read at most once per process.
    static const uint64_t s_resolved = []() -> uint64_t {
        if (CommandLine()->HasParam("-dual_addon"))
        {
            if (const auto pszValue = CommandLine()->ParamValue("-dual_addon", nullptr))
            {
                if (const uint64_t ugcId = strtoull(pszValue, nullptr, 10); ugcId > 0)
                {
                    return ugcId;
                }
            }
        }

        // configs/core.json, the same file the managed side reads. Parsed here rather than passed
        // down from Sharp.Core because FixFileSystem() needs the value before CoreCLR is up.
        try
        {
            std::ifstream stream("../../sharp/configs/core.json");

            if (!stream.is_open())
            {
                return 0;
            }

            const auto json = nlohmann::json::parse(stream, nullptr, /*allow_exception*/ true, /*ignore_comments*/ true);
            const auto it   = json.find("DualAddonId");

            if (it == json.end())
            {
                return 0;
            }

            // Accept a number or a string — a 64-bit id is long enough that people quote it.
            if (it->is_number_unsigned())
            {
                return it->get<uint64_t>();
            }

            if (it->is_string())
            {
                return strtoull(it->get<std::string>().c_str(), nullptr, 10);
            }
        }
        catch (const std::exception& e)
        {
            // A malformed core.json is the managed side's problem to report; here it just means the
            // value is unset, which is the existing behaviour when the flag is absent.
            LOG("Could not read DualAddonId from core.json: %s", e.what());
        }

        return 0;
    }();

    return s_resolved;
}

void InitApiContext()
{
    if (const auto ugcId = ResolveConfiguredDualAddonId(); ugcId > 0)
    {
        g_unDualAddonId = ugcId;
        LOG("Load dual addon = %llu", ugcId);
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
    g_unDualAddonId     = 0;
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
