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

#ifndef MS_ROOT_SDKPROXY_H
#define MS_ROOT_SDKPROXY_H

#include "platform.h"

#include <cstdint>
#include <vector>

class CBufferString;
class CServerSideClient;
class CEntityKeyValues;
class ICommandLine;

bool IsDebugMode();

/* FileSystem */
bool                 GameFileExists(const char* path);
std::vector<uint8_t> ReadGameFile(const char* path, const char* pathId);

/* Platform */
MS_IMPORT const char* Plat_GetGameDirectory(int unk = 0);
MS_IMPORT double      Plat_FloatTime();
MS_IMPORT bool        Plat_IsInDebugSession();
MS_IMPORT void        Plat_ExitProcess(int code);
MS_IMPORT int         Plat_GetExecutablePathUTF8(CBufferString* buf);

MS_IMPORT char*         MemAlloc_StrDupFunc(const char* str);
MS_IMPORT ICommandLine* CommandLine();

MS_IMPORT bool     V_StringToBool(const char* string, bool defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT int8_t   V_StringToInt8(const char* string, int8_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT uint8_t  V_StringToUint8(const char* string, uint8_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT int16_t  V_StringToInt16(const char* string, int16_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT uint16_t V_StringToUint16(const char* string, uint16_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT int32_t  V_StringToInt32(const char* string, int32_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT uint32_t V_StringToUint32(const char* string, uint32_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT int64_t  V_StringToInt64(const char* string, int64_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT uint64_t V_StringToUint64(const char* string, uint64_t defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT float    V_StringToFloat32(const char* string, float defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);
MS_IMPORT double   V_StringToFloat64(const char* string, double defaultValue, bool* successful = nullptr, char** remainder = nullptr, uint32_t flags = 0, void* err_listener = nullptr);

MS_CLASS_IMPORT const char* V_stristr_fast(const char* pStr, const char* pSearch);

#endif