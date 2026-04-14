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
#include "logging.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "module.h"
#include "sdkproxy.h"
#include "steamworks.h"

#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/IGameSpawnGroupMgr.h"
#include "cstrike/interface/IGameSystem.h"
#include "cstrike/interface/IResourceManifest.h"
#include "cstrike/interface/IScriptVM.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/VProf.h"

#include <Zydis.h>
#include <safetyhook.hpp>

// #define SERVER_HOOK_ASSERT

static bool             s_bPatchVScriptVM;
static CConVarBaseData* ms_fix_spawngroups_leak = nullptr;

BeginMemberHookScope(CSource2Server)
{
    DeclareMemberDetourHook(GameFrame, void, (CSource2Server * pServer, bool bSimulating, bool bFirstTick, bool bLastTick))
    {
#ifdef SERVER_HOOK_ASSERT
        LOG("%10s: 0x%p\n", "this", pServer);
#endif
        VPROF_MS_HOOK();

        g_pHookManager->Call_GameFrame(HookType_Pre, bSimulating, bFirstTick, bLastTick);

        forwards::OnGameFramePre->Invoke(bSimulating, bFirstTick, bLastTick);

        GameFrame(pServer, bSimulating, bFirstTick, bLastTick);

        g_pHookManager->Call_GameFrame(HookType_Post, bSimulating, bFirstTick, bLastTick);

        forwards::OnGameFramePost->Invoke(bSimulating, bFirstTick, bLastTick);

        AssertPtr(gpGlobals);
        AssertBool(g_nMainThreadId == static_cast<uint64_t>(GetCurrentThreadId()));

#if 0
        if (!bSimulating || !bFirstTick || !bLastTick)
            printf("OnGameFrame(%d, %d, %d) -> frame: %d (ft: %f) -> tick: %d (i: %f) -> %f -> %s\n",
                   bSimulating, bFirstTick, bLastTick, gpGlobals->nFrameCount, gpGlobals->flFrameTime, gpGlobals->nTickCount, gpGlobals->flIntervalPerTick, gpGlobals->flCurTime, gpGlobals->pszMapName);
#endif
    }

    DeclareVirtualHook(GameServerSteamAPIActivated, void, (CSource2Server * pServer))
    {
#ifdef SERVER_HOOK_ASSERT
        LOG("%10s: 0x%p\n", "this", pServer);
#endif

        GameServerSteamAPIActivated(pServer);
        InitApiContext();
    }

    DeclareVirtualHook(GameServerSteamAPIDeactivated, void, (CSource2Server * pServer))
    {
#ifdef SERVER_HOOK_ASSERT
        LOG("%10s: 0x%p\n", "this", pServer);
#endif

        GameServerSteamAPIDeactivated(pServer);
        DestroyApiContext();
    }
}

extern void* FindGameSystemByName(const char* name);

