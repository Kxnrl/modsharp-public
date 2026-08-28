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

#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/type/VData.h"

CCSWeaponBaseVData* CBaseWeapon::GetVData() const
{
    // m_pSubclassVData is not reflected by newer builds; schema.cpp rebuilds the entry.
    // Resolve by name so a miss aborts loudly instead of silently reading offset 0.
    static auto offset = schemas::GetOffset("CBaseEntity", "m_pSubclassVData").offset;
    return GetFieldValue<CCSWeaponBaseVData*>(offset);
}

void CBaseWeapon::Holster()
{
    VCall_AutoVoid(CBaseWeapon, Holster, this, nullptr);
}

void CBaseWeapon::Deploy()
{
    DeclareVFuncIndex(CBaseWeapon, Deploy, offset);
    CALL_VIRTUAL(bool, offset, this);
}