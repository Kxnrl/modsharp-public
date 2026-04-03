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

#ifndef CSTRIKE_TYPE_KEYVALUES3_H
#define CSTRIKE_TYPE_KEYVALUES3_H

#include "sdkproxy.h"

#include "cstrike/type/CHashKey.h"
#include "cstrike/type/Color32.h"
#include "cstrike/type/QAngle.h"
#include "cstrike/type/Vector.h"
#include "cstrike/type/matrix3x4.h"

#include <cstdint>
#include <string>

class CUtlString;
class CUtlBuffer;
struct KV3ID_t
{
    const char* m_name;
    uint64_t    m_data1;
    uint64_t    m_data2;
};

class KeyValues3;

// encodings
constexpr KV3ID_t g_KV3Encoding_Text       = {.m_name = "text", .m_data1 = 0x41C58A33E21C7F3Cull, .m_data2 = 0xDAA323A6DA77799ull};
constexpr KV3ID_t g_KV3Encoding_Binary     = {.m_name = "binary", .m_data1 = 0x40C1F7D81B860500ull, .m_data2 = 0x14E76782A47582ADull};
constexpr KV3ID_t g_KV3Encoding_BinaryLZ4  = {.m_name = "binary_lz4", .m_data1 = 0x4F5C63A16847348Aull, .m_data2 = 0x19B1D96F805397A1ull};
constexpr KV3ID_t g_KV3Encoding_BinaryZSTD = {.m_name = "binary_zstd", .m_data1 = 0x4305FEF06F620A00ull, .m_data2 = 0x29DBB14623045FA3ull};
constexpr KV3ID_t g_KV3Encoding_BinaryBC   = {.m_name = "binary_bc", .m_data1 = 0x4F6C95BC95791A46ull, .m_data2 = 0xD2DFB7A1BC050BA7ull};
constexpr KV3ID_t g_KV3Encoding_BinaryAuto = {.m_name = "binary_auto", .m_data1 = 0x45836B856EB109E6ull, .m_data2 = 0x8C06046E3A7012A3ull};

// formats
constexpr KV3ID_t g_KV3Format_Generic = {.m_name = "generic", .m_data1 = 0x469806E97412167Cull, .m_data2 = 0xE73790B53EE6F2AFull};

enum KV1TextEscapeBehavior_t
{
    KV1TEXT_ESC_BEHAVIOR_UNK1 = 0,
    KV1TEXT_ESC_BEHAVIOR_UNK2 = 1,
};

enum KV3SaveTextFlags_t
{
    KV3_SAVE_TEXT_NONE   = 0,
    KV3_SAVE_TEXT_TAGGED = (1 << 0), // adds subtype name before value
};

enum KV3Type_t : uint8_t
{
    KV3_TYPE_INVALID = 0,
    KV3_TYPE_NULL,
    KV3_TYPE_BOOL,
    KV3_TYPE_INT,
    KV3_TYPE_UINT,
    KV3_TYPE_DOUBLE,
    KV3_TYPE_STRING,
    KV3_TYPE_BINARY_BLOB,
    KV3_TYPE_ARRAY,
    KV3_TYPE_TABLE,

    KV3_TYPE_COUNT,
};

enum KV3TypeOpt_t : uint8_t
{
    KV3_TYPEOPT_NONE = 0,

    KV3_TYPEOPT_STRING_SHORT,
    KV3_TYPEOPT_STRING_EXTERN,

    KV3_TYPEOPT_BINARY_BLOB_EXTERN,

    KV3_TYPEOPT_ARRAY_FLOAT32,
    KV3_TYPEOPT_ARRAY_FLOAT64,
    KV3_TYPEOPT_ARRAY_INT16,
    KV3_TYPEOPT_ARRAY_INT32,
    KV3_TYPEOPT_ARRAY_UINT8_SHORT,
    KV3_TYPEOPT_ARRAY_INT16_SHORT,
};

enum KV3TypeEx_t : uint8_t
{
    KV3_TYPEEX_INVALID = 0,
    KV3_TYPEEX_NULL,
    KV3_TYPEEX_BOOL,
    KV3_TYPEEX_INT,
    KV3_TYPEEX_UINT,
    KV3_TYPEEX_DOUBLE,

    KV3_TYPEEX_STRING        = KV3_TYPE_STRING,
    KV3_TYPEEX_STRING_SHORT  = (KV3_TYPEEX_STRING | (KV3_TYPEOPT_STRING_SHORT << 4)),
    KV3_TYPEEX_STRING_EXTERN = (KV3_TYPEEX_STRING | (KV3_TYPEOPT_STRING_EXTERN << 4)),

