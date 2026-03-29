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

#ifndef CSTRIKE_TYPE_CCONVAR_H
#define CSTRIKE_TYPE_CCONVAR_H

#include "cstrike/type/CSplitScreenSlot.h"

#include <cstdint>

enum EConVarType : short
{
    EConVarType_Invalid = -1,
    EConVarType_Bool,
    EConVarType_Int16,
    EConVarType_UInt16,
    EConVarType_Int32,
    EConVarType_UInt32,
    EConVarType_Int64,
    EConVarType_UInt64,
    EConVarType_Float32,
    EConVarType_Float64,
    EConVarType_String,
    EConVarType_Color,
    EConVarType_Vector2,
    EConVarType_Vector3,
    EConVarType_Vector4,
    EConVarType_QAngle,
    EConVarType_MAX
};

class ConVarHandle
{
public:
    ConVarHandle(uint16_t index = 0xFFFF, uint32_t handle = 0) :
        m_convarIndex(index),
        m_handleIndex(handle)
    {}

    bool IsValid() const { return m_convarIndex != 0xFFFF; }
    void Invalidate()
    {
        m_convarIndex = 0xFFFF;
        m_handleIndex = 0x0;
    }

    uint16_t GetConVarIndex() const { return m_convarIndex; }
    uint32_t GetIndex() const { return m_handleIndex; }

private:
    uint16_t m_convarIndex;
    uint32_t m_handleIndex;
};

union CVValue_t {
    template <typename T>
    inline CVValue_t& operator=(const T& other);

    inline operator bool() const { return m_bValue; }
    inline operator int16_t() const { return m_i16Value; }
    inline operator uint16_t() const { return m_u16Value; }
    inline operator int32_t() const { return m_i32Value; }
    inline operator uint32_t() const { return m_u32Value; }
    inline operator int64_t() const { return m_i64Value; }
    inline operator uint64_t() const { return m_u64Value; }
    inline operator float() const { return m_flValue; }
    inline operator double() const { return m_dbValue; }
    inline operator const char*() const { return m_szValue; }

    bool        m_bValue;
    int16_t     m_i16Value;
    uint16_t    m_u16Value;
    int32_t     m_i32Value;
    uint32_t    m_u32Value;
    int64_t     m_i64Value;
    uint64_t    m_u64Value;
    float       m_flValue;
    double      m_dbValue;
    const char* m_szValue;

#ifdef VARIANT_SDK
    Color32  m_clrValue;
    Vector2D m_vec2Value;
    Vector   m_vec3Value;
    Vector4D m_vec4Value;
    QAngle   m_angValue;
#else
    uint8_t m_padding[16];
#endif
};

template <typename T>
constexpr EConVarType GetConVarType()
{
    if constexpr (std::is_same_v<T, bool>)
    {
        return EConVarType_Bool;
    }
    else if constexpr (std::is_same_v<T, int16_t>)
    {
        return EConVarType_Int16;
    }
    else if constexpr (std::is_same_v<T, uint16_t>)
    {
        return EConVarType_UInt16;
    }
    else if constexpr (std::is_same_v<T, int32_t>)
    {
        return EConVarType_Int32;
    }
    else if constexpr (std::is_same_v<T, uint32_t>)
    {
        return EConVarType_UInt32;
    }
    else if constexpr (std::is_same_v<T, int64_t>)
    {
        return EConVarType_Int64;
    }
    else if constexpr (std::is_same_v<T, uint64_t>)
    {
        return EConVarType_UInt64;
    }
    else if constexpr (std::is_same_v<T, float>)
    {
        return EConVarType_Float32;
    }
    else if constexpr (std::is_same_v<T, double>)
    {
        return EConVarType_Float64;
    }
    else if constexpr (std::is_same_v<T, const char*>)
    {
        return EConVarType_String;
    }
    else
    {
        return EConVarType_Invalid;
    }
}

template <typename T>
class CConVarData;

class CConVarBaseData
{
public:
    CConVarBaseData() :
        m_pszName("<undefined>"),
        m_defaultValue(nullptr),
        m_minValue(nullptr),
        m_maxValue(nullptr),
        m_pszHelpString(nullptr),
        m_eVarType(EConVarType_Invalid)
    {
    }

