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

#include "address.h"
#include "bridge/forwards/forward.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "strtool.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/IRecipientFilter.h"
#include "cstrike/interface/ISoundOpSystem.h"
#include "cstrike/type/EmitSound.h"

#include <safetyhook.hpp>

// #define SOUND_HOOK_ASSERT
// #define SOUND_SET_PARAMS_HOOK

static bool                                   s_bInSoundEmitter      = false;
static CConVarBaseData*                       ms_hook_map_music      = nullptr;
static CConVarBaseData*                       ms_map_music_threshold = nullptr;
static std::unordered_map<std::string, float> s_SoundEventDuration{};
static std::unordered_map<std::string, bool>  s_SoundEventIsMusic{};

extern bool TransmitCheckMapMusic(int slot);

BeginMemberHookScope(CSoundEmitterSystem)
{
#ifdef PLATFORM_WINDOWS
    DeclareMemberDetourHook(EmitSound, void, (void* pThis, StartSoundEventInfo_t* pInfo, IRecipientFilter* pFilter, EntityIndex_t entityIndex, EmitSound_t* pSound))
#else
    DeclareMemberDetourHook(EmitSound, StartSoundEventInfo_t*, (SndOpEventGuid_t * pThis, IRecipientFilter * pFilter, EntityIndex_t entityIndex, EmitSound_t * pSound))
#endif
    {
        // NOTE
        // 对于游戏自带的IRecipientFilter, 可以用 pBase + index * 4 来获取index
        // const auto pBase = *reinterpret_cast<int64_t*>(reinterpret_cast<int64_t>(pFilter) + 24);

#ifdef PLATFORM_WINDOWS
        if (!pSound)
            return;
#else
        if (!pSound)
            return &g_DefaultStartSoundEventInfo;
#endif

#ifdef SOUND_HOOK_ASSERT
        LOG("EmitSound ->\n%12s: %d\n%12s: %s\n%12s: %d\n%12s: %f\n%12s: %d",
            "Entity", entityIndex,
            "Sound", pSound->m_pSoundName,
            "Channel", pSound->m_nChannel,
            "Volume", pSound->m_flVolume,
            "Receivers", pFilter->GetRecipientCount());
#endif

        char soundName[128];
        StrCopyFast(soundName, sizeof(soundName), pSound->m_pSoundName);
        auto channel         = 0; // todo: remove
        auto volume          = pSound->m_flVolume;
        auto changeRecipient = false;
        auto recipients      = pFilter->GetRecipients();

        const auto action = forwards::OnEmitSound->Invoke(entityIndex, soundName, &channel, &volume, &recipients, &changeRecipient);

        switch (action)
        {
        case EHookAction::SkipCallReturnOverride: {
// blocked
#ifdef PLATFORM_WINDOWS
            *pInfo = g_DefaultStartSoundEventInfo;
            return;
#else
            return &g_DefaultStartSoundEventInfo;
#endif
        }
        case EHookAction::ChangeParamReturnDefault: {
            pSound->m_flVolume   = volume;
            pSound->m_pSoundName = soundName;
            if (changeRecipient)
            {
                pFilter->SetRecipients(recipients);
            }
            break;
        }
        case EHookAction::Ignored: {
            break;
        }
        default: {
            FatalError("Not impl yet");
            break;
        }
        }

#ifdef PLATFORM_WINDOWS
        s_bInSoundEmitter = true;
        EmitSound(pThis, pInfo, pFilter, entityIndex, pSound);
        s_bInSoundEmitter = false;

        const auto guid = pInfo->m_nSndOpEventGuid;
        forwards::OnEmitSoundPost->Invoke(entityIndex, soundName, channel, volume, pFilter->GetRecipients(), changeRecipient, action, guid);
#else
        s_bInSoundEmitter = true;
        const auto result = EmitSound(pThis, pFilter, entityIndex, pSound);
        s_bInSoundEmitter = false;
        forwards::OnEmitSoundPost->Invoke(entityIndex, soundName, channel, volume, pFilter->GetRecipients(), changeRecipient, action, result->m_nSndOpEventGuid);
        return result;
#endif
    }
}