    KV3_TYPEEX_BINARY_BLOB        = KV3_TYPE_BINARY_BLOB,
    KV3_TYPEEX_BINARY_BLOB_EXTERN = (KV3_TYPEEX_BINARY_BLOB | (KV3_TYPEOPT_BINARY_BLOB_EXTERN << 4)),

    KV3_TYPEEX_ARRAY             = KV3_TYPE_ARRAY,
    KV3_TYPEEX_ARRAY_FLOAT32     = (KV3_TYPEEX_ARRAY | (KV3_TYPEOPT_ARRAY_FLOAT32 << 4)),
    KV3_TYPEEX_ARRAY_FLOAT64     = (KV3_TYPEEX_ARRAY | (KV3_TYPEOPT_ARRAY_FLOAT64 << 4)),
    KV3_TYPEEX_ARRAY_INT16       = (KV3_TYPEEX_ARRAY | (KV3_TYPEOPT_ARRAY_INT16 << 4)),
    KV3_TYPEEX_ARRAY_INT32       = (KV3_TYPEEX_ARRAY | (KV3_TYPEOPT_ARRAY_INT32 << 4)),
    KV3_TYPEEX_ARRAY_UINT8_SHORT = (KV3_TYPEEX_ARRAY | (KV3_TYPEOPT_ARRAY_UINT8_SHORT << 4)),
    KV3_TYPEEX_ARRAY_INT16_SHORT = (KV3_TYPEEX_ARRAY | (KV3_TYPEOPT_ARRAY_INT16_SHORT << 4)),

    KV3_TYPEEX_TABLE = KV3_TYPE_TABLE,
};

enum KV3SubType_t : uint8_t
{
    KV3_SUBTYPE_INVALID = 0,

    // string types
    KV3_SUBTYPE_RESOURCE,
    KV3_SUBTYPE_RESOURCE_NAME,
    KV3_SUBTYPE_PANORAMA,
    KV3_SUBTYPE_SOUNDEVENT,

    KV3_SUBTYPE_SUBCLASS,    // table type
    KV3_SUBTYPE_ENTITY_NAME, // string type
    KV3_SUBTYPE_LOCALIZE,
    KV3_SUBTYPE_UNSPECIFIED,
    KV3_SUBTYPE_NULL,
    KV3_SUBTYPE_BINARY_BLOB,
    KV3_SUBTYPE_ARRAY,
    KV3_SUBTYPE_TABLE,
    KV3_SUBTYPE_BOOL8,
    KV3_SUBTYPE_CHAR8,
    KV3_SUBTYPE_UCHAR32,
    KV3_SUBTYPE_INT8,
    KV3_SUBTYPE_UINT8,
    KV3_SUBTYPE_INT16,
    KV3_SUBTYPE_UINT16,
    KV3_SUBTYPE_INT32,
    KV3_SUBTYPE_UINT32,
    KV3_SUBTYPE_INT64,
    KV3_SUBTYPE_UINT64,
    KV3_SUBTYPE_FLOAT32,
    KV3_SUBTYPE_FLOAT64,
    KV3_SUBTYPE_STRING,
    KV3_SUBTYPE_POINTER,
    KV3_SUBTYPE_COLOR32,

    // vector types
    KV3_SUBTYPE_VECTOR,
    KV3_SUBTYPE_VECTOR2D,
    KV3_SUBTYPE_VECTOR4D,
    KV3_SUBTYPE_ROTATION_VECTOR,
    KV3_SUBTYPE_QUATERNION,
    KV3_SUBTYPE_QANGLE,
    KV3_SUBTYPE_MATRIX3X4,
    KV3_SUBTYPE_TRANSFORM,

    KV3_SUBTYPE_STRING_TOKEN,
    KV3_SUBTYPE_EHANDLE,

    KV3_SUBTYPE_COUNT,
};

enum KV3ArrayAllocType_t
{
    KV3_ARRAY_ALLOC_EXTERN      = 0,
    KV3_ARRAY_ALLOC_NORMAL      = 1,
    KV3_ARRAY_ALLOC_EXTERN_FREE = 2,
};

enum KV3ToStringFlags_t
{
    KV3_TO_STRING_NONE                 = 0,
    KV3_TO_STRING_DONT_CLEAR_BUFF      = (1 << 0),
    KV3_TO_STRING_DONT_APPEND_STRINGS  = (1 << 1),
    KV3_TO_STRING_APPEND_ONLY_NUMERICS = (1 << 2),
    KV3_TO_STRING_RETURN_NON_NUMERICS  = (1 << 3),
};

