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

#ifndef CSTRIKE_TYPE_CPLAYERSLOT_H
#define CSTRIKE_TYPE_CPLAYERSLOT_H

#include "definitions.h"

// :( stupid ABI compatibility

struct CPlayerSlot
{
    constexpr CPlayerSlot() :
        m_nSlot(-1) {}

    constexpr CPlayerSlot(int32_t data) :
        m_nSlot(data) {}

    operator int32_t() const { return m_nSlot; }

    bool operator==(const CPlayerSlot& other) const { return other.m_nSlot == m_nSlot; }
    bool operator!=(const CPlayerSlot& other) const { return other.m_nSlot != m_nSlot; }

    EntityIndex_t GetControllerIndex() const { return m_nSlot + 1; }
    PlayerSlot_t  GetPlayerSlot() const { return static_cast<PlayerSlot_t>(m_nSlot); }

private:
    int32_t m_nSlot;
};

#endif