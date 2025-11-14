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

#include "CoreCLR/coreclr.h"
#include "CoreCLR/coreclr_delegates.h"
#include "CoreCLR/hostfxr.h"

#ifndef WIN32_LEAN_AND_MEAN
#    define WIN32_LEAN_AND_MEAN
#endif

#ifdef PLATFORM_WINDOWS
#    include <windows.h>
#    define STR(s) L##s
#else
#    include <dlfcn.h>
#    define STR(s) s
#endif

#include "logging.h"
#include "strtool.h"

#include <algorithm>
#include <array>
#include <cassert>
#include <charconv>
#include <filesystem>
#include <iostream>
#include <optional>
#include <ranges>
#include <string>
#include <vector>

struct HostFxrUtils
{
    hostfxr_initialize_for_runtime_config_fn Init;
    hostfxr_get_runtime_delegate_fn          GetDelegate;
    hostfxr_set_runtime_property_value_fn    SetProperty;
    hostfxr_close_fn                         Close;
} static s_HostFxrUtils;

static char s_DotNetVersion[128] = "1.0";

void* load_library(const char* path)
{
#ifdef PLATFORM_WINDOWS
    HMODULE h = LoadLibraryA(path);
    assert(h != nullptr);
    return (void*)h;
#else
    void* h = dlopen(path, RTLD_LAZY | RTLD_LOCAL);
    assert(h != nullptr);
    return h;
#endif
}

void* get_export(void* h, const char* name)
{
#ifdef PLATFORM_WINDOWS
    void* f = ::GetProcAddress((HMODULE)h, name);
    assert(f != nullptr);
    return f;
#else

    void* f = dlsym(h, name);
    assert(f != nullptr);
    return f;
#endif
}

struct Version
{
    Version() = default;
    explicit Version(std::string_view input)
    {
        auto sv_to_int = [](std::string_view input) -> std::optional<int> {
            int  out{};
            auto result = std::from_chars(input.data(), input.data() + input.size(), out);

            if (result.ec == std::errc::invalid_argument || result.ec == std::errc::result_out_of_range)
                return std::nullopt;

            return out;
        };

        int count = 0;

        for (auto&& str : input | std::views::split('.'))
        {
            // 应该不会遇到这个情况
            if (count >= 4)
                break;

#if defined(__GNUC__) && __GNUC__ < 12
            const std::string_view token(&*str.begin(), std::ranges::distance(str));
#else
            const std::string_view token(str.begin(), str.end());
#endif

            _numbers[count] = sv_to_int(token).value_or(0);
            count++;
        }
    }

    bool operator==(const Version& other) const
    {
        return _numbers == other._numbers;
    }

    bool operator!=(const Version& other) const
    {
        return !(*this == other);
    }

    bool operator<(const Version& other) const
    {
        return std::ranges::lexicographical_compare(_numbers, other._numbers);
    }

    bool operator<=(const Version& other) const
    {
        return !(*this > other);
    }

    bool operator>(const Version& other) const
    {
        return other < *this;
    }

    bool operator>=(const Version& other) const
    {
        return !(*this < other);
    }

    int get(int index) const
    {
        if (index < 0 || index >= 4)
            return -1;
        return _numbers[index];
    }

private:
    std::array<int, 4> _numbers{};
};

static std::vector<std::string> s_vecSearchPaths = {};
static Version                  s_highestVersionFound;
static constexpr int            MIN_DOTNET_MAJOR_VERSION = 10;

