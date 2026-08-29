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

#ifndef CSTRIKE_ROOT_CSTRIKE_H
#define CSTRIKE_ROOT_CSTRIKE_H

#include "definitions.h"

#include "cstrike/schema.h"

class CBaseHandle;
using CStrikeObjPtr = void*;

class CStrikeObject
{
public:
    template <typename T>
    T GetFieldPointer(int64_t offset) const
    {
        return reinterpret_cast<T>(reinterpret_cast<uintptr_t>(this) + offset);
    }

    template <typename T>
    T GetFieldValue(int64_t offset) const
    {
        return *reinterpret_cast<T*>(reinterpret_cast<uintptr_t>(this) + offset);
    }

    template <typename T>
    void SetFieldValue(int64_t offset, T value) const
    {
        auto offsetPtr = reinterpret_cast<T*>(reinterpret_cast<uintptr_t>(this) + offset);
        *offsetPtr     = value;
    }

    static int32_t GetFieldOffset(const char* classname, const char* field)
    {
        return schemas::GetOffset(classname, field).offset;
    }

    // NO CONSTRUCTOR!!!
    CStrikeObject() = delete;
};

class CBaseHandle
{
    friend class CEntityIdentity;

public:
    CBaseHandle() { value = UINT32_MAX; }

    explicit CBaseHandle(EHandle_t value) :
        value(value)
    {}

    [[nodiscard]] bool IsValid() const { return value != UINT32_MAX; }

    [[nodiscard]] EntityIndex_t GetEntryIndex() const
    {
        if (IsValid())
        {
            return static_cast<EntityIndex_t>(m_Parts.m_EntityIndex);
        }

        return INVALID_ENTITY_INDEX;
    }

    [[nodiscard]] EHandle_t ToInt() const
    {
        return value;
    }

    [[nodiscard]] uint32_t GetSerialNum() const
    {
        return m_Parts.m_Serial;
    }

    [[nodiscard]] uint32_t GetPackedValue() const
    {
        if (!IsValid())
            return 0xFFFFFF;

        return (value & 0x7FFF) | (((value >> 15) & 0x3FF) << 14);
    }

    [[nodiscard]] static CBaseHandle FromPackedValue(uint32_t packed)
    {
        if (packed == 0xFFFFFF)
            return {};

        const auto index  = packed & 0x3FFF;
        const auto serial = (packed >> 14) & 0x3FF;
        return CBaseHandle(index | (serial << 15));
    }

    bool operator==(const CBaseHandle& other) const
    {
        return value == other.value;
    }

    bool operator!=(const CBaseHandle& other) const
    {
        return value != other.value;
    }

protected:
    union {
        EHandle_t value;
        struct
        {
            uint32_t m_EntityIndex : 15;
            uint32_t m_Serial : 17;
        } m_Parts;
    };
};

#endif
