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
#include "gamefactory.h"
#include "logging.h"
#include "manager/HookManager.h"
#include "sdkproxy.h"
#include "strtool.h"
#include "types.h"
#include "hook/installer.h"

#include <safetyhook.hpp>

#include <cassert>

extern bool ModSharp_Init();
extern void ModSharp_Shutdown();

MS_EXPORT void ShutdownModsharp()
{
    char timestamp[64];
    GetTimeFormatString(timestamp, sizeof(timestamp));

    ConsoleText(Color(255, 255, 255), "[%s]", timestamp);
    ConsoleText(Color(35, 132, 239), " ModSharp");
    ConsoleText(Color(116, 212, 61), " [info]");
    ConsoleText(Color(255, 255, 255), ": Shutdown!\n");

    ModSharp_Shutdown();
}

BeginStaticHookScope(Source2_Init)
{
    DeclareStaticDetourHook(Source2_Init, bool, (void* p))
    {
        auto result = Source2_Init(p);

        if (!ModSharp_Init())
            FatalError("Failed to init ModSharp!");

        char timestamp[64];
        GetTimeFormatString(timestamp, sizeof(timestamp));

        ConsoleText(Color(255, 255, 255), "[%s]", timestamp);
        ConsoleText(Color(35, 132, 239), " ModSharp");
        ConsoleText(Color(116, 212, 61), " [info]");
        ConsoleText(Color(255, 255, 255), ": Bootstrap!\n");

        return result;
    }
}

MS_EXPORT bool InitModsharp(CreateInterface_t engineFactory, CreateInterface_t serverFactory)
{
#ifdef PLATFORM_LINUX
    breakpad::Init();
#endif

    factory::engine     = new GameFactory(engineFactory);
    factory::physics    = new GameFactory(engineFactory);
    factory::filesystem = new GameFactory(engineFactory);
    factory::server     = new GameFactory(serverFactory);

    if (!address::Initialize())
    {
        if (Plat_IsInDebugSession())
        {
#ifdef PLATFORM_WINDOWS
            __debugbreak();
#endif
        }
        else
        {
            FatalError("Failed to initialize address!");
        }

        return false;
    }

    SHOOK(Source2_Init, {.address = (address::engine::Source2_Init)});

    return true;
}