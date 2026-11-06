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

#include "kvproxy.h"
#include "global.h"

#include "cstrike/type/CUtlBuffer.h"
#include "cstrike/type/KeyValues.h"

CSharpKeyValuesHelper  s_CSharpKeyValuesHelper;
CSharpKeyValuesHelper* g_pKeyValuesHelper = &s_CSharpKeyValuesHelper;

KeyValues* CSharpKeyValuesHelper::CreateKeyValues(const char* name) const
{
    return new KeyValues(name);
}
void CSharpKeyValuesHelper::DestroyKeyValues(KeyValues* kv) const
{
    delete kv;
}
KeyValues* CSharpKeyValuesHelper::Clone(KeyValues* kv) const
{
    return kv->MakeCopy();
}
void CSharpKeyValuesHelper::Clear(KeyValues* kv) const
{
    kv->Clear();
}
bool CSharpKeyValuesHelper::LoadFromFile(KeyValues* kv, const char* filename, const char* pathId) const
{
    return kv->LoadFromFile((IFileSystem*)g_pFullFileSystem, filename, pathId);
}
bool CSharpKeyValuesHelper::SaveToFile(KeyValues* kv, const char* filename, const char* pathId, bool allowEmptyString) const
{
    return kv->SaveToFile((IFileSystem*)g_pFullFileSystem, filename, pathId, allowEmptyString);
}
bool CSharpKeyValuesHelper::LoadFromString(KeyValues* kv, const char* pBuffer) const
{
    return kv->LoadFromBuffer("CSharpKeyValuesHelper", pBuffer);
}
bool CSharpKeyValuesHelper::SaveToString(KeyValues* kv, char* buffer, int64_t size, int indent, bool sort, bool allowEmptyString) const
{
    CUtlBuffer buf;
    kv->RecursiveSaveToFile(buf, indent, sort, allowEmptyString);
    buf.GetString(buffer, static_cast<int32_t>(size));
    return buf.TellPut() > 0;
}
const char* CSharpKeyValuesHelper::GetSectionName(KeyValues* kv) const
{
    return kv->GetName();
}
void CSharpKeyValuesHelper::SetSectionName(KeyValues* kv, const char* name) const
{
    kv->SetName(name);
}
KeyValues* CSharpKeyValuesHelper::GetFirstSubKey(KeyValues* kv) const
{
    return kv->GetFirstSubKey();
}
KeyValues* CSharpKeyValuesHelper::FindLastSubKey(KeyValues* kv) const
{
    return kv->FindLastSubKey();
}
KeyValues* CSharpKeyValuesHelper::GetNextKey(KeyValues* kv) const
{
    return kv->GetNextKey();
}
KeyValues* CSharpKeyValuesHelper::GetFirstTrueSubKey(KeyValues* kv) const
{
    return kv->GetFirstTrueSubKey();
}
KeyValues* CSharpKeyValuesHelper::GetNextTrueSubKey(KeyValues* kv) const
{
    return kv->GetNextTrueSubKey();
}
KeyValues* CSharpKeyValuesHelper::FindKey(KeyValues* kv, const char* name, bool bCreate) const
{
    return kv->FindKey(name, bCreate);
}
bool CSharpKeyValuesHelper::FindAndDeleteSubKey(KeyValues* kv, const char* name) const
{
    return kv->FindAndDeleteSubKey(name);
}
KeyValues* CSharpKeyValuesHelper::AddKey(KeyValues* kv, const char* name) const
{
    return kv->AddKey(name);
}
int32_t CSharpKeyValuesHelper::GetDataType(KeyValues* kv, const char* name) const
{
    return static_cast<int32_t>(kv->GetDataType(name));
}
bool CSharpKeyValuesHelper::IsEmpty(KeyValues* kv, const char* name) const
{
    return kv->IsEmpty(name);
}
int CSharpKeyValuesHelper::GetInt(KeyValues* kv, const char* name, int defaultValue) const
{
    return kv->GetInt(name, defaultValue);
}
uint64_t CSharpKeyValuesHelper::GetUint64(KeyValues* kv, const char* name, uint64_t defaultValue) const
{
    return kv->GetUint64(name, defaultValue);
}
float CSharpKeyValuesHelper::GetFloat(KeyValues* kv, const char* name, float defaultValue) const
{
    return kv->GetFloat(name, defaultValue);
}
const char* CSharpKeyValuesHelper::GetString(KeyValues* kv, const char* keyName, const char* defaultValue)
{
    return kv->GetString(keyName, defaultValue);
}
void* CSharpKeyValuesHelper::GetPtr(KeyValues* kv, const char* name, void* defaultValue) const
{
    return kv->GetPtr(name, defaultValue);
}
void CSharpKeyValuesHelper::SetString(KeyValues* kv, const char* name, const char* value) const
{
    kv->SetString(name, value);
}
void CSharpKeyValuesHelper::SetInt(KeyValues* kv, const char* name, int value) const
{
    kv->SetInt(name, value);
}
void CSharpKeyValuesHelper::SetUint64(KeyValues* kv, const char* name, uint64_t value) const
{
    kv->SetUint64(name, value);
}
void CSharpKeyValuesHelper::SetFloat(KeyValues* kv, const char* name, float value) const
{
    kv->SetFloat(name, value);
}
void CSharpKeyValuesHelper::SetPtr(KeyValues* kv, const char* name, void* value) const
{
    kv->SetPtr(name, value);
}
void CSharpKeyValuesHelper::UsesEscapeSequences(KeyValues* kv, bool state) const
{
    kv->UsesEscapeSequences(state);
}
