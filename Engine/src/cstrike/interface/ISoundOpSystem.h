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

#ifndef CSTRIKE_INTERFACE_SOUNDOPYSTEM_H
#define CSTRIKE_INTERFACE_SOUNDOPYSTEM_H

#include "definitions.h"

#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/EmitSound.h"

class CUtlString;
class CSoundParameters;
class CBaseEntity;
struct CSoundEvent;

class CSoundEventManager
{
public:
    virtual uint32_t     GetSoundEventHash(const char* pszSoundEvent) = 0; // win, linux: 0
    virtual bool         IsValidSoundEventHash(uint32_t nSoundEvent)  = 0; // win, linux: 1
    virtual const char*  GetSoundEventName(uint32_t nSoundEvent)      = 0; // win, linux: 2
    virtual bool         HasSoundEvent(const char* pszSoundEventName) = 0; // win, linux: 3
    virtual CSoundEvent* GetSoundEvent(uint32_t nSoundEvent)          = 0; // Win: 5, linux: 4
    virtual CSoundEvent* GetSoundEvent(const char* pszSoundEventName) = 0; // win: 4, linux: 5
private:
    virtual void Method_6() = 0;
    virtual void Method_7() = 0;
    virtual void Method_8() = 0;

public:
    virtual void ListSoundEvents(CUtlVector<const char*>& list) = 0; // win, linux: 9
private:
    virtual void Method_10() = 0;
    virtual void Method_11() = 0;
    virtual void Method_12() = 0;

public:
    virtual void DereferenceSoundEvent(CSoundEvent* pSoundEvent) = 0; // win, linux: 13
private:
    virtual void Method_14()            = 0;
    virtual void RemoveAllSoundEvents() = 0; // win, linux: 15

public:
    virtual uint32_t GetSoundEventStackHash(uint32_t nSoundEvent)                            = 0; // win: 17, linux: 16
    virtual uint32_t GetSoundEventStackHash(const char* pszSoundEvent)                       = 0; // win: 16, linux: 17
    virtual void     GetSoundEventUpdateStackName(uint32_t nSoundEvent, CUtlString& pString) = 0; // 18
private:
    virtual void Method_19() = 0;
    virtual void Method_20() = 0;

public:
    virtual uint32_t GetSoundEventBaseHash(uint32_t nSoundEvent)                      = 0; // win: 22, linux: 21
    virtual uint32_t GetSoundEventBaseHash(const char* pszSoundEvent)                 = 0; // win: 21, linux: 22
    virtual void     GetSoundEventBaseName(uint32_t nSoundEvent, CUtlString& pString) = 0; // 23
private:
    virtual void GetSoundEventDefinitionBaseField(uint32_t nSoundEvent); // win, linux: 24

    virtual void GetSoundEventDefinitionBaseCount(uint32_t nSoundEvent);      // win: 25, linux: 26
    virtual void GetSoundEventDefinitionBaseCount(const char* pszSoundEvent); // win: 26, linux: 25

    virtual bool SoundEventHasPreloadVsnd() = 0; // 27

public:
    virtual uint8_t* GetSoundEventUpdateGroups(uint32_t nSoundEvent)      = 0; // win: 28, linux: 29
    virtual uint8_t* GetSoundEventUpdateGroups(const char* pszSoundEvent) = 0; // win: 29, linux: 28

    virtual float GetSoundEventDuration(const char* pszSoundEvent, uintptr_t a3, float flDefaultValue = 0) = 0; // win: 31, linux: 30
    virtual float GetSoundEventDuration(uint32_t nSoundEvent, uintptr_t a3, float flDefaultValue = 0)      = 0; // win: 30, linux: 31

private:
    virtual const char* GetVSndNameForSoundEvent(const char* pszSoundEvent, bool bFromSymbolTable)                                    = 0; // 32
    virtual void        GetVSndNameListForSoundEvent(const char* pszSoundEvent, CUtlVector<const char*>& list, bool bFromSymbolTable) = 0; // 33
private:
    virtual void PreloadSoundEventByName(const char* pszSoundEvent) = 0; // 34
    virtual void PreloadSoundEventByHash(uint32_t nSoundEvent)      = 0; // 35
    virtual void AddSoundEventFromKV3()                             = 0; // 36
    virtual void AddSoundEventFromResourceBinding()                 = 0; // 37

    virtual void RemoveTimedOutDeferredSoundEvent() = 0; // 38
    virtual void Method_39()                        = 0; // 39

    virtual void AddNewSoundEvent() = 0; // 40
    virtual void Method_41()        = 0; // 41
    virtual void Method_42()        = 0; // 42
    virtual void Method_43()        = 0; // 43
    virtual void Method_44()        = 0; // 44
    virtual void Method_45()        = 0; // 45

    virtual void SetSoundEventDefinitionField() = 0; // 46

    virtual void Method_47() = 0; // 47
    virtual void Method_48() = 0; // 48
    virtual void Method_49() = 0; // 49
    virtual void Method_50() = 0; // 50
    virtual void Method_51() = 0; // 51
    virtual void Method_52() = 0; // 52
    virtual void Method_53() = 0; // 53
    virtual void Method_54() = 0; // 54
    virtual void Method_55() = 0; // 55
    virtual void Method_56() = 0; // 56

    virtual void CompareSoundEventDefinitions(const char* pszSoundEventA, const char* pszSoundEventB) = 0; // 57

    virtual void Method_58() = 0;

    virtual void ListDeferredSoundEvents() = 0; // 59

    virtual void Method_60() = 0;
    virtual void Method_61() = 0;
};

class SoundOpGameSystem
{
    bool SetSoundEventParamInternal(const IRecipientFilter* pFilter, const SndOpEventGuid_t& nGuid, const char* pParamName, CSosFieldData* pValue);

public:
    template <typename T>
        requires std::derived_from<T, CSosFieldData>
    bool SetSoundEventParamAll(const SndOpEventGuid_t& nGuid, const char* pParamName, T* pValue)
    {
        CBroadcastRecipientFilter filter(true);
        filter.AddAllPlayer();
        return SetSoundEventParamInternal(&filter, nGuid, pParamName, pValue);
    }

    template <typename T>
        requires std::derived_from<T, CSosFieldData>
    bool SetSoundEventParam(const IRecipientFilter* pFilter, const SndOpEventGuid_t& nGuid, const char* pParamName, T* pValue)
    {
        return SetSoundEventParamInternal(pFilter, nGuid, pParamName, pValue);
    }
    SndOpEventGuid_t StartSoundEvent(const IRecipientFilter* pFilter, const char* pSound, CBaseEntity* pEntity = nullptr, int16_t unknown = -1, float flSoundTime = 0.0f);
    SndOpEventGuid_t StartSoundEventAll(const char* pSound, CBaseEntity* pEntity = nullptr, int16_t unknown = -1, float flSoundTime = 0.0f);
};

#endif
