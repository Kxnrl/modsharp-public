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
#include <atomic>
#include <chrono>
#include <condition_variable>
#include <cstdarg>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <deque>
#include <filesystem>
#include <mutex>
#include <string>
#include <thread>
#include <unordered_map>
#include <utility>

#ifdef PLATFORM_WINDOWS
#    include <windows.h>
#endif

thread_local bool g_bInLoggingFlow;

std::string g_pLoggerMapName;

namespace fs = std::filesystem;

constexpr const char* INFO_LOG_PATH  = "../../sharp/logs/info.log";
constexpr const char* ERROR_LOG_PATH = "../../sharp/logs/error.log";
constexpr const char* FATAL_LOG_PATH = "../../sharp/logs/fatal.log";

namespace {

struct AsyncLogEntry
{
    std::string path;
    std::string text;
};

// Log lines used to be written with fopen/fprintf/fflush/fclose on the calling
// thread, which is almost always the game thread. On slow or containerized
// storage a single flush costs multiple milliseconds and shows up as frame
// hitches. Lines are now fully formatted (including the timestamp) at the call
// site and handed to a dedicated writer thread that keeps the log files open.
class AsyncLogWriter
{
    // When the queue is full new lines are dropped (and counted) instead of
    // blocking the game thread on IO.
    static constexpr size_t MAX_QUEUED_LINES = 4096;

public:
    void Start()
    {
        std::lock_guard lock(m_Mutex);

        if (m_bRunning)
        {
            return;
        }

        m_bRunning       = true;
        m_bStopRequested = false;
        m_Thread         = std::thread(&AsyncLogWriter::Run, this);
    }

    // Returns false if the writer is not running (early startup or after
    // shutdown); the caller must then write synchronously. "text" is only
    // consumed when true is returned.
    bool TryEnqueue(const char* path, std::string&& text)
    {
        {
            std::lock_guard lock(m_Mutex);

            if (!m_bRunning || m_bStopRequested)
            {
                return false;
            }

            if (m_Queue.size() >= MAX_QUEUED_LINES)
            {
                m_nDroppedLines.fetch_add(1, std::memory_order_relaxed);
                return true;
            }

            m_Queue.push_back({path, std::move(text)});
        }

        m_WakeUp.notify_one();

        return true;
    }

    // Blocks until every queued line reached the disk or the timeout expired.
    // Only used by the fatal path right before the process goes down.
    void Flush(std::chrono::milliseconds timeout)
    {
        std::unique_lock lock(m_Mutex);

        if (!m_bRunning)
        {
            return;
        }

        m_Drained.wait_for(lock, timeout, [this] { return m_Queue.empty() && !m_bWriting; });
    }

    // Must never be called while holding m_Mutex: join would deadlock against
    // the writer thread taking the lock to grab the next batch.
    void Shutdown()
    {
        {
            std::lock_guard lock(m_Mutex);

            if (!m_bRunning)
            {
                return;
            }

            m_bStopRequested = true;
        }

        m_WakeUp.notify_one();
        m_Thread.join();

        std::lock_guard lock(m_Mutex);
        m_bRunning = false;
    }

private:
    void Run()
    {
        std::unordered_map<std::string, FILE*> files;

        std::unique_lock lock(m_Mutex);

        while (true)
        {
            m_WakeUp.wait(lock, [this] { return !m_Queue.empty() || m_bStopRequested; });

            if (m_Queue.empty() && m_bStopRequested)
            {
                break;
            }

            std::deque<AsyncLogEntry> batch;
            batch.swap(m_Queue);
            m_bWriting = true;

            // Write without holding the lock so producers never wait on IO.
            lock.unlock();
            WriteBatch(files, batch);
            lock.lock();

            m_bWriting = false;

            if (m_Queue.empty())
            {
                m_Drained.notify_all();
            }
        }

        lock.unlock();

        for (const auto& [path, file] : files)
        {
            if (file != nullptr)
            {
                fclose(file);
            }
        }
    }

