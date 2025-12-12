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

#include "address.h"
#include "bridge/adapter.h"
#include "bridge/corebridge.h"
#include "command.h"
#include "filesystemfix.h"
#include "gamedata.h"
#include "gamefactory.h"
#include "global.h"
#include "logging.h"
#include "manager/HookManager.h"
#include "module.h"
#include "sdkproxy.h"

#include "CoreCLR/coreclr.h"

#include <array>

IEngineServer*                   engine;
CSource2Server*                  server;
IServerGameClient*               gameClients;
IServerGameEntities*             gameEntities;
ISchemaSystem*                   schemaSystem;
IGameEventManager2*              eventManager;
CGlobalVars*                     gpGlobals;
IGameRules*                      gameRules;
ICvar*                           icvar;
ICvar*                           g_pCVar;
CNetworkGameServer*              sv;
IFileSystem*                     g_pFullFileSystem;
IGameResourceServiceServer*      g_pGameResourceServiceServer;
INetworkServerService*           g_pNetworkServerService;
IGameEventSystem*                g_pGameEventSystem;
INetworkMessages*                g_pNetworkMessages;
INetworkSystem*                  g_pNetworkSystem;
CSoundEventManager*              g_pSoundEventManager;
SoundOpGameSystem*               g_pSoundOpGameSystem;
CDedicatedServerWorkshopManager* g_pServerWorkshopManager;
IGameTypes*                      g_pGameTypes;
IScriptVM*                       g_pScriptVM;
INetworkStringTableContainer*    g_pStringTableContainer;
IResourceSystem*                 g_pResourceSystem;
IGameSpawnGroupMgr*              g_pSpawnGroupMgr;

unsigned int g_nMainThreadId;

#ifdef PLATFORM_WINDOWS
#    include <windows.h>

#    include <filesystem>
#    include <format>
#    include <fstream>

static void DumpVTableCount()
{
    if (!IsDebugMode())
        return;

    std::ifstream steam_inf("../../csgo/steam.inf");
    if (!steam_inf.good())
    {
        return;
    }

    std::string line{};
    std::string patch_version{};
    while (std::getline(steam_inf, line))
    {
        if (!line.starts_with("PatchVersion"))
            continue;
        patch_version = line.substr(line.find('=') + 1);
        break;
    }

    steam_inf.close();
    if (patch_version.empty())
    {
        FLOG("Failed to get patch version");
        return;
    }

    constexpr std::string_view data_path = "../../sharp/data";

    constexpr std::array server_vtables = {
        "CCSPlayerPawn",
        "CCSPlayerController",
        "CEntityInstance",
        "CBaseEntity",
        "CBaseAnimGraph",
        "CBaseCSGrenade",
        "CBaseCSGrenadeProjectile",
        "CAK47",
        "CBaseFilter",
        "CBaseTrigger",
        "CBaseToggle",
        "CCSPlayer_ItemServices",
        "CCSPlayer_WeaponServices",
        "CCSPlayer_MovementServices",
        "CCSPlayer_CameraServices",
        "CCSPlayerInventory",
        "CGameSceneNode",
        "CCSHitboxSystem",
        "CGameRulesGameSystem",
        "CSource2Server",
        "CSource2GameClients",
        "CSource2GameEntities",
    };

    constexpr std::array engine_vtables = {
        "CEngineServer",
        "CServerSideClient",
        "CNetworkGameServer",
        "CNetworkServerService",
        "CNetworkStringTable",
        "CNetworkStringTableContainer",
    };

    constexpr std::array tier0_vtables = {
        "IMemAlloc",
        "CCvar",
        "CCommandLine",
    };

    constexpr std::array vscript_vtables = {
        "IScriptVM",
        "IScriptManager",
    };

    constexpr std::array resource_vtables = {
        "CResourceSystem",
    };

    constexpr std::array vphysics_vtables = {
        "CVPhys2World",
    };

    constexpr std::array soundsystem_vtables = {
        "CSoundOpSystem",
        "CSoundEventManager",
    };

    if (!std::filesystem::exists(data_path))
        std::filesystem::create_directory(data_path);

    std::ofstream file(std::format("{}/vtable_{}.ini", data_path, patch_version));
    file << "[server.dll]" << '\n';
    for (const auto& server_vtable : server_vtables)
    {
        file << std::format("{}={}", server_vtable, modules::server->GetVFunctionsFromVTable(server_vtable).size()) << '\n';
    }
    file << '\n';

    file << "[engine2.dll]" << '\n';
    for (const auto& engine_vtable : engine_vtables)
    {
        file << std::format("{}={}", engine_vtable, modules::engine->GetVFunctionsFromVTable(engine_vtable).size()) << '\n';
    }
    file << '\n';

    file << "[tier0.dll]" << '\n';
    for (const auto& tier0_vtable : tier0_vtables)
    {
        file << std::format("{}={}", tier0_vtable, modules::tier0->GetVFunctionsFromVTable(tier0_vtable).size()) << '\n';
    }
    file << '\n';

    file << "[vscript.dll]" << '\n';
    for (const auto& vscript_vtable : vscript_vtables)
    {
        file << std::format("{}={}", vscript_vtable, modules::vscript->GetVFunctionsFromVTable(vscript_vtable).size()) << '\n';
    }
    file << '\n';

    file << "[resourcesystem.dll]" << '\n';
    for (const auto& resource_vtable : resource_vtables)
    {
        file << std::format("{}={}", resource_vtable, modules::resource->GetVFunctionsFromVTable(resource_vtable).size()) << '\n';
    }
    file << '\n';

    file << "[vphysics2.dll]" << '\n';
    for (const auto& vphysics_vtable : vphysics_vtables)
    {
        file << std::format("{}={}", vphysics_vtable, modules::vphysics2->GetVFunctionsFromVTable(vphysics_vtable).size()) << '\n';
    }
    file << '\n';

    file << "[soundsystem.dll]" << '\n';
    for (const auto& soundsystem_vtable : soundsystem_vtables)
    {
        file << std::format("{}={}", soundsystem_vtable, modules::sound->GetVFunctionsFromVTable(soundsystem_vtable).size()) << '\n';
    }
    file << '\n';
}
#endif

