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

#include "hook/network.h"
#include "address.h"
#include "bridge/forwards/forward.h"
#include "gamedata.h"
#include "global.h"
#include "module.h"
#include "vhook/hook.h"
#include "manager/HookManager.h"

#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/INetChannel.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/IProtobufBinding.h"
#include "cstrike/type/CEffectData.h"

#include <proto/cs_gameevents.pb.h>
#include <proto/te.pb.h>
#include <proto/usermessages.pb.h>

#include <safetyhook.hpp>

#include <algorithm>

// #define HOOK_NETWORK_ASSERT

extern void    TransmitCheckFireBullets(const NetworkReceiver_t*);
extern int32_t TransmitGetFireBulletState(uint32_t handle);
extern void    TransmitCheckWorldDecals(const NetworkReceiver_t*);
extern void    TransmitCheckBloodEffect(const NetworkReceiver_t*);

bool s_bBypassNetMessageHook = false;

static bool                  s_bBypassDispatchEffect = false;
static std::vector<uint16_t> s_HookedNetMsgIds;

static bool IsHookIdExist(const uint16_t msgId)
{
    return std::ranges::find(s_HookedNetMsgIds, msgId) != s_HookedNetMsgIds.end();
}

#ifdef HOOK_NETWORK_ASSERT
static CBaseEntity* GetEntityFromProtobuf(uint32_t value)
{
    if (value == 0xFFFFFF)
        return nullptr;

    EntityIndex_t index = value & 0x7FFF;
    if (index > 16384)
        index -= 16384;

    AssertBool(index >= 0 && index < 16384);

    const auto pEntity = g_pGameEntitySystem->FindEntityByIndex(index);
    if (!pEntity)
        return nullptr;

    const auto handle = pEntity->GetEHandle().ToInt();
    const auto check  = handle & 0x7FFF | (((handle >> 15) & 0x3FF) << 14);
    AssertBool(check == value);

    const auto validation = (((handle >> 15) & 0x3FF) << 14);
    AssertBool(static_cast<uint32_t>(index) == (value - validation));

    return pEntity;
}
#endif

bool AddHookedNetMsgId(const uint16_t netMsgId)
{
    if (IsHookIdExist(netMsgId))
    {
        return false;
    }
    s_HookedNetMsgIds.emplace_back(netMsgId);
    return true;
}

void RemoveHookedNetMsgId(uint16_t netMsgId)
{
    if (!IsHookIdExist(netMsgId))
    {
        WARN("Net message ID: \"%d\" does not exist in the list!", netMsgId);
        return;
    }

    std::erase(s_HookedNetMsgIds, netMsgId);
}

