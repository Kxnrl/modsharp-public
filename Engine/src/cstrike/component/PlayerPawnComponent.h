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

#ifndef CSTRIKE_COMPONENT_PLAYERPAWNCOMPONENT_H
#define CSTRIKE_COMPONENT_PLAYERPAWNCOMPONENT_H

#include "cstrike/cstrike.h"
#include "cstrike/schema.h"
#include "cstrike/type/CInButtonState.h"
#include "cstrike/type/CUtlVector.h"

class CEconItemView;
class CBaseHandle;
class CBaseWeapon;
class CBaseModelEntity;
enum class GearSlot_t;

constexpr int WEAPON_SLOT_RIFLE    = 0; // (primary slot)
constexpr int WEAPON_SLOT_PISTOL   = 1; // (secondary slot)
constexpr int WEAPON_SLOT_KNIFE    = 2;
constexpr int WEAPON_SLOT_GRENADES = 3;
constexpr int WEAPON_SLOT_C4       = 4;

class PlayerPawnComponent : public CStrikeObject
{
public:
    template <typename T>
    T GetPawn() const
    {
        static auto offset = GetFieldOffset("CPlayerPawnComponent", "__m_pChainEntity");

        return GetFieldValue<T>(offset);
    }
};

class CCSPlayer_ItemServices : public PlayerPawnComponent
{
    DECLARE_SCHEMA_CLASS(CCSPlayer_ItemServices)
public:
    SCHEMA_FIELD(bool, m_bHasDefuser)
    SCHEMA_FIELD(bool, m_bHasHelmet)

    [[nodiscard]] CBaseWeapon* GiveNamedItem(const char* classname);
    [[nodiscard]] CBaseWeapon* GiveNamedItem(const char* classname, int64_t iSubType, CEconItemView* pScriptItem, bool bForce = false, void* vector = nullptr);
    void                       RemoveAllItems(bool removeSuit);
};

class CPlayer_WeaponServices : public PlayerPawnComponent
{
    DECLARE_SCHEMA_CLASS(CPlayer_WeaponServices)

public:
    SCHEMA_POINTER_FIELD(CUtlVector<CBaseHandle>, m_hMyWeapons)
    SCHEMA_FIELD(CBaseHandle, m_hActiveWeapon)
    SCHEMA_FIELD(CBaseHandle, m_hLastWeapon)
};

class CCSPlayer_WeaponServices : public CPlayer_WeaponServices
{
    DECLARE_SCHEMA_CLASS(CCSPlayer_WeaponServices)
public:
    void                       RemovePlayerItem(CBaseWeapon* weapon);
    void                       DropWeapon(CBaseWeapon* weapon, Vector* pVecTarget = nullptr, Vector* pVelocity = nullptr);
    [[nodiscard]] CBaseWeapon* GetWeaponBySlot(GearSlot_t slot, int32_t position = -1);
    [[nodiscard]] bool         DetachWeapon(CBaseWeapon* weapon);
    [[nodiscard]] bool         SelectItem(CBaseWeapon* weapon);
    void                       SwitchWeapon(CBaseWeapon* weapon);
};

class CPlayer_MovementServices : public PlayerPawnComponent
{
    DECLARE_SCHEMA_CLASS(CPlayer_MovementServices)

public:
    SCHEMA_POINTER_FIELD(CInButtonState, m_nButtons)
    SCHEMA_FIELD(float, m_flMaxspeed)
};

class CPlayer_MovementServices_Humanoid : public CPlayer_MovementServices
{
    DECLARE_SCHEMA_CLASS(CPlayer_MovementServices_Humanoid)
public:
    SCHEMA_FIELD(bool, m_bDucked)
};

class CCSPlayer_MovementServices : public CPlayer_MovementServices_Humanoid
{
    DECLARE_SCHEMA_CLASS(CCSPlayer_MovementServices)

public:
    SCHEMA_FIELD(float, m_flDuckSpeed)
};

class CCSPlayer_BulletServices : public PlayerPawnComponent
{
    DECLARE_SCHEMA_CLASS(CCSPlayer_BulletServices)

public:
    SCHEMA_FIELD(int32_t, m_totalHitsOnServer)
};

class CCSPlayer_UseServices : public PlayerPawnComponent
{
public:
    DECLARE_SCHEMA_CLASS(CCSPlayer_UseServices)
    SCHEMA_FIELD(CBaseHandle, m_hLastKnownUseEntity)
};

class CCSPlayer_DamageReactServices : public PlayerPawnComponent
{
    DECLARE_SCHEMA_CLASS(CCSPlayer_DamageReactServices)
};

#endif