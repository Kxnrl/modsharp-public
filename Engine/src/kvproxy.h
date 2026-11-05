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

#ifndef MS_ROOT_KVPROXY_H
#define MS_ROOT_KVPROXY_H

#include <cstdint>

class KeyValues;

class CSharpKeyValuesHelper
{
public:
    virtual ~CSharpKeyValuesHelper() = default;

    // construct
    virtual KeyValues* CreateKeyValues(const char* name) const;
    virtual void       DestroyKeyValues(KeyValues* kv) const;
    virtual KeyValues* Clone(KeyValues* kv) const;
    virtual void       Clear(KeyValues* kv) const;

    // file
    virtual bool LoadFromFile(KeyValues* kv, const char* filename, const char* pathId = nullptr) const;
    virtual bool SaveToFile(KeyValues* kv, const char* filename, const char* pathId = nullptr, bool allowEmptyString = false) const;

    // text
    virtual bool LoadFromString(KeyValues* kv, const char* pBuffer) const;
    virtual bool SaveToString(KeyValues* kv, char* buffer, int64_t size, int indent = 0, bool sort = false, bool allowEmptyString = false) const;

    // section
    virtual const char* GetSectionName(KeyValues* kv) const;
    virtual void        SetSectionName(KeyValues* kv, const char* name) const;

    // iteration
    virtual KeyValues* GetFirstSubKey(KeyValues* kv) const;
    virtual KeyValues* FindLastSubKey(KeyValues* kv) const;
    virtual KeyValues* GetNextKey(KeyValues* kv) const;
    virtual KeyValues* GetFirstTrueSubKey(KeyValues* kv) const;
    virtual KeyValues* GetNextTrueSubKey(KeyValues* kv) const;

    // key
    virtual KeyValues* FindKey(KeyValues* kv, const char* name, bool bCreate = false) const;
    virtual bool       FindAndDeleteSubKey(KeyValues* kv, const char* name) const;
    virtual KeyValues* AddKey(KeyValues* kv, const char* name) const;

    // value
    virtual int32_t GetDataType(KeyValues* kv, const char* name = nullptr) const;
    virtual bool    IsEmpty(KeyValues* kv, const char* name = nullptr) const;

    virtual int         GetInt(KeyValues* kv, const char* name = nullptr, int defaultValue = 0) const;
    virtual uint64_t    GetUint64(KeyValues* kv, const char* name = nullptr, uint64_t defaultValue = 0) const;
    virtual float       GetFloat(KeyValues* kv, const char* name = nullptr, float defaultValue = 0.0f) const;
    virtual const char* GetString(KeyValues* kv, const char* keyName, const char* defaultValue = "");
    virtual void*       GetPtr(KeyValues* kv, const char* name = nullptr, void* defaultValue = nullptr) const;

    virtual void SetString(KeyValues* kv, const char* name, const char* value) const;
    virtual void SetInt(KeyValues* kv, const char* name, int value) const;
    virtual void SetUint64(KeyValues* kv, const char* name, uint64_t value) const;
    virtual void SetFloat(KeyValues* kv, const char* name, float value) const;
    virtual void SetPtr(KeyValues* kv, const char* name, void* value) const;
    virtual void UsesEscapeSequences(KeyValues* kv, bool state) const;
};

#endif