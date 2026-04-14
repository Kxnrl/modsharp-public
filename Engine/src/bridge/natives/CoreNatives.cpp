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

#include "bridge/natives/CoreNatives.h"
#include "bridge/adapter.h"
#include "bridge/hookbridge.h"

#include "address.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "memory/memory_access.h"
#include "module.h"
#include "sdkproxy.h"
#include "strtool.h"
#include "vhook/hook.h"

#include "cstrike/interface/CDedicatedServerWorkshopManager.h"
#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/IFileSystem.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/INetworkStringTable.h"
#include "cstrike/interface/IServerGameClient.h"
#include "cstrike/type/CHashKey.h"

namespace natives::core
{

static CGlobalVars* GetGlobals()
{
    return gpGlobals;
}

static CNetworkGameServer* GetIServer()
{
    return sv;
}

static IGameRules* GetGameRules()
{
    return gameRules;
}

static double GetEngineTime()
{
    return Plat_FloatTime();
}

static void EngineError(const char* text)
{
    FatalError("CoreCLR Error: %s", text);
}

static void RejectConnection(void* pNetInfo, IServerGameClient::ENetworkDisconnectionReason reason)
{
    AssertPtr(g_pNetworkSystem);
    g_pNetworkSystem->RejectNetChannel(pNetInfo, reason);
}

static void LogMsg(const char* text)
{
    ConsoleMessage("Sharp.Core", text);
}

static void LogWarning(const char* text)
{
    ConsoleWarning("Sharp.Core", text);
}

static void ColorText(char r, char g, char b, const char* text)
{
    ConsoleText(Color(r, g, b), text);
}

static void ChangeLevel(const char* map)
{
    engine->ChangeLevel(map);
}

static bool IsMapValid(const char* map)
{
    // official map
    if (g_pFullFileSystem->FileExists(FString("maps/%s.vpk", map), "GAME"))
    {
        return true;
    }

    // official community map
    if (g_pFullFileSystem->IsDirectory(map, "OFFICIAL_ADDONS") && g_pFullFileSystem->FileExists(FString("%s/%s_dir.vpk", map, map), "OFFICIAL_ADDONS"))
    {
        return true;
    }

    // workshop
    CUtlVector<WorkshopMap_t> workshopMaps;
    g_pServerWorkshopManager->ListWorkshopMaps(&workshopMaps);

    if (workshopMaps.Count() == 0)
        return false;

    for (const auto& workshop : workshopMaps)
    {
        if (strcasecmp(workshop.m_pName, map) == 0)
            return true;
    }

    return false;
}

static void ServerCommand(const char* command)
{
    char buffer[256];
    snprintf(buffer, sizeof(buffer), "%s\n", command);
    engine->ServerCommand(buffer);
}

static GameData* GetGameData()
{
    return g_pGameData;
}

static INetworkStringTable* FindStringTable(const char* name)
{
    return g_pStringTableContainer->FindTable(name);
}

static CoreBridge* GetCoreBridge()
{
    return g_pCoreBridge;
}

static const char* GetGameDirectory()
{
    return Plat_GetGameDirectory();
}

static const char* GetCommandLineNative()
{
    return CommandLine()->GetCmdLine();
}

static InlineHook* CreateInlineHook()
{
    return new InlineHook();
}

static void RemoveInlineHook(void* ptr)
{
    delete static_cast<InlineHook*>(ptr);
}

static MidFuncHook* CreateMidFuncHook()
{
    return new MidFuncHook();
}

static void RemoveMidFuncHook(void* ptr)
{
    delete static_cast<MidFuncHook*>(ptr);
}

static CModule* GetLibraryModule(const char* module)
{
    if (strcasecmp(module, "engine") == 0)
    {
        return modules::engine;
    }
    if (strcasecmp(module, "server") == 0)
    {
        return modules::server;
    }
    if (strcasecmp(module, "tier0") == 0)
    {
        return modules::tier0;
    }
    if (strcasecmp(module, "schemas") == 0)
    {
        return modules::schemas;
    }
    if (strcasecmp(module, "resource") == 0)
    {
        return modules::resource;
    }
    if (strcasecmp(module, "vscript") == 0)
    {
        return modules::vscript;
    }
    if (strcasecmp(module, "vphysics2") == 0)
    {
        return modules::vphysics2;
    }
    if (strcasecmp(module, "sound") == 0)
    {
        return modules::sound;
    }
    if (strcasecmp(module, "network") == 0)
    {
        return modules::network;
    }
    if (strcasecmp(module, "worldrenderer") == 0)
    {
        return modules::worldrenderer;
    }
    if (strcasecmp(module, "matchmaking") == 0)
    {
        return modules::matchmaking;
    }
    if (strcasecmp(module, "filesystem") == 0)
    {
        return modules::filesystem;
    }
    if (strcasecmp(module, "steamsockets") == 0)
    {
        return modules::steamsockets;
    }
    if (strcasecmp(module, "materialsystem2") == 0)
    {
        return modules::materialsystem2;
    }
    if (strcasecmp(module, "animationsystem") == 0)
    {
        return modules::animationsystem;
    }

    FatalError("Invalid module '%s'", module);

    return nullptr;
}

static void* FindValveInterface(const char* module, const char* name)
{
    return GetLibraryModule(module)->FindInterfaceEx(name);
}

static void* FindModulePattern(const char* module, const char* pattern)
{
    auto pModule = GetLibraryModule(module);
    if (!pModule)
        return nullptr;
    return pModule->FindPatternStrict(pattern).As();
}

static void* GetVTableByClass(const char* module, const char* className)
{
    auto pModule = GetLibraryModule(module);
    if (!pModule)
        return nullptr;
    return pModule->GetVirtualTableByName(className).As();
}

static bool SetMemoryAccess(uint8_t* address, size_t size, uint8_t access, uint8_t* original_acess)
{
    return ::SetMemoryAccess(address, size, access, original_acess);
}

static void* InstallVirtualHook(void* vTable, int index, void* hook)
{
    return vtablehook_hook_direct(vTable, hook, index);
}

static uint32_t NativeMakeStringToken(const char* str)
{
    CHashKey key(str);
    return key.GetHashCode();
}

void Init()
{
    bridge::CreateNative("Core.GetGameDirectory", reinterpret_cast<void*>(GetGameDirectory));
    bridge::CreateNative("Core.GetCommandLine", reinterpret_cast<void*>(GetCommandLineNative));
    bridge::CreateNative("Core.GetCoreBridge", reinterpret_cast<void*>(GetCoreBridge));
    bridge::CreateNative("Core.GetGlobals", reinterpret_cast<void*>(GetGlobals));
    bridge::CreateNative("Core.GetIServer", reinterpret_cast<void*>(GetIServer));
    bridge::CreateNative("Core.GetEngineTime", reinterpret_cast<void*>(GetEngineTime));
    bridge::CreateNative("Core.GetGameRules", reinterpret_cast<void*>(GetGameRules));
    bridge::CreateNative("Core.LogMessage", reinterpret_cast<void*>(LogMsg));
    bridge::CreateNative("Core.LogWarning", reinterpret_cast<void*>(LogWarning));
    bridge::CreateNative("Core.LogColorText", reinterpret_cast<void*>(ColorText));
    bridge::CreateNative("Core.FatalError", reinterpret_cast<void*>(EngineError));
    bridge::CreateNative("Core.RejectConnection", reinterpret_cast<void*>(RejectConnection));

    // VEngineServer
    bridge::CreateNative("Engine.ChangeLevel", reinterpret_cast<void*>(ChangeLevel));
    bridge::CreateNative("Engine.IsMapValid", reinterpret_cast<void*>(IsMapValid));
    bridge::CreateNative("Engine.ServerCommand", reinterpret_cast<void*>(ServerCommand));

    // GameData
    bridge::CreateNative("Core.GetGameData", reinterpret_cast<void*>(GetGameData));

    // StringTable
    bridge::CreateNative("Core.FindStringTable", reinterpret_cast<void*>(FindStringTable));

    // FuncHook
    bridge::CreateNative("Core.CreateInlineHook", reinterpret_cast<void*>(CreateInlineHook));
    bridge::CreateNative("Core.RemoveInlineHook", reinterpret_cast<void*>(RemoveInlineHook));
    bridge::CreateNative("Core.CreateMidFuncHook", reinterpret_cast<void*>(CreateMidFuncHook));
    bridge::CreateNative("Core.RemoveMidFuncHook", reinterpret_cast<void*>(RemoveMidFuncHook));

    // Memory/Pattern
    bridge::CreateNative("Core.FindPattern", reinterpret_cast<void*>(FindModulePattern));
    bridge::CreateNative("Core.GetVTableByClass", reinterpret_cast<void*>(GetVTableByClass));
    bridge::CreateNative("Core.GetLibraryModule", reinterpret_cast<void*>(GetLibraryModule));
    bridge::CreateNative("Core.FindValveInterface", reinterpret_cast<void*>(FindValveInterface));
    bridge::CreateNative("Core.SetMemoryAccess", reinterpret_cast<void*>(SetMemoryAccess));

    // Virtual Hook
    bridge::CreateNative("Core.InstallVirtualHook", reinterpret_cast<void*>(InstallVirtualHook));

    bridge::CreateNative("Core.MakeStringToken", reinterpret_cast<void*>(NativeMakeStringToken));
}
} // namespace natives::core
