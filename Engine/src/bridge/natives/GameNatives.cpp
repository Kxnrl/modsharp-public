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

#include "bridge/natives/GameNatives.h"
#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"

#include "hook/network.h"
#include "logging.h"
#include "netmessage.h"
#include "sdkproxy.h"

#include "CoreCLR/NativeSpan.h"
#include "CoreCLR/RuntimeProtobufMessage.h"
#include "CoreCLR/RuntimeRecipientFilter.h"

#include "cstrike/interface/CDedicatedServerWorkshopManager.h"
#include "cstrike/interface/IGameRules.h"
#include "cstrike/interface/IGameTypes.h"
#include "cstrike/interface/IResourceManifest.h"
#include "cstrike/interface/IResourceSystem.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/CTrace.h"
#include "cstrike/type/ResourceSystem.h"

extern void DualMountAddonOverrideClientCheck(SteamId_t steamId, double time);
extern void DualMountAddonPurgeClientCheck();

namespace google::protobuf
{
class Message;
}

namespace natives::game
{

static void PrecacheResource(IResourceManifest* pContext, const char* pszResource)
{
    pContext->m_pResourceManifest->AddResource(pszResource);
}

static void PrintChannelAll(HudPrint_t dest, const char* message)
{
    sv->PrintChannelAll(dest, message);
}

static void PrintChannelTeam(HudPrint_t dest, CStrikeTeam_t team, const char* message)
{
    sv->PrintChannelTeam(dest, team, message);
}

static void PrintChannelFilter(HudPrint_t channel, const char* message, RuntimeRecipientFilter* pFilter)
{
    int32_t           bitWide   = 0;
    NetworkReceiver_t receivers = 0;

    if (ParseNetworkReceivers(pFilter, &bitWide, &receivers))
    {
        UTIL_TextMsgFilter(bitWide, receivers, channel, message);
    }
}

static void RadioMessageAll(PlayerSlot_t slot, const char* name, const char* params1, const char* params2, const char* params3, const char* params4)
{
    CBroadcastRecipientFilter filter(true);
    filter.AddAllPlayer();

    address::server::UTIL_RadioMessage(&filter, slot, 3, name, params1, params2, params3, params4);
}

static void RadioMessageTeam(CStrikeTeam_t team, PlayerSlot_t slot, const char* name, const char* params1, const char* params2, const char* params3, const char* params4)
{
    CBroadcastRecipientFilter filter(true);
    filter.AddTeamPlayer(team);

    address::server::UTIL_RadioMessage(&filter, slot, 3, name, params1, params2, params3, params4);
}

static void TerminateRound(float delay, uint32_t reason, bool bypassHook, TeamRewardInfo* info, int32_t size)
{
    gameRules->TerminateRound(delay, reason, bypassHook, info, size);
}

// HACK 这里因为返回值问题可能导致C#传参问题
static CTraceResult_t g_TraceResult = {};
class CTraceFilterCustom : public CTraceFilter
{
public:
    explicit CTraceFilterCustom(uint64_t mask, uint8_t layer, RnQueryObjectFlags flags) :
        CTraceFilter(mask, layer, flags)
    {
        m_bIterateEntities = true;
    }