// const char* pszParticleName, Vector* pOrigin, Vector* pAngles, CBaseEntity* pEntity, bool bAllowDormantSpawn = false, int32_t nSplitScreenPlayerSlot = -1, IRecipientFilter* pFilter = nullptr, bool bResetAllParticlesOnEntity = false
int32_t DispatchParticleEffectFilter(const char* pszParticleName, Vector* pOrigin, Vector* pAngles, IRecipientFilter* pFilter)
{
    s_bBypassDispatchEffect = true;

#ifdef PLATFORM_WINDOWS
    const auto ret = address::server::UTIL_DispatchParticleEffectFilterPosition(pszParticleName, pOrigin, pAngles, nullptr, false, -1, pFilter, false);
#else
    auto origin_xy = *reinterpret_cast<double*>(pOrigin);
    auto angles_xy = *reinterpret_cast<double*>(pAngles);

    const auto ret = address::server::UTIL_DispatchParticleEffectFilterPosition(pszParticleName, nullptr, false, -1, pFilter, false, origin_xy, pOrigin->z, angles_xy, pAngles->z);
#endif

    s_bBypassDispatchEffect = false;
    return ret;
}
int32_t DispatchParticleEffectFilter(const char* pszParticleName, CBaseEntity* pEntity, Vector* pOrigin, Vector* pAngles, bool bResetAllParticlesOnEntity, IRecipientFilter* pFilter)
{
    s_bBypassDispatchEffect = true;

#ifdef PLATFORM_WINDOWS
    const auto ret = address::server::UTIL_DispatchParticleEffectFilterPosition(pszParticleName, pOrigin, pAngles, pEntity, false, -1, pFilter, bResetAllParticlesOnEntity);
#else
    auto origin_xy = *reinterpret_cast<double*>(pOrigin);
    auto angles_xy = *reinterpret_cast<double*>(pAngles);

    const auto ret = address::server::UTIL_DispatchParticleEffectFilterPosition(pszParticleName, pEntity, false, -1, pFilter, bResetAllParticlesOnEntity, origin_xy, pOrigin->z, angles_xy, pAngles->z);
#endif

    s_bBypassDispatchEffect = false;
    return ret;
}
// const char* pszParticleName, ParticleAttachment_t iAttachType, CBaseEntity* pEntity, uint8_t iAttachmentIndex, uint32_t /* CUtlStringToken */ iAttachmentName = 0, bool bResetAllParticlesOnEntity = false, int32_t nSplitScreenPlayerSlot = -1, IRecipientFilter* pFilter = nullptr, bool bAllowDormantSpawn = false
int32_t DispatchParticleEffectFilter(const char* pszParticleName, CBaseEntity* pEntity, ParticleAttachment_t iAttachType, IRecipientFilter* pFilter, uint8_t iAttachmentIndex, bool bResetAllParticlesOnEntity)
{
    s_bBypassDispatchEffect = true;
    const auto ret          = address::server::UTIL_DispatchParticleEffectFilterAttachment(pszParticleName, iAttachType, pEntity, iAttachmentIndex, 0, bResetAllParticlesOnEntity, -1, pFilter, false);
    s_bBypassDispatchEffect = false;
    return ret;
}

BeginStaticHookScope(UTIL_DispatchEffect)
{
    DeclareStaticDetourHook(UTIL_DispatchEffect, void, (const char* pName, CEffectData* pData))
    {
        if (s_bBypassDispatchEffect)
            return UTIL_DispatchEffect(pName, pData);

        UTIL_DispatchEffect(pName, pData);
    }
}

BeginStaticHookScope(UTIL_DispatchEffectFilter)
{
#ifdef PLATFORM_WINDOWS
    DeclareStaticDetourHook(UTIL_DispatchEffectFilter, void, (IRecipientFilter * pFilter, float delay, const char* pName, CEffectData* pData))
    {
        if (s_bBypassDispatchEffect)
            return UTIL_DispatchEffectFilter(pFilter, delay, pName, pData);

        UTIL_DispatchEffectFilter(pFilter, delay, pName, pData);
    }
#else
    DeclareStaticDetourHook(UTIL_DispatchEffectFilter, void, (IRecipientFilter * pFilter, const char* pName, CEffectData* pData))
    {
        if (s_bBypassDispatchEffect)
            return UTIL_DispatchEffectFilter(pFilter, pName, pData);

        UTIL_DispatchEffectFilter(pFilter, pName, pData);
    }
#endif
}

