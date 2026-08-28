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
#include "global.h"
#include "logging.h"

#include "CoreCLR/Nullable.h"

#include "bridge/adapter.h"
#include "bridge/natives/SoundNatives.h"

#include "cstrike/interface/ISoundOpSystem.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/EmitSound.h"

static float GetSoundDuration(const char* pszSound)
{
    return g_pSoundEventManager->GetSoundEventDuration(pszSound, 0);
}

static int64_t StartSoundEvent(CBaseEntity* pEntity, const char* pszSound, Nullable<float>* pVolume, RuntimeRecipientFilter* pFilter)
{
    CBroadcastRecipientFilter filter(pFilter, true);
    const auto                guid = g_pSoundOpGameSystem->StartSoundEvent(&filter, pszSound, pEntity);

    if (pVolume->HasValue() && guid.IsValid())
    {
        const auto         flVolume = pVolume->GetValue();
        CSosFieldDataFloat param(flVolume);
        const auto         setSoundEventVolume = g_pSoundOpGameSystem->SetSoundEventParam(&filter, guid, SE_PARAM_NAME_VOLUME, &param);
        if (!setSoundEventVolume)
            FERROR("Failed to SetSoundEventVolume (%s, %f) to guid {Guid=%d, Hash=%d}", pszSound, flVolume, guid.GetGuid().GetGuid(), guid.GetStackHash());
    }

    return guid;
}

static bool SetSoundEventParamFloat(int64_t guid, const char* pszParam, float flValue, RuntimeRecipientFilter* pFilter)
{
    AssertPtr(g_pSoundOpGameSystem);

    CBroadcastRecipientFilter filter(pFilter, true);
    CSosFieldDataFloat        param(flValue);
    SndOpEventGuid_t          event(guid);
    return g_pSoundOpGameSystem->SetSoundEventParam(&filter, event, pszParam, &param);
}

static bool SetSoundEventParamVector(int64_t guid, const char* pszParam, const Vector& vecValue, RuntimeRecipientFilter* pFilter)
{
    AssertPtr(g_pSoundOpGameSystem);

    CBroadcastRecipientFilter filter(pFilter, true);
    CSosFieldDataFloat3       param(vecValue);
    SndOpEventGuid_t          event(guid);
    return g_pSoundOpGameSystem->SetSoundEventParam(&filter, event, pszParam, &param);
}

static bool SetSoundEventParamInt32(int64_t guid, const char* pszParam, int iValue, RuntimeRecipientFilter* pFilter)
{
    AssertPtr(g_pSoundOpGameSystem);

    CBroadcastRecipientFilter filter(pFilter, true);
    CSosFieldDataInt          param(iValue);
    SndOpEventGuid_t          event(guid);
    return g_pSoundOpGameSystem->SetSoundEventParam(&filter, event, pszParam, &param);
}

static bool SetSoundEventParamUInt32(int64_t guid, const char* pszParam, uint32_t iValue, RuntimeRecipientFilter* pFilter)
{
    AssertPtr(g_pSoundOpGameSystem);

    CBroadcastRecipientFilter filter(pFilter, true);
    CSosFieldDataUInt32       param(iValue);
    SndOpEventGuid_t          event(guid);
    return g_pSoundOpGameSystem->SetSoundEventParam(&filter, event, pszParam, &param);
}

static bool SetSoundEventParamUInt64(int64_t guid, const char* pszParam, uint64_t iValue, RuntimeRecipientFilter* pFilter)
{
    AssertPtr(g_pSoundOpGameSystem);

    CBroadcastRecipientFilter filter(pFilter, true);
    CSosFieldDataUInt64       param(iValue);
    SndOpEventGuid_t          event(guid);
    return g_pSoundOpGameSystem->SetSoundEventParam(&filter, event, pszParam, &param);
}

static void StopSoundEvent(SndOpEventGuid_t* guid)
{
    AssertPtr(g_pSoundOpGameSystem);

    CBroadcastRecipientFilter filter(true);
    filter.AddAllPlayer();
    address::server::SoundOpGameSystem_StopSoundEventFilter(g_pSoundOpGameSystem, &filter, guid->GetGuid());
}

static void StopSoundEventFilter(SndOpEventGuid_t* guid, RuntimeRecipientFilter* pFilter)
{
    AssertPtr(g_pSoundOpGameSystem);

    CBroadcastRecipientFilter filter(pFilter, true);
    address::server::SoundOpGameSystem_StopSoundEventFilter(g_pSoundOpGameSystem, &filter, guid->GetGuid());
}

static bool IsSoundEventValid(const char* pszSoundEvent)
{
    auto hash = g_pSoundEventManager->GetSoundEventHash(pszSoundEvent);
    if (hash == std::numeric_limits<uint32_t>::max()) [[unlikely]]
        return false;

    return g_pSoundEventManager->IsValidSoundEventHash(hash);
}

void natives::sound::Init()
{
    bridge::CreateNative("Sound.GetSoundDuration", reinterpret_cast<void*>(GetSoundDuration));
    bridge::CreateNative("Sound.StartSoundEvent", reinterpret_cast<void*>(StartSoundEvent));
    bridge::CreateNative("Sound.StopSoundEvent", reinterpret_cast<void*>(StopSoundEvent));
    bridge::CreateNative("Sound.StopSoundEventFilter", reinterpret_cast<void*>(StopSoundEventFilter));
    bridge::CreateNative("Sound.SetSoundEventParamFloat", reinterpret_cast<void*>(SetSoundEventParamFloat));
    bridge::CreateNative("Sound.SetSoundEventParamVector", reinterpret_cast<void*>(SetSoundEventParamVector));
    bridge::CreateNative("Sound.SetSoundEventParamInt32", reinterpret_cast<void*>(SetSoundEventParamInt32));
    bridge::CreateNative("Sound.SetSoundEventParamUInt32", reinterpret_cast<void*>(SetSoundEventParamUInt32));
    bridge::CreateNative("Sound.SetSoundEventParamUInt64", reinterpret_cast<void*>(SetSoundEventParamUInt64));
    bridge::CreateNative("Sound.IsSoundEventValid", reinterpret_cast<void*>(IsSoundEventValid));
}