    inline const char* GetName() const { return m_pszName; }
    inline const char* GetDescription() const { return m_pszHelpString; }
    inline EConVarType GetType() const { return m_eVarType; }

    inline bool HasDefaultValue() const { return m_defaultValue != nullptr; }
    inline bool HasMinValue() const { return m_minValue != nullptr; }
    inline bool HasMaxValue() const { return m_maxValue != nullptr; }

    inline bool     IsFlagSet(uint64_t flag) const { return (flag & m_nFlags) != 0; }
    inline void     AddFlags(uint64_t flags) { m_nFlags |= flags; }
    inline void     RemoveFlags(uint64_t flags) { m_nFlags &= ~flags; }
    inline uint64_t GetFlags() const { return m_nFlags; }

    inline CVValue_t* GetMinBound() const { return static_cast<CVValue_t*>(m_minValue); }
    inline CVValue_t* GetMaxBound() const { return static_cast<CVValue_t*>(m_maxValue); }

    template <typename T>
    inline T GetValue()
    {
        return *reinterpret_cast<T*>(reinterpret_cast<intptr_t>(this) + 0x58);
    }

protected:
    const char* m_pszName;
    void*       m_defaultValue;
    void*       m_minValue;
    void*       m_maxValue;
    const char* m_pszHelpString;
    EConVarType m_eVarType;

    int16_t m_Version;

    uint32_t m_iTimesChanged;
    uint64_t m_nFlags;
    uint32_t m_iCallbackIndex;
    int32_t  m_GameInfoFlags;

private:
    uint8_t pad_64[4]{};
    void*   pad;

public:
    int32_t m_UserInfoByteIndex;

#pragma warning(push)
#pragma warning(disable : 4324)
    alignas(CVValue_t) uint8_t m_Values[sizeof(CVValue_t) * 2];
#pragma warning(pop)
};

template <typename T>
class CConVarData : public CConVarBaseData
{
public:
    constexpr CConVarData()
    {
        m_defaultValue = static_cast<void*>(new T());
        m_minValue     = static_cast<void*>(new T());
        m_maxValue     = static_cast<void*>(new T());
        m_eVarType     = GetConVarType<T>();
    }

    ~CConVarData()
    {
        delete static_cast<T*>(m_defaultValue);
        delete static_cast<T*>(m_minValue);
        delete static_cast<T*>(m_maxValue);
    }

protected:
    T m_value[1];
};

inline CConVarBaseData* GetInvalidConVar(EConVarType type)
{
    switch (type)
    {
    case EConVarType_Bool: {
        static CConVarData<bool> invalid_data;
        return &invalid_data;
    }
    case EConVarType_Int16: {
        static CConVarData<int16_t> invalid_data;
        return &invalid_data;
    }
    case EConVarType_UInt16: {
        static CConVarData<uint16_t> invalid_data;
        return &invalid_data;
    }
    case EConVarType_Int32: {
        static CConVarData<int32_t> invalid_data;
        return &invalid_data;
    }
    case EConVarType_UInt32: {
        static CConVarData<uint32_t> invalid_data;
        return &invalid_data;
    }
    case EConVarType_Int64: {
        static CConVarData<int64_t> invalid_data;
        return &invalid_data;
    }
    case EConVarType_UInt64: {
        static CConVarData<uint64_t> invalid_data;
        return &invalid_data;
    }
    case EConVarType_Float32: {
        static CConVarData<float> invalid_data;
        return &invalid_data;
    }
    case EConVarType_Float64: {
        static CConVarData<double> invalid_data;
        return &invalid_data;
    }
    case EConVarType_String: {
        static CConVarData<const char*> invalid_data;
        return &invalid_data;
    }
    default: {
        return nullptr;
    }
    }
}

class BaseConVar
{
public:
    ConVarHandle     m_Handle;
    CConVarBaseData* m_ConVarData;
};

using FnGenericChangeCallback_t         = void (*)(BaseConVar* ref, CSplitScreenSlot nSlot, CVValue_t* pNewValue, CVValue_t* pOldValue);
using FnGenericChangeCallbackProvider_t = void (*)(BaseConVar* ref, CSplitScreenSlot nSlot, const CVValue_t* pNewValue, const CVValue_t* pOldValue, void* __unk01, FnGenericChangeCallback_t cb);
#endif
