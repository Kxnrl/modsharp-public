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

#ifndef CSTRIKE_ROOT_SCHEMA_H
#define CSTRIKE_ROOT_SCHEMA_H

#include "address.h"
#include "murmurhash.h"

#define SCHEMA_FIELD_MANUAL(class, field, type)              \
    [[nodiscard]] type field() const                         \
    {                                                        \
        static auto offset = GetFieldOffset(#class, #field); \
        return GetFieldValue<type>(offset);                  \
    }                                                        \
    void field(type value)                                   \
    {                                                        \
        static auto offset = GetFieldOffset(#class, #field); \
        SetFieldValue(offset, value);                        \
        SetStateChanged(this, offset);                       \
    }

#define SCHEMA_SETUP_FIELD_DATA(field)                       \
private:                                                     \
    static const auto& _schema_data_##field()                \
    {                                                        \
        static const SchemaFieldData s_data = {              \
            .key    = schemas::GetOffset(ThisClass, #field), \
            .offset = GetChainEntityoffset()};               \
        return s_data;                                       \
    }                                                        \
                                                             \
public:

#define SCHEMA_FIELD_OFFSET(type, field, extra_offset)                                               \
    SCHEMA_SETUP_FIELD_DATA(field)                                                                   \
    type& field()                                                                                    \
    {                                                                                                \
        static auto accestor = SchemaAccessor<type>(_schema_data_##field(), extra_offset, IsStruct); \
        return accestor.Get(this);                                                                   \
    }                                                                                                \
    void field(type val)                                                                             \
    {                                                                                                \
        static auto accestor = SchemaAccessor<type>(_schema_data_##field(), extra_offset, IsStruct); \
        accestor.Set(this, val);                                                                     \
    }

#define SCHEMA_POINTER_FIELD_OFFSET(type, field, extra_offset)                                       \
    SCHEMA_SETUP_FIELD_DATA(field)                                                                   \
    auto field()                                                                                     \
    {                                                                                                \
        static auto accestor = SchemaAccessor<type>(_schema_data_##field(), extra_offset, IsStruct); \
        return accestor.GetPointer(this);                                                            \
    }

#define SCHEMA_FIELD(type, varName) \
    SCHEMA_FIELD_OFFSET(type, varName, 0)

#define SCHEMA_POINTER_FIELD(type, varName) \
    SCHEMA_POINTER_FIELD_OFFSET(type, varName, 0)

#define DECLARE_SCHEMA_TYPE(className, isStruct)                  \
    static constexpr const char* ThisClass = #className;          \
    static constexpr bool        IsStruct  = isStruct;            \
    static auto                  GetChainEntityoffset()           \
    {                                                             \
        static auto offset = schemas::FindChainOffset(ThisClass); \
        return offset;                                            \
    }

#define DECLARE_SCHEMA_CLASS(className) DECLARE_SCHEMA_TYPE(className, false)
#define DECLARE_SCHEMA_STRUCT(className) DECLARE_SCHEMA_TYPE(className, true)

struct SchemaKey
{
    int32_t offset;
    bool    networked;
    bool    valid;
};

namespace schemas
{
int32_t   FindChainOffset(const char* className);
SchemaKey GetOffset(const char* className, const char* memberName);
SchemaKey GetOffset(uint32_t hashKey);
void*     FindDataMapInputFunc(const char* className, const char* fieldName);

} // namespace schemas

class CBaseEntity;
void NetworkStateChanged(uintptr_t chainEntity, uint32_t offset, uint32_t nArrayIndex = -1, uint32_t nPathIndex = -1);
void SetStateChanged(CBaseEntity* pEntity, uint32_t offset, uint32_t nArrayIndex = -1, uint32_t nPathIndex = -1);
void SetStructStateChanged(void* pEntity, uint32_t offset);

// for coreclr
void InitSchemaSystem();

struct SchemaFieldData
{
    const SchemaKey key;
    const int32_t   offset;
};

template <typename T>
class SchemaAccessor
{
public:
    SchemaAccessor() = delete;

    SchemaAccessor(const SchemaFieldData& data, int32_t extraOffset, bool isStruct) :
        m_Data(data),
        m_nOffset(data.key.offset + extraOffset),
        m_bIsStruct(isStruct)
    {
    }

    [[nodiscard]] T& Get(void* instance) const
    {
        return *reinterpret_cast<T*>(reinterpret_cast<uintptr_t>(instance) + m_nOffset);
    }

    [[nodiscard]] T* GetPointer(void* instance) const
    {
        return reinterpret_cast<T*>(reinterpret_cast<uintptr_t>(instance) + m_nOffset);
    }

    void Set(void* instance, T value)
    {
        if (m_Data.key.networked)
        {
            if (m_Data.offset != 0)
            {
                ::NetworkStateChanged(reinterpret_cast<uintptr_t>(instance) + m_Data.offset, m_nOffset, 0xFFFFFFFF);
            }
            else if (!m_bIsStruct)
            {
                ::SetStateChanged(static_cast<CBaseEntity*>(instance), m_nOffset);
            }
        }
        *reinterpret_cast<T*>(reinterpret_cast<uintptr_t>(instance) + m_nOffset) = value;
    }

private:
    const SchemaFieldData m_Data;
    const int32_t         m_nOffset;
    const bool            m_bIsStruct;
};
#endif