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
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "strtool.h"

#include "cstrike/cstrike.h"
#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/IGameResourceServiceServer.h"
#include "cstrike/type/CEntityClass.h"
#include "cstrike/type/CEntityKeyValues.h"
#include "cstrike/type/CUtlSymbolLarge.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/Variant.h"

#include <safetyhook.hpp>

CGameEntitySystem* g_pGameEntitySystem;

void InitGameEntitySystem()
{
    CGameEntitySystem::Init();
}

void CGameEntitySystem::Init()
{
    g_pGameEntitySystem = g_pGameResourceServiceServer->GetGameEntitySystem();
}

CBaseEntity* CGameEntitySystem::FindEntityByEHandle(const CBaseHandle& handle)
{
    if (!handle.IsValid())
        return nullptr;

    const auto entity = FindEntityByIndex(handle.GetEntryIndex());
    if (!entity)
        return nullptr;

    const auto a = entity->GetActualEHandle().ToInt();
    const auto b = handle.ToInt();
    if (a != b)
        return nullptr;

    return entity;
}

CBaseEntity* CGameEntitySystem::FindEntityByIndex(EntityIndex_t index)
{
    return address::server::CGameEntitySystem_FindEntityByIndex(this, index);
}

const matrix3x4_t& CGameEntitySystem::GetSpawnOriginOffset() const
{
    AssertPtr(this);
    return address::server::CGameEntitySystem_GetSpawnOriginOffset(this);
}

CUtlSymbolLarge CGameEntitySystem::AllocPooledString(const char* pStr) const
{
    AssertPtr(this);
    CUtlSymbolLarge symbol;
    address::server::CGameEntitySystem_AllocPooledString(&symbol, pStr);
    return symbol;
}

CBaseEntity* CGameEntitySystem::FindByClassname(CBaseEntity* startEntity, const char* classname)
{
    return address::server::CGameEntitySystem_FindByClassname(this, startEntity, classname);
}

CBaseEntity* CGameEntitySystem::FindByName(CBaseEntity* startEntity, const char* name)
{
    return address::server::CGameEntitySystem_FindByName(this, startEntity, name, nullptr, nullptr, nullptr, nullptr);
}

CBaseEntity* CGameEntitySystem::FindInSphere(CBaseEntity* startEntity, Vector* center, float radius)
{
    return address::server::CGameEntitySystem_FindInSphere(this, startEntity, center, radius);
}

CBaseEntity* CGameEntitySystem::SpawnEntityFromKeyValuesSync(const char* classname, const KeyValuesVariantItem* items, int count) const
{
    const auto entity = address::server::CreateEntityByName(classname, -1);
    if (entity == nullptr)
        return nullptr;

    CEntityKeyValues* pKeyValues = CEntityKeyValues::Create();

    for (int i = 0; i < count; i++)
    {
        const auto& item = items[i];

        switch (item.Value.type)
        {
        case KeyValuesVariantValueItemType_Bool:
            pKeyValues->SetBool(item.Key, item.Value.bValue);
            break;
        case KeyValuesVariantValueItemType_Int32:
            pKeyValues->SetInt(item.Key, item.Value.i32Value);
            break;
        case KeyValuesVariantValueItemType_Float:
            pKeyValues->SetFloat(item.Key, item.Value.flValue);
            break;
        case KeyValuesVariantValueItemType_String:
            pKeyValues->SetString(item.Key, item.Value.szValue);
            break;
        case KeyValuesVariantValueItemType_Pointer:
            pKeyValues->SetPointer(item.Key, item.Value.pValue);
            break;
        case KeyValuesVariantValueItemType_UInt64:
            pKeyValues->SetUInt64(item.Key, item.Value.u64Value);
            break;
        default:
            FatalError("Not supported KeyValuesVariantValueItemType");
            break;
        }
    }

    if (auto classname_kv = pKeyValues->FindOrCreateKeyValues("classname"))
    {
        classname_kv->SetString(classname);
    }

    EntitySpawnInfo_t info;
    info.m_pEntityKeyValues = pKeyValues;
    info.m_pIdentity        = entity->GetEntityIdentity();

    const auto& matrix = GetSpawnOriginOffset();

    g_pGameResourceServiceServer->PrecacheEntitiesAndConfirmResourcesAreLoaded(entity->GetSpawnGroup(), 1, info, matrix);

    entity->DispatchSpawn(pKeyValues);

    return entity;
}

CBaseEntity* CGameEntitySystem::CreateEntityByName(const char* classname) const
{
    return address::server::CreateEntityByName(classname, -1);
}

void CGameEntitySystem::AddEntityIOEvent(CBaseEntity* pEntity, const char* pInputName, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue, float flDelay)
{
    return address::server::CGameEntitySystem_AddEntityIOEvent(this, pEntity, pInputName, pActivator, pCaller, pValue, flDelay, nullptr, nullptr);
}

CEntityClass* CGameEntitySystem::FindEntityClassByName(const char* classname) const
{
    using fn         = CEntityClass* (*)(const CGameEntitySystem*, const char*, const char* /*CUtlString*/);
    static auto func = g_pGameData->GetAddress<fn>("CGameEntitySystem::FindEntityClassByClassname");

    return func(this, classname, nullptr);
}

int32_t CGameEntitySystem::EnumerateByClassname(const char* classname, CBaseEntity** buffer, int32_t capacity) const
{
    auto* klass = FindEntityClassByName(classname);
    if (!klass) return 0;

    int32_t written = 0;
    int32_t total   = 0;
    for (auto* id = klass->GetEntityListHead(); id; id = id->m_pNextByClass())
    {
        if (id->IsMarkedForDeletion()) continue;
        if (auto* e = id->GetBaseEntity())
        {
            if (written < capacity) buffer[written++] = e;
            ++total;
        }
    }
    return total;
}

void CGameEntitySystem::AddListenerEntity(IEntityListener* pListener)
{
    return address::server::CGameEntitySystem_AddListenerEntity(this, pListener);
}

void CGameEntitySystem::RemoveListenerEntity(IEntityListener* pListener)
{
    return address::server::CGameEntitySystem_RemoveListenerEntity(this, pListener);
}