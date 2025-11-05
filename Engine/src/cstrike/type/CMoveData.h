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

#ifndef CSTRIKE_TYPE_MOVEDATA_H
#define CSTRIKE_TYPE_MOVEDATA_H

#include "cstrike/type/CTrace.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/QAngle.h"
#include "cstrike/type/Vector.h"

#include <cstdint>

#define USE_MV_RE

struct touchlist_t
{
    Vector     deltaVelocity;
    CGameTrace trace;
};

struct SubtickMove
{
    float    when;
    uint64_t button;

    union {
        bool pressed;

        struct
        {
            float analog_forward_delta;
            float analog_left_delta;
            float analog_pitch_delta;
            float analog_yaw_delta;
        } analogMove;
    };
};

struct SubtickAttack
{
    float    when;
    uint64_t button;
    bool     pressed;
};

#pragma pack(push, 1)
class CMoveData
{
public:
    CMoveData() = default;

    uint8_t m_nFlags; // 0x0

private:
    uint8_t m_pad_0x1[0x3];

public:
    uint32_t m_nPlayerHandle;           // 0x4
    QAngle   m_vecAbsViewAngles;        // 0x8
    QAngle   m_vecViewAngles;           // 0x14
    Vector   m_vecLastMovementImpulses; // 0x20
    float    m_flForwardMove;           // 0x2c
    float    m_flSideMove;              // 0x30
    float    m_flUpMove;                // 0x34
    Vector   m_vecVelocity;             // 0x38
    Vector   m_vecAngles;               // 0x44

private:
    [[maybe_unused]] uint8_t m_pad_0x50[0x10];

public:
    CUtlVector<SubtickMove>   m_SubtickMoves;       // 0x60
    CUtlVector<SubtickAttack> m_AttackSubtickMoves; // 0x78
    bool                      m_bHasSubtickInputs;  // 0x90

private:
    [[maybe_unused]] uint8_t m_pad_0x91[0x7];

public:
    CUtlVector<touchlist_t> m_TouchList;               // 0x98
    Vector                  m_vecCollisionNormal;      // 0xb0
    Vector                  m_vecGroundNormal;         // 0xbc
    Vector                  m_vecAbsOrigin;            // 0xc8
    int32_t                 m_nFullMoveStartTick;      // 0xd4
    int32_t                 m_nFullMoveEndTick;        // 0xd8
    float                   m_flFullMoveStartFraction; // 0xdc
    float                   m_flFullMoveEndFraction;   // 0xe0

private:
    [[maybe_unused]] uint8_t m_pad_0xe4[4]; // 0xe4

public:
    Vector m_outWishVel;           // 0xe8
    Vector m_vecOldAngles;         // 0xf4
    Vector m_vecInputRotated;      // 0x100
    Vector m_vecAccel;             // 0x10c
    Vector m_vecAccelPerSecond;    // 0x118
    float  m_flMaxSpeed;           // 0x124
    float  m_flClientMaxSpeed;     // 0x128
    float  m_flSubtickFraction;    // 0x12c
    bool   m_bInAir;               // 0x130
    bool   m_bGameCodeMovedPlayer; // 0x131

}; // Size: 0x132
#pragma pack(pop)
static_assert(sizeof(CMoveData) == 0x132, "sizeof(CMoveData) != 0x132");

#endif