static std::string FindDotnetRuntime()
{
#ifdef WIN32
    static constexpr std::string_view dll = "hostfxr.dll";
    s_vecSearchPaths.emplace_back(R"(C:\Program Files\dotnet\host\fxr)");
#else
    static constexpr std::string_view dll = "libhostfxr.so";
    s_vecSearchPaths.emplace_back("/usr/share/dotnet/host/fxr/");
    s_vecSearchPaths.emplace_back("/usr/lib/dotnet/host/fxr/");
#endif

    std::filesystem::path latest_file;
    Version               latest_file_version;
    bool                  found = false;

    for (const auto& search_path : s_vecSearchPaths)
    {
        if (!std::filesystem::exists(search_path)) continue;

        for (const auto& entry : std::filesystem::recursive_directory_iterator(search_path))
        {
            if (entry.path().filename() != dll)
                continue;

            Version version(entry.path().parent_path().filename().string());
            if (version > s_highestVersionFound)
            {
                s_highestVersionFound = version;
            }

            if (version > latest_file_version)
            {
                if (version.get(0) >= MIN_DOTNET_MAJOR_VERSION)
                {
                    latest_file_version = version;
                    latest_file         = entry;
                    found               = true;
                }
            }
        }
    }

    if (found)
    {
        snprintf(s_DotNetVersion, sizeof(s_DotNetVersion), "%d.%d.%d", latest_file_version.get(0), latest_file_version.get(1), latest_file_version.get(2));
    }

    return latest_file.string();
}

bool LoadHostFxr()
{
    std::string path = FindDotnetRuntime();

    if (path.empty())
    {
        auto str = StringJoin(s_vecSearchPaths, "\n");

        if (s_highestVersionFound.get(0) == 0)
        {
            FatalError("Failed to find dotnet runtime library in the following path: \n%s\n"
                       "Make sure you have dotnet installed. If you are running steamrt3 without a docker, please copy your dotnet runtime to game/sharp/runtime.",
                       str.c_str());
        }
        else
        {
            char found_version_str[32];
            snprintf(found_version_str, sizeof(found_version_str), "%d.%d.%d",
                     s_highestVersionFound.get(0), s_highestVersionFound.get(1), s_highestVersionFound.get(2));

            FatalError("A .NET runtime was found, but the version is too old.\n"
                       "Required major version: %d or newer.\n"
                       "Highest version found: %s\n"
                       "Please install .NET %d or a newer runtime.",
                       MIN_DOTNET_MAJOR_VERSION,
                       found_version_str,
                       MIN_DOTNET_MAJOR_VERSION);
        }
        return false;
    }

    void* lib = load_library(path.c_str());
    if (lib == nullptr)
    {
        FatalError("Failed to load hostfxr library at %s", path.c_str());
        return false;
    }

    s_HostFxrUtils.Init        = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
    s_HostFxrUtils.GetDelegate = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
    s_HostFxrUtils.SetProperty = (hostfxr_set_runtime_property_value_fn)get_export(lib, "hostfxr_set_runtime_property_value");
    s_HostFxrUtils.Close       = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

    return (s_HostFxrUtils.Init != nullptr && s_HostFxrUtils.GetDelegate != nullptr && s_HostFxrUtils.SetProperty != nullptr && s_HostFxrUtils.Close != nullptr);
}

load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t* config_path, const char_t* base_dir)
{
    // Load .NET Core
    void*          result = nullptr;
    hostfxr_handle cxt    = nullptr;
    int            rc     = s_HostFxrUtils.Init(config_path, nullptr, &cxt);
    if (rc != 0 || cxt == nullptr)
    {
        std::cerr << "Init failed: " << std::hex << std::showbase << rc << std::endl;
        s_HostFxrUtils.Close(cxt);
        return nullptr;
    }

    s_HostFxrUtils.SetProperty(cxt, STR("APP_CONTEXT_BASE_DIRECTORY"), base_dir);

    // Get the load assembly function pointer
    rc = s_HostFxrUtils.GetDelegate(
        cxt,
        hdt_load_assembly_and_get_function_pointer,
        &result);

    if (rc != 0 || result == nullptr)
        std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;

    // s_HostFxrUtils.Close(cxt);
    return (load_assembly_and_get_function_pointer_fn)result;
}

static load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;

#ifdef PLATFORM_WINDOWS
std::wstring widen(const std::string& in)
{
    std::wstring out{};

    if (in.length() > 0)
    {
        // Calculate target buffer size (not including the zero terminator).
        const auto len = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, in.c_str(), static_cast<int>(in.size()), nullptr, 0);
        if (len == 0)
        {
            throw std::runtime_error("Invalid character sequence.");
        }

        out.resize(len);
        // No error checking. We already know, that the conversion will succeed.
        MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, in.c_str(), static_cast<int>(in.size()), out.data(), static_cast<int>(out.size()));
        // Use out.data() in place of &out[0] for C++17
    }

    return out;
}
#endif