BeginMemberHookScope(IGameEventSystem)
{
    DeclareVirtualHook(PostEventAbstract, void, (IGameEventSystem * pGameEventSystem, int nSlot, bool bLocalOnly, int nClientCount, const NetworkReceiver_t* clients, INetworkMessageInternal* pEvent, CNetMessage* pData, unsigned long nSize, NetChannelBufType_t bufType))
    {
#ifdef HOOK_NETWORK_ASSERT
        WARN("%10s: 0x%p\n"   // IGameEventSystem*
             "%10s: %d\n"     // int
             "%10s: %s\n"     // bool
             "%10s: %d\n"     // int
             "%10s: 0x%llx\n" // const uint64_t*
             "%10s: 0x%p\n"   // INetworkSerializable*
             "%10s: 0x%p\n"   // const void*
             "%10s: %lu\n"    // unsigned long
             "%10s: %d",      // NetChannelBufType_t
             "this",
             pGameEventSystem,
             "nSlot", nSlot,
             "bLocalOnly", BOOLVAL(bLocalOnly),
             "nClientCount", nClientCount,
             "clients", *clients,
             "pEvent", pEvent,
             "pData", pData,
             "nSize", nSize,
             "bufType", bufType);
#endif

        if (s_bBypassNetMessageHook)
            return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);

        const auto pInfo  = pEvent->GetNetMessageInfo();
        const auto nMsgId = pInfo->m_MessageId;
        const auto pMsg   = static_cast<const google::protobuf::Message*>(pData->AsProto());

        switch (nMsgId)
        {
        case GE_FireBulletsId: {
            TransmitCheckFireBullets(clients);

            const auto msg   = reinterpret_cast<CMsgTEFireBullets*>(const_cast<google::protobuf::Message*>(pMsg));
            const auto state = TransmitGetFireBulletState(msg->weapon_id());

            if (state == 0)
            {
                // do nothing
            }
            else if (state == 1)
            {
                // blocked
                return;
            }
            else if (state == 2)
            {
                msg->set_weapon_id(0);
                msg->set_sound_type(9);
                msg->set_item_def_index(61);
            }
            else if (state == 3)
            {
                msg->set_weapon_id(0);
                msg->set_sound_type(9);
                msg->set_item_def_index(60);
            }
            else
            {
                return FatalError("Invalid FireBullet state");
            }

#ifdef HOOK_NETWORK_ASSERT
            const auto pPlayer = GetEntityFromProtobuf(msg->player());
            const auto pWeapon = GetEntityFromProtobuf(msg->weapon_id());

            CUtlString buf;
            pInfo->m_pBinding->ToString(pData, buf);

            LOG("GE_FireBullets ->\n player: %d.%s\n weapon:%d.%s\n %s",
                pPlayer ? pPlayer->GetEntityIndex() : -1, pPlayer ? pPlayer->GetClassname() : "<null>",
                pWeapon ? pWeapon->GetEntityIndex() : -1, pWeapon ? pWeapon->GetClassname() : "<null>",
                buf.String());
#endif

            return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);
        }
        case TE_MuzzleFlashId: {
            TransmitCheckFireBullets(clients);
            return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);
        }
        case TE_WorldDecalId: {
            TransmitCheckWorldDecals(clients);
            return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);
        }
        case TE_BloodStreamId:
        case TE_ArmorRicochetId: {
            TransmitCheckBloodEffect(clients);
            return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);
        }
        case UM_TextMsg: {
            const auto msg = reinterpret_cast<const CUserMessageTextMsg*>(pMsg);
            if (msg->param_size() < 1)
                return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);

            const auto dest = msg->dest();
            const auto name = msg->param(0).c_str();

            auto       changed   = false;
            auto       receivers = *clients;
            const auto action    = forwards::OnTextMsg->Invoke(dest, name, *clients, &changed, &receivers);

            if (action == EHookAction::SkipCallReturnOverride)
                return;

            if (action == EHookAction::Ignored)
                return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);

            if (action == EHookAction::ChangeParamReturnDefault)
            {
                if (changed)
                {
                    *const_cast<NetworkReceiver_t*>(clients) = receivers;
                    nClientCount                             = CS_MAX_PLAYERS;
                }

                return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);
            }

            return FatalError("You can NOT do this");
        }
        default: {
            if (!IsHookIdExist(nMsgId))
            {
                return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);
            }

            auto changed   = false;
            auto receivers = *clients;

            const auto action = forwards::OnPostEventAbstract->Invoke(nMsgId, pMsg, *clients, &changed, &receivers);

            if (action == EHookAction::Ignored)
                return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);

            if (action == EHookAction::SkipCallReturnOverride)
                return;

            if (action == EHookAction::ChangeParamReturnDefault)
            {
                if (changed)
                {
                    *const_cast<NetworkReceiver_t*>(clients) = receivers;
                    nClientCount                             = CS_MAX_PLAYERS;
                }
                return PostEventAbstract(pGameEventSystem, nSlot, bLocalOnly, nClientCount, clients, pEvent, pData, nSize, bufType);
            }

            return FatalError("You can NOT do this");
        }
        }
    }
}

void InstallNetworkHooks()
{
    InstallVirtualHookAutoWithVTableManual(IGameEventSystem, PostEventAbstract, engine, CGameEventSystem);

    InstallStaticDetourManual(UTIL_DispatchEffect, address::server::UTIL_DispatchEffect);
    InstallStaticDetourManual(UTIL_DispatchEffectFilter, address::server::UTIL_DispatchEffectFilter);
}