enum KV3MetaDataFlags_t
{
    KV3_METADATA_MULTILINE_STRING     = (1 << 0),
    KV3_METADATA_SINGLE_QUOTED_STRING = (1 << 1),
};

enum KV3LoadTextFlags_t
{
    KV3_LOAD_TEXT_NONE                  = 0,
    KV3_LOAD_TEXT_HAS_OBJECT_REFERANCES = (1 << 0), // "_class" and "__oldschema_object_name" keys
};

MS_GLOBAL_IMPORT bool LoadKV3(KeyValues3* kv, CUtlString* error, CUtlBuffer* input, const KV3ID_t& format, const char* kv_name, uint32_t flags = KV3_LOAD_TEXT_NONE);
MS_GLOBAL_IMPORT bool LoadKV3(KeyValues3* kv, CUtlString* error, const char* input, const KV3ID_t& format, const char* kv_name, uint32_t flags = KV3_LOAD_TEXT_NONE);
MS_GLOBAL_IMPORT bool LoadKV3FromFile(KeyValues3* kv, CUtlString* error, const char* filename, const char* path, const KV3ID_t& format, uint32_t flags = KV3_LOAD_TEXT_NONE);

class KeyValues3
{
public:
    KeyValues3(KV3TypeEx_t type = KV3_TYPEEX_NULL, KV3SubType_t subtype = KV3_SUBTYPE_UNSPECIFIED);
    ~KeyValues3();

    std::string ToString();

    const char* GetString(const char* default_string = "");
    void        SetString(const char* str, KV3SubType_t subtype = KV3_SUBTYPE_STRING);
    void        SetStringExternal(const char* str, KV3SubType_t subtype = KV3_SUBTYPE_STRING);

    bool     GetBool(bool defaultValue = false) { return GetValue<bool>(defaultValue); }
    int8_t   GetChar(int8_t defaultValue = 0) { return GetValue<int8_t>(defaultValue); }
    uint32_t GetUChar32(uint32_t defaultValue = 0) { return GetValue<uint32_t>(defaultValue); }
    int8_t   GetInt8(int8_t defaultValue = 0) { return GetValue<int8_t>(defaultValue); }
    uint8_t  GetUInt8(uint8_t defaultValue = 0) { return GetValue<uint8_t>(defaultValue); }
    int16_t  GetShort(int16_t defaultValue = 0) { return GetValue<int16_t>(defaultValue); }
    uint16_t GetUShort(uint16_t defaultValue = 0) { return GetValue<uint16_t>(defaultValue); }
    int32_t  GetInt(int32_t defaultValue = 0) { return GetValue<int32_t>(defaultValue); }
    uint32_t GetUInt(uint32_t defaultValue = 0) { return GetValue<uint32_t>(defaultValue); }
    int64_t  GetInt64(int64_t defaultValue = 0) { return GetValue<int64_t>(defaultValue); }
    uint64_t GetUInt64(uint64_t defaultValue = 0) { return GetValue<uint64_t>(defaultValue); }
    float    GetFloat(float defaultValue = 0.0f) { return GetValue<float>(defaultValue); }
    double   GetDouble(double defaultValue = 0.0) { return GetValue<double>(defaultValue); }

    void SetBool(bool value);
    void SetInt8(int8_t value);
    void SetShort(int16_t);
    void SetInt(int value);
    void SetInt64(int64_t value);
    void SetUInt8(uint8_t value);
    void SetUShort(uint16_t value);
    void SetUInt(uint32_t value);
    void SetUInt64(uint64_t value);
    void SetFloat(float value);
    void SetDouble(double value);

    void* GetPointer();
    void  SetPointer(void* value);

    Color32 GetColor();
    void    SetColor(const Color32& value);

    Vector GetVector();
    void   SetVector(const Vector& value);

    QAngle GetQAngle();
    void   SetQAngle(const QAngle& value);

    matrix3x4_t GetMatrix3x4();
    void        SetMatrix3x4(const matrix3x4_t& value);

    int         GetArrayElementCount();
    KeyValues3* GetArrayElement(int index);
    KeyValues3* AddArrayElementToTail();
    void        RemoveArrayElements(int element, int count);
    void        RemoveArrayElement(int element);

    KeyValues3* FindMember(const char* name);
    KeyValues3* GetMember(int index);
    KeyValues3* FindOrCreateMember(const CHashKey& name, bool* pCreated = nullptr);
    int         GetMemberCount();
    const char* GetMemberName(int index);
    bool        RemoveMember(const CHashKey& name);

