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

#include "address.h"
#include "gamedata.h"
#include "global.h"
#include "sdkproxy.h"
#include "vhook/call.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/IRecipientFilter.h"
#include "cstrike/interface/ISoundOpSystem.h"
#include "cstrike/type/CEntityKeyValues.h"
#include "cstrike/type/CNetworkVelocityVector.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/QAngle.h"
#include "cstrike/type/Variant.h"

bool CBaseEntity::s_bBypassDamageHook = false;

CBaseEntity* CBaseEntity::GetOwner()
{
    const auto handle = m_hOwnerEntity();
    return g_pGameEntitySystem->FindEntityByEHandle<CBaseEntity*>(handle);
}

bool CBaseEntity::IsPlayerPawn()
{
    static auto offset = g_pGameData->GetVFunctionIndex("CBaseEntity::IsPlayerPawn");
    return CALL_VIRTUAL(bool, offset, this);
}

bool CBaseEntity::IsPlayerController()
{
    static auto offset = g_pGameData->GetVFunctionIndex("CBaseEntity::IsPlayerController");
    return CALL_VIRTUAL(bool, offset, this);
}

bool CBaseEntity::IsWeapon()
{
    static auto offset = g_pGameData->GetVFunctionIndex("CBaseEntity::IsWeapon");
    return CALL_VIRTUAL(bool, offset, this);
}

void CBaseEntity::SetOwner(CBaseEntity* pEntity)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CBaseEntity::SetOwner");
    CALL_VIRTUAL(void, offset, this, pEntity);
}

void CBaseEntity::SetModel(const char* pszModel)
{
    address::server::UTIL_SetModel(this, pszModel);
}

void CBaseEntity::ChangeTeam(CStrikeTeam_t team)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CBaseEntity::ChangeTeam");
    CALL_VIRTUAL(void, offset, this, team);
}

void CBaseEntity::Teleport(Vector* vPosition, QAngle* vAngles, Vector* vVelocity)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CBaseEntity::Teleport");
    CALL_VIRTUAL(void, offset, this, vPosition, vAngles, vVelocity);
}

void CBaseEntity::ApplyAbsVelocityImpulse(Vector* vVelocity)
{
    address::server::CBaseEntity_ApplyAbsVelocityImpulse(this, vVelocity);
}

const QAngle& CBaseEntity::GetEyeAngles()
{
    DeclareVFuncIndex(CBaseEntity, GetEyeAngles, offset);

    static QAngle angles;
    angles.Init();

#ifdef PLATFORM_WINDOWS
    CALL_VIRTUAL(void, offset, this, &angles);
#else
    const auto temp = CALL_VIRTUAL(QAngle, offset, this);
    angles.x        = temp.x;
    angles.y        = temp.y;
    angles.z        = temp.z;
#endif
    return angles;
}

const Vector& CBaseEntity::GetEyePosition()
{
    DeclareVFuncIndex(CBaseEntity, GetEyePosition, offset);
    static Vector vector;
#ifdef PLATFORM_WINDOWS
    vector.Init();
    CALL_VIRTUAL(void, offset, this, &vector);
#else
    vector = CALL_VIRTUAL(Vector, offset, this);
#endif
    return vector;
}

const QAngle& CBaseEntity::GetAbsAngles()
{
    // this + 584 -> +m_angAbsRotation
    return address::server::CBaseEntity_AbsAngles(this);
}

void CBaseEntity::SetAbsAngles(QAngle* angles)
{
    address::server::CBaseEntity_SetAbsAngles(this, angles->x, angles->y, angles->z);
}

const Vector& CBaseEntity::GetAbsOrigin()
{
    // this + 584 (CGameSceneNode) + m_vecAbsOrigin
    return address::server::CBaseEntity_AbsOrigin(this);
}

void CBaseEntity::SetAbsOrigin(Vector* origin)
{
    address::server::CBaseEntity_SetAbsOrigin(this, origin);
}

const Vector& CBaseEntity::GetLocalVelocity()
{
    static Vector vector;
    vector.Init();

#ifdef PLATFORM_WINDOWS
    address::server::CBaseEntity_LocalVelocity(this, &vector);
#else
    vector = address::server::CBaseEntity_LocalVelocity(this);
#endif

    return vector;
}

void CBaseEntity::SetLocalVelocity(Vector* vector)
{
    const auto velocity = m_vecVelocity();

    velocity->m_vecX(vector->x);
    velocity->m_vecY(vector->y);
    velocity->m_vecZ(vector->z);
}

const Vector& CBaseEntity::GetAbsVelocity()
{
    static Vector vector;
    vector.Init();
#ifdef PLATFORM_WINDOWS
    address::server::CBaseEntity_AbsVelocity(this, &vector);
#else
    vector = address::server::CBaseEntity_AbsVelocity(this);
#endif
    return vector;
}

void CBaseEntity::SetAbsVelocity(Vector* vector)
{
    address::server::CBaseEntity_SetAbsVelocity(this, vector);
}

