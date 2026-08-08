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

#include "gamedata.h"
#include "global.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/ISoundOpSystem.h"
#include "cstrike/type/CRecipientFilter.h"

#ifdef DEBUG
#    include "logging.h"
#endif

bool SoundOpGameSystem::SetSoundEventParamInternal(const IRecipientFilter* pFilter, const SndOpEventGuid_t& nGuid, const char* pParamName, CSosFieldData* pValue)
{
    if (!nGuid.IsValid())
        return false;

#ifdef PLATFORM_WINDOWS
    using SoundOpGameSystem_SetSoundEventParam_t = void (*)(SoundOpGameSystem*, const IRecipientFilter*, const StartSoundEventInfo_t&, const char*, CSosFieldData*, int16_t, bool);
#else
    using SoundOpGameSystem_SetSoundEventParam_t = void (*)(SoundOpGameSystem*, const IRecipientFilter*, const char*, CSosFieldData*, int16_t, bool, StartSoundEventInfo_t);
#endif

    static auto fn = g_pGameData->GetAddress<SoundOpGameSystem_SetSoundEventParam_t>("SoundOpGameSystem::SetSoundEventParamString");

    StartSoundEventInfo_t info(nGuid, 0, pFilter->GetRecipients());

#ifdef PLATFORM_WINDOWS
    fn(this, pFilter, info, pParamName, pValue, 0, false);
#else
    fn(this, pFilter, pParamName, pValue, 0, false, info);
#endif

    return true;
}

SndOpEventGuid_t SoundOpGameSystem::StartSoundEvent(const IRecipientFilter* pFilter, const char* pSound, CBaseEntity* pEntity, int16_t nSeed, float flSoundTime)
{
#ifdef PLATFORM_WINDOWS
    using fn = void (*)(SoundOpGameSystem*, StartSoundEventInfo_t*, const IRecipientFilter*, const char*, int32_t, int16_t, double);
#else
    using fn = void (*)(StartSoundEventInfo_t*, SoundOpGameSystem*, const IRecipientFilter*, const char*, int32_t, int16_t, double);
#endif

    StartSoundEventInfo_t info{};

    static auto func = g_pGameData->GetAddress<fn>("SoundOpGameSystem::StartSoundEventString");

#ifdef PLATFORM_WINDOWS
    func(this, &info, pFilter, pSound, pEntity ? pEntity->GetEntityIndex() : -1, nSeed, flSoundTime);
#else
    func(&info, this, pFilter, pSound, pEntity ? pEntity->GetEntityIndex() : -1, nSeed, flSoundTime);
#endif

#ifdef DEBUG
    FLOG("[SoundOpGameSystem::StartSoundEvent] %s, Guid: %x, StackHash: %x", pSound, info.m_nStartedSound.GetGuid().GetGuid(), info.m_nStartedSound.GetStackHash());
#endif

    return info.m_nSndOpEventGuid;
}

SndOpEventGuid_t SoundOpGameSystem::StartSoundEventAll(const char* pSound, CBaseEntity* pEntity, int16_t seed, float flSoundTime)
{
    CBroadcastRecipientFilter filter(true);
    filter.AddAllPlayer();
    return StartSoundEvent(&filter, pSound, pEntity, seed, flSoundTime);
}
