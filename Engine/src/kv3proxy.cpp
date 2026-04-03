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

#include "kv3proxy.h"
#include "address.h"
#include "gamedata.h"
#include "global.h"
#include "strtool.h"

#include "cstrike/interface/IFileSystem.h"
#include "cstrike/type/CUtlBuffer.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/KeyValues3.h"
#include "cstrike/type/QAngle.h"
#include "cstrike/type/ResourceSystem.h"
#include "cstrike/type/Vector.h"
#include "cstrike/type/matrix3x4.h"

#include <vector>

static CSharpKeyValues3Helper s_CSharpKeyValues3Helper;
CSharpKeyValues3Helper*       g_pKeyValues3Helper = &s_CSharpKeyValues3Helper;

#define IMPL_KV3_GETTER(ret, type)                                          \
    ret CSharpKeyValues3Helper::Get##type(KeyValues3* kv, ret defaultValue) \
    {                                                                       \
        return kv->Get##type();                                             \
    }

#define IMPL_KV3_SETTER(ret, type)                                    \
    void CSharpKeyValues3Helper::Set##type(KeyValues3* kv, ret value) \
    {                                                                 \
        kv->Set##type(value);                                         \
    }

KeyValues3* CSharpKeyValues3Helper::CreateKeyValues3(uint8_t type, uint8_t subType) const
{
    return new KeyValues3(static_cast<KV3TypeEx_t>(type), static_cast<KV3SubType_t>(subType));
}
void CSharpKeyValues3Helper::DestroyKeyValues3(KeyValues3* kv) const
{
    delete kv;
}

/////////////////////////////////////////////////////////////////////////

bool CSharpKeyValues3Helper::LoadFromFile(KeyValues3* kv, char* error, const char* filename, const char* pathId) const
{
    CUtlString buf;
    const auto ret = LoadKV3FromFile(kv, &buf, filename, pathId, g_KV3Format_Generic, KV3_LOAD_TEXT_NONE);
    memset(error, 0, 256);
    memcpy(error, buf.Get(), std::min(buf.Length(), 255));
    return ret;
}

bool CSharpKeyValues3Helper::LoadFromCompiledFile(KeyValues3* kv, char* error, const char* filename, const char* pathId) const
{
#ifdef OPTIMIZE_LOAD_FROM_COMPILED_FILE
    CUtlBuffer buffer{0, 0, CUtlBuffer::TEXT_BUFFER};

    if (!g_pFullFileSystem->ReadFile(filename, pathId, buffer))
    {
        StrCopy(error, 256, "Invalid file");
        return false;
    }

    CResourceBlockInfo output{};
    if (!Resource_FindBlockInfo(static_cast<const ResourceFileHeader_t*>(buffer.Base()), g_ResourceBlockId_Data, output))
    {
        StrCopy(error, 256, "Failed to load resource");
        return false;
    }

    return LoadFromBuffer(kv, error, output.m_pBlockData, output.m_nSize, "");
#else

    const auto handle = g_pFullFileSystem->Open(filename, "rb", pathId);
    if (handle == nullptr)
    {
        StrCopy(error, 256, "Invalid file");
        return false;
    }

    const auto size = g_pFullFileSystem->Size(handle);

    std::vector<uint8_t> buffer{};
    buffer.resize(size);
    g_pFullFileSystem->Read(buffer.data(), size, handle);
    g_pFullFileSystem->Close(handle);

    CResourceBlockInfo output{};
    if (!Resource_FindBlockInfo(reinterpret_cast<const ResourceFileHeader_t*>(buffer.data()), g_ResourceBlockId_Data, output))
    {
        StrCopy(error, 256, "Failed to load resource");
        return false;
    }

    return LoadFromBuffer(kv, error, output.m_pBlockData, output.m_nSize, "");
#endif
}

bool CSharpKeyValues3Helper::LoadFromBuffer(KeyValues3* kv, char* error, const char* input, int bufferSize, const char* kvName) const
{
    CUtlBuffer buffer(input, bufferSize, CUtlBuffer::READ_ONLY);
    CUtlString buf;

    const auto ret = LoadKV3(kv, &buf, &buffer, g_KV3Format_Generic, kvName, KV3_LOAD_TEXT_NONE);
    memset(error, 0, 256);
    StrCopy(error, 256, buf.Get());
    return ret;
}

/////////////////////////////////////////////////////////////////////////

uint8_t CSharpKeyValues3Helper::GetType(KeyValues3* kv)
{
    return kv->GetType();
}
uint8_t CSharpKeyValues3Helper::GetTypeEx(KeyValues3* kv)
{
    return kv->GetTypeEx();
}
uint8_t CSharpKeyValues3Helper::GetSubType(KeyValues3* kv)
{
    return kv->GetSubType();
}