    void WriteBatch(std::unordered_map<std::string, FILE*>& files, std::deque<AsyncLogEntry>& batch)
    {
        if (const auto dropped = m_nDroppedLines.exchange(0, std::memory_order_relaxed); dropped > 0)
        {
            char notice[128];
            snprintf(notice, sizeof(notice), "[AsyncLogWriter] dropped %llu log line(s): queue was full\n\n", static_cast<unsigned long long>(dropped));
            batch.push_front({ERROR_LOG_PATH, notice});
        }

        for (const auto& entry : batch)
        {
            auto& file = files[entry.path];

            if (file == nullptr)
            {
                file = fopen(entry.path.c_str(), "a+");

                if (file == nullptr)
                {
                    continue;
                }
            }

            fwrite(entry.text.data(), sizeof(char), entry.text.size(), file);
        }

        // One flush per touched file per batch instead of one per line.
        for (const auto& [path, file] : files)
        {
            if (file != nullptr)
            {
                fflush(file);
            }
        }
    }

    std::mutex                m_Mutex;
    std::condition_variable   m_WakeUp;
    std::condition_variable   m_Drained;
    std::deque<AsyncLogEntry> m_Queue;
    std::thread               m_Thread;
    std::atomic<uint64_t>     m_nDroppedLines{0};
    bool                      m_bRunning       = false;
    bool                      m_bStopRequested = false;
    bool                      m_bWriting       = false;
};

AsyncLogWriter g_AsyncLogWriter;

void WriteLogLineSync(const char* path, const std::string& text)
{
    const auto logFile = fopen(path, "a+");
    if (logFile != nullptr)
    {
        fwrite(text.data(), sizeof(char), text.size(), logFile);
        fflush(logFile);
        fclose(logFile);
    }
}

void WriteLogLine(const char* path, std::string&& text)
{
    if (!g_AsyncLogWriter.TryEnqueue(path, std::move(text)))
    {
        WriteLogLineSync(path, text);
    }
}

// The map name is read on the calling thread on purpose: g_pLoggerMapName is
// mutated on map change and must not be touched from the writer thread.
std::string BuildLogLine(const char* timestamp, const char* level, const char* function, const char* text)
{
    char header[512];

    if (function != nullptr)
    {
        if (!g_pLoggerMapName.empty())
        {
            snprintf(header, sizeof(header), "[%s] | %s | %s %s\n", timestamp, level, function, g_pLoggerMapName.c_str());
        }
        else
        {
            snprintf(header, sizeof(header), "[%s] | %s | %s\n", timestamp, level, function);
        }
    }
    else
    {
        if (!g_pLoggerMapName.empty())
        {
            snprintf(header, sizeof(header), "[%s] | %s %s\n", timestamp, level, g_pLoggerMapName.c_str());
        }
        else
        {
            snprintf(header, sizeof(header), "[%s] | %s\n", timestamp, level);
        }
    }

    std::string line;
    line.reserve(strlen(header) + strlen(text) + 2);
    line.append(header);
    line.append(text);
    line.append("\n\n");

    return line;
}

} // namespace

void CreateLogging()
{
    const fs::path path = "../../sharp/logs";
    if (!fs::exists(path))
    {
        fs::create_directory(path);
    }

    g_AsyncLogWriter.Start();
}

void ShutdownLogging()
{
    g_AsyncLogWriter.Shutdown();
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

    // The process dies below: give queued lines a chance to reach the disk,
    // then write the fatal record itself synchronously.
    g_AsyncLogWriter.Flush(std::chrono::milliseconds(1000));

    WriteLogLineSync(FATAL_LOG_PATH, BuildLogLine(timestamp, "Fatal Error", nullptr, text));

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

    // Fatal records always go to the disk synchronously: the process usually
    // does not live long enough for a queued write.
    WriteLogLineSync(FATAL_LOG_PATH, BuildLogLine(timestamp, "Fatal Error", nullptr, text));
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

    WriteLogLine(ERROR_LOG_PATH, BuildLogLine(timestamp, "Error", nullptr, text));
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

    WriteLogLine(INFO_LOG_PATH, BuildLogLine(timestamp, "Information", nullptr, text));
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

    WriteLogLine(ERROR_LOG_PATH, BuildLogLine(timestamp, "Error", function, text));
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

    WriteLogLine(INFO_LOG_PATH, BuildLogLine(timestamp, "Information", function, text));
}
