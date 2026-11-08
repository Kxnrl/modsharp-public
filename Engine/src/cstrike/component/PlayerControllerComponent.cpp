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

#include "cstrike/component/PlayerControllerComponent.h"

[[nodiscard]] CCSPlayerInventory* CCSPlayerController_InventoryServices::GetPlayerInventory() const
{
    static auto inventoryOffset = g_pGameData->GetOffset("CCSPlayerController_InventoryServices::CCSPlayerInventory");
    return GetFieldPointer<CCSPlayerInventory*>(inventoryOffset);
}

CEconItemView* CCSPlayerController_InventoryServices::GetItemInLoadout(CStrikeTeam_t team, int32_t slot) const
{
    const auto inventory = GetPlayerInventory();
    AssertPtr(inventory);
    DeclareVFuncIndex(CCSPlayerInventory, GetItemInLoadout, offset);
    return VCall_Manual(offset, CEconItemView*, inventory, team, slot);
}

bool CCSPlayerController_InventoryServices::TryGetPersonaDataPublic(const CSOPersonaDataPublic*& out) const
{
    out = nullptr;

    if (sm_nPersonaDataOffset == 0)
        return false;

    const auto persona = *reinterpret_cast<void* const*>(reinterpret_cast<uintptr_t>(this) + sm_nPersonaDataOffset);
    if (persona == nullptr)
        return true;

    const auto message = reinterpret_cast<uintptr_t>(persona) + sm_nPersonaDataMessageOffset;
    if (*static_cast<void* const*>(persona) != sm_pPersonaDataVTable
        || *reinterpret_cast<void* const*>(message) != sm_pPersonaDataMessageVTable)
    {
        FERROR("CEconPersonaDataPublic vtable mismatch at +0x%x (message +0x%x), ResetClanTag is disabled",
               sm_nPersonaDataOffset, sm_nPersonaDataMessageOffset);
        sm_nPersonaDataOffset = 0;
        return false;
    }

    out = reinterpret_cast<const CSOPersonaDataPublic*>(message);
    return true;
}