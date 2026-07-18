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

#ifndef CSTRIKE_TYPE_DAMAGE_RESULT_H
#define CSTRIKE_TYPE_DAMAGE_RESULT_H

#include "cstrike/type/CTakeDamageInfo.h"

#include <cstdint>

struct CTakeDamageResult
{
    CTakeDamageInfo*                                m_pOriginatingInfo;             // 0x0
    CUtlLeanVector<DestructiblePartDamageRequest_t> m_DestructibleHitGroupRequests; // 0x8

    int32_t m_nHealthLost;         // 0x18
    int32_t m_nHealthBefore;       // 0x1c
    float   m_flDamageDealt;       // 0x20
    float   m_flPreModifiedDamage; // 0x24
    Vector  m_vDamagePosition;     // 0x28

    int32_t m_nTotalledHealthLost;         // 0x34
    float   m_flTotalledDamageDealt;       // 0x38
    float   m_flTotalledPreModifiedDamage; // 0x3c
    float   m_flNewDamageAccumulatorValue; // 0x40

private:
    [[maybe_unused]] uint8_t m_nPad0[0x4]{}; // 0x44

public:
    TakeDamageFlags_t m_nDamageFlags;         // 0x48
    bool              m_bWasDamageSuppressed; // 0x50
    bool              m_bSuppressFlinch;      // 0x51

private:
    [[maybe_unused]] uint8_t m_nPad1[0x2]{}; // 0x52

public:
    HitGroup_t m_nOverrideFlinchHitGroup; // 0x54

private:
    uint32_t                 m_nOwnedHandle{kInvalidPooledHandle}; // 0x58
    [[maybe_unused]] uint8_t m_nPad2[0x4]{};                       // 0x5c

public:
    void CopyFrom(CTakeDamageInfo* pInfo)
    {
        m_pOriginatingInfo            = pInfo;
        m_nHealthLost                 = static_cast<int32_t>(pInfo->m_flDamage);
        m_nHealthBefore               = 0;
        m_flDamageDealt               = pInfo->m_flDamage;
        m_flPreModifiedDamage         = pInfo->m_flDamage;
        m_vDamagePosition             = pInfo->m_vecDamagePosition;
        m_nTotalledHealthLost         = static_cast<int32_t>(pInfo->m_flDamage);
        m_flTotalledDamageDealt       = pInfo->m_flDamage;
        m_flTotalledPreModifiedDamage = pInfo->m_flDamage;
        m_flNewDamageAccumulatorValue = 0.0f;
        m_nDamageFlags                = pInfo->m_nDamageFlags;
        m_bWasDamageSuppressed        = false;
        m_bSuppressFlinch             = false;
        m_nOverrideFlinchHitGroup     = HITGROUP_INVALID;

        const auto& requests = pInfo->m_DestructibleHitGroupsToForceDestroy;
        for (int32_t i = 0, count = requests.Count(); i < count; i++)
        {
            static_cast<void>(m_DestructibleHitGroupRequests.AddToTail(requests[i]));
        }
    }

    CTakeDamageResult() = delete;
    CTakeDamageResult(float damage) :
        m_pOriginatingInfo(nullptr),
        m_nHealthLost(static_cast<int32_t>(damage)),
        m_nHealthBefore(0),
        m_flDamageDealt(damage),
        m_flPreModifiedDamage(damage),
        m_vDamagePosition(0.0f, 0.0f, 0.0f),
        m_nTotalledHealthLost(static_cast<int32_t>(damage)),
        m_flTotalledDamageDealt(damage),
        m_flTotalledPreModifiedDamage(damage),
        m_flNewDamageAccumulatorValue(0.0f),
        m_nDamageFlags(TakeDamageFlags_t::DFLAG_NONE),
        m_bWasDamageSuppressed(false),
        m_bSuppressFlinch(false),
        m_nOverrideFlinchHitGroup(HITGROUP_INVALID)
    {
    }
};
static_assert(sizeof(CTakeDamageResult) == 0x60);
static_assert(offsetof(CTakeDamageResult, m_vDamagePosition) == 0x28);
static_assert(offsetof(CTakeDamageResult, m_nDamageFlags) == 0x48);
static_assert(offsetof(CTakeDamageResult, m_bWasDamageSuppressed) == 0x50);

#endif