/////////////////////////////////////////////////////////////////////////

int32_t CSharpKeyValues3Helper::GetArrayElementCount(KeyValues3* kv) const
{
    return kv->GetArrayElementCount();
}
KeyValues3* CSharpKeyValues3Helper::GetArrayElement(KeyValues3* kv, int elem)
{
    return kv->GetArrayElement(elem);
}
KeyValues3* CSharpKeyValues3Helper::AddArrayElementToTail(KeyValues3* kv)
{
    return kv->AddArrayElementToTail();
}
void CSharpKeyValues3Helper::RemoveArrayElement(KeyValues3* kv, int elem) const
{
    return kv->RemoveArrayElement(elem);
}

/////////////////////////////////////////////////////////////////////////

int32_t CSharpKeyValues3Helper::GetMemberCount(KeyValues3* kv)
{
    return kv->GetMemberCount();
}
KeyValues3* CSharpKeyValues3Helper::GetMember(KeyValues3* kv, int32_t id)
{
    return kv->GetMember(id);
}
const char* CSharpKeyValues3Helper::GetMemberName(KeyValues3* kv, int32_t id)
{
    return kv->GetMemberName(id);
}

KeyValues3* CSharpKeyValues3Helper::FindMember(KeyValues3* kv, const char* name)
{
    return kv->FindMember(name);
}
KeyValues3* CSharpKeyValues3Helper::FindOrCreateMember(KeyValues3* kv, const char* name, bool* create)
{
    return kv->FindOrCreateMember(name, create);
}
bool CSharpKeyValues3Helper::RemoveMember(KeyValues3* kv, const char* name)
{
    return kv->RemoveMember(name);
}

/////////////////////////////////////////////////////////////////////////

IMPL_KV3_GETTER(bool, Bool);
IMPL_KV3_GETTER(int8_t, Int8);
IMPL_KV3_GETTER(int16_t, Short);
IMPL_KV3_GETTER(int32_t, Int);
IMPL_KV3_GETTER(int64_t, Int64);
IMPL_KV3_GETTER(uint8_t, UInt8);
IMPL_KV3_GETTER(uint16_t, UShort);
IMPL_KV3_GETTER(uint32_t, UInt);
IMPL_KV3_GETTER(uint64_t, UInt64);
IMPL_KV3_GETTER(float, Float);
IMPL_KV3_GETTER(double, Double);
IMPL_KV3_GETTER(const char*, String);

void CSharpKeyValues3Helper::GetColor(KeyValues3* kv, Color32* color)
{
    const auto v = kv->GetColor();
    color->SetColor(v.r(), v.g(), v.b(), v.a());
}

void CSharpKeyValues3Helper::GetVector(KeyValues3* kv, Vector* vector)
{
    const auto v = kv->GetVector();
    vector->Init(v.x, v.y, v.z);
}

void CSharpKeyValues3Helper::GetQAngle(KeyValues3* kv, Vector* vector)
{
    const auto v = kv->GetQAngle();
    vector->Init(v.x, v.y, v.z);
}

void CSharpKeyValues3Helper::GetMatrix(KeyValues3* kv, matrix3x4_t* matrix)
{
    const auto v = kv->GetMatrix3x4();
    *matrix      = v;
}

IMPL_KV3_SETTER(bool, Bool);
IMPL_KV3_SETTER(int8_t, Int8);
IMPL_KV3_SETTER(int16_t, Short);
IMPL_KV3_SETTER(int32_t, Int);
IMPL_KV3_SETTER(int64_t, Int64);
IMPL_KV3_SETTER(uint8_t, UInt8);
IMPL_KV3_SETTER(uint16_t, UShort);
IMPL_KV3_SETTER(uint32_t, UInt);
IMPL_KV3_SETTER(uint64_t, UInt64);
IMPL_KV3_SETTER(float, Float);
IMPL_KV3_SETTER(double, Double);
IMPL_KV3_SETTER(const char*, String);

void CSharpKeyValues3Helper::SetColor(KeyValues3* kv, Color32* color) const
{
    kv->SetColor(*color);
}

void CSharpKeyValues3Helper::SetVector(KeyValues3* kv, Vector* vector) const
{
    kv->SetVector(*vector);
}

void CSharpKeyValues3Helper::SetQAngle(KeyValues3* kv, Vector* vector) const
{
    kv->SetQAngle(QAngle(vector->x, vector->y, vector->z));
}

void CSharpKeyValues3Helper::SetMatrix(KeyValues3* kv, matrix3x4_t* matrix) const
{
    kv->SetMatrix3x4(*matrix);
}