const Vector& CBaseEntity::GetCenter()
{
    DeclareVFuncIndex(CBaseEntity, GetCenter, offset);

    static Vector vector;
    vector.Init();

#ifdef PLATFORM_WINDOWS
    CALL_VIRTUAL(void, offset, this, &vector);
#else
    const auto temp = CALL_VIRTUAL(Vector, offset, this);
    vector.x        = temp.x;
    vector.y        = temp.y;
    vector.z        = temp.z;
#endif
    return vector;
}

bool CBaseEntity::AcceptInput(const char* pszInputName, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t& Value, int outputID)
{
    return address::server::CBaseEntity_AcceptInput(this, pszInputName, pActivator, pCaller, Value, outputID, nullptr);
}

void CBaseEntity::CollisionRulesChanged()
{
    VCall_AutoVoid(CBaseEntity, CollisionRulesChanged, this);
}

SndOpEventGuid_t CBaseEntity::EmitSound(const char* pszSound, const float* pVolume, const IRecipientFilter* pFilter)
{
    // NOTE 这里Volume不生效, 需要二次下发, 所以传参先默认传1.0, 然后判断是否覆盖音量

#ifdef PLATFORM_WINDOWS
    StartSoundEventInfo_t info;
    static auto           fn = g_pGameData->GetAddress<void (*)(CBaseEntity*, StartSoundEventInfo_t*, const IRecipientFilter*, const char*, float, int64_t)>("CBaseEntity::EmitSoundFilter");
    fn(this, &info, pFilter, pszSound, 1, 0);
#else
    static auto fn   = g_pGameData->GetAddress<StartSoundEventInfo_t (*)(CBaseEntity*, const IRecipientFilter*, const char*, float, int64_t)>("CBaseEntity::EmitSoundFilter");
    const auto  info = fn(this, pFilter, pszSound, 1, 0);
#endif

    if (info.m_nSndOpEventGuid.IsValid() && pVolume != nullptr)
    {
        const auto         flVolume = *pVolume;
        CSosFieldDataFloat param(flVolume);
        const auto         setSoundEventVolume = g_pSoundOpGameSystem->SetSoundEventParam(pFilter, info.m_nSndOpEventGuid, SE_PARAM_NAME_VOLUME, &param);
        if (!setSoundEventVolume)
            FERROR("Failed to SetSoundEventVolume (%s, %f) to guid {Guid=%d, Hash=%d}", pszSound, flVolume, info.m_nSndOpEventGuid.GetGuid().GetGuid(), info.m_nSndOpEventGuid.GetStackHash());
    }

    return info.m_nSndOpEventGuid;
}

void CBaseEntity::StopSound(const char* pszSound)
{
    address::server::CBaseEntity_StopSound(this, pszSound);
}

int64_t CBaseEntity::DispatchTraceAttack(const CTakeDamageInfo* info)
{
    return address::server::CBaseEntity_DispatchTraceAttack(this, info, nullptr);
}

void CBaseEntity::SetGroundEntity(CBaseEntity* pGround, void* pPhysicsBody)
{
    address::server::CBaseEntity_SetGroundEntity(this, pGround, pPhysicsBody);
}

void CBaseEntity::SetMoveType(MoveType_t moveType)
{
    const auto c = m_MoveCollide();
    address::server::CBaseEntity_SetMoveType(this, moveType, c);
}

void CBaseEntity::SetGravityScale(float scale)
{
    address::server::CBaseEntity_SetGravityScale(this, scale);
}

void CBaseEntity::SetName(const char* pszName) const
{
    AssertPtr(m_pEntity);
    address::server::CEntityIdentity_SetEntityName(m_pEntity, pszName);
}

void CBaseEntity::DispatchSpawn(CEntityKeyValues* pEntityKeyValues)
{
    return address::server::CBaseEntity_DispatchSpawn(this, pEntityKeyValues);
}

void CBaseEntity::DispatchSpawn(CUtlVector<KeyValuesVariantItem*>* kv)
{
    CEntityKeyValues* pKeyValues = nullptr;

    if (!kv || kv->Count() == 0)
        goto end;

    pKeyValues = CEntityKeyValues::Create();

    for (int i = 0; i < kv->Count(); i++)
    {
        const auto item = kv->Element(i);

        if (item->Value.type == KeyValuesVariantValueItemType_Bool)
        {
            pKeyValues->SetBool(item->Key, item->Value.bValue);
        }
        else if (item->Value.type == KeyValuesVariantValueItemType_Int32)
        {
            pKeyValues->SetInt(item->Key, item->Value.i32Value);
        }
        else if (item->Value.type == KeyValuesVariantValueItemType_Float)
        {
            pKeyValues->SetFloat(item->Key, item->Value.flValue);
        }
        else if (item->Value.type == KeyValuesVariantValueItemType_String)
        {
            pKeyValues->SetString(item->Key, item->Value.szValue);
        }
        else if (item->Value.type == KeyValuesVariantValueItemType_Pointer)
        {
            pKeyValues->SetPointer(item->Key, item->Value.pValue);
        }
        else
        {
            FatalError("Not support KeyValuesVariantValueItemType");
        }
    }

end:
    DispatchSpawn(pKeyValues);
}
