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

#ifdef PLATFORM_WINDOWS
#    ifndef WIN32_LEAN_AND_MEAN
#        define WIN32_LEAN_AND_MEAN
#    endif
#    include <windows.h>
#    define LIB_FILE_EXTENSION ".dll"
#    define MS_EXPORT extern "C" __declspec(dllexport)
using LibModule = HMODULE;
#elif PLATFORM_POSIX

#    include <dlfcn.h>
#    include <sys/mman.h>
#    include <unistd.h>
#    define LIB_FILE_EXTENSION ".so"
#    define MS_EXPORT extern "C" __attribute__((visibility("default")))

int vtablehook_pagesize = sysconf(_SC_PAGE_SIZE);
int vtablehook_pagemask = ~(vtablehook_pagesize - 1);
using LibModule         = void*;
#else
#    error "Unsupported platform"
#endif

#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>

// https://github.com/Thordin/vtable-hook
// License: MIT

static unsigned int vtablehook_unprotect(void* region)
{
#ifdef PLATFORM_WINDOWS
    MEMORY_BASIC_INFORMATION mbi;
    VirtualQuery((LPCVOID)region, &mbi, sizeof(mbi));
    VirtualProtect(mbi.BaseAddress, mbi.RegionSize, PAGE_READWRITE, &mbi.Protect);
    return mbi.Protect;
#elif PLATFORM_POSIX
    mprotect((void*)((intptr_t)region & vtablehook_pagemask), vtablehook_pagesize, PROT_READ | PROT_WRITE | PROT_EXEC);
    return PROT_READ | PROT_EXEC;
#endif
}

static void vtablehook_protect(void* region, unsigned int protection)
{
#ifdef PLATFORM_WINDOWS
    MEMORY_BASIC_INFORMATION mbi;
    VirtualQuery((LPCVOID)region, &mbi, sizeof(mbi));
    VirtualProtect(mbi.BaseAddress, mbi.RegionSize, protection, &mbi.Protect);
#elif PLATFORM_POSIX
    mprotect((void*)((intptr_t)region & vtablehook_pagemask), vtablehook_pagesize, protection);
#endif
}

static void* vtablehook_hook(void* instance, void* hook, int offset)
{
    const auto vtable   = *reinterpret_cast<intptr_t*>(instance);
    const auto entry    = vtable + sizeof(intptr_t) * offset;
    const auto original = *reinterpret_cast<intptr_t*>(entry);

    const auto original_protection      = vtablehook_unprotect(reinterpret_cast<void*>(entry));
    *reinterpret_cast<intptr_t*>(entry) = reinterpret_cast<intptr_t>(hook);
    vtablehook_protect(reinterpret_cast<void*>(entry), original_protection);

    return reinterpret_cast<void*>(original);
}

#if defined WIN32
static void ms_GetPlatformError(char* buffer, size_t maxlength)
{
    const auto dw = GetLastError();
    FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, nullptr, dw, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)buffer, static_cast<DWORD>(maxlength), nullptr);
}
#endif

static LibModule ms_LoadLibrary(const char* path, char* buffer, size_t maxlength)
{
    LibModule lib;

#if defined PLATFORM_WINDOWS
    lib = LoadLibraryA(path);

    if (lib == nullptr)
    {
        ms_GetPlatformError(buffer, maxlength);
        return nullptr;
    }
#elif defined PLATFORM_POSIX
    lib = static_cast<LibModule>(dlopen(path, RTLD_NOW));

    if (lib == nullptr)
    {
        snprintf(buffer, maxlength, "%s", dlerror());
        return nullptr;
    }
#endif

    return lib;
}

static void* ms_GetSymbolAddress(LibModule lib, const char* name)
{
#ifdef PLATFORM_WINDOWS
    return reinterpret_cast<void*>(GetProcAddress(lib, name));
#elif defined PLATFORM_POSIX
    return dlsym(lib, name);
#endif
}

static void ms_UnloadLibrary(LibModule lib)
{
#if defined PLATFORM_WINDOWS
    FreeLibrary(lib);
#elif defined __linux__ || defined __APPLE__
    dlclose(lib);
#endif
}

static uint8_t* ResolveRelativeInstructionPointer(uint8_t* pAddress, uint32_t rva, uint32_t rip)
{
    if (!pAddress || !rva || !rip)
    {
        return nullptr;
    }

    return reinterpret_cast<std::uint8_t*>(*reinterpret_cast<std::uint32_t*>(pAddress + rva) + reinterpret_cast<std::uint64_t>(pAddress) + rip);
}

LibModule g_pGameServer = nullptr;

using CreateInterface_t         = void* (*)(const char* name, int* ret);
CreateInterface_t serverFactory = nullptr;
CreateInterface_t engineFactory = nullptr;

using Source2ServerConfig_Connect_t = bool (*)(void*, CreateInterface_t factory);
using Source2Server_Init_t          = void* (*)(void*, const char* pInterfaceName);
using Source2Server_Shutdown_t      = void (*)(void*);
Source2ServerConfig_Connect_t g_pSource2ServerConfig_Connect;
Source2Server_Init_t          g_pSource2Server_Init;
Source2Server_Shutdown_t      g_pSource2Server_Shutdown;

