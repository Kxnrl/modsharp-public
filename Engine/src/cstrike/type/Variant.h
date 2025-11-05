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

#ifndef CSTRIKE_TYPE_VARIANT_H
#define CSTRIKE_TYPE_VARIANT_H

#include "cstrike/consts.h"

enum class FieldType_t : uint8_t
{
    FIELD_VOID                                          = 0x0,
    FIELD_FLOAT32                                       = 0x1,
    FIELD_STRING                                        = 0x2,
    FIELD_VECTOR                                        = 0x3,
    FIELD_QUATERNION                                    = 0x4,
    FIELD_INT32                                         = 0x5,
    FIELD_BOOLEAN                                       = 0x6,
    FIELD_INT16                                         = 0x7,
    FIELD_CHARACTER                                     = 0x8,
    FIELD_COLOR32                                       = 0x9,
    FIELD_EMBEDDED                                      = 0xa,
    FIELD_CUSTOM                                        = 0xb,
    FIELD_CLASSPTR                                      = 0xc,
    FIELD_EHANDLE                                       = 0xd,
    FIELD_POSITION_VECTOR                               = 0xe,
    FIELD_TIME                                          = 0xf,
    FIELD_TICK                                          = 0x10,
    FIELD_SOUNDNAME                                     = 0x11,
    FIELD_INPUT                                         = 0x12,
    FIELD_FUNCTION                                      = 0x13,
    FIELD_VMATRIX                                       = 0x14,
    FIELD_VMATRIX_WORLDSPACE                            = 0x15,
    FIELD_MATRIX3X4_WORLDSPACE                          = 0x16,
    FIELD_INTERVAL                                      = 0x17,
    FIELD_UNUSED                                        = 0x18,
    FIELD_VECTOR2D                                      = 0x19,
    FIELD_INT64                                         = 0x1a,
    FIELD_VECTOR4D                                      = 0x1b,
    FIELD_RESOURCE                                      = 0x1c,
    FIELD_TYPEUNKNOWN                                   = 0x1d,
    FIELD_CSTRING                                       = 0x1e,
    FIELD_HSCRIPT                                       = 0x1f,
    FIELD_VARIANT                                       = 0x20,
    FIELD_UINT64                                        = 0x21,
    FIELD_FLOAT64                                       = 0x22,
    FIELD_POSITIVEINTEGER_OR_NULL                       = 0x23,
    FIELD_HSCRIPT_NEW_INSTANCE                          = 0x24,
    FIELD_UINT32                                        = 0x25,
    FIELD_UTLSTRINGTOKEN                                = 0x26,
    FIELD_QANGLE                                        = 0x27,
    FIELD_NETWORK_ORIGIN_CELL_QUANTIZED_VECTOR          = 0x28,
    FIELD_HMATERIAL                                     = 0x29,
    FIELD_HMODEL                                        = 0x2a,
    FIELD_NETWORK_QUANTIZED_VECTOR                      = 0x2b,
    FIELD_NETWORK_QUANTIZED_FLOAT                       = 0x2c,
    FIELD_DIRECTION_VECTOR_WORLDSPACE                   = 0x2d,
    FIELD_QANGLE_WORLDSPACE                             = 0x2e,
    FIELD_QUATERNION_WORLDSPACE                         = 0x2f,
    FIELD_HSCRIPT_LIGHTBINDING                          = 0x30,
    FIELD_V8_VALUE                                      = 0x31,
    FIELD_V8_OBJECT                                     = 0x32,
    FIELD_V8_ARRAY                                      = 0x33,
    FIELD_V8_CALLBACK_INFO                              = 0x34,
    FIELD_UTLSTRING                                     = 0x35,
    FIELD_NETWORK_ORIGIN_CELL_QUANTIZED_POSITION_VECTOR = 0x36,
    FIELD_HRENDERTEXTURE                                = 0x37,
    FIELD_HPARTICLESYSTEMDEFINITION                     = 0x38,
    FIELD_UINT8                                         = 0x39,
    FIELD_UINT16                                        = 0x3a,
    FIELD_CTRANSFORM                                    = 0x3b,
    FIELD_CTRANSFORM_WORLDSPACE                         = 0x3c,
    FIELD_HPOSTPROCESSING                               = 0x3d,
    FIELD_MATRIX3X4                                     = 0x3e,
    FIELD_SHIM                                          = 0x3f,
    FIELD_CMOTIONTRANSFORM                              = 0x40,
    FIELD_CMOTIONTRANSFORM_WORLDSPACE                   = 0x41,
    FIELD_ATTACHMENT_HANDLE                             = 0x42,
    FIELD_AMMO_INDEX                                    = 0x43,
    FIELD_CONDITION_ID                                  = 0x44,
    FIELD_AI_SCHEDULE_BITS                              = 0x45,
    FIELD_MODIFIER_HANDLE                               = 0x46,
    FIELD_ROTATION_VECTOR                               = 0x47,
    FIELD_ROTATION_VECTOR_WORLDSPACE                    = 0x48,
    FIELD_HVDATA                                        = 0x49,
    FIELD_SCALE32                                       = 0x4a,
    FIELD_STRING_AND_TOKEN                              = 0x4b,
    FIELD_ENGINE_TIME                                   = 0x4c,
    FIELD_ENGINE_TICK                                   = 0x4d,
    FIELD_WORLD_GROUP_ID                                = 0x4e,
    FIELD_GLOBALSYMBOL                                  = 0x4f,
    FIELD_TYPECOUNT                                     = 0x50,
};

