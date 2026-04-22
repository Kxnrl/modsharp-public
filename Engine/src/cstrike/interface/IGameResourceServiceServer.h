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

#ifndef CSTRIKE_INTERFACE_GAMERESOURCE_H
#define CSTRIKE_INTERFACE_GAMERESOURCE_H

#include "vhook/call.h"

#include <cstdint>

class CGameEntitySystem;

struct EntitySpawnInfo_t
{
    CEntityIdentity*  m_pIdentity;
    CEntityKeyValues* m_pEntityKeyValues;
};

class IGameResourceServiceServer
{
public:
    CGameEntitySystem* GetGameEntitySystem()
    {
#ifdef PLATFORM_WINDOWS
        return *reinterpret_cast<CGameEntitySystem**>(reinterpret_cast<uintptr_t>(this) + 88);
#else
        return *reinterpret_cast<CGameEntitySystem**>(reinterpret_cast<uintptr_t>(this) + 80);
#endif
    }

    void PrecacheEntitiesAndConfirmResourcesAreLoaded(SpawnGroup_t hSpawnGroup, int nCount, const EntitySpawnInfo_t& info, const matrix3x4_t& matrix)
    {
        static auto offset = g_pGameData->GetVFunctionIndex("CGameResourceService::PrecacheEntitiesAndConfirmResourcesAreLoaded");
        CALL_VIRTUAL(void, offset, this, hSpawnGroup, nCount, &info, &matrix);
    }
};

#endif