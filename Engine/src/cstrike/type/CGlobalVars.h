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

#ifndef CSTRIKE_TYPE_GLOBALS_H
#define CSTRIKE_TYPE_GLOBALS_H

#include <cstdint>

enum MapLoadType_t
{
    MapLoad_NewGame = 0,
    MapLoad_LoadGame,
    MapLoad_Transition,
    MapLoad_Background,
};

enum GlobalVarsUsageWarning_t
{
    GV_RENDERTIME_CALLED_DURING_SIMULATION,
    GV_CURTIME_CALLED_DURING_RENDERING
};

using FnGlobalVarsWarningFunc = void (*)(GlobalVarsUsageWarning_t);

class CGlobalVarsBase
{
public:
    CGlobalVarsBase();

public:
    float    flRealTime;                     // 0x0
    int      nFrameCount;                    // 0x4
    float    flAbsoluteFrameTime;            // 0x8
    float    flAbsoluteFrameStartTimeStdDev; // 0xc
    uint32_t MaxClients;                     // 0x10
private:
    char pad_14[0x14];

public:
    FnGlobalVarsWarningFunc m_pAssertFunction; // 0x28
    float                   flCurTime;         // 0x30
    float                   flFrameTime;       // 0x34
private:
    char pad_38[0xc];

public:
    int nTickCount; // 0x44
private:
    char pad_48[0x8];

public:
    float flSubTickFraction; // 0x50
private:
    char pad_54[0x4];
    int  m_nCurrentThreadId; // 0x68
public:
};

#ifdef PLATFORM_WINDOWS
static_assert(offsetof(CGlobalVarsBase, MaxClients) == 0x10);
static_assert(offsetof(CGlobalVarsBase, flFrameTime) == 0x34);
static_assert(offsetof(CGlobalVarsBase, flCurTime) == 0x30);
static_assert(offsetof(CGlobalVarsBase, nTickCount) == 0x44);
static_assert(offsetof(CGlobalVarsBase, flSubTickFraction) == 0x50);
#endif

class CGlobalVars : public CGlobalVarsBase
{
public:
    CGlobalVars();

public:
    char* pszMapName; // 0x58
private:
    char pad_60[0x10];

public:
    int nMaxEntities; // 0x70
    int nServerCount; // 0x74
};
#ifdef PLATFORM_WINDOWS
static_assert(offsetof(CGlobalVars, pszMapName) == 0x60);
static_assert(offsetof(CGlobalVars, nMaxEntities) == 0x78);
#endif

#endif
