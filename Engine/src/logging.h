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

#ifndef MS_ROOT_LOGGING_H
#define MS_ROOT_LOGGING_H

#define BOOLVAL(v) v ? "" #v : "not " #v

#define AssertPtr(ptr)                 \
    if ((ptr) == nullptr) [[unlikely]] \
    FatalError("MS: %s is nullptr in %s\n", #ptr, __FUNCTION__)

#define AssertBool(v)      \
    if (!(v)) [[unlikely]] \
    FatalError("MS: %s is false in %s\n", #v, __FUNCTION__)

#define AssertVal(v)       \
    if (!(v)) [[unlikely]] \
    FatalError("MS: %s is default in %s\n", #v, __FUNCTION__)

#define BooleanSTR(v) \
    v ? "true" : "false"

#include "platform.h"

#include <string>

extern std::string       g_pLoggerMapName;
extern thread_local bool g_bInLoggingFlow;

class Color
{
public:
    // constructors
    Color()
    {
        *reinterpret_cast<int*>(this) = 0;
    }
    Color(int _r, int _g, int _b)
    {
        SetColor(_r, _g, _b, 0);
    }
    Color(int _r, int _g, int _b, int _a)
    {
        SetColor(_r, _g, _b, _a);
    }

    void SetColor(int _r, int _g, int _b, int _a = 0)
    {
        _color[0] = static_cast<unsigned char>(_r);
        _color[1] = static_cast<unsigned char>(_g);
        _color[2] = static_cast<unsigned char>(_b);
        _color[3] = static_cast<unsigned char>(_a);
    }

    unsigned char r() const
    {
        return _color[0];
    }

    unsigned char g() const
    {
        return _color[1];
    }

    unsigned char b() const
    {
        return _color[2];
    }

    unsigned char a() const
    {
        return _color[3];
    }

private:
    unsigned char _color[4];
};

MS_GLOBAL_IMPORT void ConColorMsg(const Color& clr, const char* pMsg, ...) MS_FMTFUNCTION(2, 3);
MS_IMPORT void        Warning(const char* pMsg, ...) MS_FMTFUNCTION(1, 2);

/*
template <typename... Args>
void ConColorMsg(const ConsoleColor& color, const char* buffer, Args...);

template <typename... Args>
void Warning(const char* buffer, Args...);
*/

void ConsoleMessage(const char* function, const char* buffer, ...) MS_FMTFUNCTION(2, 3);
void ConsoleWarning(const char* function, const char* buffer, ...) MS_FMTFUNCTION(2, 3);
void ConsoleText(const Color& color, const char* buffer, ...) MS_FMTFUNCTION(2, 3);

void LogFatal(const char* message, ...) MS_FMTFUNCTION(1, 2);
void LogError(const char* message, ...) MS_FMTFUNCTION(1, 2);
void LogInfo(const char* message, ...) MS_FMTFUNCTION(1, 2);

void LogFuncError(const char* function, const char* message, ...) MS_FMTFUNCTION(2, 3);
void LogFuncInfo(const char* function, const char* message, ...) MS_FMTFUNCTION(2, 3);

void FatalError(const char* message, ...) MS_FMTFUNCTION(1, 2);

void CreateLogging();

void WriteTextToFile(const char* path, const char* text);

#define LOG(...) ConsoleMessage(__FUNCTION__, __VA_ARGS__)
#define WARN(...) ConsoleWarning(__FUNCTION__, __VA_ARGS__)
#define FLOG(...) LogFuncInfo(__FUNCTION__, __VA_ARGS__)
#define FERROR(...) LogFuncError(__FUNCTION__, __VA_ARGS__)

#endif