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

#ifndef MS_ROOT_GLOBAL_H
#define MS_ROOT_GLOBAL_H

#include "definitions.h"
#include "platform.h"
#include "types.h"
#include "version.h"

#define FORWARD_PREFIX "Bridge.Forwards."
#define NATIVE_PREFIX "Bridge.Natives."

inline constexpr char MODSHARP_VERSION[] = FULL_VERSION_STRING;
inline constexpr char MODSHARP_DATE[]    = BUILD_DATE;
inline constexpr char MODSHARP_MESSAGE[] = "\n \n"
                                           "      ModSharp " FULL_VERSION_STRING " \n"
                                           "      " BUILD_DATE "\n"
                                           "   " COPYRIGHT_STRING "\n"
                                           "\n"
                                           "Visit https://modsharp.net/\n ";

class CModule;
class IMemAlloc;

using CreateInterface_t = void* (*)(const char* name, int* ret);

// CStrike relate
extern class CoreBridge*                      g_pCoreBridge;
extern class IEngineServer*                   engine;
extern class CSource2Server*                  server;
extern class IServerGameClient*               gameClients;
extern class IServerGameEntities*             gameEntities;
extern class ISchemaSystem*                   schemaSystem;
extern class IGameEventManager2*              eventManager;
extern class CGameEntitySystem*               g_pGameEntitySystem;
extern class CGlobalVars*                     gpGlobals;
extern class IGameRules*                      gameRules;
extern class ICvar*                           icvar;
extern class ICvar*                           g_pCVar;
extern class CNetworkGameServer*              sv;
extern class IFileSystem*                     g_pFullFileSystem;
extern class IGameResourceServiceServer*      g_pGameResourceServiceServer;
extern class INetworkServerService*           g_pNetworkServerService;
extern class IGameEventSystem*                g_pGameEventSystem;
extern class INetworkMessages*                g_pNetworkMessages;
extern class INetworkSystem*                  g_pNetworkSystem;
extern class CSoundEventManager*              g_pSoundEventManager;
extern class SoundOpGameSystem*               g_pSoundOpGameSystem;
extern class CDedicatedServerWorkshopManager* g_pServerWorkshopManager;
extern class IGameTypes*                      g_pGameTypes;
extern class ISteamApiProxy*                  g_pSteamApiProxy;
extern class IScriptVM*                       g_pScriptVM;
extern class INetworkStringTableContainer*    g_pStringTableContainer;
extern class CSharpNetworkStringTableHelper*  g_pNetworkStringTableHelper;
extern class CSharpKeyValuesHelper*           g_pKeyValuesHelper;
extern class CSharpKeyValues3Helper*          g_pKeyValues3Helper;
extern class IResourceSystem*                 g_pResourceSystem;
extern class IGameSpawnGroupMgr*              g_pSpawnGroupMgr;
MS_IMPORT IMemAlloc*                          g_pMemAlloc;

// Global Vars
extern class CGameEventListener g_EventListener;
extern class CConVarBaseData*   g_pModSharpConVar;
extern unsigned int             g_nMainThreadId;
extern class HookManager*       g_pHookManager;
extern class GameData*          g_pGameData;

#define SHIDWORD(l) ((uint32_t)(((int64_t)(l) >> 32) & 0xFFFFFFFF))
#define HIDWORD(l) ((uint32_t)(((uint64_t)(l) >> 32) & 0xFFFFFFFF))

// Global Memory Alloc
void   FreeMemory(void* mem);
void*  AllocateMemory(size_t size);
void*  ReallocateMemory(void* mem, size_t size);
size_t GetMemorySize(void* mem);

#ifdef _LINUX
#    include <pthread.h>
inline unsigned int GetCurrentThreadId()
{
    return pthread_self();
}
#    include <malloc.h>
inline size_t _msize(void* ptr)
{
    return malloc_usable_size(ptr);
}
#endif

#ifndef strncasecmp
#    define strncasecmp _strnicmp
#endif

#ifndef strcasecmp
#    define strcasecmp _stricmp
#endif

/////////////////////////////////////////////////

#define BeginMemberHookScope(class) \
    namespace class##_Hooks

#define BeginStaticHookScope(method) \
    namespace StaticCall##method##_Hooks

// 声明VirtualCall Detour
#define DeclareVirtualHook(method, ret, args) \
    static ret(*method) args = nullptr;       \
    static ret Virtual_##method args

// 声明MemberCall Detour
#define DeclareMemberDetourHook(method, ret, args) \
    static SafetyHookInline s_h##method = {};      \
    static ret(*method) args            = nullptr; \
    static ret Detour_##method args

// 声明StaticCall Detour
#define DeclareStaticDetourHook(method, ret, args) \
    static SafetyHookInline s_pHook = {};          \
    static ret(*method) args        = nullptr;     \
    static ret Detour_##method args

