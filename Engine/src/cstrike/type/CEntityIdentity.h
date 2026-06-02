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

#ifndef CSTRIKE_TYPE_ENTITYIDENTITY_H
#define CSTRIKE_TYPE_ENTITYIDENTITY_H

#include "definitions.h"

#include "cstrike/cstrike.h"
#include "cstrike/type/CUtlSymbolLarge.h"

class CEntityInstance;
class CBaseEntity;
class CEntityClass;
class HSCRIPT;

class CEntityIdentity
{
    static constexpr uint32_t EF_FLAG_INVALID_HANDLE = 0x001;
    static constexpr uint32_t EF_FLAG_KILL_ME        = 0x200;

    DECLARE_SCHEMA_CLASS(CEntityIdentity)

public:
    CEntityIdentity() = delete;

    [[nodiscard]] const char*      GetClassname() const { return m_iszClassname.Get(); }
    [[nodiscard]] const char*      GetName() const { return m_iszName.Get(); }
    [[nodiscard]] CEntityInstance* GetEntityInstance() const { return m_pInstance; }
    [[nodiscard]] CBaseEntity*     GetBaseEntity() const { return reinterpret_cast<CBaseEntity*>(m_pInstance); }
    [[nodiscard]] CEntityClass*    GetEntityClass() const { return m_pClass; }
    [[nodiscard]] HSCRIPT*         GetPublicScriptScope() const { return m_pPublicScriptScope; }
    [[nodiscard]] uint32_t         GetFlags() const { return m_fFlags; }
    [[nodiscard]] EntityIndex_t    GetEntityIndex() const { return m_EntityHandle.GetEntryIndex(); }
    [[nodiscard]] CBaseHandle      GetEHandle() const { return m_EntityHandle; }
    [[nodiscard]] bool             IsMarkedForDeletion() const { return (m_fFlags & EF_FLAG_KILL_ME) != 0; }
    [[nodiscard]] int32_t          GetNameStringIndex() const { return m_nNameStringIndex; }
    [[nodiscard]] SpawnGroup_t     GetSpawnGroup() const { return m_hSpawnGroup; }
    [[nodiscard]] WorldGroup_t     GetWorldGroup() const { return m_hWorldGroup; }
    [[nodiscard]] uint32_t         GetDataObjectTypes() const { return m_fDataObjectTypes; }
    [[nodiscard]] int32_t          GetAccessorPathIndex() const { return m_nAccessorPathIndex; }

    // NOTE 这里也不知道为什么这么写, 内容直接参照 GetRefEHandleNative 里面的
    // GetRefEHandle
    [[nodiscard]] CBaseHandle GetActualEHandle() const
    {
        CBaseHandle handle(m_EntityHandle.ToInt());

        handle.m_Parts.m_Serial -= (m_fFlags & EF_FLAG_INVALID_HANDLE);

        return handle;
    }

    template <typename T>
    [[nodiscard]] T GetBaseEntity() const
    {
        return reinterpret_cast<T>(m_pInstance);
    }

    SCHEMA_FIELD(CEntityIdentity*, m_pNextByClass);

private:
    CEntityInstance* m_pInstance;          // 0x00
    CEntityClass*    m_pClass;             // 0x08
    CBaseHandle      m_EntityHandle;       // 0x10
    int32_t          m_nNameStringIndex;   // 0x14
    CUtlSymbolLarge  m_iszName;            // 0x18
    CUtlSymbolLarge  m_iszClassname;       // 0x20
    HSCRIPT*         m_pPublicScriptScope; // 0x28
    uint32_t         m_fFlags;             // 0x30
    SpawnGroup_t     m_hSpawnGroup;        // 0x34
    WorldGroup_t     m_hWorldGroup;        // 0x38
    uint32_t         m_fDataObjectTypes;   // 0x3c
    int32_t          m_nAccessorPathIndex; // 0x40
};

#endif