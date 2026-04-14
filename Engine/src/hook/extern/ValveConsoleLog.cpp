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

#include "global.h"
#include "hook/installer.h"
#include "logging.h"
#include "manager/ConVarManager.h"

#include "cstrike/interface/ICvar.h"

#include <safetyhook.hpp>

enum class BlockValveLog : int
{
    Default  = 0, // Default
    AllowAll = 1, // Allow all
    BlockAll = 2  // Block all
};

static CConVarBaseData* ms_block_valve_log = nullptr;

BeginStaticHookScope(LogDirect)
{
    DeclareStaticDetourHook(LogDirect, int, (void* loggingSystem, int channel, int severity, void* leafCode, char const* str, va_list* args))
    {
        if (g_bInLoggingFlow)
        {
            goto endCall;
        }

        if (ms_block_valve_log->GetValue<int>() == static_cast<int>(BlockValveLog::BlockAll))
        {
            return 0;
        }

    endCall:
        return LogDirect(loggingSystem, channel, severity, leafCode, str, args);
    }
}

// PLATFORM_INTERFACE bool LoggingSystem_IsChannelEnabled( LoggingChannelID_t channelID, LoggingSeverity_t severity );
BeginStaticHookScope(LoggingSystem_IsChannelEnabled)
{
    DeclareStaticDetourHook(LoggingSystem_IsChannelEnabled, bool, (int channelID, int severity))
    {
        if (g_bInLoggingFlow)
            return true;

        switch (ms_block_valve_log->GetValue<int>())
        {
        case 2: return false;
        case 1: return true;
        default: return LoggingSystem_IsChannelEnabled(channelID, severity);
        }
    }
}

// PLATFORM_OVERLOAD bool LoggingSystem_IsChannelEnabled( LoggingChannelID_t channelID, LoggingVerbosity_t verbosity );
BeginStaticHookScope(LoggingSystem_IsChannelEnabled_Overload)
{
    DeclareStaticDetourHook(LoggingSystem_IsChannelEnabled_Overload, bool, (int channelID, int verbosity))
    {
        if (g_bInLoggingFlow)
            return true;

        switch (ms_block_valve_log->GetValue<int>())
        {
        case 2: return false;
        case 1: return true;
        default: return LoggingSystem_IsChannelEnabled_Overload(channelID, verbosity);
        }
    }
}

BeginMemberHookScope(CVProfile)
{
    DeclareMemberDetourHook(OutputReport, void, (void* pVProfile, void* pReportSetting, const char* unknown1, int32_t unknown2))
    {
        g_bInLoggingFlow = true;
        OutputReport(pVProfile, pReportSetting, unknown1, unknown2);
        g_bInLoggingFlow = false;
    }
}

void InstallValveConsoleLog()
{
    ms_block_valve_log = g_ConVarManager.CreateConVar(
        "ms_block_valve_log", IsDebugMode() ? 1 : 2,
        "Block valve's logging system. Default is \"false\". [0 = Default, 1 = Allow All, 2 = Block All]",
        FCVAR_RELEASE);

    SHOOK(LogDirect);
    SHOOK(LoggingSystem_IsChannelEnabled);
    SHOOK(LoggingSystem_IsChannelEnabled_Overload);
    HOOK(CVProfile, OutputReport);
}
