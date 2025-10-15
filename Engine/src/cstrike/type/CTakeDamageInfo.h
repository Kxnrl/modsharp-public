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

#ifndef CSTRIKE_TYPE_DAMAGEINFO_H
#define CSTRIKE_TYPE_DAMAGEINFO_H

#include "cstrike/cstrike.h"
#include "cstrike/type/CTrace.h"
#include "cstrike/type/Vector.h"

#include <cstdint>

enum class TakeDamageFlags_t : uint64_t
{
    DFLAG_NONE                         = 0,       // 0x0
    DFLAG_SUPPRESS_HEALTH_CHANGES      = 1 << 0,  // 0x1
    DFLAG_SUPPRESS_PHYSICS_FORCE       = 1 << 1,  // 0x2
    DFLAG_SUPPRESS_EFFECTS             = 1 << 2,  // 0x4
    DFLAG_PREVENT_DEATH                = 1 << 3,  // 0x8
    DFLAG_FORCE_DEATH                  = 1 << 4,  // 0x10
    DFLAG_ALWAYS_GIB                   = 1 << 5,  // 0x20
    DFLAG_NEVER_GIB                    = 1 << 6,  // 0x40
    DFLAG_REMOVE_NO_RAGDOLL            = 1 << 7,  // 0x80
    DFLAG_SUPPRESS_DAMAGE_MODIFICATION = 1 << 8,  // 0x100
    DFLAG_ALWAYS_FIRE_DAMAGE_EVENTS    = 1 << 9,  // 0x200
    DFLAG_RADIUS_DMG                   = 1 << 10, // 0x400
    DFLAG_FORCEREDUCEARMOR_DMG         = 1 << 11, // 0x800
    DFLAG_SUPPRESS_INTERRUPT_FLINCH    = 1 << 12, // 0x1000
    DFLAG_IGNORE_DESTRUCTIBLE_PARTS    = 1 << 13, // 0x2000
    DFLAG_SUPPRESS_BREAKABLES          = 1 << 14, // 0x4000
    DFLAG_FORCE_PHYSICS_FORCE          = 1 << 15, // 0x8000
    DFLAG_IGNORE_ARMOR                 = 1 << 16, // 0x10000
    DFLAG_SUPPRESS_UTILREMOVE          = 1 << 17, // 0x20000
};

enum class DamageTypes_t : int32_t
{
    DMG_GENERIC       = 0,       // 0x0
    DMG_CRUSH         = 1 << 0,  // 0x1
    DMG_BULLET        = 1 << 1,  // 0x2
    DMG_SLASH         = 1 << 2,  // 0x4
    DMG_BURN          = 1 << 3,  // 0x8
    DMG_VEHICLE       = 1 << 4,  // 0x10
    DMG_FALL          = 1 << 5,  // 0x20
    DMG_BLAST         = 1 << 6,  // 0x40
    DMG_CLUB          = 1 << 7,  // 0x80
    DMG_SHOCK         = 1 << 8,  // 0x100
    DMG_SONIC         = 1 << 9,  // 0x200
    DMG_ENERGYBEAM    = 1 << 10, // 0x400
    DMG_BUCKSHOT      = 1 << 11, // 0x800
    DMG_DROWN         = 1 << 14, // 0x4000
    DMG_POISON        = 1 << 15, // 0x8000
    DMG_RADIATION     = 1 << 16, // 0x10000
    DMG_DROWNRECOVER  = 1 << 17, // 0x20000
    DMG_ACID          = 1 << 18, // 0x40000
    DMG_PHYSGUN       = 1 << 20, // 0x100000
    DMG_DISSOLVE      = 1 << 21, // 0x200000
    DMG_BLAST_SURFACE = 1 << 22, // 0x400000
    DMG_HEADSHOT      = 1 << 23, // 0x800000
};

struct AttackerInfo_t
{
    bool        m_bNeedInit;
    bool        m_bIsPawn;
    bool        m_bIsWorld;
    CBaseHandle m_hAttackerPawn;
    int         m_nAttackerPlayerSlot;
    int         m_nTeamChecked;
    int         m_nTeam;
};
static_assert(sizeof(AttackerInfo_t) == 20);

