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

#ifndef CSTRIKE_TYPE_CUTLSYMBOLLARGE_H
#define CSTRIKE_TYPE_CUTLSYMBOLLARGE_H

class CUtlSymbolLarge
{
public:
    constexpr CUtlSymbolLarge() :
        m_pString(nullptr) {}
    constexpr CUtlSymbolLarge(const char* pString) :
        m_pString(pString) {}

    bool operator==(const CUtlSymbolLarge& src) const { return (uintptr_t)m_pString == (uintptr_t)src.m_pString; }
    bool operator!=(const CUtlSymbolLarge& src) const { return (uintptr_t)m_pString != (uintptr_t)src.m_pString; }

    bool operator<(const CUtlSymbolLarge& src) const { return (uintptr_t)m_pString < (uintptr_t)src.m_pString; }

    [[nodiscard]] const char* Get() const
    {
        if (!m_pString)
            return "";
        return m_pString;
    }

    [[nodiscard]] bool IsValid() const { return m_pString != nullptr; }

private:
    const char* m_pString;
};

#endif