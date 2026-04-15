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

enum EDestructibleParts_DestroyParameterFlags : uint32_t
{
    EDestructibleParts_DestroyParameterFlags_None                          = 0,
    EDestructibleParts_DestroyParameterFlags_GenerateBreakpieces           = 1,
    EDestructibleParts_DestroyParameterFlags_SetBodyGroupAndCollisionState = 2,
    EDestructibleParts_DestroyParameterFlags_EnableFlinches                = 4,
    EDestructibleParts_DestroyParameterFlags_ForceDamageApply              = 8,
    EDestructibleParts_DestroyParameterFlags_IgnoreKillEntityFlag          = 16,
    EDestructibleParts_DestroyParameterFlags_IgnoreHealthCheck             = 32,
    EDestructibleParts_DestroyParameterFlags_Default                       = 7,
};

struct DestructiblePartDamageRequest_t
{
    HitGroup_t m_nHitGroup;      // 0x0
    int32_t    m_nDamageLevel;   // 0x4
    uint16_t   m_nDesiredHealth; // 0x8
private:
    [[maybe_unused]] uint8_t pad_a[0x2];

public:
    EDestructibleParts_DestroyParameterFlags m_nDestroyFlags;        // 0xc
    DamageTypes_t                            m_nDamageType;          // 0x10
    float                                    m_flBreakDamage;        // 0x14
    float                                    m_flBreakDamageRadius;  // 0x18
    Vector                                   m_vWsBreakDamageOrigin; // 0x1c
    Vector                                   m_vWsBreakDamageForce;  // 0x28
private:
    [[maybe_unused]] uint8_t pad_34[0x4];

public:
}; // Size: 0x38

struct CTakeDamageResult
{
    CTakeDamageInfo*                                m_pOriginatingInfo;             // 0x0
    CUtlLeanVector<DestructiblePartDamageRequest_t> m_DestructibleHitGroupRequests; // 0x8

    int32_t           m_nHealthLost;                 // 0x18
    int32_t           m_nHealthBefore;               // 0x1c
    float             m_flDamageDealt;               // 0x20
    float             m_flPreModifiedDamage;         // 0x24
    int32_t           m_nTotalledHealthLost;         // 0x28
    float             m_flTotalledDamageDealt;       // 0x2c
    float             m_flTotalledPreModifiedDamage; // 0x30
    float             m_flNewDamageAccumulatorValue; // 0x34
    TakeDamageFlags_t m_nDamageFlags;                // 0x38
    bool              m_bWasDamageSuppressed;        // 0x40
    bool              m_bSuppressFlinch;             // 0x41

private:
    [[maybe_unused]] uint8_t m_nPad0[0x2]{}; // 0x42

public:
    HitGroup_t m_nOverrideFlinchHitGroup; // 0x44

private:
    [[maybe_unused]] uint8_t m_nPad1[0x8]{}; // 0x48

public:
    void CopyFrom(CTakeDamageInfo* pInfo)
    {
        m_pOriginatingInfo            = pInfo;
        m_nHealthLost                 = static_cast<int32_t>(pInfo->m_flDamage);
        m_nHealthBefore               = 0;
        m_flDamageDealt               = pInfo->m_flDamage;
        m_flPreModifiedDamage         = pInfo->m_flDamage;
        m_nTotalledHealthLost         = static_cast<int32_t>(pInfo->m_flDamage);
        m_flTotalledDamageDealt       = pInfo->m_flDamage;
        m_flTotalledPreModifiedDamage = pInfo->m_flDamage;
        m_flNewDamageAccumulatorValue = 0.0f;
        m_nDamageFlags                = {};
        m_bWasDamageSuppressed        = false;
        m_bSuppressFlinch             = false;
        m_nOverrideFlinchHitGroup     = HITGROUP_INVALID;
    }

    CTakeDamageResult() = delete;
    CTakeDamageResult(float damage) :
        m_pOriginatingInfo(nullptr),
        m_nHealthLost(static_cast<int32_t>(damage)),
        m_nHealthBefore(0),
        m_flDamageDealt(damage),
        m_flPreModifiedDamage(damage),
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
static_assert(sizeof(CTakeDamageResult) == 0x50);

#endif