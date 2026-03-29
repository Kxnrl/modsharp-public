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

#include "gamedata.h"
#include "global.h"

#include "cstrike/type/KeyValues3.h"
#include "cstrike/type/matrix3x4.h"

KeyValues3::KeyValues3(KV3TypeEx_t type, KV3SubType_t subtype)
{
    using fn         = void (*)(KeyValues3*, KV3TypeEx_t, KV3SubType_t);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::KeyValues3");

    func(this, type, subtype);
}

KeyValues3::~KeyValues3()
{
    using fn         = void (*)(KeyValues3*, bool bClearingContext);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::~KeyValues3");

    func(this, false);
}

std::string KeyValues3::ToString()
{
    switch (GetType())
    {
    case KV3_TYPE_NULL:
        return "<KV3_TYPE_NULL>";
    case KV3_TYPE_BOOL:
        return GetBool() ? "true" : "false";
    case KV3_TYPE_INT:
        return std::to_string(GetInt64());
    case KV3_TYPE_UINT:
        return std::to_string(GetUInt64());
    case KV3_TYPE_DOUBLE:
        return std::to_string(GetDouble());
    case KV3_TYPE_STRING:
        return GetString();
    case KV3_TYPE_BINARY_BLOB:
        return "<KV3_TYPE_BINARY_BLOB>";
    case KV3_TYPE_ARRAY:
        return "<KV3_TYPE_ARRAY>";
    case KV3_TYPE_TABLE:
        return "<KV3_TYPE_TABLE>";
    default:
        return "<KV3_TYPE_INVALID>";
    }
}

