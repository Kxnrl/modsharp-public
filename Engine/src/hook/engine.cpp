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

#include "address.h"
#include "bridge/forwards/forward.h"
#include "gamedata.h"
#include "global.h"
#include "hook/installer.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "sdkproxy.h"
#include "steamproxy.h"
#include "strtool.h"

#include "cstrike/entity/PlayerController.h"
#include "cstrike/interface/ICvar.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/INetworkServerService.h"
#include "cstrike/interface/IServerGameClient.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/CUtlBuffer.h"
#include "cstrike/type/VProf.h"
#include "cstrike/type/netadr_t.h"

#include <proto/connectionless_netmessages.pb.h>

#include <safetyhook.hpp>

CConVarBaseData*        g_pModSharpConVar;
static CConVarBaseData* ms_fix_server_query_players = nullptr;

static std::unordered_map<uint64_t, int32_t> s_bBannedSteamIds;

#define ENGINE_HOOK_ASSERT
// #define CONNECT_HOOK_ASSERT

BeginMemberHookScope(CNetworkGameServer)
{
    DeclareMemberDetourHook(ActiveServer, bool, (CNetworkGameServer * pServer))
    {
#ifdef ENGINE_HOOK_ASSERT
        WARN("%10s: 0x%p\n"
             "%10s: %f\n"
             "%10s: %f",
             "this", pServer, "flCurTime", gpGlobals->flCurTime, "time", Plat_FloatTime());
#endif

        g_pHookManager->Call_ServerActivate(HookType_Pre);

        const auto ret = ActiveServer(pServer);

        g_pHookManager->Call_ServerActivate(HookType_Post);

        forwards::OnServerActivate->Invoke();

        return ret;
    }

    DeclareVirtualHook(DisconnectClient, void, (CNetworkGameServer * pServer, CServerSideClient * pClient, IServerGameClient::ENetworkDisconnectionReason eReason))
    {
#ifdef CONNECT_HOOK_ASSERT
        WARN("%10s: 0x%p\n"
             "%10s: 0x%p\n"
             "%10s: %d\n"
             "%10s: %s\n"
             "%10s: %d\n"
             "%10s: %f\n"
             "%10s: %f",
             "this", pServer,
             "pClient", pClient,
             "eReason", eReason,
             "Name", pClient->GetName(),
             "State", pClient->GetSignonState(),
             "flCurTime", gpGlobals->flCurTime,
             "time", Plat_FloatTime());
#endif

        // Active以下就不会发送事件
        // 引擎会在 Loading 阶段强制 SIGNONSTATE_CHANGELEVEL 的玩家断线重连
        // 所以这里的解决方案就是
        // 手动判断符合条件的数据然后手动调用Disconnect

        // Edit [23/12/19]
        // 貌似现在换图已经不会在换图时为 ss_Dead 了

        if (pClient->GetControllerEntityIndex() && pClient->GetSignonState() < CServerSideClient::SIGNONSTATE_CHANGELEVEL && pClient->GetSignonState() >= CServerSideClient::SIGNONSTATE_NEW && pServer->GetState() < CNetworkGameServer::Active)
        {
            FLOG("%12s: %s<%llu>(%d.%d)\n"
                 "%12s: %d\n"
                 "%12s: %d",
                 "Client", pClient->GetName(), pClient->GetSteamId(), pClient->GetSlot(), pClient->GetUserId(),
                 "Reason", eReason,
                 "SignonState", pClient->GetSignonState());

            forwards::OnClientDisconnecting->Invoke(pClient, static_cast<int32_t>(eReason));
            forwards::OnClientDisconnected->Invoke(pClient, static_cast<int32_t>(eReason));
        }

        DisconnectClient(pServer, pClient, eReason);

        const auto steamId = pClient->GetSteamId();
        if (steamId > 0 && (eReason == 152 || eReason == 153 || eReason == 154 || eReason == 161))
        {
            s_bBannedSteamIds[steamId] = eReason;
            FLOG("Banned SteamId: %llu", steamId);
        }
    }

    DeclareMemberDetourHook(SpawnServer_Unknown, bool, (CNetworkGameServer * pServer))
    {
        // if hibernate is on, the frame won't process :)
        engine->ServerCommand("sv_hibernate_when_empty 0\n");

        // earliest available for get this shit
        sv        = pServer;
        gpGlobals = pServer->GetGlobalVars();

#ifdef ENGINE_HOOK_ASSERT
        WARN("%10s: 0x%p\n"
             "%10s: %f\n"
             "%10s: %f",
             "this", pServer, "flCurTime", gpGlobals->flCurTime, "time", Plat_FloatTime());
#endif

        g_pHookManager->Call_ServerInit(HookType_Pre);

        const auto bRet = SpawnServer_Unknown(pServer);

        g_pHookManager->Call_ServerInit(HookType_Post);

        forwards::OnServerInit->Invoke();

        return bRet;
    }

    DeclareMemberDetourHook(SpawnServer, bool, (CNetworkGameServer * pServer, const char* pOptions))
    {
#ifdef ENGINE_HOOK_ASSERT
        WARN("%10s: 0x%p\n"
             "%10s: %f\n"
             "%10s: %f",
             "this", pServer, "flCurTime", gpGlobals->flCurTime, "time", Plat_FloatTime());
#endif

        g_pHookManager->Call_ServerSpawn(HookType_Pre);

        const auto bRet = SpawnServer(pServer, pOptions);

        g_pHookManager->Call_ServerSpawn(HookType_Post);

        forwards::OnServerSpawn->Invoke();

        return bRet;
    }

    DeclareMemberDetourHook(PrintStatus, void, (CNetworkGameServer * pServer, uint32_t slot))
    {
#ifdef ENGINE_HOOK_ASSERT
        WARN("%10s: 0x%p\n" // CNetworkGameServer*
             "%10s: %d",    // uint32_t
             "this", pServer, "slot", slot);
#endif

        CServerSideClient* pClient = nullptr;
        if (slot != 0xFFFFFFFF)
            pClient = pServer->GetClient(static_cast<PlayerSlot_t>(slot));

        const auto action = forwards::OnPrintStatus->Invoke(pClient);
        if (action == EHookAction::SkipCallReturnOverride)
            return;

        if (action == EHookAction::Ignored)
        {
            g_bInLoggingFlow = true;
            PrintStatus(pServer, slot);
            g_bInLoggingFlow = false;
            return;
        }

        FatalError("Not impl yet");
    }

    DeclareMemberDetourHook(GetFreeClient, CServerSideClient*, (CNetworkGameServer * pServer, void* a2, int64_t a3, int64_t a4, int8_t a5, void* a6))
    {
        const auto pResult = GetFreeClient(pServer, a2, a3, a4, a5, a6);
        if (pResult)
            return pResult;

        const auto pClients = pServer->GetClients();
        if (pClients->Count() < static_cast<int>(gpGlobals->MaxClients))
            return pResult;

        for (auto i = pClients->Count() - 1; i >= 0; i--)
        {
            const auto pClient = pClients->Element(i);
            if (pClient == nullptr)
                continue;

            if (pClient->GetSignonState() < CServerSideClient::SIGNONSTATE_CONNECTED)
                return pClient;
        }

        return nullptr;
    }

    DeclareMemberDetourHook(ConnectClient, CServerSideClient*, (CNetworkGameServer * pServer, const char* pName, netadr_t* pNetAddress, void* pNetInfo, C2S_CONNECT_Message* pMsg, const char* pszPassword, const void* hashedCdKey, int cdkeyLength, bool bLowViolence))
    {
#ifdef CONNECT_HOOK_ASSERT
        WARN("%10s: 0x%p\n" // CNetworkGameServer*
             "%10s: %s\n"   // const char*
             "%10s: 0x%p\n" // void*
             "%10s: 0x%p\n" // void*
             "%10s: 0x%p\n" // C2S_CONNECT_Message*
             "%10s: %s\n"   // const char*
             "%10s: 0x%p\n" // const void*
             "%10s: %d\n"   // int
             "%10s: %s",    // bool -> String
             "this", pServer,
             "pName", pName,
             "pNetAddress", pNetAddress,
             "pNetInfo", pNetInfo,
             "pMsg", pMsg,
             "pszPassword", pszPassword,
             "hashedCdKey", hashedCdKey,
             "cdkeyLength", cdkeyLength,
             "bLowViolence", BOOLVAL(bLowViolence));
#endif

        VPROF_MS_HOOK();

        CUtlBuffer buffer{hashedCdKey, cdkeyLength, CUtlBuffer::READ_ONLY};
        const SteamId_t steamId = *(SteamId_t*)(buffer.PeekGet(sizeof(SteamId_t), 0));

        // 无法获取SteamId直接拒绝连接
        if (steamId == 0 || (steamId & 0xFFFFFFFF) == 0) [[unlikely]]
        {
            // AUTH INVALID
            g_pNetworkSystem->RejectNetChannel(pNetInfo, 12);
            return nullptr;
        }

        const auto ban = s_bBannedSteamIds.find(steamId);
        if (ban != s_bBannedSteamIds.end())
        {
            LOG("Banned %llu try connecting...", steamId);
            g_pNetworkSystem->RejectNetChannel(pNetInfo, ban->second);
            return nullptr;
        }

        const auto ip = pNetAddress->GetIPHostByteOrder();

        switch (forwards::OnConnectClient->Invoke(steamId, pName, pNetInfo, ip))
        {
        case EHookAction::SkipCallReturnOverride:
            return nullptr;
        case EHookAction::Ignored:
            break;
        default:
            FatalError("Not impl yet");
        }

        return ConnectClient(pServer, pName, pNetAddress, pNetInfo, pMsg, pszPassword, hashedCdKey, cdkeyLength, bLowViolence);
    }
}

