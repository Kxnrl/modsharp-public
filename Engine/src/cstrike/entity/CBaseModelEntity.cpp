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
#include "logging.h"

#include "cstrike/component/CBodyComponent.h"
#include "cstrike/component/CGameSceneNode.h"
#include "cstrike/entity/CBaseModelEntity.h"
#include "cstrike/type/CTransform.h"

void CBaseModelEntity::SetBodyGroupByName(const char* name, int32_t value)
{
    address::server::CBaseModelEntity_SetBodyGroupByName(this, name, value);
}

void CBaseModelEntity::SetMaterialGroupMask(uint64_t mask)
{
    address::server::CBaseModelEntity_SetMaterialGroupMask(this, mask);
}

int32_t CBaseModelEntity::LookupAttachment(const char* pAttachmentName)
{
    const auto pNode  = m_CBodyComponent()->m_pSceneNode();
    const auto pModel = pNode->GetStudioModel();

#ifdef PLATFORM_WINDOWS
    uint8_t iAttachment = 0;
    address::server::StudioModel_LookupAttachment(pModel, &iAttachment, pAttachmentName);
    return iAttachment;
#else
    return address::server::StudioModel_LookupAttachment(pModel, pAttachmentName);
#endif
}

void CBaseModelEntity::GetAttachment(int32_t iAttachment, Vector* absOrigin, Vector* absAngles)
{
    if (iAttachment <= 0)
        return;

    const auto pNode  = m_CBodyComponent()->m_pSceneNode();
    const auto pModel = pNode->GetStudioModel();

    absOrigin->Init();
    absAngles->Init();

    address::server::StudioModel_GetAttachment(pModel, static_cast<uint8_t>(iAttachment), absOrigin, absAngles);
}

int32_t CBaseModelEntity::LookupBone(const char* pBoneName)
{
    return address::server::CBaseModelEntity_LookupBone(this, pBoneName);
}

// Columns 0..2 are the rotated basis axes, column 3 is the origin - the same convention
// CGameSceneNode::EntityToWorldTransform builds via AngleMatrix.
static void TransformToMatrix(const CTransform& in, matrix3x4_t* out)
{
    const Quaternion& q = in.m_orientation;

    (*out)[0][0] = 1.0f - 2.0f * q.y * q.y - 2.0f * q.z * q.z;
    (*out)[1][0] = 2.0f * q.x * q.y + 2.0f * q.w * q.z;
    (*out)[2][0] = 2.0f * q.x * q.z - 2.0f * q.w * q.y;

    (*out)[0][1] = 2.0f * q.x * q.y - 2.0f * q.w * q.z;
    (*out)[1][1] = 1.0f - 2.0f * q.x * q.x - 2.0f * q.z * q.z;
    (*out)[2][1] = 2.0f * q.y * q.z + 2.0f * q.w * q.x;

    (*out)[0][2] = 2.0f * q.x * q.z + 2.0f * q.w * q.y;
    (*out)[1][2] = 2.0f * q.y * q.z - 2.0f * q.w * q.x;
    (*out)[2][2] = 1.0f - 2.0f * q.x * q.x - 2.0f * q.y * q.y;

    (*out)[0][3] = in.m_vPosition.x;
    (*out)[1][3] = in.m_vPosition.y;
    (*out)[2][3] = in.m_vPosition.z;
}

void CBaseModelEntity::GetBoneTransform(int32_t iBone, matrix3x4_t* transform)
{
    if (iBone < 0)
        return;

    CTransform bone;

#ifdef PLATFORM_WINDOWS
    address::server::CBaseModelEntity_GetBoneTransform(this, &bone, iBone);
#else
    address::server::CBaseModelEntity_GetBoneTransform(&bone, this, iBone);
#endif

    TransformToMatrix(bone, transform);
}

void CBaseModelEntity::SetModelScale(float scale)
{
    address::server::CBaseModelEntity_SetModelScale(this, scale);
}

void CBaseModelEntity::SetCollisionBounds(const Vector* mins, const Vector* maxs)
{
    address::server::CBaseModelEntity_SetCollisionBounds(this, mins, maxs);
}