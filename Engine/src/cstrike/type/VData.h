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

#ifndef CSTRIKE_TYPE_VDATA_H
#define CSTRIKE_TYPE_VDATA_H

#include "cstrike/consts.h"
#include "cstrike/cstrike.h"

class CBasePlayerWeaponVData : public CStrikeObject
{
public:
    DECLARE_SCHEMA_CLASS(CBasePlayerWeaponVData)

    SCHEMA_FIELD(int32_t, m_iMaxClip1)
    SCHEMA_FIELD(ItemFlagTypes_t, m_iFlags)
    SCHEMA_FIELD(int8_t, m_nPrimaryAmmoType)
};

class CCSWeaponBaseVData : public CBasePlayerWeaponVData
{
public:
    DECLARE_SCHEMA_CLASS(CCSWeaponBaseVData)

    SCHEMA_FIELD(GearSlot_t, m_GearSlot)
    SCHEMA_FIELD(int32_t, m_nPrice)
    SCHEMA_FIELD(int32_t, m_nPrimaryReserveAmmoMax)
    SCHEMA_FIELD(int32_t, m_nKillAward)
    SCHEMA_FIELD(bool, m_bMeleeWeapon)
    SCHEMA_FIELD(int32_t, m_nZoomLevels)
    SCHEMA_FIELD(int32_t, m_nDamage)
    SCHEMA_FIELD(float, m_flHeadshotMultiplier)
    SCHEMA_FIELD(float, m_flArmorRatio)
    SCHEMA_FIELD(float, m_flPenetration)
    SCHEMA_FIELD(float, m_flRange)
    SCHEMA_FIELD(float, m_flRangeModifier)
};

#endif