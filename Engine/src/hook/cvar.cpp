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

#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"
#include "global.h"
#include "logging.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "netmessage.h"

#include "CoreCLR/Nullable.h"

#include "cstrike/interface/ICvar.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/CServerSideClient.h"

#include <vector>

namespace natives::cvar
{
static void OnConVarChanged(CConVarBaseData* pConVar);
}

static void ConVarGlobalChanged(BaseConVar* cvar, int32_t splitScreenSlot, const char* pNewValue, const char* pOldValue)
{
    // auto val = icvar->GetConVarHandleValue(cvar->m_pConVar->GetName());
    // auto var = icvar->FindVar(cvar->m_pConVar->GetName());

    natives::cvar::OnConVarChanged(cvar->m_ConVarData);
}

static void InstallConVarHooks()
{
    icvar->InstallGlobalChangeCallback(ConVarGlobalChanged);
}

namespace natives::cvar
{

static std::vector<CConVarBaseData*> s_hookedConVar;

static bool InstallConVarHook(CConVarBaseData* pVar)
{
    if (pVar == nullptr)
        return false;

    for (const auto& hooked : s_hookedConVar)
    {
        if (hooked == pVar)
        {
            WARN("Attempt to hook ConVar '%s', but it was already in hook!\n", pVar->GetName());
            return false;
        }
    }

    s_hookedConVar.push_back(pVar);
    return true;
}

static bool RemoveConVarHook(CConVarBaseData* pVar)
{
    if (pVar == nullptr)
        return false;

    for (auto it = s_hookedConVar.begin(); it != s_hookedConVar.end(); ++it)
    {
        if (*it == pVar)
        {
            s_hookedConVar.erase(it);
            return true;
        }
    }

    WARN("Attempt to remove hook ConVar '%s', but it was not in hook!\n", pVar->GetName());
    return false;
}

static void OnConVarChanged(CConVarBaseData* pConVar)
{
    for (const auto& hooked : s_hookedConVar)
    {
        if (hooked == pConVar)
        {
            forwards::OnConVarChanged->Invoke(pConVar);
            break;
        }
    }
}

static CConVarBaseData* CreateConVar(const char*          name,
                                     const CVValue_t*     defaultValue,
                                     const char*          helpString,
                                     EConVarType          type,
                                     int                  flags,
                                     Nullable<CVValue_t>* min,
                                     Nullable<CVValue_t>* max)
{
    if (type == EConVarType_Bool)
    {
        return g_ConVarManager.CreateConVar<bool>(name, defaultValue->m_bValue, helpString, flags);
    }

    if (type == EConVarType_Int16)
    {
        return g_ConVarManager.CreateConVar<int16_t>(name, defaultValue->m_i16Value, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                     helpString,
                                                     flags);
    }

    if (type == EConVarType_UInt16)
    {
        return g_ConVarManager.CreateConVar<uint16_t>(name, defaultValue->m_u16Value, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                      helpString,
                                                      flags);
    }

    if (type == EConVarType_Int32)
    {
        return g_ConVarManager.CreateConVar<int32_t>(name, defaultValue->m_i32Value, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                     helpString,
                                                     flags);
    }

    if (type == EConVarType_UInt32)
    {
        return g_ConVarManager.CreateConVar<uint32_t>(name, defaultValue->m_u32Value, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                      helpString,
                                                      flags);
    }

    if (type == EConVarType_Int64)
    {
        return g_ConVarManager.CreateConVar<int64_t>(name, defaultValue->m_i64Value, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                     helpString,
                                                     flags);
    }

    if (type == EConVarType_UInt64)
    {
        return g_ConVarManager.CreateConVar<uint64_t>(name, defaultValue->m_u64Value, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                      helpString,
                                                      flags);
    }

    if (type == EConVarType_Float32)
    {
        return g_ConVarManager.CreateConVar<float>(name, defaultValue->m_flValue, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                   helpString,
                                                   flags);
    }

    if (type == EConVarType_Float64)
    {
        return g_ConVarManager.CreateConVar<double>(name, defaultValue->m_dbValue, min->HasValue(), min->GetValue(), max->HasValue(), max->GetValue(),
                                                    helpString,
                                                    flags);
    }

    // string, dup the str to make sure quit dont crash
    return g_ConVarManager.CreateConVar<const char*>(name, defaultValue->m_szValue, helpString, flags);
}

static CConVarBaseData* FindConVar(const char* name, bool useIterator)
{
    return useIterator ? icvar->FindConVarIterator(name) : icvar->GetConVar(icvar->FindConVar(name));
}

static void SetConVarValue(CConVarBaseData* pConVar, CVValue_t* value)
{
    g_ConVarManager.SetValue(pConVar, value);
}

static void SetConVarMinBound(CConVarBaseData* pConVar, CVValue_t* value)
{
    g_ConVarManager.SetMin(pConVar, value);
}

static void SetConVarMaxBound(CConVarBaseData* pConVar, CVValue_t* value)
{
    g_ConVarManager.SetMax(pConVar, value);
}

static int64_t CreateCommand(const char* name, const char* helpString, int flags)
{
    auto handle = g_ConVarManager.CreateSharpConsoleCommand(name, helpString, flags);
    return *reinterpret_cast<int64_t*>(&handle);
}

static bool ReleaseCommand(const char* name)
{
    return g_ConVarManager.ReleaseCommand(name);
}

static int64_t FindGameCommandHandle(const char* name)
{
    auto handle = g_ConVarManager.FindGameCommandHandle(name);
    return *reinterpret_cast<int64_t*>(&handle);
}

static int64_t FindSharpCommandHandle(const char* name)
{
    auto handle = g_ConVarManager.FindSharpCommandHandle(name);
    return *reinterpret_cast<int64_t*>(&handle);
}

static void ReplicateToClient(const CConVarBaseData* pConVar, const char* pszValue, const CServerSideClient* pClient)
{
    CSingleRecipientFilter filter(pClient->GetSlot(), true);
    UTIL_ReplicateConVarFilter(&filter, pConVar->GetName(), pszValue);
}

void Init()
{
    bridge::CreateNative("Cvar.InstallChangeHook", reinterpret_cast<void*>(InstallConVarHook));
    bridge::CreateNative("Cvar.RemoveChangeHook", reinterpret_cast<void*>(RemoveConVarHook));

    // ConVar
    bridge::CreateNative("Cvar.FindConVar", reinterpret_cast<void*>(FindConVar));
    bridge::CreateNative("Cvar.CreateConVar", reinterpret_cast<void*>(CreateConVar));
    bridge::CreateNative("Cvar.CreateCommand", reinterpret_cast<void*>(CreateCommand));
    bridge::CreateNative("Cvar.SetValue", reinterpret_cast<void*>(SetConVarValue));
    bridge::CreateNative("Cvar.SetMinBound", reinterpret_cast<void*>(SetConVarMinBound));
    bridge::CreateNative("Cvar.SetMaxBound", reinterpret_cast<void*>(SetConVarMaxBound));
    bridge::CreateNative("Cvar.ReleaseCommand", reinterpret_cast<void*>(ReleaseCommand));
    bridge::CreateNative("Cvar.FindGameCommandHandle", reinterpret_cast<void*>(FindGameCommandHandle));
    bridge::CreateNative("Cvar.FindSharpCommandHandle", reinterpret_cast<void*>(FindSharpCommandHandle));
    bridge::CreateNative("Cvar.ReplicateToClient", reinterpret_cast<void*>(ReplicateToClient));

    // hook
    InstallConVarHooks();
}
} // namespace natives::cvar