void* GetDotnetFunctionPointer(const char* typeName, const char* method)
{
    void* pFunc = nullptr;
    int   rc    = load_assembly_and_get_function_pointer(
        STR("../../sharp/core/Sharp.Core.dll"),
#ifdef PLATFORM_WINDOWS
        widen(typeName).c_str(), widen(method).c_str(),
#else
        typeName, method,
#endif
        UNMANAGEDCALLERSONLY_METHOD, nullptr, &pFunc);
    assert(rc == 0 && pFunc != nullptr && "Failure: load_assembly_and_get_function_pointer()");
    return pFunc;
}

template <typename T>
T GetDotnetFunctionPointer(const char* typeName, const char* method)
{
    return reinterpret_cast<T>(GetDotnetFunctionPointer(typeName, method));
}

bool coreclr::Init(const char* baseDir)
{
    std::filesystem::path dir(baseDir);
    dir /= "sharp";

    s_vecSearchPaths.emplace_back((dir / "runtime" / "host" / "fxr").generic_string());

    dir /= "core";

    if (!LoadHostFxr())
    {
        assert(false && "Failure: LoadHostFxr()");
    }

    if (!std::filesystem::exists(dir))
    {
        assert(false && "Failure: directory not found.");
    }

    const auto abs = std::filesystem::absolute(dir);
    const auto str = abs.string();

    std::cout << "AppContext.BaseDirectory = " << str << std::endl;
    std::cout << ".NET runtime version = " << GetDotNetVersion() << std::endl;

    load_assembly_and_get_function_pointer = get_dotnet_load_assembly(STR("../../sharp/core/Sharp.Core.runtimeconfig.json"),
#ifdef PLATFORM_WINDOWS
                                                                      widen(str).c_str()
#else
                                                                      str.c_str()
#endif
    );

    assert(load_assembly_and_get_function_pointer != nullptr && "Failure: get_dotnet_load_assembly()");

    return true;
}

// bool coreclr::CreateNative(const char* name, void* func)
// {
//     using CreateNative_t               = bool (*)(const char*, intptr_t);
//     static CreateNative_t createNative = nullptr;
//     if (createNative == nullptr)
//         createNative = GetDotnetFunctionPointer<CreateNative_t>("Sharp.Core.Bootstrap, Sharp.Core", "CreateNative");
//
//     const auto ret = createNative(name, reinterpret_cast<intptr_t>(func));
//
//     if (!ret)
//     {
// #ifdef PLATFORM_WINDOWS
//         if (IsDebuggerPresent())
//         {
//             DebugBreak();
//         }
// #endif
//     }
//
//     return ret;
// }

void* coreclr::GetManagedFunction(const char* name)
{
    std::string _name(name);
    auto        methodPos    = _name.find_last_of('.');
    auto        assemblyName = _name.substr(0, methodPos);

    char target[512];
    snprintf(target, sizeof(target), "Sharp.Core.%s, Sharp.Core", assemblyName.c_str());
    char methodName[512];
    snprintf(methodName, sizeof(methodName), "%sExport", _name.substr(methodPos + 1).c_str());

    return GetDotnetFunctionPointer(target, methodName);
}

const char* coreclr::GetDotNetVersion()
{
    return s_DotNetVersion;
}

int coreclr::Bootstrap(void* natives, void* forwards)
{
    using sharp_init_fn = int(CORECLR_DELEGATE_CALLTYPE*)(void*, void*);

    sharp_init_fn sharpInit;
    sharpInit = GetDotnetFunctionPointer<sharp_init_fn>("Sharp.Core.Bootstrap, Sharp.Core", "Init");

    return sharpInit(natives, forwards);
}

void coreclr::Shutdown()
{
    using sharp_shutdown_fn = void(CORECLR_DELEGATE_CALLTYPE*)();

    sharp_shutdown_fn sharpShutdown;
    sharpShutdown = GetDotnetFunctionPointer<sharp_shutdown_fn>("Sharp.Core.Bootstrap, Sharp.Core", "Shutdown");

    sharpShutdown();
}
