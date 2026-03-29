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

#ifndef CSTRIKE_INTERFACE_CVAR_H
#define CSTRIKE_INTERFACE_CVAR_H

#include "cstrike/interface/IAppSystem.h"
#include "cstrike/type/CCommand.h"
#include "cstrike/type/CConVar.h"
#include "cstrike/type/ConCommand.h"

#include <cstdint>
#include <functional>

enum
{
    FCVAR_NONE                    = 0,
    FCVAR_LINKED_CONCOMMAND       = (1 << 0),
    FCVAR_DEVELOPMENTONLY         = (1 << 1),
    FCVAR_GAMEDLL                 = (1 << 2),
    FCVAR_CLIENTDLL               = (1 << 3),
    FCVAR_HIDDEN                  = (1 << 4),
    FCVAR_PROTECTED               = (1 << 5),
    FCVAR_SPONLY                  = (1 << 6),
    FCVAR_ARCHIVE                 = (1 << 7),
    FCVAR_NOTIFY                  = (1 << 8),
    FCVAR_USERINFO                = (1 << 9),
    FCVAR_REFERENCE               = (1 << 10), // Actual name unavailable
    FCVAR_UNLOGGED                = (1 << 11),
    FCVAR_INITIAL_SETVALUE        = (1 << 12),
    FCVAR_REPLICATED              = (1 << 13),
    FCVAR_CHEAT                   = (1 << 14),
    FCVAR_PER_USER                = (1 << 15),
    FCVAR_DEMO                    = (1 << 16),
    FCVAR_DONTRECORD              = (1 << 17),
    FCVAR_PERFORMING_CALLBACKS    = (1 << 18),
    FCVAR_RELEASE                 = (1 << 19),
    FCVAR_MENUBAR_ITEM            = (1 << 20),
    FCVAR_COMMANDLINE_ENFORCED    = (1 << 21),
    FCVAR_NOT_CONNECTED           = (1 << 22),
    FCVAR_VCONSOLE_FUZZY_MATCHING = (1 << 23),
    FCVAR_SERVER_CAN_EXECUTE      = (1 << 24),
    FCVAR_CLIENT_CAN_EXECUTE      = (1 << 25),
    FCVAR_SERVER_CANNOT_QUERY     = (1 << 26),
    FCVAR_VCONSOLE_SET_FOCUS      = (1 << 27),
    FCVAR_CLIENTCMD_CAN_EXECUTE   = (1 << 28),
    FCVAR_EXECUTE_PER_TICK        = (1 << 29)
};

struct CVarCreationBase_t
{
    CVarCreationBase_t() :
        m_pszName(nullptr),
        m_pszHelpString(nullptr),
        m_nFlags(0)
    {}

    bool    IsFlagSet(int64_t flag) const;
    void    AddFlags(int64_t flags);
    void    RemoveFlags(int64_t flags);
    int64_t GetFlags() const;

    const char* m_pszName;
    const char* m_pszHelpString;
    int64_t     m_nFlags;
};

struct ConCommandCreation_t : CVarCreationBase_t
{
    ConCommandCreation_t()
    {}

    // Call this function when executing the command
    struct CallbackInfo_t
    {
        CallbackInfo_t() :
            m_fnCommandCallback(nullptr),
            m_bIsInterface(false),
            m_bIsVoidCallback(false),
            m_bIsContextLess(false)
        {}

        union {
            FnCommandCallback_t          m_fnCommandCallback;
            FnCommandCallbackVoid_t      m_fnVoidCommandCallback;
            FnCommandCallbackNoContext_t m_fnContextlessCommandCallback;
            ICommandCallback*            m_pCommandCallback;
        };

        bool m_bIsInterface;
        bool m_bIsVoidCallback;
        bool m_bIsContextLess;
    };

    CallbackInfo_t m_fnCallback;

    struct CompletionInfo_t
    {
        CompletionInfo_t()
        {
            m_fnCompletionCallback       = nullptr;
            m_pCommandCompletionCallback = nullptr;
            m_bHasCompletionCallback     = false;
            m_bIsInterface               = false;
        }
        union {
            FnCommandCompletionCallback m_fnCompletionCallback;
            ICommandCompletionCallback* m_pCommandCompletionCallback;
        };

        bool m_bHasCompletionCallback;
        bool m_bIsInterface;
    };

    CompletionInfo_t m_fnCompletion;
};

struct ConVarCreation_t : CVarCreationBase_t
{
    ConVarCreation_t() :
        m_bHasDefault(false),
        m_bHasMin(false),
        m_bHasMax(false)
    {
        memset(m_defaultValue, 0, sizeof(CVValue_t));
        memset(m_minValue, 0, sizeof(CVValue_t));
        memset(m_maxValue, 0, sizeof(CVValue_t));
    }

    template <typename T>
    T& DefaultValue()
    {
        return *reinterpret_cast<T*>(m_defaultValue);
    }
    template <typename T>
    T& MinValue()
    {
        return *reinterpret_cast<T*>(m_minValue);
    }
    template <typename T>
    T& MaxValue()
    {
        return *reinterpret_cast<T*>(m_maxValue);
    }

    int32_t m_Version; // 0x18