struct ShootInfo_t
{
    int                      m_iAttackTick;          // 0x0
    float                    m_iAttackTickFraction;  // 0x4
    float                    m_flAttackTime;         // 0x8
    int                      m_iRenderTickCount;     // 0xc
    float                    m_flRenderTickFraction; // 0x10
    Vector                   m_vecAttackPos;         // 0x14
    Vector                   m_vecAttackAngles;      // 0x20
    Vector                   m_vecPunchAngles;       // 0x2c
    bool                     m_bFirstShot;           // 0x38
    int                      m_iState;               // 0x3c
    int                      m_iServerTick;          // 0x40
    float                    m_flInaccuracy;         // 0x44
    float                    m_flGroundInaccuracy;   // 0x48
    float                    m_flAirInaccuracy;      // 0x4c
    float                    m_flRecoilIndex;        // 0x50
    [[maybe_unused]] uint8_t pad_54[0x8];            // 0x54
}; // Size: 0x54
static_assert(sizeof(ShootInfo_t) == 0x5C);

class CTakeDamageInfo
{
    virtual ~CTakeDamageInfo() = 0;
    virtual void Method_001()  = 0;

public:
    Vector      m_vecDamageForce;      // 0x8  |  8
    Vector      m_vecDamagePosition;   // 0x14 | 20
    Vector      m_vecReportedPosition; // 0x20 | 32
    Vector      m_vecDamageDirection;  // 0x2c | 44
    CBaseHandle m_hInflictor;          // 0x38 | 56
    CBaseHandle m_hAttacker;           // 0x3c | 60
    CBaseHandle m_hAbility;            // 0x40 | 64
    float       m_flDamage;            // 0x44 | 68
    float       m_flTotalledDamage;    // 0x48 | 72
    int32_t     m_bitsDamageType;      // 0x4c | 76
    int32_t     m_iDamageCustom;       // 0x50 | 80
    int8_t      m_iAmmoType;           // 0x54 | 84

private:
    [[maybe_unused]] uint8_t m_nUnknown0[0xb]; // 0x55 | 85

public:
    float m_flOriginalDamage; // 0x60 | 96
    bool  m_bShouldBleed;     // 0x64 | 100
    bool  m_bShouldSpark;     // 0x65 | 101

private:
    [[maybe_unused]] uint8_t m_nUnknown1[0x2]{}; // 0x66

public:
    CGameTrace*       m_pTrace;       // 0x68 | 104
    TakeDamageFlags_t m_nDamageFlags; // 0x70 | 112

private:
    [[maybe_unused]] uint8_t m_sDamageSourceName[0x8]{}; // 0x78 | 120

public:
    HitGroup_t     m_iHitGroupId;                        // 0x80 | 128
    int32_t        m_nNumObjectsPenetrated;              // 0x84 | 132
    float          m_flFriendlyFireDamageReductionRatio; // 0x88 | 136
    ShootInfo_t    m_ShootInfo;                          // 0x9c | 140
    void*          m_hScriptInstance;                    // 0xe8 | 232
    AttackerInfo_t m_AttackerInfo;                       // 0xf0 | 240
private:
    [[maybe_unused]] uint8_t m_nUnknown2[0x1C]{}; // 0x104 | 260

public:
    bool m_bInTakeDamageFlow; // 0x120 | 288

private:
    [[maybe_unused]] int32_t m_nUnknown3; // 0x124 | 292

public:
    [[nodiscard]] bool HasTakeDamageFlags(TakeDamageFlags_t flags) const
    {
        auto currentFlags = static_cast<uint64_t>(m_nDamageFlags);
        auto flagToCheck  = static_cast<uint64_t>(flags);
        return (currentFlags & flagToCheck) != 0;
    }
};
static_assert(sizeof(CTakeDamageInfo) == 296);
#ifdef PLATFORM_WINDOWS
static_assert(offsetof(CTakeDamageInfo, m_flOriginalDamage) == 96);
static_assert(offsetof(CTakeDamageInfo, m_pTrace) == 104);
static_assert(offsetof(CTakeDamageInfo, m_hScriptInstance) == 232);
static_assert(offsetof(CTakeDamageInfo, m_AttackerInfo) == 240);
static_assert(offsetof(CTakeDamageInfo, m_bInTakeDamageFlow) == 288);
#endif

#endif
