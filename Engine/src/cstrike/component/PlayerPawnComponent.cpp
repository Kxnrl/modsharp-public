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

#include "gamedata.h"
#include "global.h"
#include "vhook/call.h"

#include "cstrike/component/PlayerPawnComponent.h"
#include "cstrike/interface/CGameEntitySystem.h"

CBaseWeapon* CCSPlayer_ItemServices::GiveNamedItem(const char* classname)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CCSPlayer_ItemServices::GiveNamedItem");
    return CALL_VIRTUAL(CBaseWeapon*, offset, this, classname);
}
CBaseWeapon* CCSPlayer_ItemServices::GiveNamedItem(const char* classname, int64_t iSubType, CEconItemView* pScriptItem, bool bForce, void* vector)
{
    return address::server::PlayerPawnItemServices_GiveNamedItem(this, classname, iSubType, pScriptItem, bForce, vector);
}
void CCSPlayer_ItemServices::RemoveAllItems(bool removeSuit)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CCSPlayer_ItemServices::RemoveAllItems");
    return CALL_VIRTUAL(void, offset, this, removeSuit);
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void CPlayer_WeaponServices::SetAmmo(int32_t ammoType, uint16_t count)
{
    AssertBool(ammoType >= 0 && ammoType < MAX_AMMO_TYPES);

    auto& ammo = m_iAmmo()[ammoType];
    if (ammo == count)
        return;

    const auto& data = _schema_data_m_iAmmo();
    NetworkStateChanged(reinterpret_cast<uintptr_t>(this) + data.offset, data.key.offset, ammoType);

    ammo = count;
}

void CCSPlayer_WeaponServices::RemovePlayerItem(CBaseWeapon* weapon)
{
    return address::server::PlayerPawnWeaponServices_RemovePlayerItem(this, weapon);
}
void CCSPlayer_WeaponServices::DropWeapon(CBaseWeapon* weapon, Vector* pVecTarget, Vector* pVelocity)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CCSPlayer_WeaponServices::DropWeapon");
    CALL_VIRTUAL(void, offset, this, weapon, pVecTarget, pVelocity);
}
CBaseWeapon* CCSPlayer_WeaponServices::GetWeaponBySlot(GearSlot_t slot, int32_t position)
{
    return address::server::PlayerPawnWeaponServices_GetWeaponBySlot(this, slot, position);
}
bool CCSPlayer_WeaponServices::DetachWeapon(CBaseWeapon* weapon)
{
    return address::server::PlayerPawnWeaponServices_DetachWeapon(this, weapon);
}
bool CCSPlayer_WeaponServices::SelectItem(CBaseWeapon* weapon)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CCSPlayer_WeaponServices::SelectItem");
    return CALL_VIRTUAL(bool, offset, this, weapon, 0);
}
void CCSPlayer_WeaponServices::SwitchWeapon(CBaseWeapon* weapon)
{
    static auto offset = g_pGameData->GetVFunctionIndex("CCSPlayer_WeaponServices::SwitchWeapon");
    return CALL_VIRTUAL(void, offset, this, weapon);
}
