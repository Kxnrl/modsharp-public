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

#ifndef CSTRIKE_ENTITY_PLAYERPAWN_H
#define CSTRIKE_ENTITY_PLAYERPAWN_H

#include "cstrike/entity/CBaseAnimGraph.h"

class CCSPlayer_WeaponServices;
class CCSPlayer_ItemServices;
class CCSPlayer_ViewModelServices;
class CBaseWeapon;
class CEconItemView;

// CBasePlayer
class CBasePlayerPawn : public CBaseAnimGraph
{
public:
    DECLARE_SCHEMA_CLASS(CBasePlayerPawn)

    SCHEMA_FIELD(CCSPlayer_WeaponServices*, m_pWeaponServices)
    SCHEMA_FIELD(CCSPlayer_ItemServices*, m_pItemServices)
    SCHEMA_FIELD(CBaseHandle, m_hController)

private:
    [[nodiscard]] CBasePlayerController* GetControllerInternal();

public:
    [[nodiscard]] bool IsAlive() { return GetLifeState() == LIFE_ALIVE && GetHealth() > 0; }
    CBaseWeapon*       GiveNamedItem(const char* pszWeaponName);
    void               RemoveAllItems(bool removeSuit);

    template <typename T>
    [[nodiscard]] T GetController()
    {
        return reinterpret_cast<T>(GetControllerInternal());
    }

    static CBasePlayerPawn* FindBySlot(PlayerSlot_t slot);

    void ClientPrint(HudPrint_t dest, const char* name, const char* param1 = nullptr, const char* param2 = nullptr, const char* param3 = nullptr, const char* param4 = nullptr);
    void ClientPrintF(HudPrint_t dest, const char* message, ...);

    void             CommitSuicide(bool explode);
    void             TransientChangeTeam(CStrikeTeam_t team) const;
    SndOpEventGuid_t EmitSoundClient(const char* pszSound, const float* pVolume = nullptr);

    /* Proxy of weapon service */
    CBaseWeapon* GetActiveWeapon();
    CBaseWeapon* GetWeaponBySlot(GearSlot_t slot, int32_t position = -1);
    void         RemovePlayerItem(CBaseWeapon* pItem);
    bool         DetachWeapon(CBaseWeapon* pWeapon);
    void         DropWeapon(CBaseWeapon* pWeapon);
    bool         SelectItem(CBaseWeapon* pWeapon);
    void         SwitchWeapon(CBaseWeapon* pWeapon);
};

class CCSPlayerPawnBase : public CBasePlayerPawn
{
public:
    DECLARE_SCHEMA_CLASS(CCSPlayerPawnBase)

    SCHEMA_FIELD(CBaseHandle, m_hOriginalController)

private:
    [[nodiscard]] CBasePlayerController* GetOriginalControllerInternal();

public:
    bool IsPlayer();

    template <typename T>
    [[nodiscard]] T GetOriginalController()
    {
        return reinterpret_cast<T>(GetOriginalControllerInternal());
    }
};

class CCSPlayerPawn : public CCSPlayerPawnBase
{
public:
    DECLARE_SCHEMA_CLASS(CCSPlayerPawn)

    SCHEMA_POINTER_FIELD(CEconItemView, m_EconGloves)
    SCHEMA_FIELD(uint16_t, m_nCharacterDefIndex)
    SCHEMA_FIELD(float, m_flVelocityModifier)
    SCHEMA_FIELD(int32_t, m_ArmorValue)

    void SetDefaultGloves(bool hide);
    void GiveGloves(int32_t itemDefIndex, int32_t prefab, float wear, int32_t seed);
};

class CCSObserverPawn : public CCSPlayerPawnBase
{
};

#endif