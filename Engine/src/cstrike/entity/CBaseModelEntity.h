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

#ifndef CSTRIKE_ENTITY_MODELENTITY_H
#define CSTRIKE_ENTITY_MODELENTITY_H

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/type/CCollisionProperty.h"

// +584 = this->m_CBodyComponent()->m_pSceneNode()

class CCollisionProperty;

class CBaseModelEntity : public CBaseEntity
{
    DECLARE_SCHEMA_CLASS(CBaseModelEntity)
public:
    SCHEMA_FIELD(CCollisionProperty, m_Collision)

    void SetBodyGroupByName(const char* name, int32_t value);
    void SetMaterialGroupMask(uint64_t mask);

    [[nodiscard]] int32_t LookupAttachment(const char* pAttachmentName);
    void                  GetAttachment(int32_t iAttachment, Vector* absOrigin, Vector* absAngles);

    [[nodiscard]] int32_t LookupBone(const char* pBoneName);
    void                  GetBoneTransform(int32_t iBone, matrix3x4_t* transform);

    void SetModelScale(float scale);
    void SetCollisionBounds(const Vector* mins, const Vector* maxs);
};

#endif
