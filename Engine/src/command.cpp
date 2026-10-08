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

#include "command.h"
#include "CoreCLR/coreclr.h"
#include "bridge/forwards/forward.h"
#include "logging.h"
#include "manager/ConVarManager.h"
#include "netmessage.h"

#include "cstrike/consts.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"

static void CommandPrintConsole(CServerSideClient* pClient, const char* string)
{
    if (pClient)
    {
        UTIL_TextMsgSingle(pClient->GetSlot(), Hud_Console, string);
        return;
    }

    g_bInLoggingFlow = true;
    ConColorMsg({255, 255, 255, 255}, "%s", string);
    g_bInLoggingFlow = false;
}

static void OnModSharpCommand(const CCommandContext& context, const CCommand& command)
{
    CServerSideClient* pClient = nullptr;
    const auto         slot    = context.GetPlayerSlot();
    if (slot >= 0)
    {
        pClient = sv->GetClient(static_cast<uint8_t>(slot));
    }

    if (command.ArgC() <= 1)
    {
        constexpr char credits[] = "  ModSharp " FULL_VERSION_STRING ", by " AUTHORS_STRING "\n"
                                   "To see credits, type \"ms credits\"\n"
                                   "To see version, type \"ms version\"\n"
                                   "To see modules, type \"ms modules\"\n"
                                   "Visit https://modsharp.net/\n\n";

        CommandPrintConsole(pClient, credits);
        return;
    }

    if (strcasecmp(command.Arg(1), "version") == 0)
    {
        static char* buffer = nullptr;
        if (!buffer)
        {
            buffer = static_cast<char*>(malloc(512));
            snprintf(buffer, 512,
                     "  ModSharp Version Information:\n"
                     "    ModSharp Version: %s\n"
                     "    DotNet Version: %s\n"
                     "    Compiled on: %s %s\n"
                     "    Build from: https://github.com/Kxnrl/modsharp-public/commit/%s\n"
                     "    Build ID: %d:%s\n"
                     "    https://modsharp.net/\n\n",
                     VERSION_STRING_BUILD,
                     coreclr::GetDotNetVersion(),
                     __DATE__,
                     __TIME__,
                     COMMIT_HASH,
                     GIT_VERSION,
                     COMMIT_HASH);
        }

        CommandPrintConsole(pClient, buffer);
        return;
    }

    if (strcasecmp(command.Arg(1), "modules") == 0)
    {
        forwards::OnModSharpModulesCommand->Invoke(pClient);
        return;
    }

    constexpr char message[] =
        "      ModSharp " FULL_VERSION_STRING " \n"
        "      " BUILD_DATE "\n"
        "   " COPYRIGHT_STRING "\n"
        "Visit https://modsharp.net/\n\n";

    CommandPrintConsole(pClient, message);
}

void InitialModSharpCommand()
{
    g_ConVarManager.CreateConsoleCommand("ms", OnModSharpCommand, nullptr, FCVAR_CLIENT_CAN_EXECUTE | FCVAR_LINKED_CONCOMMAND | FCVAR_GAMEDLL);
    g_ConVarManager.CreateConsoleCommand("modsharp", OnModSharpCommand, nullptr, FCVAR_CLIENT_CAN_EXECUTE | FCVAR_LINKED_CONCOMMAND | FCVAR_GAMEDLL);
}