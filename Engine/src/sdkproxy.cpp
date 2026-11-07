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

#include "sdkproxy.h"
#include "gamedata.h"
#include "global.h"

#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/IFileSystem.h"
#include "cstrike/type/CUtlBuffer.h"

bool IsDebugMode()
{
    return CommandLine()->HasParam("-dev") || CommandLine()->HasParam("-debug") || CommandLine()->HasParam("-stardebug");
}

bool GameFileExists(const char* path)
{
    return (g_pFullFileSystem->FileExists(path, "GAME") || g_pFullFileSystem->FileExists(path, "CONTENT"));
}

std::vector<uint8_t> ReadGameFile(const char* path, const char* pathId)
{
    if (!g_pFullFileSystem->FileExists(path, pathId))
        return {};

    auto handle = g_pFullFileSystem->Open(path, "rb", pathId);
    if (handle == nullptr)
        return {};

    const auto           size = g_pFullFileSystem->Size(handle);
    std::vector<uint8_t> buffer{};
    buffer.resize(size);
    const auto read = g_pFullFileSystem->Read(buffer.data(), size, handle);
    g_pFullFileSystem->Close(handle);

    buffer.resize(read > 0 ? static_cast<size_t>(read) : 0);

    return buffer;
}

/*const char* Plat_GetGameDirectory()
{
    using fn         = const char* (*)(int);
    static auto func = g_pGameData->GetAddress<fn>("Plat_GetGameDirectory");

    return func(0);
}

double Plat_FloatTime()
{
    using fn         = double (*)();
    static auto func = g_pGameData->GetAddress<fn>("Plat_FloatTime");

    return func();
}

bool Plat_IsInDebugSession()
{
    using fn         = bool (*)();
    static auto func = g_pGameData->GetAddress<fn>("Plat_IsInDebugSession");
    return func();
}

void Plat_ExitProcess(int code)
{
    using fn         = void (*)(int);
    static auto func = g_pGameData->GetAddress<fn>("Plat_ExitProcess");
    func(code);
}*/

/*
bool V_StringToBool(const char* string, bool defaultValue)
{
    using fn         = bool (*)(const char*, bool);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToBool");

    return func(string, defaultValue);
}

int8_t V_StringToInt8(const char* string, int8_t defaultValue)
{
    using fn         = int8_t (*)(const char*, int8_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToInt8");

    return func(string, defaultValue);
}

uint8_t V_StringToUint8(const char* string, uint8_t defaultValue)
{
    using fn         = int8_t (*)(const char*, uint8_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToUint8");

    return func(string, defaultValue);
}

int16_t V_StringToInt16(const char* string, int16_t defaultValue)
{
    using fn         = int16_t (*)(const char*, int16_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToInt16");

    return func(string, defaultValue);
}

uint16_t V_StringToUint16(const char* string, uint16_t defaultValue)
{
    using fn         = uint16_t (*)(const char*, uint16_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToUint16");

    return func(string, defaultValue);
}

int32_t V_StringToInt32(const char* string, int32_t defaultValue)
{
    using fn         = int32_t (*)(const char*, int32_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToInt32");

    return func(string, defaultValue);
}

uint32_t V_StringToUint32(const char* string, uint32_t defaultValue)
{
    using fn         = uint32_t (*)(const char*, uint32_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToUint32");

    return func(string, defaultValue);
}

int64_t V_StringToInt64(const char* string, int64_t defaultValue)
{
    using fn         = int64_t (*)(const char*, int64_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToInt64");

    return func(string, defaultValue);
}

uint64_t V_StringToUint64(const char* string, uint64_t defaultValue)
{
    using fn         = uint64_t (*)(const char*, uint64_t);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToUint64");

    return func(string, defaultValue);
}

float V_StringToFloat32(const char* string, float defaultValue)
{
    using fn         = float (*)(const char*, float);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToFloat32");

    return func(string, defaultValue);
}

double V_StringToFloat64(const char* string, double defaultValue)
{
    using fn         = double (*)(const char*, double);
    static auto func = g_pGameData->GetAddress<fn>("V_StringToFloat64");

    return func(string, defaultValue);
}*/