#define InstallMemberDetourAutoSig(class, method)                                                                                                                  \
    auto class##_Hooks_s_h##method##_result = safetyhook::InlineHook::create(g_pGameData->GetAddress<void*>(#class "::" #method), class##_Hooks::Detour_##method); \
    if (!class##_Hooks_s_h##method##_result)                                                                                                                       \
    {                                                                                                                                                              \
        const auto& err = class##_Hooks_s_h##method##_result.error();                                                                                              \
        FatalError("MS: Failed to hook %s. Reason: %s", #class "::" #method, g_szInlineHookErrors[err.type]);                                                      \
    }                                                                                                                                                              \
    class##_Hooks::s_h##method = std::move(*class##_Hooks_s_h##method##_result);                                                                                   \
    class##_Hooks::method      = class##_Hooks::s_h##method.original<decltype(class##_Hooks::method)>();                                                           \
    g_pHookManager->Register(&class##_Hooks::s_h##method)

#define InstallMemberDetourManual(class, method, address)                                                              \
    auto class##_Hooks_s_h##method##_result = safetyhook::InlineHook::create(address, class##_Hooks::Detour_##method); \
    if (!class##_Hooks_s_h##method##_result)                                                                           \
    {                                                                                                                  \
        const auto& err = class##_Hooks_s_h##method##_result.error();                                                  \
        FatalError("MS: Failed to hook %s. Reason: %s", #class "::" #method, g_szInlineHookErrors[err.type]);          \
    }                                                                                                                  \
    class##_Hooks::s_h##method = std::move(*class##_Hooks_s_h##method##_result);                                       \
    class##_Hooks::method      = class##_Hooks::s_h##method.original<decltype(class##_Hooks::method)>();               \
    g_pHookManager->Register(&class##_Hooks::s_h##method)

#define UninstallMemberDetour(class, method) \
    class##_Hooks::s_h##method.reset();

// 装载StaticCall Detour
// 使用自动GameData Signature
#define InstallStaticDetourAutoSig(method)                                                                                                                        \
    auto StaticCall##method##_hook_result = safetyhook::InlineHook::create(g_pGameData->GetAddress<void*>(#method), StaticCall##method##_Hooks::Detour_##method); \
    if (!StaticCall##method##_hook_result)                                                                                                                        \
    {                                                                                                                                                             \
        const auto& err = StaticCall##method##_hook_result.error();                                                                                               \
        FatalError("MS: Failed to hook %s. Reason: %s", #method, g_szInlineHookErrors[err.type]);                                                                 \
    }                                                                                                                                                             \
    StaticCall##method##_Hooks::s_pHook = std::move(*StaticCall##method##_hook_result);                                                                           \
    StaticCall##method##_Hooks::method  = StaticCall##method##_Hooks::s_pHook.original<decltype(StaticCall##method##_Hooks::method)>();                           \
    g_pHookManager->Register(&StaticCall##method##_Hooks::s_pHook)

// 装载StaticCall Detour
// 使用手动传入的void*指针
#define InstallStaticDetourManual(method, address)                                                                                      \
    auto StaticCall##method##_hook_result = safetyhook::InlineHook::create(address, StaticCall##method##_Hooks::Detour_##method);       \
    if (!StaticCall##method##_hook_result)                                                                                              \
    {                                                                                                                                   \
        const auto& err = StaticCall##method##_hook_result.error();                                                                     \
        FatalError("MS: Failed to hook %s. Reason: %s", #method, g_szInlineHookErrors[err.type]);                                       \
    }                                                                                                                                   \
    StaticCall##method##_Hooks::s_pHook = std::move(*StaticCall##method##_hook_result);                                                 \
    StaticCall##method##_Hooks::method  = StaticCall##method##_Hooks::s_pHook.original<decltype(StaticCall##method##_Hooks::method)>(); \
    g_pHookManager->Register(&StaticCall##method##_Hooks::s_pHook)

// 因为install的时候已经确保这个hook一定是生效的
// 所以我们这时候直接卸载就行
#define UninstallStaticDetour(function) \
    StaticCall##function##_Hooks::s_pHook.reset();

// 装载VirtualHook
// 使用自动GameData
#define InstallVirtualHookAutoWithVTableAuto(class, method, module)                                                                                                                                                                                                   \
    class##_Hooks::method = reinterpret_cast<decltype(class##_Hooks::method)>(vtablehook_hook_direct(modules::module->GetVirtualTableByName(#class), reinterpret_cast<void*>(class##_Hooks::Virtual_##method), g_pGameData->GetVFunctionIndex(#class "::" #method))); \
    AssertPtr(class##_Hooks::method)

#define InstallVirtualHookAutoWithVTableManual(class, method, module, c)                                                                                                                                                                                          \
    class##_Hooks::method = reinterpret_cast<decltype(class##_Hooks::method)>(vtablehook_hook_direct(modules::module->GetVirtualTableByName(#c), reinterpret_cast<void*>(class##_Hooks::Virtual_##method), g_pGameData->GetVFunctionIndex(#class "::" #method))); \
    AssertPtr(class##_Hooks::method)

// 装载VirtualHook
// 使用指定GameData
#define InstallVirtualHookManualWithVTableAuto(class, method, module, gc)                                                                                                                                                                            \
    class##_Hooks::method = reinterpret_cast<decltype(class##_Hooks::method)>(vtablehook_hook_direct(modules::module->GetVirtualTableByName(#class), reinterpret_cast<void*>(class##_Hooks::Virtual_##method), g_pGameData->GetVFunctionIndex(gc))); \
    AssertPtr(class##_Hooks::method)

#define InstallVirtualHookManualWithVTableManual(class, method, module, c, gc)                                                                                                                                                                   \
    class##_Hooks::method = reinterpret_cast<decltype(class##_Hooks::method)>(vtablehook_hook_direct(modules::module->GetVirtualTableByName(#c), reinterpret_cast<void*>(class##_Hooks::Virtual_##method), g_pGameData->GetVFunctionIndex(gc))); \
    AssertPtr(class##_Hooks::method)

// 检查VirtualHook是否已经装载
#define CheckVirtualHookNotInstalled(class, method) \
    (class##_Hooks::method == nullptr)

/////////////////////////////////////////////////
#endif