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

#ifndef CSTRIKE_TYPE_CENTITYKEYVALUES_H
#define CSTRIKE_TYPE_CENTITYKEYVALUES_H

#include "cstrike/type/CUtlLeanVector.h"
#include "cstrike/type/KeyValues3.h"

#include <cstdint>
#include <string>

enum EntityKVAllocatorType_t : uint8_t
{
    EKV_ALLOCATOR_NORMAL      = 0,
    EKV_ALLOCATOR_ENTSYSTEM_1 = 1,
    EKV_ALLOCATOR_ENTSYSTEM_2 = 2,
    EKV_ALLOCATOR_EXTERNAL    = 3,
};

enum EntityIOTargetType_t
{
    ENTITY_IO_TARGET_INVALID                 = -1,
    ENTITY_IO_TARGET_CLASSNAME               = 0,
    ENTITY_IO_TARGET_CLASSNAME_DERIVES_FROM  = 1,
    ENTITY_IO_TARGET_ENTITYNAME              = 2,
    ENTITY_IO_TARGET_CONTAINS_COMPONENT      = 3,
    ENTITY_IO_TARGET_SPECIAL_ACTIVATOR       = 4,
    ENTITY_IO_TARGET_SPECIAL_CALLER          = 5,
    ENTITY_IO_TARGET_EHANDLE                 = 6,
    ENTITY_IO_TARGET_ENTITYNAME_OR_CLASSNAME = 7,
};

struct EntityIOConnectionDescFat_t
{
    const char*          m_pszOutputName;
    EntityIOTargetType_t m_eTargetType;
    const char*          m_pszTargetName;
    const char*          m_pszInputName;
    const char*          m_pszOverrideParam;
    float                m_flDelay;
    int32_t              m_nTimesToFire;
    KeyValues3           m_KeyValues;
};

class CEntityKeyValues
{
public:
    CEntityKeyValues() = delete;
    CEntityKeyValues(void* context = nullptr, EntityKVAllocatorType_t allocator_type = EntityKVAllocatorType_t::EKV_ALLOCATOR_NORMAL);
    ~CEntityKeyValues();
    static CEntityKeyValues* Create(void* context = nullptr, EntityKVAllocatorType_t allocator_type = EntityKVAllocatorType_t::EKV_ALLOCATOR_NORMAL);

    [[nodiscard]] KeyValues3* FindOrCreateKeyValues(const CHashKey& key);
    [[nodiscard]] KeyValues3* FindKeyValues(const CHashKey& key, bool* is_attribute = nullptr) const;
    void                      RemoveKeyValues(const CHashKey& key);

    void AddConnectionDesc(const char* pszOutputName, int32_t eTargetType, const char* pszTargetName, const char* pszInputName, const char* pszOverrideParam, float flDelay, int32_t nTimesToFire);
    void RemoveConnectionDesc(int nDesc);

    [[nodiscard]] bool        GetBool(const CHashKey& key, bool defaultValue) const;
    [[nodiscard]] int         GetInt(const CHashKey& key, int defaultValue = 0) const;
    [[nodiscard]] float       GetFloat(const CHashKey& key, float defaultValue = 0.0f) const;
    [[nodiscard]] const char* GetString(const CHashKey& key, const char* defaultValue = "") const;
    [[nodiscard]] void*       GetPointer(const CHashKey& key, void* defaultValue = nullptr) const;

    void SetBool(const CHashKey& key, bool value, bool as_attribute = false);
    void SetInt(const CHashKey& key, int value, bool as_attribute = false);
    void SetFloat(const CHashKey& key, float value, bool as_attribute = false);
    void SetString(const CHashKey& key, const char* value, bool as_attribute = false);
    void SetPointer(const CHashKey& key, void* value, bool as_attribute = false);

    [[nodiscard]] int GetNumConnectionDesc() const
    {
        return m_connectionDescs.Count();
    }

    EntityIOConnectionDescFat_t* GetConnectionDesc(int i)
    {
        return &m_connectionDescs[i];
    }

    void AddRef()
    {
        m_nRefCount++;
    }

    void Release()
    {
        m_nRefCount--;

        if (m_nRefCount <= 0)
            delete this;
    }

    [[nodiscard]] KeyValues3* GetKeyValues() const
    {
        return m_pValues;
    }

private:
    void SetString(KeyValues3* kv, const char* string);

    void*                                            m_pAllocator;
    void*                                            m_pComplexKeys;
    KeyValues3*                                      m_pValues;
    KeyValues3*                                      m_pAttributes;
    int16_t                                          m_nRefCount;
    int16_t                                          m_nQueuedForSpawnCount;
    bool                                             m_bAllowLogging;
    EntityKVAllocatorType_t                          m_eAllocatorType;
    CUtlLeanVector<EntityIOConnectionDescFat_t, int> m_connectionDescs;
};

#endif
