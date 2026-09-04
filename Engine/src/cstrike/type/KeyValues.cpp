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

#include "address.h"
#include "gamedata.h"
#include "global.h"
#include "module.h"

#include "cstrike/type/KeyValues.h"
#include "cstrike/interface/IFileSystem.h"

class CUtlBuffer;

KeyValues::KeyValues(const char* setName, void* kvsystem, bool unkState)
{
    using fn = void (*)(KeyValues*, const char*, void*, bool);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "??0KeyValues@@QEAA@PEBDPEAVIKeyValuesSystem@@_N@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValuesC2EPKcP16IKeyValuesSystemb";
#endif

    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();

    func(this, setName, kvsystem, unkState);
}

KeyValues::~KeyValues()
{
    using fn = void (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "??1KeyValues@@QEAA@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValuesD2Ev";
#endif

    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();

    func(this);
}

bool KeyValues::LoadFromBuffer(const char* resourceName, const char* pBuffer, void* pFileSystem, const char* pPathID, void* pfnEvaluateSymbolProc, void* pErrorSpew, void* pUnk1, const char* pUnk2)
{
    using fn = bool (*)(KeyValues*, const char*, const char*, void*, const char*, void*, void*, void*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?LoadFromBuffer@KeyValues@@QEAA_NPEBD0PEAVIFileSystem@@0P6A_N0PEAX@ZPEAVIKeyValuesErrorSpew@@20@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues14LoadFromBufferEPKcS1_P11IFileSystemS1_PFbS1_PvEP19IKeyValuesErrorSpewS4_S1_";
#endif

    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();

    return func(this, resourceName, pBuffer, pFileSystem, pPathID, pfnEvaluateSymbolProc, pErrorSpew, pUnk1, pUnk2);
}

bool KeyValues::LoadFromBuffer(const char* resourceName, CUtlBuffer& buf, void* pFileSystem, const char* pPathID, void* pfnEvaluateSymbolProc, void* pErrorSpew, void* pUnk1, const char* pUnk2)
{
    using fn = bool (*)(KeyValues*, const char*, CUtlBuffer&, void*, const char*, void*, void*, void*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?LoadFromBuffer@KeyValues@@QEAA_NPEBDAEAVCUtlBuffer@@PEAVIFileSystem@@0P6A_N0PEAX@ZPEAVIKeyValuesErrorSpew@@30@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues14LoadFromBufferEPKcR10CUtlBufferP11IFileSystemS1_PFbS1_PvEP19IKeyValuesErrorSpewS6_S1_";
#endif

    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();

    return func(this, resourceName, buf, pFileSystem, pPathID, pfnEvaluateSymbolProc, pErrorSpew, pUnk1, pUnk2);
}

void KeyValues::SetName(const char* name)
{
    using fn = void (*)(KeyValues*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SetName@KeyValues@@QEAAXPEBD@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues7SetNameEPKc";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();

    func(this, name);
}

const char* KeyValues::GetName() const
{
    using fn = const char* (*)(const KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?Internal_GetName@CKeyValues_Data@@IEBAPEBDXZ";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues7GetNameEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

KeyValues* KeyValues::FindKey(const char* keyName, bool bCreate)
{
    using fn = KeyValues* (*)(KeyValues*, const char*, bool);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?FindKey@KeyValues@@QEAAPEAV1@PEBD_N@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues7FindKeyEPKcb";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName, bCreate);
}

const KeyValues* KeyValues::FindKey(const char* keyName) const
{
    using fn = KeyValues* (*)(const KeyValues*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?FindKey@KeyValues@@QEBAPEBV1@PEBD@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues7FindKeyEPKc";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName);
}

KeyValues* KeyValues::AddKey(const char* keyName)
{
    using fn = KeyValues* (*)(KeyValues*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?AddKey@KeyValues@@QEAAPEAV1@PEBD@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues6AddKeyEPKc";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName);
}

KeyValues* KeyValues::GetFirstSubKey()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?Internal_GetSubKey@CKeyValues_Data@@IEBAPEAVKeyValues@@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues14GetFirstSubKeyEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

KeyValues* KeyValues::FindLastSubKey()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?FindLastSubKey@KeyValues@@QEAAPEAV1@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues14FindLastSubKeyEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

bool KeyValues::FindAndDeleteSubKey(const char* name)
{
    using fn = bool (*)(KeyValues*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?FindAndDeleteSubKey@KeyValues@@QEAA_NPEBD@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues19FindAndDeleteSubKeyEPKc";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, name);
}

KeyValues* KeyValues::GetNextKey()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetNextKey@KeyValues@@QEAAPEAV1@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues10GetNextKeyEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

KeyValues* KeyValues::GetFirstTrueSubKey()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetFirstTrueSubKey@KeyValues@@QEAAPEAV1@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues18GetFirstTrueSubKeyEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

KeyValues* KeyValues::GetNextTrueSubKey()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetNextTrueSubKey@KeyValues@@QEAAPEAV1@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues17GetNextTrueSubKeyEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

KeyValues* KeyValues::GetFirstValue()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetFirstValue@KeyValues@@QEAAPEAV1@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues13GetFirstValueEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

KeyValues* KeyValues::GetNextValue()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetNextValue@KeyValues@@QEAAPEAV1@XZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues12GetNextValueEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

int KeyValues::GetInt(const char* keyName, int defaultValue) const
{
    using fn = int (*)(const KeyValues*, const char*, int);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetInt@KeyValues@@QEBAHPEBDH@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues6GetIntEPKci";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName, defaultValue);
}

uint64_t KeyValues::GetUint64(const char* keyName, uint64_t defaultValue) const
{
    using fn = uint64_t (*)(const KeyValues*, const char*, uint64_t);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetUint64@KeyValues@@QEBA_KPEBD_K@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues9GetUint64EPKcy";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName, defaultValue);
}

float KeyValues::GetFloat(const char* keyName, float defaultValue) const
{
    using fn = float (*)(const KeyValues*, const char*, float);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetFloat@KeyValues@@QEBAMPEBDM@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues8GetFloatEPKcf";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName, defaultValue);
}

const char* KeyValues::GetString(const char* keyName, const char* defaultValue, char* pszOut, size_t maxlen)
{
    auto key = FindKey(keyName);
    return key ? key->Internal_GetString(keyName, defaultValue, pszOut, maxlen) : defaultValue;
}

const char* KeyValues::Internal_GetString(const char* keyName, const char* defaultValue, char* pszOut, size_t maxlen)
{
    using fn = const char* (*)(KeyValues*, const char*, const char*, char*, size_t);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?Internal_GetString@CKeyValues_Data@@IEAAPEBDPEBDPEAD_K@Z";
#else
    constexpr std::string_view symbol_name = "_ZN15CKeyValues_Data18Internal_GetStringEPKcPcm";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName, defaultValue, pszOut, maxlen);
}

void* KeyValues::GetPtr(const char* keyName, void* defaultValue) const
{
    using fn = void* (*)(const KeyValues*, const char*, void*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetPtr@KeyValues@@QEBAPEAXPEBDPEAX@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues6GetPtrEPKcPv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName, defaultValue);
}

Color32 KeyValues::GetColor(const char* keyName, const Color32& defaultColor) const
{
    using fn = Color32 (*)(const KeyValues*, const char*, const Color32&);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetColor@KeyValues@@QEBA?AVColor@@PEBDAEBV2@@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues8GetColorEPKcRK5Color";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName, defaultColor);
}

bool KeyValues::IsEmpty(const char* keyName) const
{
    using fn = bool (*)(const KeyValues*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?IsEmpty@KeyValues@@QEBA_NPEBD@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues7IsEmptyEPKc";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, keyName);
}

void KeyValues::SetString(const char* keyName, const char* value)
{
    using fn = void (*)(KeyValues*, const char*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SetString@KeyValues@@QEAAXPEBD0@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues9SetStringEPKcS1_";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    func(this, keyName, value);
}

void KeyValues::SetInt(const char* keyName, int value)
{
    using fn = void (*)(KeyValues*, const char*, int);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SetInt@KeyValues@@QEAAXPEBDH@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues6SetIntEPKci";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    func(this, keyName, value);
}

void KeyValues::SetUint64(const char* keyName, uint64_t value)
{
    using fn = void (*)(KeyValues*, const char*, uint64_t);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SetUint64@KeyValues@@QEAAXPEBD_K@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues9SetUint64EPKcy";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    func(this, keyName, value);
}

void KeyValues::SetFloat(const char* keyName, float value)
{
    using fn = void (*)(KeyValues*, const char*, float);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SetFloat@KeyValues@@QEAAXPEBDM@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues8SetFloatEPKcf";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    func(this, keyName, value);
}

void KeyValues::SetPtr(const char* keyName, void* value)
{
    using fn = void (*)(KeyValues*, const char*, void*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SetPtr@KeyValues@@QEAAXPEBDPEAX@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues6SetPtrEPKcPv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    func(this, keyName, value);
}

void KeyValues::SetColor(const char* keyName, Color32 value)
{
    using fn = void (*)(KeyValues*, const char*, Color32);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SetColor@KeyValues@@QEAAXPEBDVColor@@@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues8SetColorEPKc5Color";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    func(this, keyName, value);
}

KeyValues* KeyValues::MakeCopy()
{
    using fn = KeyValues* (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?MakeCopy@KeyValues@@QEBAPEAV1@XZ";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues8MakeCopyEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this);
}

void KeyValues::Clear()
{
    using fn = void (*)(KeyValues*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?Clear@KeyValues@@QEAAXXZ";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues5ClearEv";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    func(this);
}

bool KeyValues::LoadFromFile(class IFileSystem* file_system, const char* filename, const char* path_id)
{
    using fn = bool (*)(KeyValues*, IFileSystem*, const char*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?LoadFromFile@KeyValues@@QEAA_NPEAVIFileSystem@@PEBD1P6A_N1PEAX@Z21@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues12LoadFromFileEP11IFileSystemPKcS3_PFbS3_PvES4_S3_";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, file_system, filename, path_id);
}

bool KeyValues::SaveToFile(class IFileSystem* file_system, const char* filename, const char* path_id, bool allow_empty_string)
{
    using fn = bool (*)(KeyValues*, IFileSystem*, const char*, const char*, bool);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?SaveToFile@KeyValues@@QEAA_NPEAVIFileSystem@@PEBD1_N2@Z";
#else
    constexpr std::string_view symbol_name = "_ZN9KeyValues10SaveToFileEP11IFileSystemPKcS3_bb";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, file_system, filename, path_id, allow_empty_string);
}

void KeyValues::RecursiveSaveToFile(const CUtlBuffer& buf, int indent, bool sort, bool allow_empty_string)
{
    using fn = void (*)(KeyValues*, const CUtlBuffer&, int, bool, bool);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?RecursiveSaveToFile@KeyValues@@QEBAXAEAVCUtlBuffer@@H_N1@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues19RecursiveSaveToFileER10CUtlBufferibb";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, buf, indent, sort, allow_empty_string);
}

CKeyValues_Data::types_t KeyValues::GetDataType(const char* name)
{
    using fn = CKeyValues_Data::types_t (*)(KeyValues*, const char*);
#ifdef PLATFORM_WINDOWS
    constexpr std::string_view symbol_name = "?GetDataType@KeyValues@@QEBA?AW4types_t@CKeyValues_Data@@PEBD@Z";
#else
    constexpr std::string_view symbol_name = "_ZNK9KeyValues11GetDataTypeEPKc";
#endif
    static auto func = modules::tier0->GetExportByName(symbol_name).As<fn>();
    return func(this, name);
}