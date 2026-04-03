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

#ifndef MS_HOOK_INSTALLER_H
#define MS_HOOK_INSTALLER_H

#include "definitions.h"
#include "gamedata.h"
#include "logging.h"
#include "manager/HookManager.h"
#include "module.h"
#include "vhook/hook.h"

#include <safetyhook.hpp>

struct DetourOptions
{
    void* address = nullptr;
};

struct VHookOptions
{
    const char* vtable   = nullptr;
    const char* gamedata = nullptr;
    int         index    = -1;
};

template <typename Fn>
void InstallDetour(SafetyHookInline& hook, Fn& original, Fn detour, const char* name, DetourOptions opt = {})
{
    void* target = opt.address ? opt.address : g_pGameData->GetAddress<void*>(name);
    auto  result = safetyhook::InlineHook::create(target, reinterpret_cast<void*>(detour));
    if (!result)
    {
        FatalError("MS: Failed to hook %s. Reason: %s", name, g_szInlineHookErrors[result.error().type]);
    }
    hook     = std::move(*result);
    original = hook.original<Fn>();
    g_pHookManager->Register(&hook);
}

template <typename Fn>
void InstallVHook(Fn& original, Fn detour, CModule* mod, const char* default_vt, const char* default_gd, VHookOptions opt = {})
{
    const char* final_vt = opt.vtable ? opt.vtable : default_vt;

    int vfunc_index;
    if (opt.index != -1)
    {
        vfunc_index = opt.index;
    }
    else
    {
        const char* final_gd = opt.gamedata ? opt.gamedata : default_gd;
        vfunc_index          = g_pGameData->GetVFunctionIndex(final_gd);
    }

    original = reinterpret_cast<Fn>(
        vtablehook_hook_direct(mod->GetVirtualTableByName(final_vt),
                               reinterpret_cast<void*>(detour),
                               vfunc_index));
    AssertPtr(original);
}

// Member detour: HOOK(CCSGameRules, Constructor)
//                HOOK(CCSGameRules, Constructor, { .address = addr })
#define HOOK(cls, method, ...) \
    InstallDetour(cls##_Hooks::s_h##method, cls##_Hooks::method, cls##_Hooks::Detour_##method, #cls "::" #method, ##__VA_ARGS__)

// Static detour: SHOOK(HandleGCBanInfo)
//                SHOOK(HandleGCBanInfo, { .address = addr })
#define SHOOK(method, ...) \
    InstallDetour(StaticCall##method##_Hooks::s_pHook, StaticCall##method##_Hooks::method, StaticCall##method##_Hooks::Detour_##method, #method, ##__VA_ARGS__)

// VTable hook:   VHOOK(CSource2GameClients, CheckConnect, server)
//                VHOOK(INetworkServerService, StartupServer, engine, { .vtable = "CNetworkServerService" })
//                VHOOK(CTriggerGravity, Precache, server, { .gamedata = "CBaseEntity::Precache" })
//                VHOOK(CGamePlayerEquip, Use, server, { .vtable = "CBaseEntity", .gamedata = "CBaseEntity::Use" })
//                VHOOK(CVPhys2World, GenerateIntersectionNotifications, vphysics2, { .index = idx })
#define VHOOK(cls, method, mod, ...) \
    InstallVHook(cls##_Hooks::method, cls##_Hooks::Virtual_##method, modules::mod, #cls, #cls "::" #method, ##__VA_ARGS__)

#endif
