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

#ifndef MS_ROOT_ADDRESS_RESOLVER_H
#define MS_ROOT_ADDRESS_RESOLVER_H

#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "module.h"

#include <Zydis.h>

#define RESOLVE_GAMEDATA_ADDRESS(name, variable) \
    (variable) = g_pGameData->GetAddress<decltype(variable)>(name)

inline void try_overwrite_vfunc(const char* name, int32_t resolved)
{
    if (resolved == -1)
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    int current = 0;
    if (g_pGameData->GetVFunctionIndex(name, &current) && current != resolved)
    {
        WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (mismatch!).", name, resolved, current);
    }

    g_pGameData->OverwriteVFuncIndex(name, resolved);
}

inline void try_overwrite_offset(const char* name, int32_t resolved)
{
    if (resolved == -1)
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    int32_t current = 0;
    if (g_pGameData->GetOffset(name, &current) && current != resolved)
    {
        WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (mismatch!).", name, resolved, current);
    }

    g_pGameData->OverwriteOffset(name, resolved);
}

inline void try_overwrite_address(const char* name, uintptr_t resolved)
{
    if (resolved == 0)
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    g_pGameData->OverwriteAddress(name, resolved);
}

template <typename T>
static void AssignOrFallback(CModule* mod, T& target_ptr, uintptr_t found_addr, const char* func_name, const char* gamedata_key = nullptr)
{
    const char* key = gamedata_key ? gamedata_key : func_name;

    if (found_addr != 0)
    {
        FLOG("Found %s at server+0x%llx", func_name, found_addr - mod->Base());
#ifdef DEBUG
        uintptr_t gamedata_addr = 0;
        if (g_pGameData->GetAddress(key, gamedata_addr) && gamedata_addr != 0)
        {
            if (gamedata_addr != found_addr)
            {
                WARN("GameData auto-resolve: %s resolved=server+0x%llx, gamedata=server+0x%llx (mismatch!).",
                     key,
                     found_addr - mod->Base(),
                     gamedata_addr - mod->Base());
            }
            else
            {
                FLOG("GameData auto-resolve: %s resolved matches gamedata at server+0x%llx.", key, found_addr - mod->Base());
            }
        }
#endif

        target_ptr = reinterpret_cast<T>(found_addr);

        try_overwrite_address(key, found_addr);
    }
    else
    {
        target_ptr = g_pGameData->GetAddress<T>(key);

        if (target_ptr == nullptr)
        {
            FatalError("Failed to find %s (Both Scanner and GameData failed)", func_name);
        }
        else
        {
            FLOG("Fallback: Loaded %s from GameData.\n", func_name);
        }
    }
}

// address_server.cpp
void FindCEntityIdentity_SetEntityName();
void FindGameSystemFactory();
void FindCCSPlayerWeaponServices_DestroyWeapon();
void FindSetModel();
void FindCCSPlayerWeaponService_FindWeaponBySlot();
void ResolveCCSPlayerPawnStateActive();
void ResolveCBaseEntity_IsWeapon();
void ResolveCBaseEntityTeleport();
void ResolveCBaseEntity_GetEyeAngles();
void ResolveCBaseEntity_GetEyePosition();
void ResolveCBaseEntity_ChangeTeam();
void ResolveCBaseEntity_AbsOrigin();
void ResolveCBaseEntity_EventKill();
void ResolveCBaseEntity_GetCenter();
void Resolve_CBaseEntity_Use_StartTouch_Touch_EndTouch();
void ResolveCEntityInstance_GetDynamicBinding();
void ResolveCBaseEntity_Precache();
void ResolveCCSPlayerWeaponServices();
void ResolveCCSPlayerItemServices();
void ResolveCCSPlayerPawn_IsPlayer();
void ResolveCBasePlayerController_CanHearAndReadChatFrom();
void ResolveCCSPlayerController_RoundRespawn();
void ResolveCBasePlayerPawn_CommitSuicide();
void ResolveCCSPlayerPawn_SetDefaultGloves();
void ResolveCEntityClassEntityListOffset();
void ResolveCBasePlayerPawn_SnapViewAngles();
void ResolveCreateTriggerInternal();

// address_engine.cpp
void ResolveServerSideClientOffsets();
void ResolveNetworkGameServerOffsets();

#endif // MS_ROOT_ADDRESS_RESOLVER_H