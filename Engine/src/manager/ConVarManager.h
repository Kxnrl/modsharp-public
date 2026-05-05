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

#ifndef MS_MANAGER_CONVAR_H
#define MS_MANAGER_CONVAR_H

#include "address.h"
#include "global.h"
#include "logging.h"
#include "sdkproxy.h"

#include "cstrike/interface/ICommandLine.h"
#include "cstrike/interface/ICvar.h"
#include "cstrike/type/ConCommand.h"

#include <string>
#include <unordered_map>

class SharpCommandCallback : public ICommandCallback
{
public:
    void CommandCallback(const CCommandContext& context, const CCommand& command) override;

    void SetHandle(ConCommandHandle val) { m_Handle = val; }

private:
    ConCommandHandle m_Handle;
};

class ConVarManager
{
public:
    template <typename T>
    CConVarBaseData* CreateConVar(const char* name, T defaultValue, bool hasMin, T minValue, bool hasMax, T maxValue, const char* description = nullptr, int32_t flags = 0, FnGenericChangeCallback_t callback = nullptr)
    {
        // is already exists
        if (const auto exist = icvar->GetConVar(icvar->FindConVar(name)))
        {
            return exist;
        }

        ConVarCreation_t setup;
        setup.m_bHasDefault = true;

        if constexpr (std::is_same_v<const char*, T>)
            setup.DefaultValue<T>() = MemAlloc_StrDupFunc(defaultValue);
        else
            setup.DefaultValue<T>() = defaultValue;

        setup.m_bHasMin     = hasMin;
        setup.m_bHasMax     = hasMax;
        setup.MinValue<T>() = minValue;
        setup.MaxValue<T>() = maxValue;

        setup.m_eVarType           = GetConVarType<T>();
        setup.m_fnCallBack         = callback;
        setup.m_fnProviderCallBack = nullptr;

        setup.m_pszName       = MemAlloc_StrDupFunc(name);
        setup.m_pszHelpString = description != nullptr ? MemAlloc_StrDupFunc(description) : nullptr;
        setup.m_nFlags        = flags;

        ConVarHandle handle;
        handle.Invalidate();
        CConVarBaseData* cvarData = GetInvalidConVar(setup.m_eVarType);
        AssertPtr(cvarData);

        icvar->RegisterConVar(setup, 0, &handle, &cvarData);

        m_CreatedConVarHandles[cvarData] = handle;
        m_CreatedConVarStrHandles[name]  = handle;

        return cvarData;
    }

    template <typename T>
    CConVarBaseData* CreateConVar(const char* name, T defaultValue, const char* description = nullptr, int32_t flags = 0)
    {
        return CreateConVar(name, defaultValue, false, defaultValue, false, defaultValue, description, flags);
    }

    template <typename T>
    CConVarBaseData* CreateConVar(const char* name, T defaultValue, const char* description, int32_t flags, FnGenericChangeCallback_t callback)
    {
        return CreateConVar(name, defaultValue, false, defaultValue, false, defaultValue, description, flags, callback);
    }

    void             CreateConsoleCommand(const char* pName, FnCommandCallback_t callback, const char* pHelpString = nullptr, int64_t flags = 0, FnCommandCompletionCallback completionFunc = nullptr);
    ConCommandHandle CreateSharpConsoleCommand(const char* pName, const char* pHelpString = nullptr, int64_t flags = 0);

    void SetValue(CConVarBaseData* cvarData, CVValue_t* value)
    {
        BaseConVar cvar;
        cvar.m_ConVarData = cvarData;
        const auto type   = cvarData->GetType();
        if (type == EConVarType_Bool || type == EConVarType_Int16 || type == EConVarType_UInt16
            || type == EConVarType_Int32 || type == EConVarType_UInt32
            || type == EConVarType_Int64 || type == EConVarType_UInt64)
        {
            address::server::ScriptSetConVarNumber(&cvar, 0, *value);
        }
        else if (type == EConVarType_Float32 || type == EConVarType_Float64)
        {
#ifdef PLATFORM_WINDOWS
            address::server::ScriptSetConVarDouble(&cvar, *value);
#else
            address::server::ScriptSetConVarDouble(&cvar, 0, *value);
#endif
        }
        else
        {
            address::server::ScriptSetConVarString(&cvar, 0, *value);
        }
    }

    bool SetMin(CConVarBaseData* cvarData, CVValue_t* value) const
    {
        if (!cvarData->HasMinValue())
            return false;

        return SetBound(cvarData->GetType(), cvarData->GetMinBound(), value);
    }

    bool SetMax(CConVarBaseData* cvarData, CVValue_t* value) const
    {
        if (!cvarData->HasMaxValue())
            return false;

        return SetBound(cvarData->GetType(), cvarData->GetMaxBound(), value);
    }

private:
    bool SetBound(EConVarType type, CVValue_t* pBound, CVValue_t* value) const
    {
        switch (type)
        {
        case EConVarType_Float32:
            pBound->m_flValue = value->m_flValue;
            break;
        case EConVarType_Int16:
            pBound->m_i16Value = value->m_i16Value;
            break;
        case EConVarType_Int32:
            pBound->m_i32Value = value->m_i32Value;
            break;
        case EConVarType_Int64:
            pBound->m_i64Value = value->m_i64Value;
            break;
        case EConVarType_UInt16:
            pBound->m_u16Value = value->m_u16Value;
            break;
        case EConVarType_UInt32:
            pBound->m_u32Value = value->m_u32Value;
            break;
        case EConVarType_UInt64:
            pBound->m_u64Value = value->m_u64Value;
            break;
        default:
            return false;
        }
        return true;
    }

public:
    bool ReleaseCommand(const char* name);

private:
    std::unordered_map<CConVarBaseData*, ConVarHandle>     m_CreatedConVarHandles;
    std::unordered_map<std::string, ConVarHandle>          m_CreatedConVarStrHandles;
    std::unordered_map<std::string, ConCommandHandle>      m_CreatedCommandStrHandles;
    std::unordered_map<std::string, SharpCommandCallback*> m_CreatedSharpCommandCallbacks;
};

extern ConVarManager g_ConVarManager;

#endif