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

#include "logging.h"
#include "gamedata.h"
#include "global.h"
#include "sdkproxy.h"
#include "strtool.h"

// sdk
#include "cstrike/interface/ICommandLine.h"

// crt
#include <cstdarg>
#include <cstring>
#include <filesystem>
#include <mutex>
#include <string>
#include <system_error>

#ifdef PLATFORM_WINDOWS
#    include <windows.h>
#endif

thread_local bool g_bInLoggingFlow;

std::string g_pLoggerMapName;

namespace fs = std::filesystem;

static constexpr const char* kLogDirectory = "../../sharp/logs";

static void EnsureLogDirectory()
{
    static std::once_flag once;
    std::call_once(once, [] {
        std::error_code ec;
        fs::create_directories(kLogDirectory, ec);
    });
}

static FILE* OpenLogFile(const char* fileName)
{
    EnsureLogDirectory();

    const auto path = std::string(kLogDirectory) + '/' + fileName;
    return fopen(path.c_str(), "a+");
}

void CreateLogging()
{
    EnsureLogDirectory();
}

void WriteTextToFile(const char* path, const char* text)
{
    const auto logFile = fopen(path, "wt");
    if (logFile != nullptr)
    {
        fwrite(text, sizeof(char), strlen(text), logFile);
        fflush(logFile);
        fclose(logFile);
    }
}

void ConsoleMessage(const char* function, const char* buffer, ...)
{
    va_list args;
    va_start(args, buffer);

    char message[2048];
    vsnprintf(message, sizeof(message), buffer, args);

    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    ConColorMsg({0, 255, 255, 255}, "L<Engine> [%s] | Information | %s %s\n%s\n\n", timestamp, function, g_pLoggerMapName.c_str(), message);
    g_bInLoggingFlow = false;
}

void ConsoleWarning(const char* function, const char* buffer, ...)
{
    va_list args;
    va_start(args, buffer);

    char message[2048];
    vsnprintf(message, sizeof(message), buffer, args);

    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    Warning("L<Engine> [%s] | Warning | %s %s\n%s\n\n", timestamp, function, g_pLoggerMapName.c_str(), message);
    g_bInLoggingFlow = false;
}

void ConsoleText(const Color& color, const char* buffer, ...)
{
    va_list args;
    va_start(args, buffer);

    char message[2048];
    vsnprintf(message, sizeof(message), buffer, args);

    va_end(args);

    g_bInLoggingFlow = true;
    ConColorMsg(color, message);
    g_bInLoggingFlow = false;
}

void FatalError(const char* message, ...)
{
    char    text[2048];
    va_list args;

    va_start(args, message);
    vsnprintf(text, sizeof(text), message, args);
    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    ConColorMsg({233, 0, 0, 255}, "L<Engine> [%s] | Fatal Error %s\n%s\n\n", timestamp, g_pLoggerMapName.c_str(), text);
    g_bInLoggingFlow = false;

    // 写入日志文件
    const auto logFile = OpenLogFile("fatal.log");
    if (logFile != nullptr)
    {
        fprintf(logFile, "[%s] | Fatal Error | %s\n", timestamp, g_pLoggerMapName.c_str());
        fprintf(logFile, "%s\n\n", text);
        fflush(logFile);
        fclose(logFile);
    }

#ifdef PLATFORM_WINDOWS
    if (IsDebuggerPresent())
    {
        DebugBreak();
    }
#endif

#ifdef PLATFORM_WINDOWS
    if (!CommandLine()->HasParam("-nodialog"))
        MessageBoxA(nullptr, text, "Error", MB_ICONERROR | MB_OK);
#elif _LINUX
    // TODO
#endif

    // TODO CoreCLR exit single

    if (!CommandLine()->HasParam("-dev"))
    {
        // make minidump
        volatile int64_t* p = nullptr;
        *p      = 0x55667788;
    }

    Plat_ExitProcess(100);
}