bool ModSharp_Init()
{
    extern void* FindGameSystemByName(const char* name);
    extern void  InitGameEntitySystem();
    extern void  InitHookManager();
    extern void  InitEconItemSchema();
    extern void  InitSchemaSystem();

    g_pLoggerMapName = {};
    g_nMainThreadId  = GetCurrentThreadId();

    auto gameEventManagerVtable = modules::server->GetVirtualTableByName("CGameEventManager");
    eventManager                = modules::server->FindPtr(gameEventManagerVtable).As<IGameEventManager2*>();

    const auto pSoundOpSystem = factory::engine->GetInterface<ISoundOpSystem*>("SoundOpSystem001");
    AssertPtr(pSoundOpSystem);

    g_pSoundEventManager     = reinterpret_cast<CSoundEventManager*>(reinterpret_cast<intptr_t>(pSoundOpSystem) + 8);
    g_pSoundOpGameSystem     = static_cast<SoundOpGameSystem*>(FindGameSystemByName("SoundOpGameSystem"));
    g_pServerWorkshopManager = static_cast<CDedicatedServerWorkshopManager*>(FindGameSystemByName("DedicatedServerWorkshopManager"));

    AssertPtr(eventManager);
    AssertPtr(g_pSoundEventManager);
    AssertPtr(g_pSoundOpGameSystem);
    AssertPtr(g_pServerWorkshopManager);

    CreateLogging();
    FixFileSystem();

#ifdef PLATFORM_WINDOWS
    DumpVTableCount();
#endif

    InitSchemaSystem();
    InitGameEntitySystem();
    InitHookManager();
    InitEconItemSchema();
    InitCoreBridge();
    InitialModSharpCommand();

#ifdef PLATFORM_WINDOWS
    SetConsoleOutputCP(CP_UTF8);
#endif

    coreclr::Init(Plat_GetGameDirectory());

    bridge::InitNatives();

    const auto clr = !!coreclr::Bootstrap(bridge::GetNatives(), bridge::GetForwards());

    return !clr;
}

void ModSharp_Shutdown()
{
    coreclr::Shutdown();

    HookManager::Uninstall();
}