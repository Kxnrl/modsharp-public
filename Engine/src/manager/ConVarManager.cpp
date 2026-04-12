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

#include "manager/ConVarManager.h"
#include "bridge/forwards/forward.h"
#include "logging.h"
#include "sdkproxy.h"

#include "cstrike/interface/IMemAlloc.h"
#include "cstrike/type/CNetworkGameServer.h"

/*#include <memalloc.h>*/

ConVarManager g_ConVarManager;

void ConVarManager::CreateConsoleCommand(const char* pName, FnCommandCallback_t callback, const char* pHelpString, int64_t flags, FnCommandCompletionCallback completionFunc)
{
    ConCommandCreation_t creation;
    creation.m_fnCallback.m_fnCommandCallback = callback;
    creation.m_fnCallback.m_bIsInterface      = false;
    creation.m_fnCallback.m_bIsVoidCallback   = false;
    creation.m_fnCallback.m_bIsContextLess    = false;

    creation.m_fnCompletion.m_fnCompletionCallback   = completionFunc ? completionFunc : DefaultCompletionFunc;
    creation.m_fnCompletion.m_bHasCompletionCallback = completionFunc != nullptr;
    creation.m_fnCompletion.m_bIsInterface           = false;

    // Setup the rest
    static auto empty_string = static_cast<char*>(g_pMemAlloc->IndirectAlloc(1));

    // Name should be static data
    AssertPtr(pName);
    const auto dup = MemAlloc_StrDupFunc(pName);

    creation.m_pszName       = dup;
    creation.m_pszHelpString = pHelpString ? MemAlloc_StrDupFunc(pHelpString) : empty_string;

    creation.m_nFlags = flags;

    creation.m_nFlags &= ~FCVAR_DEVELOPMENTONLY;

    const auto handle = icvar->RegisterConCommand(creation, 0);

    m_CreatedCommandStrHandles[dup] = handle;
}

void SharpCommandCallback::CommandCallback(const CCommandContext& context, const CCommand& command)
{
    const char* cmd_string = command.GetCommandString();

    CServerSideClient* client = nullptr;

    if (sv)
    {
        const auto slot = context.GetPlayerSlot();
        if (auto clients = sv->GetClients())
        {
            auto count = clients->Count();
            if (slot >= 0 && slot < count)
            {
                client = clients->Element(slot);
            }
            else if (slot != -1)
            {
                WARN("Invalid player slot #%d detected for command \"%s\"", slot, cmd_string);
            }
        }
    }

    uint64_t handle = 0;
    if (m_Handle.IsValid())
    {
        handle = (static_cast<uint64_t>(m_Handle.GetIndex()) << 32) | static_cast<uint64_t>(m_Handle.GetConCommandIndex());
    }

    forwards::OnConCommandTrigger->Invoke(static_cast<int64_t>(handle), client, cmd_string);
}

static uint64_t g_iFlagsToRemove = (FCVAR_HIDDEN | FCVAR_DEVELOPMENTONLY);

ConCommandHandle ConVarManager::CreateSharpConsoleCommand(const char* pName, const char* pHelpString, int64_t flags)
{
    auto cb = new SharpCommandCallback();

    ConCommandCreation_t creation;
    creation.m_fnCallback.m_pCommandCallback = cb;
    creation.m_fnCallback.m_bIsInterface     = true;
    creation.m_fnCallback.m_bIsVoidCallback  = false;
    creation.m_fnCallback.m_bIsContextLess   = false;

    creation.m_fnCompletion.m_fnCompletionCallback   = DefaultCompletionFunc;
    creation.m_fnCompletion.m_bHasCompletionCallback = false;
    creation.m_fnCompletion.m_bIsInterface           = true;

    // Setup the rest
    static auto empty_string = static_cast<char*>(g_pMemAlloc->IndirectAlloc(1));

    // Name should be static data
    AssertPtr(pName);

    const auto dup = MemAlloc_StrDupFunc(pName);

    creation.m_pszName       = dup;
    creation.m_pszHelpString = pHelpString ? MemAlloc_StrDupFunc(pHelpString) : empty_string;

    creation.m_nFlags = flags;

    creation.m_nFlags &= ~FCVAR_DEVELOPMENTONLY;

    const auto handle = icvar->RegisterConCommand(creation, 0);

    cb->SetHandle(handle);
    m_CreatedCommandStrHandles[dup]     = handle;
    m_CreatedSharpCommandCallbacks[dup] = cb;

    return handle;
}

bool ConVarManager::ReleaseCommand(const char* name)
{
    if (!m_CreatedCommandStrHandles.contains(name))
    {
        return false;
    }

    icvar->UnregisterConCommand(m_CreatedCommandStrHandles[name]);
    m_CreatedCommandStrHandles.erase(name);

    if (m_CreatedSharpCommandCallbacks.contains(name))
    {
        const auto cb = m_CreatedSharpCommandCallbacks[name];
        delete cb;
        m_CreatedSharpCommandCallbacks.erase(name);
    }

    return true;
}