    KV3Type_t    GetType() { return static_cast<KV3Type_t>(GetTypeEx() & 0xF); }
    KV3TypeEx_t  GetTypeEx() { return static_cast<KV3TypeEx_t>(*reinterpret_cast<uintptr_t*>(this) >> 2); }
    KV3SubType_t GetSubType() { return static_cast<KV3SubType_t>(*reinterpret_cast<uintptr_t*>(this) >> 10); }

private:
    bool ReadArrayFloat32(int size, float* data);
    bool ReadArrayInt32(int size, int* data);

    void AllocArray_Int32_Internal(int size, int* data, KV3ArrayAllocType_t alloc_type, KV3TypeEx_t type_short, KV3TypeEx_t type_ptr, KV3SubType_t subtype, KV3TypeEx_t type_elem, KV3SubType_t subtype_elem);
    void AllocArray_Float32_Internal(int size, float* data, KV3ArrayAllocType_t alloc_type, KV3TypeEx_t type_short, KV3TypeEx_t type_ptr, KV3SubType_t subtype, KV3TypeEx_t type_elem, KV3SubType_t subtype_elem);

    template <typename T>
    T GetValue(T defaultValue);

    template <typename T>
    T FromString(T defaultValue);

    template <typename T>
    T GetVecBaseObj(int size)
    {
        T obj;
        ReadArrayFloat32(size, reinterpret_cast<float*>(&obj));
        return obj;
    }

    void PrepareForType(KV3TypeEx_t type, KV3SubType_t subtype);

    uint64_t m_bExternalStorage : 1;
    uint64_t m_bFreeArrayMemory : 1;
    uint64_t m_TypeEx : 8;
    uint64_t m_SubType : 8;
    uint64_t m_nFlags : 8;
    uint64_t m_nClusterElement : 16;
    uint64_t m_nNumArrayElements : 5;
    uint64_t m_nReserved : 17;

    union {
        bool     m_Bool;
        int64_t  m_Int;
        uint64_t m_UInt;
        double   m_Double;

        const char* m_pString;
        char        m_szStringShort[8];

        void* m_pBinaryBlob;

        void*    m_pArray;
        float*   m_f32Array;
        double*  m_f64Array;
        int16_t* m_i16Array;
        int32_t* m_i32Array;
        uint8_t  m_u8ArrayShort[8];
        int16_t  m_i16ArrayShort[4];

        void* m_pTable;

        uint64_t m_nData;
        void*    m_pData;
        char     m_Data[1];
    };
};

template <typename T>
T KeyValues3::GetValue(T defaultValue)
{
    switch (GetType())
    {
    case KV3_TYPE_BOOL:
        return (T)m_Bool;
    case KV3_TYPE_INT:
        return (T)m_Int;
    case KV3_TYPE_UINT:
        return (GetSubType() != KV3_SUBTYPE_POINTER) ? (T)m_UInt : defaultValue;
    case KV3_TYPE_DOUBLE:
        return (T)m_Double;
    case KV3_TYPE_STRING:
        return FromString<T>(defaultValue);
    default:
        return defaultValue;
    }
}

template <typename T>
inline T KeyValues3::FromString(T defaultValue)
{
    return defaultValue;
}

template <>
inline bool KeyValues3::FromString(bool defaultValue)
{
    return V_StringToBool(GetString(), defaultValue);
}

template <>
inline int8_t KeyValues3::FromString(int8_t defaultValue)
{
    return V_StringToInt8(GetString(), defaultValue);
}

template <>
inline uint8_t KeyValues3::FromString(uint8_t defaultValue)
{
    return V_StringToUint8(GetString(), defaultValue);
}

template <>
inline int16_t KeyValues3::FromString(int16_t defaultValue)
{
    return V_StringToInt16(GetString(), defaultValue);
}

template <>
inline uint16_t KeyValues3::FromString(uint16_t defaultValue)
{
    return V_StringToUint16(GetString(), defaultValue);
}

template <>
inline int32_t KeyValues3::FromString(int32_t defaultValue)
{
    return V_StringToInt32(GetString(), defaultValue);
}

template <>
inline uint32_t KeyValues3::FromString(uint32_t defaultValue)
{
    return V_StringToUint32(GetString(), defaultValue);
}

template <>
inline int64_t KeyValues3::FromString(int64_t defaultValue)
{
    return V_StringToInt64(GetString(), defaultValue);
}

template <>
inline uint64_t KeyValues3::FromString(uint64_t defaultValue)
{
    return V_StringToUint64(GetString(), defaultValue);
}

template <>
inline float KeyValues3::FromString(float defaultValue)
{
    return V_StringToFloat32(GetString(), defaultValue);
}

template <>
inline double KeyValues3::FromString(double defaultValue)
{
    return V_StringToFloat64(GetString(), defaultValue);
}
#endif