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

#include "cstrike/type/CEntityKeyValues.h"

#include "gamedata.h"
#include "global.h"

CEntityKeyValues::CEntityKeyValues(void* context, EntityKVAllocatorType_t allocator_type)
{
    using fn         = void (*)(CEntityKeyValues*, void*, EntityKVAllocatorType_t);
    static auto func = g_pGameData->GetAddress<fn>("CEntityKeyValues::CEntityKeyValues");

    func(this, context, allocator_type);
}

CEntityKeyValues::~CEntityKeyValues()
{
    using fn         = void (*)(CEntityKeyValues*);
    static auto func = g_pGameData->GetAddress<fn>("CEntityKeyValues::~CEntityKeyValues");

    func(this);
}

CEntityKeyValues* CEntityKeyValues::Create(void* context, EntityKVAllocatorType_t allocator_type)
{
    return new CEntityKeyValues(context, allocator_type);
}

KeyValues3* CEntityKeyValues::FindOrCreateKeyValues(const CHashKey& key)
{
    using fn         = KeyValues3* (*)(CEntityKeyValues*, const CHashKey&);
    static auto func = g_pGameData->GetAddress<fn>("CEntityKeyValues::FindOrCreateKeyValues");

    return func(this, key);
}

KeyValues3* CEntityKeyValues::FindKeyValues(const CHashKey& key, bool* is_attribute) const
{
    using fn         = KeyValues3* (*)(const CEntityKeyValues*, const CHashKey&, bool*);
    static auto func = g_pGameData->GetAddress<fn>("CEntityKeyValues::FindKeyValues");

    return func(this, key, is_attribute);
}

void CEntityKeyValues::RemoveKeyValues(const CHashKey& key)
{
    using fn         = void (*)(CEntityKeyValues*, const CHashKey&);
    static auto func = g_pGameData->GetAddress<fn>("CEntityKeyValues::RemoveKeyValues");

    func(this, key);
}

void CEntityKeyValues::SetString(KeyValues3* kv, const char* string)
{
    using fn         = void (*)(CEntityKeyValues*, KeyValues3*, const char*);
    static auto func = g_pGameData->GetAddress<fn>("CEntityKeyValues::SetString");

    func(this, kv, string);
}

void CEntityKeyValues::AddConnectionDesc(const char* pszOutputName, int32_t eTargetType, const char* pszTargetName, const char* pszInputName, const char* pszOverrideParam, float flDelay, int32_t nTimesToFire)
{
    using fn         = void (*)(CEntityKeyValues*, const char*, int32_t, const char*, const char*, const char*, int32_t, float, KeyValues3*);
    static auto func = g_pGameData->GetAddress<fn>("CEntityKeyValues::AddConnectionDesc");

    KeyValues3 kv(KV3_TYPEEX_NULL, KV3_SUBTYPE_UNSPECIFIED);

    func(this, pszOutputName, eTargetType, pszTargetName, pszInputName, pszOverrideParam, nTimesToFire, flDelay, &kv);
}

// TODO: 把CUtlLeanVector给弄过来，然后sigscan m_connectionDescs->m_nCount的offset
void CEntityKeyValues::RemoveConnectionDesc(int nDesc)
{
    if (m_nQueuedForSpawnCount > 0)
        return;
    m_connectionDescs.Remove(nDesc);
}

bool CEntityKeyValues::GetBool(const CHashKey& key, bool defaultValue) const
{
    const auto kv = FindKeyValues(key);
    return kv ? kv->GetBool() : defaultValue;
}

int CEntityKeyValues::GetInt(const CHashKey& key, int defaultValue) const
{
    const auto kv = FindKeyValues(key);
    return kv ? kv->GetInt() : defaultValue;
}

float CEntityKeyValues::GetFloat(const CHashKey& key, float defaultValue) const
{
    const auto kv = FindKeyValues(key);
    return kv ? kv->GetFloat() : defaultValue;
}

const char* CEntityKeyValues::GetString(const CHashKey& key, const char* defaultValue) const
{
    const auto kv = FindKeyValues(key);
    return kv ? kv->GetString() : defaultValue;
}

void* CEntityKeyValues::GetPointer(const CHashKey& key, void* defaultValue) const
{
    const auto kv = FindKeyValues(key);
    return kv ? kv->GetPointer() : defaultValue;
}

void CEntityKeyValues::SetBool(const CHashKey& key, bool value, bool as_attribute)
{
    if (auto kv = FindOrCreateKeyValues(key))
    {
        kv->SetBool(value);
    }
}

void CEntityKeyValues::SetInt(const CHashKey& key, int value, bool as_attribute)
{
    if (auto kv = FindOrCreateKeyValues(key))
    {
        kv->SetInt(value);
    }
}

void CEntityKeyValues::SetFloat(const CHashKey& key, float value, bool as_attribute)
{
    if (auto kv = FindOrCreateKeyValues(key))
    {
        kv->SetFloat(value);
    }
}

void CEntityKeyValues::SetString(const CHashKey& key, const char* value, bool as_attribute)
{
    if (auto kv = FindOrCreateKeyValues(key))
    {
        SetString(kv, value);
    }
}

void CEntityKeyValues::SetPointer(const CHashKey& key, void* value, bool as_attribute)
{
    if (auto kv = FindOrCreateKeyValues(key))
    {
        kv->SetPointer(value);
    }
}
