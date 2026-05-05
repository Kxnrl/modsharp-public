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

#include "global.h"

#include "cstrike/type/CUtlString.h"

#ifdef PLATFORM_POSIX
#    include <cstring>
#endif

CUtlString::CUtlString() :
    m_pString(nullptr)
{
}

CUtlString::CUtlString(const char* pString) :
    m_pString(nullptr)
{
    Set(pString);
}

CUtlString::CUtlString(const char* pString, int length) :
    m_pString(nullptr)
{
    SetDirect(pString, length);
}

CUtlString::CUtlString(const CUtlString& string) :
    m_pString(nullptr)
{
    SetDirect(string.Get(), string.Length());
}

CUtlString::~CUtlString()
{
    Purge();
}

CUtlString& CUtlString::operator=(const CUtlString& src)
{
    SetDirect(src.Get(), src.Length());
    return *this;
}

CUtlString& CUtlString::operator=(const char* src)
{
    Set(src);
    return *this;
}

const char* CUtlString::Get() const
{
    if (!m_pString)
    {
        return "";
    }
    return m_pString;
}

int CUtlString::Length() const
{
    if (m_pString)
    {
        return static_cast<int>(strlen(m_pString));
    }
    return 0;
}

bool CUtlString::IsEmpty() const
{
    return !m_pString || m_pString[0] == 0;
}

void UtlString_Set(CUtlString* pThis, const char* string)
{
    pThis->Set(string);
}

void UtlString_SetDirect(CUtlString* pThis, const char* string, int length)
{
    pThis->SetDirect(string, length);
}

void UtlString_Purge(CUtlString* pThis)
{
    pThis->Purge();
}
