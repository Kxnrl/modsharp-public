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

#ifndef CSTRIKE_ENTITY_ENTITY_H
#define CSTRIKE_ENTITY_ENTITY_H

#include "cstrike/consts.h"
#include "cstrike/schema.h"
#include "cstrike/type/CEntityInstance.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/QAngle.h"
#include "cstrike/type/Vector.h"

class CBodyComponent;
class CCSPlayerPawn;
class CCSPlayerController;
class CBaseWeapon;
class CTakeDamageInfo;
class CEntityKeyValues;
class IRecipientFilter;
class CNetworkVelocityVector;

struct KeyValuesVariantItem;

class CBaseEntity : public CEntityInstance
{
public:
    DECLARE_SCHEMA_CLASS(CBaseEntity)

    SCHEMA_FIELD(uint64_t, m_isSteadyState)
    SCHEMA_FIELD(float, m_lastNetworkChange)
    SCHEMA_POINTER_FIELD(void*, m_NetworkTransmitComponent)
    SCHEMA_FIELD(int32_t, m_iHealth)
    SCHEMA_FIELD(CStrikeTeam_t, m_iTeamNum)

    SCHEMA_FIELD(int32_t, m_iMaxHealth)
    SCHEMA_FIELD(lifeState_t, m_lifeState)
    SCHEMA_FIELD(MoveCollide_t, m_MoveCollide)
    SCHEMA_FIELD(MoveType_t, m_MoveType)
    SCHEMA_FIELD(MoveType_t, m_nActualMoveType)
    SCHEMA_FIELD(uint32_t, m_spawnflags)
    SCHEMA_FIELD(EntityFlags_t, m_fFlags)
    SCHEMA_FIELD(uint32_t, m_fEffects)
    SCHEMA_FIELD(const char*, m_sUniqueHammerID)
    SCHEMA_FIELD(float, m_flGravityScale)
    SCHEMA_FIELD(float, m_flActualGravityScale)
    SCHEMA_FIELD(float, m_flFriction)
    SCHEMA_FIELD(CCollisionProperty*, m_pCollision)
    SCHEMA_FIELD(CBaseHandle, m_hOwnerEntity)
    SCHEMA_FIELD(CBodyComponent*, m_CBodyComponent)
    SCHEMA_FIELD(Vector, m_vecBaseVelocity)
    SCHEMA_FIELD(char*, m_target)
    SCHEMA_FIELD(float, m_flSpeed)
    SCHEMA_POINTER_FIELD(CNetworkVelocityVector, m_vecVelocity)
    SCHEMA_FIELD(CBaseHandle, m_hGroundEntity)

    [[nodiscard]] CStrikeTeam_t GetTeam() { return m_iTeamNum(); }
    [[nodiscard]] lifeState_t   GetLifeState() { return m_lifeState(); }
    [[nodiscard]] int32_t       GetHealth() { return m_iHealth(); }
    [[nodiscard]] int32_t       GetMaxHealth() { return m_iMaxHealth(); }
    [[nodiscard]] MoveType_t    GetMoveType() { return m_MoveType(); }
    [[nodiscard]] uint32_t      GetSpawnFlags() { return m_spawnflags(); }
    [[nodiscard]] EntityFlags_t GetFlags() { return m_fFlags(); }
    [[nodiscard]] CBaseEntity*  GetOwner();
    [[nodiscard]] const char*   GetHammerId() const
    {
        static auto offset = GetFieldOffset("CBaseEntity", "m_sUniqueHammerID");
        return GetFieldValue<const char*>(offset);
    }
    void AddFlags(EntityFlags_t add)
    {
        uint32_t flags = m_fFlags();
        flags |= add;
        m_fFlags(static_cast<EntityFlags_t>(flags));
    }

    [[nodiscard]] bool IsWorld() const { return GetEntityIndex() == 0; }
    [[nodiscard]] bool IsPlayerPawn();
    [[nodiscard]] bool IsPlayerController();
    [[nodiscard]] bool IsWeapon();

#ifdef VIRTUAL_NetworkStateChanged
    void NetworkStateChanged(uint16_t offset)
    {
        static auto _offset = g_pGameData->GetOffset("CBaseEntity::NetworkStateChanged");
        return CALL_VIRTUAL(void, _offset, this, offset, static_cast<int64_t>(0xFFFFFFFF), static_cast<int64_t>(0xFFFF));
    }
#endif

    void SetOwner(CBaseEntity* pEntity);
    void SetModel(const char* pszModel);

    void ChangeTeam(CStrikeTeam_t team);

    void Teleport(Vector* vPosition, QAngle* vAngles, Vector* vVelocity);

    void ApplyAbsVelocityImpulse(Vector* vVelocity);

    [[nodiscard]] const QAngle& GetEyeAngles();
    [[nodiscard]] const Vector& GetEyePosition();

    [[nodiscard]] const QAngle& GetAbsAngles();
    void                        SetAbsAngles(QAngle*);
    [[nodiscard]] const Vector& GetAbsOrigin();
    void                        SetAbsOrigin(Vector*);

    [[nodiscard]] const Vector& GetLocalVelocity();
    void                        SetLocalVelocity(Vector*);
    [[nodiscard]] const Vector& GetAbsVelocity();
    void                        SetAbsVelocity(Vector*);

    [[nodiscard]] const Vector& GetCenter();

    bool AcceptInput(const char* pszInputName, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t& Value, int outputID);

    [[nodiscard]] bool IsMarkedForDeletion() const { return m_pEntity->IsMarkedForDeletion(); }

    void CollisionRulesChanged();

    SndOpEventGuid_t EmitSound(const char* pszSound, const float* pVolume, const IRecipientFilter* pFilter);

    void StopSound(const char* pszSound);

    bool CanTakeDamage() const
    {
        static auto offset = GetFieldOffset("CBaseEntity", "m_bTakesDamage");
        return GetFieldValue<bool>(offset);
    }

    int64_t DispatchTraceAttack(const CTakeDamageInfo* info);

    void TransientChangeCanTakeDamage(bool can) const
    {
        static auto offset = GetFieldOffset("CBaseEntity", "m_bTakesDamage");
        SetFieldValue(offset, can);
    }

    void TransientChangeLifeState(lifeState_t state) const
    {
        static auto offset = GetFieldOffset("CBaseEntity", "m_lifeState");
        SetFieldValue(offset, state);
    }

    void SetGroundEntity(CBaseEntity* pGround, void* pPhysicsBody);

    void SetMoveType(MoveType_t moveType);
    void SetGravityScale(float scale);

    void SetName(const char* pszName) const;

    [[nodiscard]] CBasePlayerPawn* ToPlayerPawn()
    {
        if (IsPlayerPawn())
            return reinterpret_cast<CBasePlayerPawn*>(this);
        return nullptr;
    }

    [[nodiscard]] CCSPlayerController* ToPlayerController()
    {
        if (IsPlayerController())
            return reinterpret_cast<CCSPlayerController*>(this);

        return nullptr;
    }

    [[nodiscard]] CBaseWeapon* ToWeapon()
    {
        if (IsWeapon())
            return reinterpret_cast<CBaseWeapon*>(this);

        return nullptr;
    }

    [[nodiscard]] static bool InBypassHook() { return s_bBypassDamageHook; }
    static void               SetBypassHook(bool state) { s_bBypassDamageHook = state; }

    void DispatchSpawn(CEntityKeyValues* pEntityKeyValues);
    void DispatchSpawn(CUtlVector<KeyValuesVariantItem*>* kv = nullptr);

private:
    static bool s_bBypassDamageHook;
};

#endif