static bool Source2ServerConfig_Connect(void* pThis, CreateInterface_t factory)
{
    engineFactory = factory;

    const auto ret = g_pSource2ServerConfig_Connect(pThis, factory);

    vtablehook_hook(pThis, reinterpret_cast<void*>(g_pSource2ServerConfig_Connect), 0);

    return ret;
}

class ModsharpModule
{
public:
    ModsharpModule() :
        m_Init(nullptr), m_Shutdown(nullptr), m_pModule(nullptr) {}

    bool Load()
    {
        char error[256];
#ifdef PLATFORM_WINDOWS
        m_pModule = ms_LoadLibrary("../../sharp/bin/modsharp.dll", error, sizeof(error));
#else
        m_pModule = ms_LoadLibrary("../../sharp/bin/libmodsharp.so", error, sizeof(error));
#endif

        if (!m_pModule)
        {
            printf("Error: %s\n", error);
            return false;
        }

        m_Init     = reinterpret_cast<decltype(m_Init)>(ms_GetSymbolAddress(m_pModule, "InitModsharp"));
        m_Shutdown = reinterpret_cast<decltype(m_Shutdown)>(ms_GetSymbolAddress(m_pModule, "ShutdownModsharp"));

        if (!m_Init || !m_Shutdown)
        {
            return false;
        }

        return true;
    }

    void Unload() const { ms_UnloadLibrary(m_pModule); }
    bool Init(CreateInterface_t engine, CreateInterface_t server) const { return m_Init(engine, server); }
    void Shutdown() const { return m_Shutdown(); }

private:
    bool (*m_Init)(CreateInterface_t engineFactory, CreateInterface_t serverFactory);
    void (*m_Shutdown)();
    LibModule m_pModule;
} g_ModsharpModule;

#ifndef PLATFORM_WINDOWS
static void LoadBreakpad()
{
    char error[256];
    auto handle = ms_LoadLibrary("../../sharp/bin/libaccelerator.so", error, sizeof(error));
    if (!handle)
    {
        printf("Failed to load accelerator/breakpad: %s\n", error);
        return;
    }

    using fn = bool (*)();

    auto func = reinterpret_cast<fn>(ms_GetSymbolAddress(handle, "InitBreakpad"));
    if (!func())
    {
        printf("Failed to initialize breakpad\n");
    }
}
#endif

static void Source2Server_Shutdown(void* pThis)
{
    printf("[Loader] Source2Server_Shutdown\n");

    g_ModsharpModule.Shutdown();
    g_ModsharpModule.Unload();

    g_pSource2Server_Shutdown(pThis);
}

static void* Source2Server_Init(void* pThis, const char* pInterfaceName)
{
    const auto ret = g_pSource2Server_Init(pThis, pInterfaceName);

    vtablehook_hook(pThis, reinterpret_cast<void*>(g_pSource2Server_Init), 3);

    printf("[Loader] Source2Server_Init\n");

    if (!g_ModsharpModule.Load())
    {
        printf("Failed to load modsharp module!\n");
        return ret;
    }

#ifndef PLATFORM_WINDOWS
    LoadBreakpad();
#endif

    g_ModsharpModule.Init(engineFactory, serverFactory);

    g_pSource2Server_Shutdown = reinterpret_cast<Source2Server_Shutdown_t>(vtablehook_hook(pThis, reinterpret_cast<void*>(Source2Server_Shutdown), 5));

    return ret;
}

MS_EXPORT void* CreateInterface(const char* name, int* ret)
{
    printf("[Loader] CreateInterface(%s)\n", name);

    if (serverFactory == nullptr)
    {
        char error[256];
#ifdef PLATFORM_WINDOWS
        g_pGameServer = ms_LoadLibrary("../../csgo/bin/win64/server.dll", error, sizeof(error));
#else
        g_pGameServer = ms_LoadLibrary("../../csgo/bin/linuxsteamrt64/libserver.so", error, sizeof(error));
#endif
        serverFactory = reinterpret_cast<CreateInterface_t>(ms_GetSymbolAddress(g_pGameServer, "CreateInterface"));
    }

    if (strncmp(name, "Source2ServerConfig", 19) == 0)
    {
        const auto ptr = serverFactory(name, ret);

        g_pSource2ServerConfig_Connect = reinterpret_cast<Source2ServerConfig_Connect_t>(vtablehook_hook(ptr, reinterpret_cast<void*>(Source2ServerConfig_Connect), 0));

        if (ret != nullptr)
            *ret = 0;

        return ptr;
    }

    if (strncmp(name, "Source2Server", 13) == 0 && atoi(&name[13]) != 0)
    {
        const auto ptr = serverFactory(name, ret);

        g_pSource2Server_Init = reinterpret_cast<Source2Server_Init_t>(vtablehook_hook(ptr, reinterpret_cast<void*>(Source2Server_Init), 3));
        if (ret != nullptr)
            *ret = 0;

        return ptr;
    }

    return serverFactory(name, ret);
}