class Variant_t
{
    // casting
    // \x48\x89\x5C\x24\x10\x48\x89\x6C\x24\x18\x48\x89\x7C\x24\x20\x41\x56\x48\x83\xEC\x20\x48\x8B\xDA

    union {
        bool        bVal;
        const char* iszVal;
        int         iVal;
        float       flVal;
        intptr_t    pVal;
    };
    FieldType_t fieldType;
    // this+10 不知道什么, 应该是Flags

public:
    Variant_t() :
        iVal(0), fieldType(FieldType_t::FIELD_VOID) {}

    [[nodiscard]] FieldType_t FieldType() const { return fieldType; }

    void SetBool(bool b)
    {
        bVal      = b;
        fieldType = FieldType_t::FIELD_BOOLEAN;
    }
    void SetString(const char* string)
    {
        iszVal    = string;
        fieldType = FieldType_t::FIELD_CSTRING;
    }
    void SetInt(int val)
    {
        iVal      = val;
        fieldType = FieldType_t::FIELD_INT32;
    }
    void SetFloat(float val)
    {
        flVal     = val;
        fieldType = FieldType_t::FIELD_FLOAT32;
    }
    void SetPointer(void* val)
    {
        pVal      = reinterpret_cast<intptr_t>(val);
        fieldType = FieldType_t::FIELD_CLASSPTR;
    }

    [[nodiscard]] int         Int() const { return (fieldType == FieldType_t::FIELD_INT32) ? iVal : 0; }
    [[nodiscard]] bool        Bool() const { return (fieldType == FieldType_t::FIELD_BOOLEAN) ? bVal : false; }
    [[nodiscard]] float       Float() const { return (fieldType == FieldType_t::FIELD_FLOAT32) ? flVal : 0; }
    [[nodiscard]] const char* CString() const { return (fieldType == FieldType_t::FIELD_CSTRING) ? iszVal : nullptr; }
    [[nodiscard]] const char* String() const { return (fieldType == FieldType_t::FIELD_STRING) ? (pVal ? *reinterpret_cast<char**>(pVal) : nullptr) : nullptr; }
    [[nodiscard]] const char* AutoString() const
    {
        if (fieldType == FieldType_t::FIELD_CSTRING)
            return iszVal;

        if (fieldType == FieldType_t::FIELD_STRING || fieldType == FieldType_t::FIELD_GLOBALSYMBOL)
            return pVal ? *reinterpret_cast<const char**>(pVal) : nullptr;

        return nullptr;
    }

