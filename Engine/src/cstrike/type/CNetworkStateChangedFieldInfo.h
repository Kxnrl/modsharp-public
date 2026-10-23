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

#ifndef CSTRIKE_TYPE_NETWORK_STATE_CHANGED_FIELD_INFO_H
#define CSTRIKE_TYPE_NETWORK_STATE_CHANGED_FIELD_INFO_H

#include "cstrike/type/CUtlVector.h"

struct CNetworkStateChangedInfo
{
    CNetworkStateChangedInfo() = delete;

    CNetworkStateChangedInfo(uint32_t nOffset, uint32_t nArrayIndex, uint32_t nPathIndex)
    {
        m_nUseOffsetData = 1;
        m_vecOffsetData.AddToTail(nOffset);

        m_nLine             = -1;
        m_nRequirePathIndex = 0;

        m_nArrayIndex = nArrayIndex;
        m_nPathIndex  = nPathIndex;
    }

private:
    int                  m_nUseOffsetData{};    // 0x0
    CUtlVector<uint32_t> m_vecOffsetData;       // 0x8
    const char*          m_pszFieldName{};      // 0x20
    const char*          m_pszFileName{};       // 0x28
    int32_t              m_nLine = -1;          // 0x30
    uint32_t             m_nArrayIndex{};       // 0x34
    uint32_t             m_nPathIndex{};        // 0x38
    uint16_t             m_nRequirePathIndex{}; // 0x3c
}; // Size: 0x40
static_assert(sizeof(CNetworkStateChangedInfo) == 0x40);
#endif
