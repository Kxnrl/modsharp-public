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

#include "gamedata.h"
#include "global.h"
#include "vhook/call.h"

#include "cstrike/component/PlayerPawnComponent.h"
#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/type/CEconItemView.h"
#include "cstrike/type/EmitSound.h"

#include <cstdarg>

CBasePlayerController* CBasePlayerPawn::GetControllerInternal()
{
    return g_pGameEntitySystem->FindEntityByEHandle<CBasePlayerController*>(m_hController());
}

CBaseWeapon* CBasePlayerPawn::GiveNamedItem(const char* pszWeaponName)
{
    const auto pItemServices = m_pItemServices();
    if (!pItemServices)
    {
        WARN("GiveNamedItem with nullptr item services -> weapon [%s] ", pszWeaponName);
        return nullptr;
    }
    return pItemServices->GiveNamedItem(pszWeaponName);
}

void CBasePlayerPawn::RemoveAllItems(bool removeSuit)
{
    const auto pItemServices = m_pItemServices();
    if (!pItemServices)
    {
        WARN("RemoveAllItems with nullptr item services");
        return;
    }
    return pItemServices->RemoveAllItems(removeSuit);
}

CBasePlayerPawn* CBasePlayerPawn::FindBySlot(PlayerSlot_t slot)
{
    if (const auto pController = CBasePlayerController::FindBySlot(slot))
        return g_pGameEntitySystem->FindEntityByEHandle<CBasePlayerPawn*>(pController->m_hPawn());

    return nullptr;
}

void CBasePlayerPawn::ClientPrint(HudPrint_t dest, const char* name, const char* param1, const char* param2, const char* param3, const char* param4)
{
    const auto pController = GetController<CBasePlayerController*>();
    if (!pController)
        return;

    pController->ClientPrint(dest, name, param1, param2, param3, param4);
}

void CBasePlayerPawn::ClientPrintF(HudPrint_t dest, const char* message, ...)
{
    va_list args;
    va_start(args, message);

    char buffer[256];
    vsnprintf(buffer, sizeof(buffer), message, args);

    va_end(args);

    ClientPrint(dest, buffer);
}

void CBasePlayerPawn::CommitSuicide(bool explode)
{
    VCall_AutoVoid(CBasePlayerPawn, CommitSuicide, this, explode, true);
}

CBaseWeapon* CBasePlayerPawn::GetActiveWeapon()
{
    const auto pWeaponServices = m_pWeaponServices();
    AssertPtr(pWeaponServices);

    return g_pGameEntitySystem->FindEntityByEHandle<CBaseWeapon*>(pWeaponServices->m_hActiveWeapon());
}

CBaseWeapon* CBasePlayerPawn::GetWeaponBySlot(GearSlot_t slot, int32_t position)
{
    const auto pWeaponServices = m_pWeaponServices();
    if (!pWeaponServices)
        return nullptr;

    return pWeaponServices->GetWeaponBySlot(slot, position);
}

void CBasePlayerPawn::RemovePlayerItem(CBaseWeapon* pItem)
{
    const auto pWeaponServices = m_pWeaponServices();
    if (!pWeaponServices)
        return;

    pWeaponServices->RemovePlayerItem(pItem);
}

bool CBasePlayerPawn::DetachWeapon(CBaseWeapon* pWeapon)
{
    const auto pWeaponServices = m_pWeaponServices();
    if (!pWeaponServices)
        return false;

    return pWeaponServices->DetachWeapon(pWeapon);
}

void CBasePlayerPawn::DropWeapon(CBaseWeapon* pWeapon)
{
    const auto pWeaponServices = m_pWeaponServices();
    if (!pWeaponServices)
        return;

    pWeaponServices->DropWeapon(pWeapon, nullptr, nullptr);
}

bool CBasePlayerPawn::SelectItem(CBaseWeapon* pWeapon)
{
    const auto pWeaponServices = m_pWeaponServices();
    if (!pWeaponServices)
        return false;

    return pWeaponServices->SelectItem(pWeapon);
}

void CBasePlayerPawn::SwitchWeapon(CBaseWeapon* pWeapon)
{
    const auto pWeaponServices = m_pWeaponServices();
    if (!pWeaponServices)
        return;

    pWeaponServices->SwitchWeapon(pWeapon);
}

void CBasePlayerPawn::TransientChangeTeam(CStrikeTeam_t team) const
{
    static auto offset = GetFieldOffset("CBaseEntity", "m_iTeamNum");
    SetFieldValue(offset, team);
}

SndOpEventGuid_t CBasePlayerPawn::EmitSoundClient(const char* pszSound, const float* pVolume)
{
    const auto pController = GetController<CBasePlayerController*>();
    if (!pController)
    {
        FERROR("EmitSoundClient with nullptr controller -> sound [%s] ", pszSound);
        return g_DefaultSndOpEventGuid;
    }

    return pController->EmitSoundClient(pszSound, pVolume);
}

void CCSPlayerPawn::GiveGloves(int itemDefIndex, int prefab, float wear, int seed)
{
    AssertBool(itemDefIndex >= 0 && itemDefIndex <= 65535);

    const auto pItem = m_EconGloves();
    if (const auto attributes = pItem->m_NetworkedDynamicAttributes())
    {
        pItem->m_iItemDefinitionIndex(static_cast<uint16_t>(itemDefIndex));
        attributes->SetOrAddAttributeValueByName("set item texture prefab", static_cast<float>(prefab));
        attributes->SetOrAddAttributeValueByName("set item texture wear", static_cast<float>(wear));
        attributes->SetOrAddAttributeValueByName("set item texture seed", static_cast<float>(seed));

        pItem->m_iItemIDLow(-1);
        pItem->m_bInitialized(true);

        SetBodyGroupByName("default_gloves", 1);
    }
}

CBasePlayerController* CCSPlayerPawnBase::GetOriginalControllerInternal()
{
    return g_pGameEntitySystem->FindEntityByEHandle<CBasePlayerController*>(m_hOriginalController());
}

bool CCSPlayerPawnBase::IsPlayer()
{
    static auto offset = g_pGameData->GetVFunctionIndex("CCSPlayerPawnBase::IsPlayer");
    return CALL_VIRTUAL(bool, offset, this);
}