BeginMemberHookScope(SoundOpGameSystem)
{
    static inline const char* GetSoundEventName(uint32_t pSound)
    {
        return g_pSoundEventManager->GetSoundEventName(pSound);
    }

    static inline float GetSoundDuration(const char* pszSound)
    {
        const auto map = s_SoundEventDuration.find(pszSound);
        if (map != s_SoundEventDuration.end())
            return map->second;

        auto duration = g_pSoundEventManager->GetSoundEventDuration(pszSound, 0);

        s_SoundEventDuration.emplace(pszSound, duration);

        return duration;
    }

    static inline bool IsSoundMusic(const char* pszSound)
    {
        if (!pszSound[0])
            return false;

        const auto map = s_SoundEventIsMusic.find(pszSound);
        if (map != s_SoundEventIsMusic.end())
            return map->second;

        // Hash Value of 'csgo_music'
        constexpr uint32_t music_stack_hash = MurmurHash2("csgo_music", MURMURHASH_SOUNDSTACK_SEED);
        static_assert(music_stack_hash == 0x9e23cf7f);

        const auto statck_hash = g_pSoundEventManager->GetSoundEventStackHash(pszSound);
        if (statck_hash == std::numeric_limits<uint32_t>::max())
        {
            s_SoundEventIsMusic.emplace(pszSound, false);
            return false;
        }

        const auto is_music = statck_hash == music_stack_hash;

        s_SoundEventIsMusic.emplace(pszSound, is_music);

        return is_music;
    }

    static inline bool CheckMusicLength(float duration)
    {
        return duration >= ms_map_music_threshold->GetValue<float>();
    }

    static inline bool IsFromSoundEntity(EntityIndex_t entityIndex)
    {
        // point_soundevent always 0
        if (entityIndex == WORLD_ENTITY_INDEX)
            return true;

        if (entityIndex == INVALID_ENTITY_INDEX)
            return false;

        const auto pEntity = g_pGameEntitySystem->FindEntityByIndex(entityIndex);
        if (!pEntity)
            return false;

        return strcasecmp(pEntity->GetClassname(), "ambient_generic") == 0;
    }

    struct PackedData
    {
        void*    m_pData;
        uint32_t m_nType = ~0u; // default -1
    };

#ifdef PLATFORM_WINDOWS
    DeclareMemberDetourHook(DoStartSoundEvent, void, (SoundOpGameSystem * pThis, StartSoundEventInfo_t * pInfo, IRecipientFilter * pFilter, uint32_t nGuid, uint32_t nSoundEventHash, int entityIndex, int16_t nSeed, void* pPackedFieldData, int size, PackedData* pPackedData))
#else
    DeclareMemberDetourHook(DoStartSoundEvent, void, (StartSoundEventInfo_t * pInfo, SoundOpGameSystem * pThis, IRecipientFilter * pFilter, uint32_t nGuid, uint32_t nSoundEventHash, int entityIndex, int16_t nSeed, void* pPackedFieldData, int size, void* pPackedData, int32_t nPackedDataType))
#endif
    {
        if (s_bInSoundEmitter)
#ifdef PLATFORM_WINDOWS
            return DoStartSoundEvent(pThis, pInfo, pFilter, nGuid, nSoundEventHash, entityIndex, nSeed, pPackedFieldData, size, pPackedData);
#else
            return DoStartSoundEvent(pInfo, pThis, pFilter, nGuid, nSoundEventHash, entityIndex, nSeed, pPackedFieldData, size, pPackedData, nPackedDataType);
#endif

#ifdef PLATFORM_WINDOWS
#    define CALL_DoStartSoundEvent() DoStartSoundEvent(pThis, pInfo, pFilter, nGuid, nSoundEventHash, entityIndex, nSeed, pPackedFieldData, size, pPackedData)
#else
#    define CALL_DoStartSoundEvent() DoStartSoundEvent(pInfo, pThis, pFilter, nGuid, nSoundEventHash, entityIndex, nSeed, pPackedFieldData, size, pPackedData, nPackedDataType)
#endif

        const auto soundName     = g_pSoundEventManager->GetSoundEventName(nSoundEventHash);
        const auto soundDuration = GetSoundDuration(soundName);

        auto recipients = pFilter->GetRecipients();

        // MapMusic
        if ((IsSoundMusic(soundName) || CheckMusicLength(soundDuration)) && IsFromSoundEntity(entityIndex))
        {
            if (ms_hook_map_music->GetValue<bool>())
            {
                for (PlayerSlot_t i = 0u; i < CS_MAX_PLAYERS; i++)
                {
                    if (!pFilter->IsPlayerSlotInFilter(i))
                        continue;

                    if (TransmitCheckMapMusic(i))
                    {
                        recipients &= ~(BASE_RECEIVER_MAGIC << i);
                    }
                }
            }

#ifdef SOUND_HOOK_ASSERT
            LOG("Handled map music from entity <%d> with sound [%s] (%fs)", entityIndex, soundName, soundDuration);
#endif
            pFilter->SetRecipients(recipients);
            CALL_DoStartSoundEvent();
            const SndOpEventGuid_t guid = pInfo->m_nSndOpEventGuid;

            forwards::OnEmitMusicPost->Invoke(entityIndex, soundName, soundDuration, pFilter->GetRecipients(), guid);

#ifdef SOUND_HOOK_ASSERT
            FLOG("[OnSoundEventPost] SoundEvent: %s(%x), entityIndex: %i, nSeed: %i, Guid: %x, StackHash: %x",
                 soundName,
                 nSoundEventHash,
                 entityIndex,
                 nSeed,
                 guid.GetGuid().GetGuid(),
                 guid.GetStackHash());
#endif

            return;
        }

        auto changeRecipient = false;

        const auto action = forwards::OnSoundEvent->Invoke(entityIndex, soundName, soundDuration, &recipients, &changeRecipient);
        switch (action)
        {
        case EHookAction::SkipCallReturnOverride: {
            pInfo->m_nSndOpEventGuid = g_DefaultSndOpEventGuid;
            return;
        }
        case EHookAction::ChangeParamReturnDefault: {
            if (changeRecipient)
            {
                pFilter->SetRecipients(recipients);
            }
            break;
        }
        case EHookAction::Ignored: {
            break;
        }
        default: {
            FatalError("Not impl yet");
            break;
        }
        }

        CALL_DoStartSoundEvent();
        const SndOpEventGuid_t guid = pInfo->m_nSndOpEventGuid;

#ifdef SOUND_HOOK_ASSERT
        FLOG("[OnSoundEventPost] SoundEvent: %s(%x), entityIndex: %i, nSeed: %i, Guid: %x, StackHash: %x",
             soundName,
             nSoundEventHash,
             entityIndex,
             nSeed,
             guid.GetGuid().GetGuid(),
             guid.GetStackHash());
#endif

        forwards::OnSoundEventPost->Invoke(entityIndex, soundName, soundDuration, pFilter->GetRecipients(), changeRecipient, action, guid);
    }

#ifdef SOUND_SET_PARAMS_HOOK
    DeclareMemberDetourHook(SetSoundEventParam, bool, (SoundOpGameSystem * pThis, IRecipientFilter * pFilter, int64_t nGuid, const char* pszParamName, SoundEventParam* pParamValue, int16_t nIndex, bool bIsOptional))
    {
        SndOpEventGuid_t v(nGuid);
        LOG("SetSoundEventParam {Guid: %d, Hash: %d} (%s) -> %f", v.m_nGuid, v.m_hStackHash, pszParamName, pParamValue->m_flOpValue);
        return SetSoundEventParam(pThis, pFilter, nGuid, pszParamName, pParamValue, nIndex, bIsOptional);
    }
#endif
}

