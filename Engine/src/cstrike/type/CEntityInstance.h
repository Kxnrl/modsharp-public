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

#ifndef CSTRIKE_TYPE_ENTITYINSTANCE_H
#define CSTRIKE_TYPE_ENTITYINSTANCE_H

#include "definitions.h"

#include "cstrike/cstrike.h"
#include "cstrike/type/CEntityIdentity.h"

class HSCRIPT;
class CEntityInstance;
class CEntityKeyValues;
struct SchemaClassInfoData_t;

class CEntityInstance : public CStrikeObject
{
public:
    CEntityInstance() = delete;

    [[nodiscard]] const char*   GetClassname() const { return m_pEntity->GetClassname(); }
    [[nodiscard]] const char*   GetName() const { return m_pEntity->GetName(); }
    [[nodiscard]] CBaseHandle   GetEHandle() const { return m_pEntity->GetEHandle(); }
    [[nodiscard]] CBaseHandle   GetActualEHandle() const { return m_pEntity->GetActualEHandle(); }
    [[nodiscard]] EntityIndex_t GetEntityIndex() const { return m_pEntity->GetEntityIndex(); }
    [[nodiscard]] SpawnGroup_t  GetSpawnGroup() const { return m_pEntity->GetSpawnGroup(); }

    // 以下方法需要Patch VScript后才会有返回值
    [[nodiscard]] HSCRIPT* GetPrivateScriptScope() const { return m_pPrivateScope; }
    [[nodiscard]] HSCRIPT* GetPublicScriptScope() const { return m_pEntity->GetPublicScriptScope(); }
    [[nodiscard]] HSCRIPT* GetOrCreatePublicScriptScope();
    [[nodiscard]] HSCRIPT* GetOrCreatePrivateScriptScope();
    // 以上方法需要Patch VScript后才会有返回值

    [[nodiscard]] CEntityIdentity* GetEntityIdentity() const { return m_pEntity; }

    [[nodiscard]] EntityIndex_t GetEntityIndexNative();
    [[nodiscard]] CBaseHandle   GetRefEHandleNative();

    [[nodiscard]] SchemaClassInfoData_t* GetSchemaClassInfo();
    [[nodiscard]] bool                   IsEntityClass(const CEntityClass* pClass) const { return m_pEntity->GetEntityClass() == pClass; }
    void                                 RemoveSelf() { Kill(); }
    void                                 Kill();

    bool IsPreservedEntity() const { return IsPreservedEntity(GetClassname()); }

    /// \brief 判断这个实体类名是否为永久实体
    /// \param sClassname Classname
    /// \return True => OK, False otherwise.
    static bool IsPreservedEntity(const char* sClassname);

private:
    [[maybe_unused]] void* m_pVTable;            // 0x00
    [[maybe_unused]] void* m_iszPrivateVScripts; // 0x08
protected:
    CEntityIdentity*  m_pEntity;       // 0x10
    HSCRIPT*          m_pPrivateScope; // 0x18
    CEntityKeyValues* m_pKeyValues;    // 0x20
};

#endif