void LogFatal(const char* message, ...)
{
    char    text[2048];
    va_list args;

    va_start(args, message);
    vsnprintf(text, sizeof(text), message, args);
    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    ConColorMsg({233, 0, 0, 255}, "L<Engine> [%s] | Fatal Error %s\n%s\n\n", timestamp, g_pLoggerMapName.c_str(), text);
    g_bInLoggingFlow = false;

    const auto logFile = OpenLogFile("fatal.log");
    if (logFile != nullptr)
    {
        if (!g_pLoggerMapName.empty())
            fprintf(logFile, "[%s] | Fatal Error %s\n", timestamp, g_pLoggerMapName.c_str());
        else
            fprintf(logFile, "[%s] | Fatal Error\n", timestamp);

        fprintf(logFile, "%s\n\n", text);
        fflush(logFile);
        fclose(logFile);
    }
}

void LogError(const char* message, ...)
{
    char    text[2048];
    va_list args;

    va_start(args, message);
    vsnprintf(text, sizeof(text), message, args);
    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    ConColorMsg({233, 0, 0, 255}, "L<Engine> [%s] | Error %s\n%s\n\n", timestamp, g_pLoggerMapName.c_str(), text);
    g_bInLoggingFlow = false;

    const auto logFile = OpenLogFile("error.log");
    if (logFile != nullptr)
    {
        if (!g_pLoggerMapName.empty())
            fprintf(logFile, "[%s] | Error %s\n", timestamp, g_pLoggerMapName.c_str());
        else
            fprintf(logFile, "[%s] | Error\n", timestamp);

        fprintf(logFile, "%s\n\n", text);
        fflush(logFile);
        fclose(logFile);
    }
}

void LogInfo(const char* message, ...)
{
    char    text[2048];
    va_list args;

    va_start(args, message);
    vsnprintf(text, sizeof(text), message, args);
    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    ConColorMsg(Color(0, 255, 255, 255), "L<Engine> [%s] | Information %s\n%s\n\n", timestamp, g_pLoggerMapName.c_str(), text);
    g_bInLoggingFlow = false;

    const auto logFile = OpenLogFile("info.log");
    if (logFile != nullptr)
    {
        if (!g_pLoggerMapName.empty())
            fprintf(logFile, "[%s] | Information %s\n", timestamp, g_pLoggerMapName.c_str());
        else
            fprintf(logFile, "[%s] | Information\n", timestamp);

        fprintf(logFile, "%s\n\n", text);
        fflush(logFile);
        fclose(logFile);
    }
}

void LogFuncError(const char* function, const char* message, ...)
{
    char    text[2048];
    va_list args;

    va_start(args, message);
    vsnprintf(text, sizeof(text), message, args);
    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    ConColorMsg({233, 0, 0, 255}, "L<Engine> [%s] | Error | %s %s\n%s\n\n", timestamp, function, g_pLoggerMapName.c_str(), text);
    g_bInLoggingFlow = false;

    const auto logFile = OpenLogFile("error.log");
    if (logFile != nullptr)
    {
        if (!g_pLoggerMapName.empty())
            fprintf(logFile, "[%s] | Error | %s %s\n", timestamp, function, g_pLoggerMapName.c_str());
        else
            fprintf(logFile, "[%s] | Error | %s\n", timestamp, function);

        fprintf(logFile, "%s\n\n", text);
        fflush(logFile);
        fclose(logFile);
    }
}

void LogFuncInfo(const char* function, const char* message, ...)
{
    char    text[2048];
    va_list args;

    va_start(args, message);
    vsnprintf(text, sizeof(text), message, args);
    va_end(args);

    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    g_bInLoggingFlow = true;
    ConColorMsg(Color(0, 255, 255, 255), "L<Engine> [%s] | Information | %s %s\n%s\n\n", timestamp, function, g_pLoggerMapName.c_str(), text);
    g_bInLoggingFlow = false;

    const auto logFile = OpenLogFile("info.log");
    if (logFile != nullptr)
    {
        if (!g_pLoggerMapName.empty())
            fprintf(logFile, "[%s] | Information | %s %s\n", timestamp, function, g_pLoggerMapName.c_str());
        else
            fprintf(logFile, "[%s] | Information | %s\n", timestamp, function);

        fprintf(logFile, "%s\n\n", text);
        fflush(logFile);
        fclose(logFile);
    }
}