BeginMemberHookScope(CGameRulesGameSystem)
{
    DeclareVirtualHook(GameInit, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        LOG("%10s: %s\n%10s: %f", "pszMapName", gpGlobals->pszMapName, "time", Plat_FloatTime());

        g_pHookManager->Call_GameInit(HookType_Pre);

        GameInit(pGameSystem, pMsg);

        g_pHookManager->Call_GameInit(HookType_Post);

        forwards::OnGameInit->Invoke();
    }

    DeclareVirtualHook(GameShutdown, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        LOG("%10s: %f", "time", Plat_FloatTime());

        g_pHookManager->Call_GameShutdown(HookType_Pre);

        GameShutdown(pGameSystem, pMsg);

        g_pHookManager->Call_GameShutdown(HookType_Post);

        forwards::OnGameShutdown->Invoke();

        g_pLoggerMapName.clear();
    }

    DeclareVirtualHook(GamePostInit, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        LOG("%10s: %f", "time", Plat_FloatTime());

        GamePostInit(pGameSystem, pMsg);

        forwards::OnGamePostInit->Invoke();

        g_pSpawnGroupMgr = *static_cast<IGameSpawnGroupMgr**>(FindGameSystemByName("SpawnGroupManagerGameSystem"));
        AssertPtr(g_pSpawnGroupMgr);
    }

    DeclareVirtualHook(GamePreShutdown, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        LOG("%10s: %f", "time", Plat_FloatTime());

        GamePreShutdown(pGameSystem, pMsg);

        forwards::OnGamePreShutdown->Invoke();
    }

    DeclareVirtualHook(GameActivate, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        LOG("%10s: %f", "time", Plat_FloatTime());

        if (s_bPatchVScriptVM)
            AssertPtr(g_pScriptVM);

        g_pHookManager->Call_GameActivate(HookType_Pre);

        GameActivate(pGameSystem, pMsg);

        g_pHookManager->Call_GameActivate(HookType_Post);

        forwards::OnGameActivate->Invoke();
    }

    DeclareVirtualHook(GameDeactivate, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        LOG("%10s: %f", "time", Plat_FloatTime());

        g_pHookManager->Call_GameDeactivate(HookType_Pre);

        GameDeactivate(pGameSystem, pMsg);

        g_pHookManager->Call_GameDeactivate(HookType_Post);

        forwards::OnGameDeactivate->Invoke();
    }

    DeclareVirtualHook(OnPostSpawnGroupLoad, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        OnPostSpawnGroupLoad(pGameSystem, pMsg);

        if (!g_pSpawnGroupMgr || !ms_fix_spawngroups_leak->GetValue<bool>())
            return;

        CUtlVector<SpawnGroup_t> vecSpawnGroups;

        g_pSpawnGroupMgr->GetSpawnGroups(&vecSpawnGroups);

        const auto clients = sv->GetClients();

        for (int i = clients->Count() - 1; i >= 0; i--)
        {
            const auto client = clients->Element(i);

            if (client == nullptr)
                continue;

            client->SetLoadedSpawnGroups(vecSpawnGroups);
        }
    }

    DeclareVirtualHook(OnPrecacheResource, void, (CGameRulesGameSystem * pGameSystem, IResourceManifest * pContext))
    {
        LOG("%10s: %f", "time", Plat_FloatTime());

        forwards::OnResourcePrecache->Invoke(pContext);

        OnPrecacheResource(pGameSystem, pContext);
    }

    DeclareVirtualHook(FrameUpdatePreEntityThink, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        forwards::OnFrameUpdatePreEntityThink->Invoke();

        FrameUpdatePreEntityThink(pGameSystem, pMsg);
    }

    DeclareVirtualHook(FrameUpdatePostEntityThink, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        FrameUpdatePostEntityThink(pGameSystem, pMsg);

        forwards::OnFrameUpdatePostEntityThink->Invoke();
    }

    DeclareVirtualHook(OnServerGamePostSimulate, void, (CGameRulesGameSystem * pGameSystem, void* pMsg))
    {
        OnServerGamePostSimulate(pGameSystem, pMsg);

        forwards::OnServerGamePostSimulate->Invoke();
    }
}

class CNavMesh;
BeginMemberHookScope(CNavMesh)
{
    DeclareMemberDetourHook(GetNearestNavArea, void*, (void*, float*, unsigned int*, unsigned int, int64_t, int64_t, float, int64_t))
    {
        return nullptr;
    }
}

BeginMemberHookScope(CCSGOVScriptGameSystem)
{
    DeclareMemberDetourHook(DestroyVM, void, (CCSGOVScriptGameSystem * pScriptSystem))
    {
        g_pScriptVM = nullptr;
        DestroyVM(pScriptSystem);
    }
}

BeginMemberHookScope(IScriptVM)
{
    DeclareMemberDetourHook(CreateVM, bool, (IScriptVM * pScriptVM))
    {
        const auto value = CreateVM(pScriptVM);

        if (value)
        {
            g_pScriptVM = pScriptVM;
        }

        return value;
    }
}

