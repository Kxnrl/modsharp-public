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

#ifndef CSTRIKE_INTERFACE_SCHEMA_H
#define CSTRIKE_INTERFACE_SCHEMA_H

#include <cstddef>
#include <cstdint>

enum class FieldType_t : uint8_t;
struct DataMap_t;
class CSchemaSystemTypeScope;
struct SchemaClassInfoData_t;

struct CSchemaNetworkValue
{
    union {
        const char* m_sz_value;
        int         m_n_value;
        float       m_f_value;
        uintptr_t   m_p_value;
    };
};

struct SchemaMetadataEntryData_t
{
    const char*          m_name;
    CSchemaNetworkValue* m_value;
};

// taken from https://github.com/alliedmodders/hl2sdk/blob/cs2/public/schemasystem/schematypes.h
enum SchemaTypeCategory_t : uint8_t
{
    SCHEMA_TYPE_BUILTIN = 0,
    SCHEMA_TYPE_POINTER,
    SCHEMA_TYPE_BITFIELD,
    SCHEMA_TYPE_FIXED_ARRAY,
    SCHEMA_TYPE_ATOMIC,
    SCHEMA_TYPE_DECLARED_CLASS,
    SCHEMA_TYPE_DECLARED_ENUM,
    SCHEMA_TYPE_INVALID,
};

struct SchemaType_t
{
private:
    void* vtable;

public:
    const char*             m_pszTypeName;
    CSchemaSystemTypeScope* m_pTypeScope;
    SchemaTypeCategory_t    m_eTypeCategory;

    SchemaType_t() = delete;
};

struct SchemaClassFieldData_t
{
    const char*                m_pszName;
    SchemaType_t*              m_pType;
    int32_t                    m_nSingleInheritanceOffset;
    int32_t                    m_nMetadataCount;
    SchemaMetadataEntryData_t* m_pMetadata;
};

struct SchemaBaseClassInfoData_t
{
    uint32_t               m_nOffset;
    SchemaClassInfoData_t* m_pClass;
};

struct TypeDescription_t
{
    FieldType_t    fieldType;
    const char*    fieldName;
    int            fieldOffset;
    unsigned short fieldSize;
    int            flags;
    const char*    externalName;
    union {
        DataMap_t*  td;
        const char* enumName;
    };
    int   fieldSizeInBytes;
    float fieldTolerance;
};

static_assert(sizeof(TypeDescription_t) == 0x38);
static_assert(offsetof(TypeDescription_t, fieldName) == 0x08);
static_assert(offsetof(TypeDescription_t, fieldOffset) == 0x10);
static_assert(offsetof(TypeDescription_t, externalName) == 0x20);
static_assert(offsetof(TypeDescription_t, td) == 0x28);
static_assert(offsetof(TypeDescription_t, fieldSizeInBytes) == 0x30);

struct DataMap_t
{
    TypeDescription_t* dataDesc;
    int                dataNumFields;
    const char*        dataClassName;
    DataMap_t*         baseMap;
};

static_assert(sizeof(DataMap_t) == 0x20);
static_assert(offsetof(DataMap_t, baseMap) == 0x18);

struct SchemaClassInfoData_t
{
    const char* GetName() const
    {
        return m_pszName;
    }

    int16_t GetFieldsSize() const
    {
        return m_nNumFields;
    }

    SchemaClassFieldData_t* GetFields() const
    {
        return m_pFields;
    }

    SchemaClassInfoData_t* GetParent() const
    {
        if (!m_BaseClasses)
            return nullptr;

        return m_BaseClasses->m_pClass;
    }

    int8_t GetBaseClassSize() const
    {
        return m_nNumBaseClasses;
    }

    SchemaBaseClassInfoData_t* GetBaseClasses() const
    {
        return m_BaseClasses;
    }

    int16_t GetMetadataSize() const
    {
        return m_nStaticMetadataCount;
    }

    SchemaMetadataEntryData_t* GetStaticMetadata() const
    {
        return m_pStaticMetadata;
    }

    DataMap_t* GetDataMap() const
    {
        return m_pDataMap;
    }

    SchemaClassInfoData_t() = delete;

private:
    SchemaClassInfoData_t*     m_pClassInfo;
    const char*                m_pszName;
    const char*                m_pszModule;
    const char*                m_pCPPName;
    int32_t                    m_nSize;
    int16_t                    m_nNumFields;
    int16_t                    m_nStaticMetadataCount;
    int8_t                     m_nAlignOf;
    int8_t                     m_nNumBaseClasses;
    int16_t                    m_nMultipleInheritanceDepth;
    int16_t                    m_nSingleInheritanceDepth;
    SchemaClassFieldData_t*    m_pFields;
    SchemaBaseClassInfoData_t* m_BaseClasses;
    DataMap_t*                 m_pDataMap;
    SchemaMetadataEntryData_t* m_pStaticMetadata;
};

class CSchemaSystemTypeScope
{
public:
    SchemaClassInfoData_t* FindDeclaredClass(const char* pClass);
};

class ISchemaSystem
{
public:
    CSchemaSystemTypeScope* GetGlobalTypeScope();
    CSchemaSystemTypeScope* FindTypeScopeForModule(const char* module);
};

#endif