// convar

// sos_debug_emit
// snd_list
// snd_showstart
// snd_sos_show_soundevent_start
// snd_sos_print_full_field_info
// snd_sos_ingame_debug

// command

// snd_print_soundevent
// snd_list_soundevents
// snd_sos_print_strings
// snd_sos_print_field_name_strings
// snd_sos_print_groups

void InstallSoundHooks()
{
    InstallMemberDetourAutoSig(CSoundEmitterSystem, EmitSound);
    InstallMemberDetourAutoSig(SoundOpGameSystem, DoStartSoundEvent);

#ifdef SOUND_SET_PARAMS_HOOK
    InstallMemberDetourManual(SoundOpGameSystem, SetSoundEventParam, reinterpret_cast<decltype(SoundOpGameSystem_Hooks::SetSoundEventParam)>(address::server::SoundOpGameSystem_SetSoundEventParam));
#endif

    g_pHookManager->Hook_GameInit(HookType_Post, [] {
        s_SoundEventDuration.clear();
        s_SoundEventIsMusic.clear();
    });

    ms_hook_map_music      = g_ConVarManager.CreateConVar("ms_hook_map_music", false, "Hook map music and send to transmit hook.", FCVAR_RELEASE);
    ms_map_music_threshold = g_ConVarManager.CreateConVar("ms_map_music_threshold", 15.f, true, 5.f, true, 300.f, "Hook map music and send to transmit hook.", FCVAR_RELEASE);

    UnlockHiddenCommand("snd_print_soundevent");
    UnlockHiddenCommand("snd_list_soundevents");
    UnlockHiddenCommand("snd_sos_print_strings");
    UnlockHiddenCommand("snd_sos_print_field_name_strings");
    UnlockHiddenCommand("snd_sos_print_groups");
}
