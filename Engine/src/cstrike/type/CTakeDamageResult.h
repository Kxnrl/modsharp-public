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

#ifndef CSTRIKE_TYPE_DAMAGE_RESULT_H
#define CSTRIKE_TYPE_DAMAGE_RESULT_H

#include "cstrike/type/CTakeDamageInfo.h"

#include <cstdint>

struct CTakeDamageResult
{
    CTakeDamageInfo* m_pOriginatingInfo;
    int32_t          m_nHealthLost;
    int32_t          m_nHealthBefore;
    int32_t          m_nDamageDealt;
    float            m_flPreModifiedDamage;
    int32_t          m_nTotalledHealthLost;
    int32_t          m_nTotalledDamageDealt;
    float            m_flTotalledPreModifiedDamage;
    bool             m_bWasDamageSuppressed;
    bool             m_bSuppressFlinch;
    HitGroup_t       m_nOverrideFlinchHitGroup;

    void CopyFrom(CTakeDamageInfo* pInfo)
    {
        m_pOriginatingInfo            = pInfo;
        m_nHealthLost                 = static_cast<int32_t>(pInfo->m_flDamage);
        m_nHealthBefore               = 0;
        m_nDamageDealt                = static_cast<int32_t>(pInfo->m_flDamage);
        m_flPreModifiedDamage         = pInfo->m_flDamage;
        m_nTotalledHealthLost         = static_cast<int32_t>(pInfo->m_flDamage);
        m_nTotalledDamageDealt        = static_cast<int32_t>(pInfo->m_flDamage);
        m_flTotalledPreModifiedDamage = pInfo->m_flDamage;
        m_bWasDamageSuppressed        = false;
        m_bSuppressFlinch             = false;
        m_nOverrideFlinchHitGroup     = HITGROUP_INVALID;
    }

    CTakeDamageResult() = delete;

    constexpr CTakeDamageResult(float damage) :
        m_pOriginatingInfo(nullptr),
        m_nHealthLost(static_cast<int32_t>(damage)),
        m_nHealthBefore(0),
        m_nDamageDealt(static_cast<int32_t>(damage)),
        m_flPreModifiedDamage(damage),
        m_nTotalledHealthLost(static_cast<int32_t>(damage)),
        m_nTotalledDamageDealt(static_cast<int32_t>(damage)),
        m_flTotalledPreModifiedDamage(damage),
        m_bWasDamageSuppressed(false),
        m_bSuppressFlinch(false),
        m_nOverrideFlinchHitGroup(HITGROUP_INVALID)
    {
    }
};
static_assert(sizeof(CTakeDamageResult) == 48);

#endif