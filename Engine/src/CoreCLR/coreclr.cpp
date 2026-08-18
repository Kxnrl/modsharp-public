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

#ifndef PLATFORM_WINDOWS
#    ifndef _GNU_SOURCE
#        define _GNU_SOURCE
#    endif
#endif

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
#    include <link.h>
#    define STR(s) s
#    include <safetyhook.hpp>
#endif

#include "logging.h"
#include "strtool.h"

#include <algorithm>
#include <array>
#include <cassert>
#include <charconv>
#include <cstdlib>
#include <cstring>
#include <filesystem>
#include <iostream>
#include <new>
#include <optional>
#include <ranges>
#include <string>
#include <string_view>
#include <vector>

struct HostFxrUtils
{
    hostfxr_initialize_for_runtime_config_fn Init;
    hostfxr_get_runtime_delegate_fn          GetDelegate;
    hostfxr_get_runtime_property_value_fn    GetProperty;
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

        for (auto subrange : input | std::views::split('.'))
        {
            // 应该不会遇到这个情况
            if (count >= 4)
                break;

            std::string_view token(subrange.begin(), subrange.end());
            
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
#ifdef PLATFORM_WINDOWS
        FatalError("Failed to load hostfxr library at %s", path.c_str());
#else
        const char* error = dlerror();
        FatalError("Failed to load hostfxr library at %s: %s",
                   path.c_str(), error != nullptr ? error : "unknown error");
#endif
        return false;
    }

    s_HostFxrUtils.Init        = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
    s_HostFxrUtils.GetDelegate = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
    s_HostFxrUtils.GetProperty = (hostfxr_get_runtime_property_value_fn)get_export(lib, "hostfxr_get_runtime_property_value");
    s_HostFxrUtils.SetProperty = (hostfxr_set_runtime_property_value_fn)get_export(lib, "hostfxr_set_runtime_property_value");
    s_HostFxrUtils.Close       = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

    return (s_HostFxrUtils.Init != nullptr && s_HostFxrUtils.GetDelegate != nullptr && s_HostFxrUtils.GetProperty != nullptr
            && s_HostFxrUtils.SetProperty != nullptr && s_HostFxrUtils.Close != nullptr);
}