BeginMemberHookScope(INetworkServerService)
{
    DeclareVirtualHook(StartupServer, void, (INetworkServerService * pService, const GameSessionConfiguration_t& config, ISource2WorldSession* pWorldSession, const char* pszMapName))
    {
        g_pSpawnGroupMgr = nullptr;

#ifdef ENGINE_HOOK_ASSERT
        WARN("%10s: 0x%p\n"
             "%10s: 0x%p\n"
             "%10s: 0x%p",
             "this", pService, "config", pWorldSession, "unknown", pszMapName);
#endif

        g_pLoggerMapName = "| ";
        g_pLoggerMapName += pszMapName;
        g_pLoggerMapName += " ";

        g_pSpawnGroupMgr = nullptr;

        forwards::OnStartupServerPre->Invoke();

        StartupServer(pService, config, pWorldSession, pszMapName);

        sv = g_pNetworkServerService->GetIGameServer();
    }
}

void InstallEngineHooks()
{
    HOOK(CNetworkGameServer, SpawnServer_Unknown);
    HOOK(CNetworkGameServer, SpawnServer);
    HOOK(CNetworkGameServer, PrintStatus);
    HOOK(CNetworkGameServer, GetFreeClient);

    HOOK(CNetworkGameServer, ActiveServer);

    // BUG 运行在客户端模式下时, 该功能会导致游戏无法启动
    if (CommandLine()->HasParam("-dedicated"))
    {
        VHOOK(CNetworkGameServer, DisconnectClient, engine);
        HOOK(CNetworkGameServer, ConnectClient);
    }

    // TODO steamnetworkingsockets -> str 'SendServerBrowserPacket' patch MTU
    if (CommandLine()->HasParam("-dedicated"))
    {
        static double s_flNextUpdate;
        g_pHookManager->Hook_GameFrame(HookType_Post, [](const bool s, bool f, bool l) {
            if (!s || !sv || !gpGlobals)
                return;

            const auto flTime = Plat_FloatTime();
            if (flTime <= s_flNextUpdate || !ms_fix_server_query_players->GetValue<bool>())
                return;

            s_flNextUpdate = flTime + 2.0;

            for (PlayerSlot_t i = 0u; i < CS_MAX_PLAYERS && i < gpGlobals->MaxClients; i++)
            {
                const auto pSteamId = engine->GetClientSteamId(i);
                if (!pSteamId)
                    continue;

                const auto pController = CBasePlayerController::FindBySlot(i);
                if (!pController)
                    continue;

                g_pSteamApiProxy->BUpdateUserData(*pSteamId, pController->GetPlayerName(), gameClients->GetPlayerScore(i));
            }
        });
    }

    VHOOK(INetworkServerService, StartupServer, engine, {.vtable = "CNetworkServerService"});

    g_pModSharpConVar           = g_ConVarManager.CreateConVar("modsharp_engine_version", FString("%s (%s)", MODSHARP_VERSION, MODSHARP_DATE), "Version of ModSharp Engine.", FCVAR_RELEASE | FCVAR_REPLICATED);
    ms_fix_server_query_players = g_ConVarManager.CreateConVar("ms_fix_server_query_players", false, "Fix A2S_Players response.", FCVAR_RELEASE);

    UnlockHiddenCommand("path", true);
    UnlockHiddenCommand("addons", true);
    UnlockHiddenCommand("vprof_on", true);
    UnlockHiddenCommand("vprof_off", true);
    UnlockHiddenCommand("vprof_reset", true);
    UnlockHiddenCommand("vprof_vtrace", true);
    UnlockHiddenCommand("vprof_dump_counters", true);
    UnlockHiddenCommand("vprof_generate_report", true);
    UnlockHiddenCommand("vprof_generate_report_budget", true);
    UnlockHiddenCommand("vprof_generate_report_hierarchy", true);
    UnlockHiddenCommand("dumpstringtable", true);
}

/*

- Engine
  - StartupServer
    - SpawnServer_Unknown
  - LevelInit
  - SpawnServer
  - WorldSpawn
  - ActiveServer

*/