    bool m_bHasDefault; // 0x1c
    bool m_bHasMin;     // 0x1d
    bool m_bHasMax;     // 0x1e

private:
    uint8_t m_defaultValue[sizeof(CVValue_t)];
    uint8_t m_minValue[sizeof(CVValue_t)];
    uint8_t m_maxValue[sizeof(CVValue_t)];

public:
    FnGenericChangeCallbackProvider_t m_fnProviderCallBack; // 0x50
    FnGenericChangeCallback_t         m_fnCallBack;         // 0x58
private:
    void* callback_96  = nullptr;
    void* callback_104 = nullptr;
    void* callback_112 = nullptr; // 0x70
public:
    EConVarType m_eVarType; // 0x78
};

class ICvar : public IAppSystem
{
public:
    using FnChangeCallbackGlobal_t = void (*)(BaseConVar* cvar, int32_t splitScreenSlot, const char* pNewValue, const char* pOldValue);
    using FnCvarCallbacksReader_t  = std::function<void(void*, FnGenericChangeCallback_t)>;

public:
    // bAllowDeveloper - Allows finding convars with FCVAR_DEVELOPMENTONLY flag
    virtual ConVarHandle FindConVar(const char* name, bool bAllowDeveloper = false) = 0;
    virtual ConVarHandle FindFirstConVar()                                          = 0;
    virtual ConVarHandle FindNextConVar(ConVarHandle prevHandle)                    = 0;

    virtual void CallChangeCallback(ConVarHandle cvar, const CSplitScreenSlot nSlot, const CVValue_t* pNewValue, const CVValue_t* pOldValue, void* unk = nullptr) = 0;
    virtual void IterateConVarCallbacks(ConVarHandle cvar, FnCvarCallbacksReader_t cb)                                                                            = 0;

    virtual void Metohd_16() = 0;

    virtual ConCommandHandle FindCommand(const char* name)                                   = 0;
    virtual ConCommandHandle FindFirstCommand()                                              = 0;
    virtual ConCommandHandle FindNextCommand(ConCommandHandle prev)                          = 0;
    virtual void             DispatchConCommand(ConCommandHandle cmd, void* ctx, void* args) = 0;

    // Install a global change callback (to be called when any convar changes)
    virtual void InstallGlobalChangeCallback(FnChangeCallbackGlobal_t callback)                                                                      = 0;
    virtual void RemoveGlobalChangeCallback(FnChangeCallbackGlobal_t callback)                                                                       = 0;
    virtual void CallGlobalChangeCallbacks(BaseConVar* ref, CSplitScreenSlot nSlot, const char* newValue, const char* oldValue, void* unk = nullptr) = 0;

    // Reverts cvars which contain a specific flag
    virtual void ResetConVarsToDefaultValuesByFlag(int32_t nFlag) = 0;

    virtual void    SetMaxSplitScreenSlots(int32_t nSlots) = 0;
    virtual int32_t GetMaxSplitScreenSlots() const         = 0;

    virtual void RegisterCreationListeners(void* callbacks) = 0;
    virtual void RemoveCreationListeners(void* callbacks)   = 0;

    virtual void Method_29() = 0;

    virtual void ResetConVarsToDefaultValues(const char* pszPrefix) = 0;

    virtual void* TakeConVarSnapshot()                    = 0;
    virtual void  ResetConVarsToSnapshot(void* pSnapshot) = 0;
    virtual void  DestroyConVarSnapshot(void* pSnaoshot)  = 0;

    virtual void GetCharacterSet()                 = 0;
    virtual void SetConVarsFromGameInfo(void* pKV) = 0;

    virtual void StripDevelopmentFlags() = 0;

    virtual int32_t GetTotalUserInfoCvarsByteSize()                                                           = 0;
    virtual void    CopyUserInfoCvarDefaults(uint8_t* buffer, int32_t from, int32_t to, bool copy_or_cleanup) = 0;

    virtual void Method_39() = 0;

    // Register, unregister vars
    virtual void             RegisterConVar(const ConVarCreation_t& setup, int64_t nAdditionalFlags, ConVarHandle* pCvarRef, CConVarBaseData** pCvar) = 0;
    virtual void             UnregisterConVar(ConVarHandle handle)                                                                                    = 0;
    virtual void             LockConVarValueInitialisation(bool state)                                                                                = 0;
    virtual CConVarBaseData* GetConVar(ConVarHandle handle)                                                                                           = 0;

    // Register, unregister commands
    virtual ConCommandHandle RegisterConCommand(const ConCommandCreation_t& setup, int64_t nAdditionalFlags = 0) = 0;
    virtual void             UnregisterConCommand(ConCommandHandle handle)                                       = 0;
    virtual ConCommand*      GetCommand(ConCommandHandle handle)                                                 = 0;

    // 11 = FindConVar( const char *name, bool bAllowDeveloper = false ) -> ConVarHandle
    // 12 = FindFirstConVar() -> ConVarHandle
    // 13 = FindNextConVar( ConVarHandle prevHandle ) -> ConVarHandle
    // 15 = FindCommand( const char *name ) -> ConCommandHandle
    // 16 = FindFirstCommand() -> ConCommandHandle
    // 17 = FindNextCommand( ConCommandHandle prevHandle ) -> ConCommandHandle
    // 19 = InstallGlobalChangeCallback( FnChangeCallbackGlobal_t callback )
    // 37 = GetConVar
    // 36 = RegisterConVar
    // 38 = RegisterConCommand

    CConVarBaseData* FindConVarIterator(const char* name);
    ConCommand*      FindConCommandIterator(const char* name);
};

int32_t DefaultCompletionFunc(const char* partial, /*CUtlVector<CUtlString>&*/ void* commands);
void    UnlockHiddenCommand(const char* command, bool release = false);

#endif
