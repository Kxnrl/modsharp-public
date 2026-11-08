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

#ifndef CSTRIKE_COMPONENT_PLAYERCONTROLLERCOMPONENT_H
#define CSTRIKE_COMPONENT_PLAYERCONTROLLERCOMPONENT_H

#include "cstrike/cstrike.h"
#include "cstrike/schema.h"

class PlayerControllerComponent : public CStrikeObject
{
public:
    template <typename T>
    T GetController() const
    {   
        static auto offset = GetFieldOffset("CPlayerControllerComponent", "__m_pChainEntity");

        return GetFieldValue<T>(offset);
    }
};

class CEconItemView;
class CCSPlayerInventory;
class CSOPersonaDataPublic;

class CCSPlayerController_InventoryServices : public PlayerControllerComponent
{
    DECLARE_SCHEMA_CLASS(CCSPlayerController_InventoryServices)
public:
    inline static uint32_t sm_nPersonaDataOffset        = 0;
    inline static uint32_t sm_nPersonaDataMessageOffset = 0;
    inline static void*    sm_pPersonaDataVTable        = nullptr;
    inline static void*    sm_pPersonaDataMessageVTable = nullptr;

    SCHEMA_FIELD(uint16_t, m_unMusicID)

    [[nodiscard]] CCSPlayerInventory* GetPlayerInventory() const;
    [[nodiscard]] CEconItemView*      GetItemInLoadout(CStrikeTeam_t team, int32_t slot) const;
    [[nodiscard]] bool                TryGetPersonaDataPublic(const CSOPersonaDataPublic*& out) const;
};

#endif