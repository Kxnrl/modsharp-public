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

#ifndef MS_ROOT_DEFINITIONS_H
#define MS_ROOT_DEFINITIONS_H

#include <cstdint>

enum class ECommandAction : uint8_t
{
    Skipped, // 默认行为
    Handled, // 继续执行 ExecuteStringCommand
    Stopped, // 跳过执行 ExecuteStringCommand
};

enum class EHookAction : uint8_t
{
    Ignored,                   // 不关心返回值, 也不关心参数
    ChangeParamReturnDefault,  // 修改参数后调用原始Function -> 原始返回值
    ChangeParamReturnOverride, // 修改参数后调用原始Function -> 我们提供的返回值
    IgnoreParamReturnOverride, // 忽略参数后调用原始Function -> 我们提供的返回值
    SkipCallReturnOverride,    // 忽略原始Function, 使用我们提供的返回值
};

using PlayerSlot_t      = uint8_t;
using EntityIndex_t     = int32_t;
using UserId_t          = uint16_t;
using SteamId_t         = uint64_t;
using NetworkReceiver_t = uint64_t;
using EHandle_t         = uint32_t;
using SpawnGroup_t      = uint32_t;
using WorldGroup_t      = uint32_t;

inline constexpr PlayerSlot_t      INVALID_PLAYER_SLOT  = static_cast<PlayerSlot_t>(~0);
inline constexpr UserId_t          INVALID_USER_ID      = static_cast<UserId_t>(~0);
inline constexpr EntityIndex_t     INVALID_ENTITY_INDEX = -1;
inline constexpr EntityIndex_t     WORLD_ENTITY_INDEX   = 0;
inline constexpr NetworkReceiver_t BASE_RECEIVER_MAGIC  = 1;
inline constexpr EHandle_t         INVALID_EHANDLE      = static_cast<EHandle_t>(~0);
inline constexpr PlayerSlot_t      CS_MAX_PLAYERS       = 64;
inline constexpr EntityIndex_t     MAX_NETWORKED_ENTITY = 16384;

inline constexpr float CStrikeMaxSpeed   = 260.f;
inline constexpr float DefaultSpeedValue = 1.f;

inline constexpr const char* g_szInlineHookErrors[7] = {
    "BAD_ALLOCATION",
    "FAILED_TO_DECODE_INSTRUCTION",
    "SHORT_JUMP_IN_TRAMPOLINE",
    "IP_RELATIVE_INSTRUCTION_OUT_OF_RANGE",
    "UNSUPPORTED_INSTRUCTION_IN_TRAMPOLINE",
    "FAILED_TO_UNPROTECT",
    "NOT_ENOUGH_SPACE",
};

inline constexpr const char* g_szMidFuncHookErrors[2] = {
    "BAD_ALLOCATION",
    "BAD_INLINEHOOK",
};

#endif