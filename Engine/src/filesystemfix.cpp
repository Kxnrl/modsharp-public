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

#include "filesystemfix.h"
#include "global.h"
#include "manager/HookManager.h"
#include "sdkproxy.h"

#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/IFileSystem.h"
#include "cstrike/type/CBufferString.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/CUtlVector.h"

#include <string>

// #define ASSERT_FS_LOG

void FixFileSystem()
{
    static constexpr const char* pathIds[] = {
        "ADDONS",
        "CONTENT",
        "CONTENTADDONS",
        "CONTENTROOT",
        "EXECUTABLE_PATH",
        "GAME",
        "GAMEBIN",
        "GAMEROOT",
        "MOD",
        "PLATFORM",
        "SHADER_SOURCE",
        "SHADER_SOURCE_MOD",
        "SHADER_SOURCE_ROOT"};

    const auto enableDualAddon = CommandLine()->HasParam("-dual_addon");
    auto       hasReplaceValue = false;

    std::string assets_paths{};

#ifdef ASSERT_FS_LOG
    g_pFullFileSystem->PrintSearchPaths();
    Msg("\n\n\n----------------------------------------------\n\n\n");
#endif

    std::string_view game_dir = Plat_GetGameDirectory();

    for (auto& pathId : pathIds)
    {
        CUtlVector<CUtlString> search_paths;
        g_pFullFileSystem->GetSearchPathsForPathID(pathId, static_cast<GetSearchPathTypes_t>(0), &search_paths);

        for (auto i = 0; i < search_paths.Count(); i++)
        {
            const auto&            search_path    = search_paths[i];
            const std::string_view search_path_sv = search_path.Get();

            if (!search_path_sv.starts_with(game_dir) || search_path_sv.length() <= game_dir.length()) continue;

            std::string_view path = search_path_sv.substr(game_dir.size() + 1);
            if (path.empty()) continue;

            auto first_slash = path.find_first_of('/');
            if (first_slash == std::string_view::npos) first_slash = path.find_first_of('\\');

            std::string_view directory_name = (first_slash != std::string_view::npos) ? path.substr(0, first_slash) : path;
            if (directory_name.empty()) continue;

            if (directory_name == "sharp")
            {
                g_pFullFileSystem->RemoveSearchPath(search_path.Get(), pathId);

                if (enableDualAddon)
                {
                    if (strcasecmp(pathId, "game") == 0)
                    {
                        assets_paths = search_path_sv;
                        assets_paths += "assets\\";
                        
                        hasReplaceValue = true;
                    }
                }
            }
        }
    }

#ifdef ASSERT_FS_LOG
    g_pFullFileSystem->PrintSearchPaths();
    Msg("\n\n\n----------------------------------------------\n\n\n");
#endif

    g_pFullFileSystem->RemoveSearchPaths("DEFAULT_WRITE_PATH");

    CFixedBufferString<260> searchPath;
    g_pFullFileSystem->GetSearchPath("GAME", static_cast<GetSearchPathTypes_t>(0), &searchPath, 1);
    g_pFullFileSystem->AddSearchPath(searchPath.Get(), "DEFAULT_WRITE_PATH");

    if (hasReplaceValue && !assets_paths.empty())
    {
        g_pFullFileSystem->AddSearchPath(assets_paths.c_str(), "GAME");
    }

#ifdef ASSERT_FS_LOG
    g_pFullFileSystem->PrintSearchPaths();
#endif

#ifdef ASSERT_FS_LOG
    g_pHookManager->Hook_LevelInit(HookType_Post, []() {
        g_pFullFileSystem->PrintSearchPaths();
    });
#endif

    g_pHookManager->Hook_ServerInit(HookType_Post, []() {
        FixAddonEmptyPath();
    });
}

void FixAddonEmptyPath()
{
    const char* pathIds[] = {"DEFAULT_WRITE_PATH", "GAME"};

#ifdef ASSERT_FS_LOG
    g_pFullFileSystem->PrintSearchPaths();
    Msg("\n\n\n----------------------------------------------\n\n\n");
#endif

    for (auto& pathId : pathIds)
    {
        CUtlVector<CUtlString> searchPaths;
        g_pFullFileSystem->GetSearchPathsForPathID(pathId, static_cast<GetSearchPathTypes_t>(0), &searchPaths);

        for (auto i = 0; i < searchPaths.Count(); i++)
        {
            if (strcmp(searchPaths[i].Get(), "") == 0 && strlen(searchPaths[i].Get()) == 0)
            {
                g_pFullFileSystem->RemoveSearchPath(searchPaths[i].Get(), pathId);

#ifdef ASSERT_FS_LOG
                Msg("Remove Empty Search Path -> %s\n", pathId);
#endif
            }
        }
    }

#ifdef ASSERT_FS_LOG
    g_pFullFileSystem->PrintSearchPaths();
#endif
}