    bool ShouldHit(CBaseEntity* pHitEntity) override
    {
        return forwards::OnRuntimeTraceFilterCallback->Invoke(pHitEntity);
    }
};

static void TraceLine(Vector* start, Vector* end, uint64_t layers, uint8_t collisionGroup, RnQueryObjectFlags flag, uint64_t excludeLayers, CBaseModelEntity* pIgnore1, CBaseModelEntity* pIgnore2, bool ignorePlayers, CTraceResult_t& result)
{
    static auto physicsQuery = g_pGameData->GetAddress<CGamePhysicsQueryInterface*>("g_pPhysicsQuery");
    AssertPtr(physicsQuery);

    CRay_t     ray;
    CGameTrace trace;

    if (ignorePlayers)
    {
        CTraceFilterIgnorePlayers filter(layers, collisionGroup, flag);
        filter.SetIgnoreEntities(pIgnore1, pIgnore2);
        filter.SetExcludeLayers(excludeLayers);

        physicsQuery->TraceLine(&ray, start, end, &filter, &trace);
    }
    else
    {
        CTraceFilterIgnoreEntities filter(layers, collisionGroup, flag);
        filter.SetIgnoreEntities(pIgnore1, pIgnore2);
        filter.SetExcludeLayers(excludeLayers);

        physicsQuery->TraceLine(&ray, start, end, &filter, &trace);
    }

    result.Init(trace);
}

static void TraceLineFilter(Vector* start, Vector* end, uint64_t layers, uint8_t collisionGroup, RnQueryObjectFlags flag, uint64_t excludeLayers, CTraceResult_t& result)
{
    static auto physicsQuery = g_pGameData->GetAddress<CGamePhysicsQueryInterface*>("g_pPhysicsQuery");
    AssertPtr(physicsQuery);
    AssertPtr(forwards::OnRuntimeTraceFilterCallback);

    CRay_t             ray;
    CGameTrace         trace;
    CTraceFilterCustom filter(layers, collisionGroup, flag);
    filter.SetExcludeLayers(excludeLayers);

    physicsQuery->TraceLine(&ray, start, end, &filter, &trace);

    result.Init(trace);
}

static void TraceShape(ShapeRay_t* ray, Vector* start, Vector* end, uint64_t layers, uint8_t collisionGroup, RnQueryObjectFlags flag, uint64_t excludeLayers, CBaseModelEntity* pIgnore1, CBaseModelEntity* pIgnore2, bool ignorePlayers, CTraceResult_t& result)
{
    static auto physicsQuery = g_pGameData->GetAddress<CGamePhysicsQueryInterface*>("g_pPhysicsQuery");
    AssertPtr(physicsQuery);

    CGameTrace trace;

    if (ignorePlayers)
    {
        CTraceFilterIgnorePlayers filter(layers, collisionGroup, flag);
        filter.SetIgnoreEntities(pIgnore1, pIgnore2);
        filter.SetExcludeLayers(excludeLayers);

        physicsQuery->TraceShape(ray, start, end, &filter, &trace);
    }
    else
    {
        CTraceFilterIgnoreEntities filter(layers, collisionGroup, flag);
        filter.SetIgnoreEntities(pIgnore1, pIgnore2);
        filter.SetExcludeLayers(excludeLayers);

        physicsQuery->TraceShape(ray, start, end, &filter, &trace);
    }

    result.Init(trace);
}

static void TraceShapeFilter(ShapeRay_t* ray, Vector* start, Vector* end, uint64_t layers, uint8_t collisionGroup, RnQueryObjectFlags flag, uint64_t excludeLayers, CTraceResult_t& result)
{
    static auto physicsQuery = g_pGameData->GetAddress<CGamePhysicsQueryInterface*>("g_pPhysicsQuery");
    AssertPtr(physicsQuery);
    AssertPtr(forwards::OnRuntimeTraceFilterCallback);

    CGameTrace         trace;
    CTraceFilterCustom filter(layers, collisionGroup, flag);
    filter.SetExcludeLayers(excludeLayers);

    physicsQuery->TraceShape(ray, start, end, &filter, &trace);

    result.Init(trace);
}

static int DispatchParticleEffectPosition(const char* pszParticleName, Vector* pOrigin, Vector* pAngles, RuntimeRecipientFilter* pFilter)
{
    if (pFilter->Type == RuntimeRecipientFilterType::All)
    {
        return DispatchParticleEffectFilter(pszParticleName, pOrigin, pAngles, nullptr);
    }

    CBroadcastRecipientFilter filter(pFilter, true);
    return DispatchParticleEffectFilter(pszParticleName, pOrigin, pAngles, &filter);
}

static int DispatchParticleEffectEntityPosition(const char* pszParticleName, CBaseEntity* pEntity, Vector* pOrigin, Vector* pAngles, bool bResetAllParticlesOnEntity, RuntimeRecipientFilter* pFilter)
{
    if (pFilter->Type == RuntimeRecipientFilterType::All)
    {
        return DispatchParticleEffectFilter(pszParticleName, pEntity, pOrigin, pAngles, bResetAllParticlesOnEntity, nullptr);
    }

    CBroadcastRecipientFilter filter(pFilter, true);
    return DispatchParticleEffectFilter(pszParticleName, pEntity, pOrigin, pAngles, bResetAllParticlesOnEntity, &filter);
}

static int DispatchParticleEffectAttachment(const char* pszParticleName, ParticleAttachment_t iAttachType, CBaseEntity* pEntity, uint8_t iAttachmentIndex, bool bResetAllParticlesOnEntity, RuntimeRecipientFilter* pFilter)
{
    if (pFilter->Type == RuntimeRecipientFilterType::All)
    {
        return DispatchParticleEffectFilter(pszParticleName, pEntity, iAttachType, nullptr, iAttachmentIndex, bResetAllParticlesOnEntity);
    }

    CBroadcastRecipientFilter filter(pFilter, true);
    return DispatchParticleEffectFilter(pszParticleName, pEntity, iAttachType, &filter, iAttachmentIndex, bResetAllParticlesOnEntity);
}

static void* GetMapGroupMapList(const char* mapGroup)
{
    return g_pGameTypes->GetMapGroupMapList(mapGroup);
}

static NativeSpan<uint8_t> FindResourceDataBlockInfo(const char* filePath, const char* pathId)
{
    const auto buffer = ReadGameFile(filePath, pathId);
    if (buffer.empty())
        return NativeSpan<uint8_t>{nullptr, 0};

    CResourceBlockInfo output{};
    if (Resource_FindBlockInfo(reinterpret_cast<const ResourceFileHeader_t*>(buffer.data()), g_ResourceBlockId_Data, output))
        return NativeSpan{reinterpret_cast<uint8_t*>(output.m_pBlockData), output.m_nSize};

    return NativeSpan<uint8_t>{nullptr, 0};
}

static ResourceStatus_t GetResourceStatus(const char* fileName)
{
    CResourceNameTyped resource{};
    if (address::resource::CResourceNameTyped_ResolveResourceName(resource, fileName))
        return g_pResourceSystem->GetResourceStatus(resource);
    return RESOURCE_STATUS_UNKNOWN;
}

static const char* GetAddonName()
{
    AssertPtr(sv);
    return sv->GetAddonName();
}

static const char* GetMapName()
{
    AssertPtr(sv);
    return sv->GetMapName();
}

static CCSWeaponBaseVData* FindWeaponVDataByName(const char* name)
{
    return address::server::FindWeaponVDataByName(1, name);
}

static void DualAddonPurgeCheck()
{
    ::DualMountAddonPurgeClientCheck();
}

static void DualAddonOverrideCheck(SteamId_t steamId, double time)
{
    ::DualMountAddonOverrideClientCheck(steamId, time);
}

static bool AddWorkshopMap(uint64_t sharedFileId, const char* mapName, const char* path)
{
    return g_pServerWorkshopManager->AddWorkshopMap(sharedFileId, mapName, path);
}

static bool WorkshopMapExists(uint64_t sharedFileId)
{
    return g_pServerWorkshopManager->WorkshopMapExists(sharedFileId);
}

static bool RemoveWorkshopMap(uint64_t sharedFileId)
{
    return g_pServerWorkshopManager->RemoveWorkshopMap(sharedFileId);
}

static void* ListWorkshopMaps()
{
    static CUtlVector<WorkshopMap_t> list{};
    list.Purge();
    g_pServerWorkshopManager->ListWorkshopMaps(&list);
    return &list;
}

void Init()
{
    bridge::CreateNative("Engine.PrecacheResource", reinterpret_cast<void*>(PrecacheResource));

    bridge::CreateNative("Game.PrintChannelAll", reinterpret_cast<void*>(PrintChannelAll));
    bridge::CreateNative("Game.PrintChannelTeam", reinterpret_cast<void*>(PrintChannelTeam));
    bridge::CreateNative("Game.PrintChannelFilter", reinterpret_cast<void*>(PrintChannelFilter));

    bridge::CreateNative("Game.RadioMessageAll", reinterpret_cast<void*>(RadioMessageAll));
    bridge::CreateNative("Game.RadioMessageTeam", reinterpret_cast<void*>(RadioMessageTeam));

    bridge::CreateNative("Game.TerminateRound", reinterpret_cast<void*>(TerminateRound));

    bridge::CreateNative("Game.TraceLine", reinterpret_cast<void*>(TraceLine));
    bridge::CreateNative("Game.TraceLineFilter", reinterpret_cast<void*>(TraceLineFilter));
    bridge::CreateNative("Game.TraceShape", reinterpret_cast<void*>(TraceShape));
    bridge::CreateNative("Game.TraceShapeFilter", reinterpret_cast<void*>(TraceShapeFilter));

    bridge::CreateNative("Game.DispatchParticleEffectPosition", reinterpret_cast<void*>(DispatchParticleEffectPosition));
    bridge::CreateNative("Game.DispatchParticleEffectEntityPosition", reinterpret_cast<void*>(DispatchParticleEffectEntityPosition));
    bridge::CreateNative("Game.DispatchParticleEffectAttachment", reinterpret_cast<void*>(DispatchParticleEffectAttachment));

    bridge::CreateNative("Game.GetMapGroupMapList", reinterpret_cast<void*>(GetMapGroupMapList));

    bridge::CreateNative("Game.FindResourceDataBlockInfo", reinterpret_cast<void*>(FindResourceDataBlockInfo));
    bridge::CreateNative("Game.GetResourceStatus", reinterpret_cast<void*>(GetResourceStatus));

    bridge::CreateNative("Game.GetAddonName", reinterpret_cast<void*>(GetAddonName));
    bridge::CreateNative("Game.GetMapName", reinterpret_cast<void*>(GetMapName));

    bridge::CreateNative("Game.FindWeaponVDataByName", reinterpret_cast<void*>(FindWeaponVDataByName));

    bridge::CreateNative("Game.DualAddonPurgeCheck", reinterpret_cast<void*>(DualAddonPurgeCheck));
    bridge::CreateNative("Game.DualAddonOverrideCheck", reinterpret_cast<void*>(DualAddonOverrideCheck));

    bridge::CreateNative("Game.AddWorkshopMap", reinterpret_cast<void*>(AddWorkshopMap));
    bridge::CreateNative("Game.WorkshopMapExists", reinterpret_cast<void*>(WorkshopMapExists));
    bridge::CreateNative("Game.RemoveWorkshopMap", reinterpret_cast<void*>(RemoveWorkshopMap));
    bridge::CreateNative("Game.ListWorkshopMaps", reinterpret_cast<void*>(ListWorkshopMaps));
}
} // namespace natives::game