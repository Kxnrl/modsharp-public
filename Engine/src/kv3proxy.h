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

#ifndef MS_ROOT_KV3PROXY_H
#define MS_ROOT_KV3PROXY_H

#include <cstdint>

class Color32;
struct matrix3x4_t;
class Vector;
class KeyValues3;

#define DECL_KV3_GETTER(ret, type, defV) \
    virtual ret Get##type(KeyValues3* kv, ret defaultValue = (defV))

#define DECL_KV3_SETTER(ret, type) \
    virtual void Set##type(KeyValues3* kv, ret value)

class CSharpKeyValues3Helper
{
public:
    virtual ~CSharpKeyValues3Helper() = default;

    // construct
    virtual KeyValues3* CreateKeyValues3(uint8_t type = 1, uint8_t subType = 7) const;
    virtual void        DestroyKeyValues3(KeyValues3* kv) const;

    // loader
    virtual bool LoadFromFile(KeyValues3* kv, char* error, const char* filename, const char* pathId) const;
    virtual bool LoadFromCompiledFile(KeyValues3* kv, char* error, const char* filename, const char* pathId, uint32_t resource_block_type) const;
    virtual bool LoadFromBuffer(KeyValues3* kv, char* error, const char* input, int bufferSize, const char* kvName) const;

    virtual uint8_t GetType(KeyValues3* kv);
    virtual uint8_t GetTypeEx(KeyValues3* kv);
    virtual uint8_t GetSubType(KeyValues3* kv);

    virtual int32_t     GetArrayElementCount(KeyValues3* kv) const;
    virtual KeyValues3* GetArrayElement(KeyValues3* kv, int elem);
    virtual KeyValues3* AddArrayElementToTail(KeyValues3* kv);
    virtual void        RemoveArrayElement(KeyValues3* kv, int elem) const;

    virtual int32_t     GetMemberCount(KeyValues3* kv);
    virtual KeyValues3* GetMember(KeyValues3* kv, int32_t id);
    virtual const char* GetMemberName(KeyValues3* kv, int32_t id);
    virtual KeyValues3* FindMember(KeyValues3* kv, const char* name);
    virtual KeyValues3* FindOrCreateMember(KeyValues3* kv, const char* name, bool* create);
    virtual bool        RemoveMember(KeyValues3* kv, const char* name);

    DECL_KV3_GETTER(bool, Bool, false);
    DECL_KV3_GETTER(int8_t, Int8, 0);
    DECL_KV3_GETTER(int16_t, Short, 0);
    DECL_KV3_GETTER(int32_t, Int, 0);
    DECL_KV3_GETTER(int64_t, Int64, 0);
    DECL_KV3_GETTER(uint8_t, UInt8, 0);
    DECL_KV3_GETTER(uint16_t, UShort, 0);
    DECL_KV3_GETTER(uint32_t, UInt, 0);
    DECL_KV3_GETTER(uint64_t, UInt64, 0);
    DECL_KV3_GETTER(float, Float, 0);
    DECL_KV3_GETTER(double, Double, 0);
    DECL_KV3_GETTER(const char*, String, "");

    virtual uint8_t* GetBinaryBlob(KeyValues3* kv);
    virtual size_t   GetBinaryBlobSize(KeyValues3* kv);

    virtual void GetColor(KeyValues3* kv, Color32* color);
    virtual void GetVector(KeyValues3* kv, Vector* vector);
    virtual void GetQAngle(KeyValues3* kv, Vector* vector);
    virtual void GetMatrix(KeyValues3* kv, matrix3x4_t* matrix);

    DECL_KV3_SETTER(bool, Bool);
    DECL_KV3_SETTER(int8_t, Int8);
    DECL_KV3_SETTER(int16_t, Short);
    DECL_KV3_SETTER(int32_t, Int);
    DECL_KV3_SETTER(int64_t, Int64);
    DECL_KV3_SETTER(uint8_t, UInt8);
    DECL_KV3_SETTER(uint16_t, UShort);
    DECL_KV3_SETTER(uint32_t, UInt);
    DECL_KV3_SETTER(uint64_t, UInt64);
    DECL_KV3_SETTER(float, Float);
    DECL_KV3_SETTER(double, Double);
    DECL_KV3_SETTER(const char*, String);

    virtual void SetColor(KeyValues3* kv, Color32* color) const;
    virtual void SetVector(KeyValues3* kv, Vector* vector) const;
    virtual void SetQAngle(KeyValues3* kv, Vector* vector) const;
    virtual void SetMatrix(KeyValues3* kv, matrix3x4_t* matrix) const;
};

#endif