/* 
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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
#include "global.h"
#include "hook/network.h"
#include "logging.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "sdkproxy.h"
#include "steamworks.h"
#include "strtool.h"

#include "cstrike/interface/IFileSystem.h"
#include "cstrike/interface/INetChannel.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/IProtobufBinding.h"
#include "cstrike/type/CHostState.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/KeyValues.h"

#include <proto/networkbasetypes.pb.h>

#include <safetyhook.hpp>

// #define DUAL_ADDON_ASSERT

constexpr int32_t NET_MESSAGE_ID_SIGNON = 7;
constexpr double  DUAL_ADDON_TIMEOUT    = 20;
constexpr double  BYPASS_ADDON_TIMEOUT  = 600;

static std::unordered_map<SteamId_t, double> s_PendingMountClient;
static std::unordered_map<SteamId_t, double> s_BypassCheckingTime;
static bool                                  s_IsOfficialWorkshopMap = false;
static bool                                  s_bReconnectionRequired = false;
static std::string                           s_CurrentWorkshopMap;

static double GetPendingClientState(SteamId_t steamid)
{
    const auto data = s_PendingMountClient.find(steamid);
    if (data == s_PendingMountClient.end())
        return -1;

    return data->second;
}

static bool IsBypassChecking(SteamId_t steamid)
{
    const auto data = s_BypassCheckingTime.find(steamid);
    if (data == s_BypassCheckingTime.end())
        return false;

    if ((Plat_FloatTime() - data->second) < BYPASS_ADDON_TIMEOUT)
        return true;

    s_BypassCheckingTime.erase(steamid);
    return false;
}

static CServerSideClient* GetPendingClient(const INetChannel* pNetChan)
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

BeginStaticHookScope(HostStateRequest)
{
    DeclareStaticDetourHook(HostStateRequest, void, (void* a1, CHostStateRequest* pRequest))
    {
        s_IsOfficialWorkshopMap = false;
        s_bReconnectionRequired = false;
        s_CurrentWorkshopMap.clear();

        const auto addonId = GetDualAddonId();
        if (addonId == 0)
            return HostStateRequest(a1, pRequest);

        LOG("HostStateRequest -> Addons=[%s] LevelName=[%s] ChangeLevel=[%s]", pRequest->m_Addons.Get(), pRequest->m_LevelName.Get(), BooleanSTR(pRequest->m_bChangeLevel));

        if (auto kv = pRequest->m_pKV; kv != nullptr)
        {
            const std::string kv_name = kv->GetName();
            if (kv_name.starts_with("map_workshop")) // default is 'ChangeLevel'
            {
                s_CurrentWorkshopMap    = kv->GetString("customgamemode", "");
                s_bReconnectionRequired = true;

#ifdef DUAL_ADDON_ASSERT
                LOG("HostStateRequest -> kv = %s -> map_workshop = %s", kv_name.c_str(), s_CurrentWorkshopMap.c_str());
#endif
            }
            else
            {
#ifdef DUAL_ADDON_ASSERT
                LOG("HostStateRequest -> kv = %s ", kv_name.c_str());
                kv->SaveToFile(g_pFullFileSystem, "HostStateRequest.kv", "GAME", true);
#endif
            }
        }
        else if (const std::string sOriginalAddonString = pRequest->m_Addons.Get();
                 !pRequest->m_LevelName.IsEmpty() && pRequest->m_bChangeLevel && !sOriginalAddonString.empty() && StrIsNumber(sOriginalAddonString))
        {
            s_CurrentWorkshopMap    = sOriginalAddonString;
            s_bReconnectionRequired = true;

#ifdef DUAL_ADDON_ASSERT
            LOG("HostStateRequest -> kv = %s -> addons = %s", "nullptr", sOriginalAddonString.c_str());
#endif
        }

        // 这里不能用pRequest->m_Addon
        // 因为有时候如果从de_mirage换成官方的第三方图，比如cs_agency
        // m_Addon不会带cs_agency而是DualAddonId
        // 导致客户端不会加载cs_agency，从而出现材质缺失的情况
        if (!pRequest->m_LevelName.IsEmpty()
            && g_pFullFileSystem->IsDirectory(pRequest->m_LevelName.Get(), "OFFICIAL_ADDONS")
            && g_pFullFileSystem->FileExists(FString("%s/%s_dir.vpk", pRequest->m_LevelName.Get(), pRequest->m_LevelName.Get()), "OFFICIAL_ADDONS"))
        {
            s_CurrentWorkshopMap    = pRequest->m_LevelName.Get();
            s_IsOfficialWorkshopMap = true;
            s_bReconnectionRequired = false;
        }

        std::vector<std::string> addons{};
        const auto addonIdString = std::to_string(addonId);

        if (!s_CurrentWorkshopMap.empty() && strcasecmp(addonIdString.c_str(), s_CurrentWorkshopMap.c_str()) == 0)
        {
            addons.emplace_back(addonIdString);
        }
        else if (s_IsOfficialWorkshopMap)
        {
            if (std::ranges::find(addons, addonIdString) == addons.end())
                addons.emplace_back(addonIdString);

            if (!s_CurrentWorkshopMap.empty() && std::ranges::find(addons, s_CurrentWorkshopMap) == addons.end())
                addons.emplace_back(s_CurrentWorkshopMap);
        }
        else
        {
            if (!s_CurrentWorkshopMap.empty() && std::ranges::find(addons, s_CurrentWorkshopMap) == addons.end())
                addons.emplace_back(s_CurrentWorkshopMap);

            if (std::ranges::find(addons, addonIdString) == addons.end())
                addons.emplace_back(addonIdString);
        }

        pRequest->m_Addons = StringJoin(addons, ",").c_str();

        LOG("HostStateRequest --> Addons -> [%s]\n  -> current_workshop_map   = %s\n  -> official_workshop_map  = %s\n  -> request_reconnection   = %s",
            pRequest->m_Addons.Get(),
            s_CurrentWorkshopMap.c_str(),
            BooleanSTR(s_IsOfficialWorkshopMap),
            BooleanSTR(s_bReconnectionRequired));

        HostStateRequest(a1, pRequest);
    }
}

BeginMemberHookScope(INetChannel)
{
    DeclareMemberDetourHook(SendNetMessage, bool, (INetChannel * pNetChannel, CNetMessagePB<CNETMsg_SignonState> * pData, int a4))
    {
        if (s_bBypassNetMessageHook)
            return SendNetMessage(pNetChannel, pData, a4);

        const auto pInfo = pData->GetNetMessage()->GetNetMessageInfo();

        if (pInfo->m_MessageId != NET_MESSAGE_ID_SIGNON || !s_bReconnectionRequired)
            return SendNetMessage(pNetChannel, pData, a4);

#ifdef DUAL_ADDON_ASSERT
        LOG("SignonState State=%d Addons=[%s]", pData->signon_state(), pData->addons().c_str());
#endif

        const auto addonId = GetDualAddonId();
        if (addonId == 0)
            return SendNetMessage(pNetChannel, pData, a4);

        const auto pClient = GetPendingClient(pNetChannel);
        if (!pClient)
            return SendNetMessage(pNetChannel, pData, a4);

        // HACK Valve 24/7/27更新损坏了这个, 如果你在服务器里的时候, 因为原生换图下发了2个或以上的ID, 客户端会直接卡住
        if (strchr(pData->addons().c_str(), ',') != nullptr)
        {
            if (auto vecAddons = StringSplit(pData->addons().c_str(), ","); !vecAddons.empty() && !s_IsOfficialWorkshopMap)
            {
#ifdef DUAL_ADDON_ASSERT
                LOG("Override addon: %s -> %s", pData->addons().c_str(), vecAddons[0].c_str());
#endif
                pData->set_addons(vecAddons[0]);
            }
        }
        else if (GetPendingClientState(pClient->GetSteamId()) >= 0)
        {
            const auto buffer = std::to_string(addonId);

            s_PendingMountClient[pClient->GetSteamId()] = Plat_FloatTime();

#ifdef DUAL_ADDON_ASSERT
            LOG("Sending addon %llu to client %s <%llu> :: %s -> %d", addonId, pClient->GetName(), pClient->GetSteamId(), buffer.c_str(), pData->signon_state());
#endif

            pData->set_addons(buffer);
            pData->set_signon_state(SIGNONSTATE_CHANGELEVEL);
        }

        return SendNetMessage(pNetChannel, pData, a4);
    }
}

static void OnClientConnectPre(PlayerSlot_t slot, const char* name, SteamId_t steamId, bool bot)
{
    if (bot)
        return;

    if (GetDualAddonId() == 0)
        return;

    const auto pClient = sv->GetClientSafety(slot);
    if (!pClient)
    {
        LogInfo("DualMountAddon::OnClientConnectPre -> %s <%llu> with nullptr", name, steamId);
        return;
    }

    if (!s_bReconnectionRequired)
    {
        s_PendingMountClient.erase(steamId);
        return;
    }

    AssertBool(pClient->GetSteamId() == steamId);

    if (IsBypassChecking(steamId))
        return;

    const auto state = GetPendingClientState(steamId);

    if (state < 0)
    {
#ifdef DUAL_ADDON_ASSERT
        LOG("Client %s <%llu> will retry for dual addon", name, steamId);
#endif
        s_PendingMountClient[steamId] = 0;
    }
    else if ((Plat_FloatTime() - state) < DUAL_ADDON_TIMEOUT)
    {
#ifdef DUAL_ADDON_ASSERT
        LOG("Client %s <%llu> reconnect within time", name, steamId);
#endif
        s_PendingMountClient.erase(steamId);
    }
    else
    {
#ifdef DUAL_ADDON_ASSERT
        LOG("Client %s <%llu>  has reconnected after the timeout or did not receive the addon message, will send addon message again", name, steamId);
#endif
    }
}

void InstallDualMountAddonHooks()
{
    g_pHookManager->Hook_ClientConnect(HookType_Pre, OnClientConnectPre);

    static double s_flNextUpdate;
    g_pHookManager->Hook_GameFrame(HookType_Post, [](const bool s, bool f, bool l) {
        if (!sv)
            return;

        const auto flTime = Plat_FloatTime();
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
            if (const auto find = s_BypassCheckingTime.find(steamId); find != s_BypassCheckingTime.end() && find->second > flTime)
                continue;

            s_BypassCheckingTime[steamId] = flTime;
        }
    });

    InstallStaticDetourAutoSig(HostStateRequest);
    InstallMemberDetourAutoSig(INetChannel, SendNetMessage);
}

void DualMountAddonOverrideClientCheck(SteamId_t steamId, double time)
{
    s_BypassCheckingTime[steamId] = time;
}

void DualMountAddonPurgeClientCheck()
{
    s_BypassCheckingTime.clear();
}