    // 所有内容都当做 char*
    [[nodiscard]] const char* AutoCastString() const
    {
        if (fieldType == FieldType_t::FIELD_CSTRING || fieldType == FieldType_t::FIELD_STRING || fieldType == FieldType_t::FIELD_GLOBALSYMBOL)
            return iszVal && &iszVal[0] ? iszVal : nullptr;

        return nullptr;
    }
};

class HSCRIPT;

enum ScriptFlags_t : uint16_t
{
    None = 0,
    Free = 0x01
};

struct ScriptVariant_t
{
    ScriptVariant_t(int val) :
        m_type(FieldType_t::FIELD_INT32), m_flags(ScriptFlags_t::None) { m_int32 = val; }
    ScriptVariant_t(float val) :
        m_type(FieldType_t::FIELD_FLOAT32), m_flags(ScriptFlags_t::None) { m_float = val; }
    ScriptVariant_t(double val) :
        m_type(FieldType_t::FIELD_FLOAT64), m_flags(ScriptFlags_t::None) { m_double = val; }
    ScriptVariant_t(char val) :
        m_type(FieldType_t::FIELD_CHARACTER), m_flags(ScriptFlags_t::None) { m_char = val; }
    ScriptVariant_t(bool val) :
        m_type(FieldType_t::FIELD_BOOLEAN), m_flags(ScriptFlags_t::None) { m_bool = val; }
    ScriptVariant_t(HSCRIPT* val) :
        m_type(FieldType_t::FIELD_HSCRIPT), m_flags(ScriptFlags_t::None) { m_hScript = val; }
    ScriptVariant_t(int64_t val) :
        m_type(FieldType_t::FIELD_INT64), m_flags(ScriptFlags_t::None) { m_int64 = val; }

    union {
        int           m_int32;
        float         m_float;
        double        m_double;
        const char*   m_pszString;
        const Vector* m_pVector;
        char          m_char;
        bool          m_bool;
        int64_t       m_int64;
        HSCRIPT*      m_hScript;
    };

    FieldType_t   m_type;
    ScriptFlags_t m_flags;
};
static_assert(sizeof(ScriptVariant_t) == 16);

class CBaseEntity;

class InputData_t
{
public:
    CBaseEntity* pActivator;
    CBaseEntity* pCaller;
    Variant_t    value;
    int          nOutputID;
};

enum KeyValuesVariantValueItemType : uint32_t
{
    KeyValuesVariantValueItemType_Bool,
    KeyValuesVariantValueItemType_Int32,
    KeyValuesVariantValueItemType_Float,
    KeyValuesVariantValueItemType_String,
    KeyValuesVariantValueItemType_Pointer,
};

#define KEY_VALUES_VARIANT_VALUE_ITEM_MAX_STRING_LENGTH 256

struct KeyValuesVariantValueItem
{
    KeyValuesVariantValueItemType type;
    union {
        bool    bValue;
        int32_t i32Value;
        float   flValue;
        char    szValue[KEY_VALUES_VARIANT_VALUE_ITEM_MAX_STRING_LENGTH];
        void*   pValue;
    };

    // 如果是string确保 this alive
    Variant_t ToEngineVariant() const
    {
        Variant_t val;

        if (type == KeyValuesVariantValueItemType_Bool)
        {
            val.SetBool(bValue);
        }
        else if (type == KeyValuesVariantValueItemType_Int32)
        {
            val.SetInt(i32Value);
        }
        else if (type == KeyValuesVariantValueItemType_Float)
        {
            val.SetFloat(flValue);
        }
        else if (type == KeyValuesVariantValueItemType_String)
        {
            val.SetString(szValue);
        }
        else if (type == KeyValuesVariantValueItemType_Pointer)
        {
            val.SetPointer(pValue);
        }
        return val;
    }
};
static_assert(sizeof(KeyValuesVariantValueItem) == 264);

struct KeyValuesVariantItem
{
    char                      Key[KEY_VALUES_VARIANT_VALUE_ITEM_MAX_STRING_LENGTH];
    KeyValuesVariantValueItem Value;
};

#endif
