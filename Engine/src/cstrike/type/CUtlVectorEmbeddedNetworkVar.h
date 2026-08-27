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

#ifndef CSTRIKE_TYPE_CUTLVECTOREMBEDDEDNETWORKVAR_H
#define CSTRIKE_TYPE_CUTLVECTOREMBEDDEDNETWORKVAR_H

#include <cstdint>

int32_t UtlVectorEmbeddedNetworkVarSize(const char* classname);

// Read-only accessor for a CUtlVectorEmbeddedNetworkVar< T > schema field.
template <typename T>
class CUtlVectorEmbeddedNetworkVarImpl
{
public:
    CUtlVectorEmbeddedNetworkVarImpl(uintptr_t field, const char* className) :
        m_pField(field),
        m_pszClassName(className)
    {
    }

    [[nodiscard]] int32_t Count() const { return *reinterpret_cast<const int32_t*>(m_pField); }

    [[nodiscard]] bool IsValidIndex(int32_t i) const { return i >= 0 && i < Count(); }

    [[nodiscard]] T* Element(int32_t i) const
    {
        const auto base = *reinterpret_cast<const uintptr_t*>(m_pField + 8);
        return reinterpret_cast<T*>(base + static_cast<uintptr_t>(static_cast<uint32_t>(i)) * Stride());
    }

    [[nodiscard]] T* operator[](int32_t i) const { return Element(i); }

private:
    // Element stride == the element's schema class size (e.g. 408). Cached per T.
    int32_t Stride() const
    {
        static const int32_t s_stride = UtlVectorEmbeddedNetworkVarSize(m_pszClassName);
        return s_stride;
    }

    uintptr_t   m_pField;
    const char* m_pszClassName;
};

#endif
