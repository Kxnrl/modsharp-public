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

#include "bridge/natives/GameNatives.h"
#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"

#include "hook/network.h"
#include "logging.h"
#include "netmessage.h"
#include "sdkproxy.h"
#include "steamworks.h"

#include "CoreCLR/NativeSpan.h"
#include "CoreCLR/RuntimeProtobufMessage.h"
#include "CoreCLR/RuntimeRecipientFilter.h"

#include "cstrike/interface/CDedicatedServerWorkshopManager.h"
#include "cstrike/interface/IGameRules.h"
#include "cstrike/interface/IGameSystem.h"
#include "cstrike/interface/IGameTypes.h"
#include "cstrike/interface/IResourceManifest.h"
#include "cstrike/interface/IResourceSystem.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/CTrace.h"
#include "cstrike/type/ResourceSystem.h"

#include "hook/extern/ExtraAddon.h"
#include "steamproxy.h"
#include "strtool.h"

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
    auto resource = CResourceNameTyped::Create(fileName);
    if (resource.m_nResourceId == 0)
        return RESOURCE_STATUS_UNKNOWN;

    return g_pResourceSystem->GetResourceStatus(resource);
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

static void* GetGameSystemFactory()
{
    return CBaseGameSystemFactory::GetFirst();
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

static uint64_t DualAddonGetPublishFileId()
{
    return ::GetDualAddonId();
}

static bool DualAddonSetPublishFileId(uint64_t publishFileId)
{
    return ::SetDualAddonId(publishFileId);
}

static NativeSpan<uint64_t> ExtraAddonGetIds()
{
    static std::vector<uint64_t> s_idCache;
    s_idCache.clear();
    for (const auto& a : ExtraAddon::GetServerAddons())
    {
        if (const auto id = strtoull(a.c_str(), nullptr, 10); id > 0)
            s_idCache.push_back(id);
    }
    return NativeSpan<uint64_t>(s_idCache.data(), static_cast<int>(s_idCache.size()));
}

static const char* ExtraAddonGetServerAddons()
{
    static std::string s_buffer;
    s_buffer = StringJoin(ExtraAddon::GetServerAddons(), ",");
    return s_buffer.c_str();
}

static const char* ExtraAddonGetGlobalClientAddons()
{
    static std::string s_buffer;
    s_buffer = StringJoin(ExtraAddon::GetGlobalClientAddons(), ",");
    return s_buffer.c_str();
}

static const char* ExtraAddonGetMountedAddons()
{
    static std::string s_buffer;
    s_buffer = StringJoin(ExtraAddon::GetMountedAddons(), ",");
    return s_buffer.c_str();
}

static const char* ExtraAddonGetClientAddons(SteamId_t steamId)
{
    static std::string s_buffer;
    s_buffer = StringJoin(ExtraAddon::GetClientAddons(steamId), ",");
    return s_buffer.c_str();
}

static const char* ExtraAddonGetCurrentWorkshopMap()
{
    return ExtraAddon::GetCurrentWorkshopMap().c_str();
}

static bool ExtraAddonAddAddon(const char* addon, bool refresh)
{
    return ExtraAddon::AddAddon(addon, refresh);
}

static bool ExtraAddonRemoveAddon(const char* addon, bool refresh)
{
    return ExtraAddon::RemoveAddon(addon, refresh);
}

static void ExtraAddonClearAddons()
{
    ExtraAddon::ClearAddons();
}

static void ExtraAddonRefreshAddons(bool reloadMap)
{
    ExtraAddon::RefreshAddons(reloadMap);
}

static void ExtraAddonReloadMap()
{
    ExtraAddon::ReloadMap();
}

static bool ExtraAddonMount(const char* addon, bool addToTail)
{
    return ExtraAddon::MountAddon(addon, addToTail);
}

static bool ExtraAddonUnmount(const char* addon)
{
    return ExtraAddon::UnmountAddon(addon);
}

static bool ExtraAddonIsMounted(const char* addon, bool checkWorkshopMap)
{
    return ExtraAddon::IsAddonMounted(addon, checkWorkshopMap);
}

static void ExtraAddonAddClientAddon(const char* addon, SteamId_t steamId, bool refresh)
{
    ExtraAddon::AddClientAddon(addon, steamId, refresh);
}

static void ExtraAddonRemoveClientAddon(const char* addon, SteamId_t steamId)
{
    ExtraAddon::RemoveClientAddon(addon, steamId);
}

static void ExtraAddonClearClientAddons(SteamId_t steamId)
{
    ExtraAddon::ClearClientAddons(steamId);
}

static bool ExtraAddonDownload(const char* addon, bool important, bool force)
{
    return ExtraAddon::DownloadAddon(addon, important, force);
}

static bool ExtraAddonHasUGCConnection()
{
    return g_pSteamApiProxy && g_pSteamApiProxy->GetSteamUGC() != nullptr;
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

    bridge::CreateNative("Game.GetGameSystemFactory", reinterpret_cast<void*>(GetGameSystemFactory));

    bridge::CreateNative("Game.DualAddonPurgeCheck", reinterpret_cast<void*>(DualAddonPurgeCheck));
    bridge::CreateNative("Game.DualAddonOverrideCheck", reinterpret_cast<void*>(DualAddonOverrideCheck));
    bridge::CreateNative("Game.DualAddonGetPublishFileId", reinterpret_cast<void*>(DualAddonGetPublishFileId));
    bridge::CreateNative("Game.DualAddonSetPublishFileId", reinterpret_cast<void*>(DualAddonSetPublishFileId));

    bridge::CreateNative("Game.ExtraAddonGetIds", reinterpret_cast<void*>(ExtraAddonGetIds));
    bridge::CreateNative("Game.ExtraAddonGetServerAddons", reinterpret_cast<void*>(ExtraAddonGetServerAddons));
    bridge::CreateNative("Game.ExtraAddonGetGlobalClientAddons", reinterpret_cast<void*>(ExtraAddonGetGlobalClientAddons));
    bridge::CreateNative("Game.ExtraAddonGetMountedAddons", reinterpret_cast<void*>(ExtraAddonGetMountedAddons));
    bridge::CreateNative("Game.ExtraAddonGetClientAddons", reinterpret_cast<void*>(ExtraAddonGetClientAddons));
    bridge::CreateNative("Game.ExtraAddonGetCurrentWorkshopMap", reinterpret_cast<void*>(ExtraAddonGetCurrentWorkshopMap));
    bridge::CreateNative("Game.ExtraAddonAddAddon", reinterpret_cast<void*>(ExtraAddonAddAddon));
    bridge::CreateNative("Game.ExtraAddonRemoveAddon", reinterpret_cast<void*>(ExtraAddonRemoveAddon));
    bridge::CreateNative("Game.ExtraAddonClearAddons", reinterpret_cast<void*>(ExtraAddonClearAddons));
    bridge::CreateNative("Game.ExtraAddonRefreshAddons", reinterpret_cast<void*>(ExtraAddonRefreshAddons));
    bridge::CreateNative("Game.ExtraAddonReloadMap", reinterpret_cast<void*>(ExtraAddonReloadMap));
    bridge::CreateNative("Game.ExtraAddonMount", reinterpret_cast<void*>(ExtraAddonMount));
    bridge::CreateNative("Game.ExtraAddonUnmount", reinterpret_cast<void*>(ExtraAddonUnmount));
    bridge::CreateNative("Game.ExtraAddonIsMounted", reinterpret_cast<void*>(ExtraAddonIsMounted));
    bridge::CreateNative("Game.ExtraAddonAddClientAddon", reinterpret_cast<void*>(ExtraAddonAddClientAddon));
    bridge::CreateNative("Game.ExtraAddonRemoveClientAddon", reinterpret_cast<void*>(ExtraAddonRemoveClientAddon));
    bridge::CreateNative("Game.ExtraAddonClearClientAddons", reinterpret_cast<void*>(ExtraAddonClearClientAddons));
    bridge::CreateNative("Game.ExtraAddonDownload", reinterpret_cast<void*>(ExtraAddonDownload));
    bridge::CreateNative("Game.ExtraAddonHasUGCConnection", reinterpret_cast<void*>(ExtraAddonHasUGCConnection));

    bridge::CreateNative("Game.AddWorkshopMap", reinterpret_cast<void*>(AddWorkshopMap));
    bridge::CreateNative("Game.WorkshopMapExists", reinterpret_cast<void*>(WorkshopMapExists));
    bridge::CreateNative("Game.RemoveWorkshopMap", reinterpret_cast<void*>(RemoveWorkshopMap));
    bridge::CreateNative("Game.ListWorkshopMaps", reinterpret_cast<void*>(ListWorkshopMaps));
}
} // namespace natives::game