static void PatchEnableVScript()
{
    ZydisDecoder decoder{};
    if (ZYAN_FAILED(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
    {
        FatalError("Failed to initialize decoder");
    }

    ZydisDecodedInstruction instr{};
    ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

    modules::server->LoopVFunctions("CCSGOVScriptGameSystem", [&](CAddress address) {
        if (auto status = ZydisDecoderDecodeFull(&decoder, address.As<uint8_t*>(), ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, operands); !ZYAN_SUCCESS(status))
            return false;

        if (instr.mnemonic != ZYDIS_MNEMONIC_MOV)
            return false;

        auto op1 = &operands[0];
        auto op2 = &operands[1];
        if (op1->type != ZYDIS_OPERAND_TYPE_REGISTER || op1->reg.value != ZYDIS_REGISTER_EAX || op2->type != ZYDIS_OPERAND_TYPE_IMMEDIATE || op2->imm.value.s != 2)
            return false;
        if (auto unprotect_guard = safetyhook::unprotect(address, instr.length))
        {
            *address.Offset(1).As<uint8_t*>() = 0x1;
            LOG("Patched CCSGOVScriptGameSystem::GetVScriptType @ server+0x%llx", address.GetPtr() - modules::server->Base());
            return true;
        }
        FERROR("Failed to unprotect memory region for CCSGOVScriptGameSystem::GetVScriptType");
        return true;
    });

    HOOK(IScriptVM, CreateVM);
    HOOK(CCSGOVScriptGameSystem, DestroyVM);

    s_bPatchVScriptVM = true;
}

void InstallServerHooks()
{
    HOOK(CSource2Server, GameFrame);
    VHOOK(CSource2Server, GameServerSteamAPIActivated, server);
    VHOOK(CSource2Server, GameServerSteamAPIDeactivated, server);

    VHOOK(CGameRulesGameSystem, GameInit, server);
    VHOOK(CGameRulesGameSystem, GameShutdown, server);
    VHOOK(CGameRulesGameSystem, GamePostInit, server);
    VHOOK(CGameRulesGameSystem, GamePreShutdown, server);
    VHOOK(CGameRulesGameSystem, GameActivate, server);
    VHOOK(CGameRulesGameSystem, GameDeactivate, server);
    VHOOK(CGameRulesGameSystem, OnPostSpawnGroupLoad, server);
    VHOOK(CGameRulesGameSystem, OnPrecacheResource, server);
    VHOOK(CGameRulesGameSystem, FrameUpdatePreEntityThink, server);
    VHOOK(CGameRulesGameSystem, FrameUpdatePostEntityThink, server);
    VHOOK(CGameRulesGameSystem, OnServerGamePostSimulate, server);

    g_pScriptVM      = nullptr;
    g_pSpawnGroupMgr = nullptr;

    ms_fix_spawngroups_leak = g_ConVarManager.CreateConVar("ms_fix_spawngroups_leak", false, "Fix spawn groups leak after release", FCVAR_RELEASE);

    if (CommandLine()->HasParam("-nobots"))
    {
        HOOK(CNavMesh, GetNearestNavArea);
    }

    if (CommandLine()->HasParam("-fucking_lua_map"))
        PatchEnableVScript();

    g_pHookManager->Hook_ClientPutInServer(HookType_Post, [](PlayerSlot_t slot, const char* a2, SteamId_t steamid) {
        if (!g_pSpawnGroupMgr)
            return;

        const auto pClient = sv->GetClientSafety(slot);
        if (pClient == nullptr || !ms_fix_spawngroups_leak->GetValue<bool>())
            return;

        CUtlVector<SpawnGroup_t> vecSpawnGroups;

        g_pSpawnGroupMgr->GetSpawnGroups(&vecSpawnGroups);

        pClient->SetLoadedSpawnGroups(vecSpawnGroups);
    });
}