#ifndef PLATFORM_WINDOWS
namespace
{
using SystemOperatorNewFn        = void* (*)(std::size_t);
using SystemAlignedOperatorNewFn = void* (*)(std::size_t, std::align_val_t);

SystemOperatorNewFn        s_SystemOperatorNew        = nullptr;
SystemAlignedOperatorNewFn s_SystemAlignedOperatorNew = nullptr;

void* CoreClrOperatorNew(std::size_t size)
{
    return s_SystemOperatorNew(size);
}

void* CoreClrOperatorNewNoThrow(std::size_t size, const std::nothrow_t&) noexcept
{
    try
    {
        return s_SystemOperatorNew(size);
    }
    catch (...)
    {
        return nullptr;
    }
}

void* CoreClrAlignedOperatorNew(std::size_t size, std::align_val_t alignment)
{
    return s_SystemAlignedOperatorNew(size, alignment);
}

void* CoreClrAlignedOperatorNewNoThrow(std::size_t size, std::align_val_t alignment, const std::nothrow_t&) noexcept
{
    try
    {
        return s_SystemAlignedOperatorNew(size, alignment);
    }
    catch (...)
    {
        return nullptr;
    }
}

void CoreClrOperatorDelete(void* memory) noexcept
{
    std::free(memory);
}

bool IsCppAllocatorImport(std::string_view name)
{
    return name.starts_with("_Znwm") || name.starts_with("_Znam")
        || name.starts_with("_ZdlPv") || name.starts_with("_ZdaPv");
}

void* ResolveCoreClrAllocator(std::string_view name, void* libstdcpp)
{
    // libstdc++ overloads forward through interposable GOT slots, so CoreCLR must target leaf wrappers.
    if (name.starts_with("_ZdlPv") || name.starts_with("_ZdaPv"))
        return reinterpret_cast<void*>(&CoreClrOperatorDelete);

    if (!name.starts_with("_Znwm") && !name.starts_with("_Znam"))
        return nullptr;

    const auto suffix = name.substr(5);
    if (suffix.empty())
        return reinterpret_cast<void*>(&CoreClrOperatorNew);
    if (suffix == "RKSt9nothrow_t")
        return reinterpret_cast<void*>(&CoreClrOperatorNewNoThrow);

    const bool aligned = suffix == "St11align_val_t" || suffix == "St11align_val_tRKSt9nothrow_t";
    if (!aligned)
        return nullptr;

    if (s_SystemAlignedOperatorNew == nullptr)
    {
        s_SystemAlignedOperatorNew = reinterpret_cast<SystemAlignedOperatorNewFn>(dlsym(libstdcpp, "_ZnwmSt11align_val_t"));
        if (s_SystemAlignedOperatorNew == nullptr)
            return nullptr;
    }

    return suffix == "St11align_val_t"
        ? reinterpret_cast<void*>(&CoreClrAlignedOperatorNew)
        : reinterpret_cast<void*>(&CoreClrAlignedOperatorNewNoThrow);
}

struct LoadedElfImage
{
    ElfW(Addr)        BaseAddress{};
    const ElfW(Phdr)* ProgramHeaders{};
    ElfW(Half)        ProgramHeaderCount{};
};

int FindCoreClrImage(dl_phdr_info* info, std::size_t, void* data)
{
    const char* path = info->dlpi_name;
    if (path == nullptr)
        return 0;

    const char* filename = std::strrchr(path, '/');
    filename             = filename != nullptr ? filename + 1 : path;
    if (std::strcmp(filename, "libcoreclr.so") != 0)
        return 0;

    auto* image                 = static_cast<LoadedElfImage*>(data);
    image->BaseAddress          = info->dlpi_addr;
    image->ProgramHeaders       = info->dlpi_phdr;
    image->ProgramHeaderCount   = info->dlpi_phnum;
    return 1;
}

bool RebindCoreClrAllocatorImports()
{
    LoadedElfImage image{};
    dl_iterate_phdr(FindCoreClrImage, &image);
    if (image.ProgramHeaders == nullptr)
        return false;

    const ElfW(Dyn)* dynamic = nullptr;
    for (ElfW(Half) i = 0; i < image.ProgramHeaderCount; ++i)
    {
        if (image.ProgramHeaders[i].p_type == PT_DYNAMIC)
        {
            dynamic = reinterpret_cast<const ElfW(Dyn)*>(image.BaseAddress + image.ProgramHeaders[i].p_vaddr);
            break;
        }
    }

    if (dynamic == nullptr)
        return false;

    const ElfW(Rela)* dynamicRelocations     = nullptr;
    std::size_t       dynamicRelocationsSize = 0;
    const ElfW(Rela)* pltRelocations         = nullptr;
    std::size_t       pltRelocationsSize     = 0;
    const ElfW(Sym)*  symbols                = nullptr;
    const char*        strings                = nullptr;
    ElfW(Sword)        pltRelocationType      = DT_NULL;

    for (const ElfW(Dyn)* entry = dynamic; entry->d_tag != DT_NULL; ++entry)
    {
        switch (entry->d_tag)
        {
            case DT_JMPREL:
                pltRelocations = reinterpret_cast<const ElfW(Rela)*>(entry->d_un.d_ptr);
                break;
            case DT_PLTRELSZ:
                pltRelocationsSize = entry->d_un.d_val;
                break;
            case DT_RELA:
                dynamicRelocations = reinterpret_cast<const ElfW(Rela)*>(entry->d_un.d_ptr);
                break;
            case DT_RELASZ:
                dynamicRelocationsSize = entry->d_un.d_val;
                break;
            case DT_SYMTAB:
                symbols = reinterpret_cast<const ElfW(Sym)*>(entry->d_un.d_ptr);
                break;
            case DT_STRTAB:
                strings = reinterpret_cast<const char*>(entry->d_un.d_ptr);
                break;
            case DT_PLTREL:
                pltRelocationType = entry->d_un.d_val;
                break;
            default:
                break;
        }
    }

    const bool hasDynamicRelocations = dynamicRelocations != nullptr && dynamicRelocationsSize != 0;
    const bool hasPltRelocations     = pltRelocations != nullptr && pltRelocationsSize != 0 && pltRelocationType == DT_RELA;
    if ((!hasDynamicRelocations && !hasPltRelocations) || symbols == nullptr || strings == nullptr)
        return false;

    const char* libstdcppName = nullptr;
    for (const ElfW(Dyn)* entry = dynamic; entry->d_tag != DT_NULL; ++entry)
    {
        if (entry->d_tag != DT_NEEDED)
            continue;

        const char*                neededLibrary   = strings + entry->d_un.d_val;
        constexpr std::string_view libstdcppPrefix = "libstdc++.so.";
        if (std::string_view{neededLibrary}.starts_with(libstdcppPrefix))
        {
            libstdcppName = neededLibrary;
            break;
        }
    }

    if (libstdcppName == nullptr)
        return false;

    void* libstdcpp = dlopen(libstdcppName, RTLD_NOW | RTLD_LOCAL);
    if (libstdcpp == nullptr)
        return false;

    s_SystemOperatorNew = reinterpret_cast<SystemOperatorNewFn>(dlsym(libstdcpp, "_Znwm"));
    if (s_SystemOperatorNew == nullptr)
        return false;

    std::size_t reboundAllocatorCount = 0;

    const auto applyRelocations = [&](const ElfW(Rela)* relocations, std::size_t size) {
        const std::size_t relocationCount = size / sizeof(ElfW(Rela));
        for (std::size_t i = 0; i < relocationCount; ++i)
        {
            const auto& relocation = relocations[i];
            const auto  type       = ELF64_R_TYPE(relocation.r_info);
            if (type != R_X86_64_GLOB_DAT && type != R_X86_64_JUMP_SLOT)
                continue;

            const auto symbolIndex = ELF64_R_SYM(relocation.r_info);
            const char* symbolName = strings + symbols[symbolIndex].st_name;
            if (!IsCppAllocatorImport(symbolName))
                continue;

            void* replacement = ResolveCoreClrAllocator(symbolName, libstdcpp);
            if (replacement == nullptr)
                return false;

            auto** slot       = reinterpret_cast<void**>(image.BaseAddress + relocation.r_offset);
            auto   protection = safetyhook::unprotect(reinterpret_cast<std::uint8_t*>(slot), sizeof(*slot));
            if (!protection.has_value())
                return false;

            *slot = replacement;
            ++reboundAllocatorCount;
        }

        return true;
    };

    if (hasDynamicRelocations && !applyRelocations(dynamicRelocations, dynamicRelocationsSize))
        return false;
    if (hasPltRelocations && !applyRelocations(pltRelocations, pltRelocationsSize))
        return false;

    return reboundAllocatorCount != 0;
}

void* s_CoreClrLibrary = nullptr;

bool PreloadAndRebindCoreClr(hostfxr_handle context)
{
    const char_t* fxDepsFile = nullptr;
    const int     rc         = s_HostFxrUtils.GetProperty(context, STR("FX_DEPS_FILE"), &fxDepsFile);
    if (rc != 0 || fxDepsFile == nullptr)
        return false;

    const auto coreClrPath = std::filesystem::path(fxDepsFile).parent_path() / "libcoreclr.so";
    s_CoreClrLibrary       = dlopen(coreClrPath.c_str(), RTLD_NOW | RTLD_LOCAL);
    return s_CoreClrLibrary != nullptr && RebindCoreClrAllocatorImports();
}
} // namespace
#endif

load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t* config_path, const char_t* base_dir)
{
    // Load .NET Core
    void*          result = nullptr;
    hostfxr_handle cxt    = nullptr;
    int            rc     = s_HostFxrUtils.Init(config_path, nullptr, &cxt);
    if (rc < 0 || cxt == nullptr)
    {
        std::cerr << "Init failed: " << std::hex << std::showbase << rc << std::endl;
        s_HostFxrUtils.Close(cxt);
        return nullptr;
    }

    s_HostFxrUtils.SetProperty(cxt, STR("APP_CONTEXT_BASE_DIRECTORY"), base_dir);

#ifndef PLATFORM_WINDOWS
    if (!PreloadAndRebindCoreClr(cxt))
    {
        const char* error = dlerror();
        FatalError("Failed to preload CoreCLR and bind its allocator imports: %s", error != nullptr ? error : "unknown error");
        return nullptr;
    }

    std::cout << "CoreCLR allocator imports bound to the system allocator." << std::endl;
#endif

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
