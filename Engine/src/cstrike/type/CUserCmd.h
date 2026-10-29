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

#ifndef CSTRIKE_TYPE_USERCMD_H
#define CSTRIKE_TYPE_USERCMD_H

#include <cstdint>

enum class EButtons : uint64_t
{
    IN_ATTACK         = 1ULL << 0,
    IN_JUMP           = 1ULL << 1,
    IN_DUCK           = 1ULL << 2,
    IN_FORWARD        = 1ULL << 3,
    IN_BACK           = 1ULL << 4,
    IN_USE            = 1ULL << 5,
    IN_TURNLEFT       = 1ULL << 7,
    IN_TURNRIGHT      = 1ULL << 8,
    IN_MOVELEFT       = 1ULL << 9,
    IN_MOVERIGHT      = 1ULL << 10,
    IN_ATTACK2        = 1ULL << 11,
    IN_RELOAD         = 1ULL << 13,
    IN_SPEED          = 1ULL << 16,
    IN_JOYAUTOSPRINT  = 1ULL << 17,
    IN_USEORRELOAD    = 1ULL << 32,
    IN_SCORE          = 1ULL << 33,
    IN_ZOOM           = 1ULL << 34,
    IN_LOOK_AT_WEAPON = 1ULL << 35,
};

struct CButtons
{
    EButtons m_nKeyButtons;
    EButtons m_nKeyButtonsChanged;
    EButtons m_nScrollButtons;
};

#endif