const char* KeyValues3::GetString(const char* default_string)
{
    using fn         = const char* (*)(KeyValues3*, const char*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::GetString");

    return func(this, default_string);
}

void KeyValues3::SetString(const char* str, KV3SubType_t subtype)
{
    using fn         = void (*)(KeyValues3*, const char*, KV3SubType_t);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::SetString");

    func(this, str, subtype);
}

void KeyValues3::SetStringExternal(const char* str, KV3SubType_t subtype)
{
    if (strlen(str) < sizeof(m_szStringShort))
    {
        PrepareForType(KV3_TYPEEX_STRING_SHORT, subtype);
        strncpy(m_szStringShort, str, sizeof(m_szStringShort));
    }
    else
    {
        PrepareForType(KV3_TYPEEX_STRING_EXTERN, subtype);
        m_pString = str;
    }
}

void KeyValues3::SetBool(bool value)
{
    PrepareForType(KV3_TYPEEX_BOOL, KV3_SUBTYPE_BOOL8);

    *reinterpret_cast<bool*>(reinterpret_cast<uintptr_t>(this) + 8) = value;
}

void KeyValues3::SetInt8(int8_t value)
{
    PrepareForType(KV3_TYPEEX_INT, KV3_SUBTYPE_INT8);
    *reinterpret_cast<int64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = static_cast<int64_t>(value);
}

void KeyValues3::SetShort(int16_t value)
{
    PrepareForType(KV3_TYPEEX_INT, KV3_SUBTYPE_INT16);
    *reinterpret_cast<int64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = static_cast<int64_t>(value);
}

void KeyValues3::SetFloat(float value)
{
    PrepareForType(KV3_TYPEEX_DOUBLE, KV3_SUBTYPE_FLOAT32);

    *reinterpret_cast<double*>(reinterpret_cast<uintptr_t>(this) + 8) = static_cast<double>(value);
}

void KeyValues3::SetInt(int value)
{
    PrepareForType(KV3_TYPEEX_INT, KV3_SUBTYPE_INT32);
    *reinterpret_cast<int64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = static_cast<int64_t>(value);
}

void KeyValues3::SetInt64(int64_t value)
{
    PrepareForType(KV3_TYPEEX_INT, KV3_SUBTYPE_INT64);
    *reinterpret_cast<int64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = value;
}

void KeyValues3::SetUInt8(uint8_t value)
{
    PrepareForType(KV3_TYPEEX_UINT, KV3_SUBTYPE_UINT8);
    *reinterpret_cast<uint64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = static_cast<uint64_t>(value);
}

void KeyValues3::SetUShort(uint16_t value)
{
    PrepareForType(KV3_TYPEEX_UINT, KV3_SUBTYPE_UINT16);
    *reinterpret_cast<uint64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = static_cast<uint64_t>(value);
}

void KeyValues3::SetUInt(uint32_t value)
{
    PrepareForType(KV3_TYPEEX_UINT, KV3_SUBTYPE_UINT32);
    *reinterpret_cast<uint64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = static_cast<uint64_t>(value);
}

void KeyValues3::SetUInt64(uint64_t value)
{
    PrepareForType(KV3_TYPEEX_UINT, KV3_SUBTYPE_UINT64);
    *reinterpret_cast<uint64_t*>(reinterpret_cast<uintptr_t>(this) + 8) = value;
}

void KeyValues3::SetDouble(double value)
{
    PrepareForType(KV3_TYPEEX_DOUBLE, KV3_SUBTYPE_FLOAT64);
    *reinterpret_cast<double*>(reinterpret_cast<uintptr_t>(this) + 8) = value;
}

void* KeyValues3::GetPointer()
{
    if (GetType() == KV3_TYPE_UINT && GetSubType() == KV3_SUBTYPE_POINTER)
    {
        return reinterpret_cast<void*>(m_UInt);
    }
    return nullptr;
}

void KeyValues3::SetPointer(void* value)
{
    PrepareForType(KV3_TYPEEX_UINT, KV3_SUBTYPE_POINTER);
    *reinterpret_cast<uintptr_t*>(reinterpret_cast<uintptr_t>(this) + 8) = reinterpret_cast<uintptr_t>(value);
}

Color32 KeyValues3::GetColor()
{
    int32_t color[4] = {};
    if (ReadArrayInt32(4, color))
    {
        return {color[0], color[1], color[2], color[3]};
    }

    if (ReadArrayInt32(3, color))
    {
        return {color[0], color[1], color[2], 255};
    }

    return {0, 0, 0, 0};
}

void KeyValues3::SetColor(const Color32& value)
{
    int color[4] = {value.r(), value.g(), value.b(), value.a()};

    if (value.a() == 255)
        AllocArray_Int32_Internal(3, color, KV3_ARRAY_ALLOC_NORMAL, KV3_TYPEEX_ARRAY_UINT8_SHORT, KV3_TYPEEX_INVALID, KV3_SUBTYPE_COLOR32, KV3_TYPEEX_UINT, KV3_SUBTYPE_UINT8);
    else
        AllocArray_Int32_Internal(4, color, KV3_ARRAY_ALLOC_NORMAL, KV3_TYPEEX_ARRAY_UINT8_SHORT, KV3_TYPEEX_INVALID, KV3_SUBTYPE_COLOR32, KV3_TYPEEX_UINT, KV3_SUBTYPE_UINT8);
}

Vector KeyValues3::GetVector()
{
    return GetVecBaseObj<Vector>(3);
}

void KeyValues3::SetVector(const Vector& value)
{
    AllocArray_Float32_Internal(3, const_cast<float*>(&value.x), KV3_ARRAY_ALLOC_NORMAL, KV3_TYPEEX_INVALID, KV3_TYPEEX_ARRAY_FLOAT32, KV3_SUBTYPE_VECTOR, KV3_TYPEEX_DOUBLE, KV3_SUBTYPE_FLOAT32);
}

QAngle KeyValues3::GetQAngle()
{
    return GetVecBaseObj<QAngle>(3);
}

void KeyValues3::SetQAngle(const QAngle& value)
{
    AllocArray_Float32_Internal(3, const_cast<float*>(&value.x), KV3_ARRAY_ALLOC_NORMAL, KV3_TYPEEX_INVALID, KV3_TYPEEX_ARRAY_FLOAT32, KV3_SUBTYPE_QANGLE, KV3_TYPEEX_DOUBLE, KV3_SUBTYPE_FLOAT32);
}

matrix3x4_t KeyValues3::GetMatrix3x4()
{
    return GetVecBaseObj<matrix3x4_t>(3 * 4);
}

void KeyValues3::SetMatrix3x4(const matrix3x4_t& value)
{
    AllocArray_Float32_Internal(3 * 4, const_cast<float*>(&value[0][0]), KV3_ARRAY_ALLOC_NORMAL, KV3_TYPEEX_INVALID, KV3_TYPEEX_ARRAY_FLOAT32, KV3_SUBTYPE_MATRIX3X4, KV3_TYPEEX_DOUBLE, KV3_SUBTYPE_FLOAT32);
}

int KeyValues3::GetArrayElementCount()
{
    using fn         = int (*)(KeyValues3*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::GetArrayElementCount");
    return func(this);
}

KeyValues3* KeyValues3::GetArrayElement(int index)
{
    using fn         = KeyValues3* (*)(KeyValues3*, int);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::GetArrayElement");

    return func(this, index);
}

KeyValues3* KeyValues3::AddArrayElementToTail()
{
    using fn         = KeyValues3* (*)(KeyValues3*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::GetArrayElement");

    return func(this);
}

void KeyValues3::RemoveArrayElements(int element, int count)
{
    using fn = void (*)(KeyValues3*, int, int);

    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::RemoveArrayElements");

    return func(this, element, count);
}

void KeyValues3::RemoveArrayElement(int element)
{
    RemoveArrayElements(element, 1);
}

bool KeyValues3::ReadArrayFloat32(int size, float* data)
{
    using fn         = bool (*)(KeyValues3*, int, float*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::ReadArrayFloat32");

    return func(this, size, data);
}

bool KeyValues3::ReadArrayInt32(int size, int* data)
{
    using fn         = bool (*)(KeyValues3*, int, int*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::ReadArrayInt32");

    return func(this, size, data);
}

void KeyValues3::AllocArray_Int32_Internal(int size, int* data, KV3ArrayAllocType_t alloc_type, KV3TypeEx_t type_short, KV3TypeEx_t type_ptr, KV3SubType_t subtype, KV3TypeEx_t type_elem, KV3SubType_t subtype_elem)
{
    using fn = void (*)(KeyValues3*, int, int*, KV3ArrayAllocType_t, KV3TypeEx_t, KV3TypeEx_t, KV3SubType_t, KV3TypeEx_t, KV3SubType_t);

    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::AllocArray_Int32_Internal");

    func(this, size, data, alloc_type, type_short, type_ptr, subtype, type_elem, subtype_elem);
}

void KeyValues3::AllocArray_Float32_Internal(int size, float* data, KV3ArrayAllocType_t alloc_type, KV3TypeEx_t type_short, KV3TypeEx_t type_ptr, KV3SubType_t subtype, KV3TypeEx_t type_elem, KV3SubType_t subtype_elem)
{
    using fn = void (*)(KeyValues3*, int, float*, KV3ArrayAllocType_t, KV3TypeEx_t, KV3TypeEx_t, KV3SubType_t, KV3TypeEx_t, KV3SubType_t);

    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::AllocArray_Float32_Internal");

    func(this, size, data, alloc_type, type_short, type_ptr, subtype, type_elem, subtype_elem);
}

KeyValues3* KeyValues3::FindMember(const char* name)
{
    using fn         = KeyValues3* (*)(KeyValues3*, const CHashKey&, int*, void*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::FindMember");

    CHashKey member_name(name);
    int      unk;
    return func(this, member_name, &unk, nullptr);
}

KeyValues3* KeyValues3::GetMember(int index)
{
    using fn = KeyValues3* (*)(KeyValues3*, int);

    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::GetMember");

    return func(this, index);
}

KeyValues3* KeyValues3::FindOrCreateMember(const CHashKey& name, bool* pCreated)
{
    using fn         = KeyValues3* (*)(KeyValues3*, const CHashKey&, bool*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::FindOrCreateMember");

    return func(this, name, pCreated);
}

int KeyValues3::GetMemberCount()
{
    using fn         = int (*)(KeyValues3*);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::GetMemberCount");

    return func(this);
}

const char* KeyValues3::GetMemberName(int index)
{
    using fn = const char* (*)(KeyValues3*, int);

    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::GetMemberName");
    return func(this, index);
}

bool KeyValues3::RemoveMember(const CHashKey& name)
{
    using fn = bool (*)(KeyValues3*, const CHashKey&);

    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::RemoveMember_MemberName");
    return func(this, name);
}

void KeyValues3::PrepareForType(KV3TypeEx_t type, KV3SubType_t subtype)
{
    using fn         = void (*)(KeyValues3*, KV3TypeEx_t, KV3SubType_t, int a4, uint64_t a5, int a6, uint8_t a7);
    static auto func = g_pGameData->GetAddress<fn>("KeyValues3::PrepareForType");

    func(this, type, subtype, 0, 0, 0, 0);
}