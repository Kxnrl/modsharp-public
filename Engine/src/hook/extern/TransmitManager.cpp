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
#include "gamedata.h"
#include "global.h"
#include "hook/installer.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"

#include "cstrike/component/PlayerPawnComponent.h"
#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/type/CBitVec.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/VProf.h"

#include <algorithm>
#include <mutex>
#include <shared_mutex>
#include <unordered_set>

#include <safetyhook.hpp>

// TODO https://github.com/Kxnrl/modsharp/issues/348
// 当前TransmitManager是从SM Extension移植的
// 存在很多Source1的逻辑和早期历史遗留问题
// 应该根据Source2特性重构
// 且Source2中的Parallel并不会真正的并行CheckTransmit
// 与CSGO中的行为完全不一致, 这里可能需要把锁取消以提高性能

// #define HOOK_EXTERN_TRANSMITMANAGER_ASSERT

class ISource2GameEntities;

using read_guard  = std::shared_lock<std::shared_mutex>;
using write_guard = std::unique_lock<std::shared_mutex>;

#define CLAMP(x, low, high) (MIN((MAX((x), (low))), (high)))
#define RLOCK \
    read_guard lock(g_MutexHooks)

#define WLOCK \
    write_guard lock(g_MutexHooks)

#define TICK_BASE_VALUE(v) \
    ((v) * TICK_RATE)

constexpr int MAX_ENTITY_COUNT = 16384;
constexpr int MAX_CHANNEL      = 5;
constexpr int TICK_RATE        = 64;

static std::shared_mutex g_MutexHooks;

enum BlockTE_t : int64_t
{
    BT_FireBullets = 0,
    BT_WorldDecals,
    BT_BloodEffect,
    BT_MapMusic,
    BT_Count
};

enum FireBulletState_t : int32_t
{
    FBS_None = 0,
    FBS_Block,
    FBS_PistolSilencer,
    FBS_RifleSilencer,
};

static uint64_t g_bitsBlockTempEnt[BT_Count];

static CConVarBaseData*                                ms_transmit_block_dead_player_pawn = nullptr;
static CConVarBaseData*                                ms_transmit_block_ownerless_pawn   = nullptr;
static std::unordered_map<uint32_t, FireBulletState_t> s_fire_bullet_state;

static uint32_t GetEHandleProtobufHandle(EHandle_t value)
{
    return (value & 0x7FFF) | (((value >> 15) & 0x3FF) << 14);
}

static uint32_t GetEntityProtobufHandle(const CBaseEntity* entity)
{
    const auto value = entity->GetActualEHandle().ToInt();
    return GetEHandleProtobufHandle(value);
}

static FireBulletState_t GetWeaponFireBulletState(const CBaseEntity* pWeapon)
{
    if (!pWeapon)
        return FBS_None;

    const auto it = s_fire_bullet_state.find(GetEntityProtobufHandle(pWeapon));
    if (it == s_fire_bullet_state.end())
        return FBS_None;

    return it->second;
}

static void SetBlockTempEntState(const BlockTE_t type, const PlayerSlot_t slot, const bool state)
{
    if (state)
        g_bitsBlockTempEnt[type] |= (1ull << slot);
    else
        g_bitsBlockTempEnt[type] &= ~(1ull << slot);
}

static bool GetBlockTempEntState(const BlockTE_t type, const PlayerSlot_t slot)
{
    return g_bitsBlockTempEnt[type] & (1ull << slot);
}

struct Entity2Networkable_t;

#ifdef USE_FULL_RE_TRANSMIT_INFO
class CCheckTransmitInfo
{
public:
    CBitVec<MAX_ENTITY_COUNT>* m_pTransmitEntity; // entity n is already marked for transmission

private:
    [[maybe_unused]] CBitVec<MAX_ENTITY_COUNT>* m_pUnkBitVec1; // 8
    [[maybe_unused]] CBitVec<MAX_ENTITY_COUNT>* m_pUnkBitVec2; // 16
    [[maybe_unused]] CBitVec<MAX_ENTITY_COUNT>* m_pUnkBitVec3; // 24

public:
    CBitVec<MAX_ENTITY_COUNT>* m_pTransmitAlways; // entity n is always send even if not in PVS (HLTV and Replay only)

private:
    [[maybe_unused]] CUtlVector<int32_t> m_pUnkVec; // 40
    [[maybe_unused]] void*               m_pVisInfo;
    [[maybe_unused]] int8_t              m_unk72[0x200];

public:
    int32_t m_nPlayerSlot;
    bool    m_bFullUpdate;

    PlayerSlot_t GetPlayerSlot() const
    {
        AssertBool(m_nPlayerSlot >= 0 && m_nPlayerSlot < CS_MAX_PLAYERS);

        return static_cast<PlayerSlot_t>(m_nPlayerSlot);
    }

    void ClearUnknown() const
    {
        m_pUnkBitVec1->ClearAll();
        m_pUnkBitVec2->ClearAll();
        m_pUnkBitVec3->ClearAll();
    }
};
#else
struct CCheckTransmitInfo
{
public:
    CBitVec<MAX_ENTITY_COUNT>* m_pTransmitEntity; // entity n is already marked for transmission

private:
    [[maybe_unused]] int8_t m_pad8[568];

public:
    int32_t m_nPlayerSlot;
    bool    m_bFullUpdate;

    PlayerSlot_t GetPlayerSlot() const
    {
        AssertBool(m_nPlayerSlot >= 0 && m_nPlayerSlot < CS_MAX_PLAYERS);

        return static_cast<PlayerSlot_t>(m_nPlayerSlot);
    }
};
#endif

static char* DumpString(const char* pOriginal)
{
    const auto nLen    = strlen(pOriginal);
    const auto pResult = new char[nLen + 1];
    memcpy(pResult, pOriginal, nLen + 1);
    return pResult;
}

class CHook
{
public:
    CHook(CBaseEntity* pEntity, bool defaultTransmit) :
        m_pEntity(pEntity),
        m_iEntityIndex(pEntity->GetEntityIndex()),
        m_Handle(pEntity->GetActualEHandle()),
        m_nOwnerEntity(INVALID_ENTITY_INDEX),
        m_bDefaultTransmit(defaultTransmit),
        m_bBlockAll(false),
        m_pszClassname(DumpString(pEntity->GetClassname()))
    {
        for (PlayerSlot_t i = 0; i <= CS_MAX_PLAYERS; i++)
        {
            // can see by default
            SetAllChannel(i, defaultTransmit);
        }

        // m_bRemoveFlags = (m_pszClassname != nullptr && (V_strcasecmp(m_pszClassname, "info_particle_system") == 0 || V_strcasecmp(m_pszClassname, "light_dynamic") == 0 || V_strcasecmp(m_pszClassname, "env_cascade_light") == 0 || V_strcasecmp(m_pszClassname, "env_projectedtexture") == 0 || V_strcasecmp(m_pszClassname, "env_screenoverlay") == 0 || V_strcasecmp(m_pszClassname, "env_fog_controller") == 0 || V_strcasecmp(m_pszClassname, "env_lightglow") == 0 || V_strcasecmp(m_pszClassname, "env_particlesmokegrenade") == 0 || V_strcasecmp(m_pszClassname, "env_global_light") == 0 || V_strcasecmp(m_pszClassname, "env_sun") == 0 || V_strcasecmp(m_pszClassname, "env_sprite") == 0 || V_strcasecmp(m_pszClassname, "point_camera") == 0 || V_strcasecmp(m_pszClassname, "point_viewproxy") == 0 || V_strcasecmp(m_pszClassname, "inferno") == 0 || V_strcasecmp(m_pszClassname, "sunshine_shadow_control") == 0 || V_strcasecmp(m_pszClassname, "cfe_player_decal") == 0 || V_strcasecmp(m_pszClassname, "func_precipitation") == 0 || V_strcasecmp(m_pszClassname, "cs_ragdoll") == 0 || V_strcasecmp(m_pszClassname, "info_target") == 0 || V_strncasecmp(m_pszClassname, "point_viewcontrol", 17) == 0 || V_strncasecmp(m_pszClassname, "env_fire", 8) == 0 || V_strncasecmp(m_pszClassname, "color_correction", 16) == 0));

#ifdef TRACE
        LOG("Construct::%d.%s::(%s) -> m_bRemoveFlags = %s\n", m_iEntityIndex, m_pszClassname, BOOLEAN(defaultTransmit), BOOLEAN(m_bRemoveFlags));
#endif
    }

    ~CHook()
    {
        delete[] m_pszClassname;
    }

private:
    void SetAllChannel(EntityIndex_t client, bool v)
    {
#ifdef TRACE
        if (m_iEntityIndex < CS_MAX_PLAYERS)
            LOG("SetAllChannel::%d.%s::(%d, %s)\n", m_iEntityIndex, m_pszClassname, client, BOOLEAN(v));
#endif

        for (auto c = MAX_CHANNEL; c >= 0; c--)
        {
            m_bCanTransmit[client][c] = v;
        }
    }

public:
    [[nodiscard]] bool CanSee(EntityIndex_t client) const
    {
        if (m_iEntityIndex == client)
            return true;

        for (auto c = MAX_CHANNEL; c >= 0; c--)
        {
            if (!m_bCanTransmit[client][c])
            {
#ifdef TRACE
                if (m_iEntityIndex < CS_MAX_PLAYERS && playerhelpers->GetGamePlayer(m_iEntityIndex) && playerhelpers->GetGamePlayer(client))
                    LOG("CanSee[%s] -> [%s] -> channel = %d\n", playerhelpers->GetGamePlayer(m_iEntityIndex)->GetName(), playerhelpers->GetGamePlayer(client)->GetName(), c);
#endif
                return false;
            }
        }

        return true;
    }

    void SetSee(EntityIndex_t client, bool can, int channel)
    {
#ifdef TRACE
        if (m_iEntityIndex < CS_MAX_PLAYERS)
            LOG("SetSee::%d.%s::(%d, %s, %d)\n", m_iEntityIndex, m_pszClassname, client, BOOLEAN(can), channel);
#endif
        if (channel == -1)
        {
            SetAllChannel(client, can);
            return;
        }

        const auto c              = std::clamp(channel, 0, MAX_CHANNEL);
        m_bCanTransmit[client][c] = can;
    }

    [[nodiscard]] bool GetState(EntityIndex_t client, int channel) const
    {
        return m_bCanTransmit[client][channel];
    }

    void SetDefault(EntityIndex_t client)
    {
#ifdef TRACE
        if (m_iEntityIndex < CS_MAX_PLAYERS)
            LOG("SetDefault::%d.%s::(%d)\n", client);
#endif
        SetAllChannel(client, m_bDefaultTransmit);
    }

    [[nodiscard]] EntityIndex_t GetOwner() const
    {
        return m_nOwnerEntity;
    }

    void SetOwner(EntityIndex_t owner)
    {
#ifdef TRACE
        if (m_iEntityIndex < CS_MAX_PLAYERS)
            LOG("SetOwner::%d.%s::(%d)\n", m_iEntityIndex, m_pszClassname, owner);
#endif
        m_nOwnerEntity = owner;
    }

    void SetBlockAll(bool state)
    {
#ifdef TRACE
        if (m_iEntityIndex < CS_MAX_PLAYERS)
            LOG("SetBlockAll::%d.%s::(%s)\n", m_iEntityIndex, m_pszClassname, BOOLEAN(state));
#endif
        m_bBlockAll = state;
    }

    [[nodiscard]] bool GetBlockAll() const
    {
        return m_bBlockAll;
    }

    [[nodiscard]] const char* GetClassname() const
    {
        return m_pszClassname;
    }

    [[nodiscard]] const CBaseHandle& GetEntityHandle() const
    {
        return m_Handle;
    }

private:
    CBaseEntity*  m_pEntity = nullptr;
    EntityIndex_t m_iEntityIndex;
    CBaseHandle   m_Handle;
    bool          m_bCanTransmit[CS_MAX_PLAYERS + 1][MAX_CHANNEL + 1];
    EntityIndex_t m_nOwnerEntity;
    bool          m_bRemoveFlags;
    bool          m_bDefaultTransmit;
    bool          m_bBlockAll;
    const char*   m_pszClassname = nullptr;
};

static CHook*  g_pHooks[MAX_ENTITY_COUNT];
static int64_t g_iBypassTick[CS_MAX_PLAYERS];
static bool    g_bEverSpawned[CS_MAX_PLAYERS];

inline bool IsEntityIndexInRange(int index)
{
    return index > 0 && index < MAX_ENTITY_COUNT;
}

void HookEntity(CBaseEntity* pEntity, bool defaultTransmit)
{
    // NOTE 在外部使用🔒, 否则会循环等待

    const auto index = pEntity->GetEntityIndex();

    if (!IsEntityIndexInRange(index))
    {
        // out-of-range
        WARN("Failed to hook entity %d -> out-of-range.", index);
        return;
    }

    if (pEntity->IsMarkedForDeletion())
    {
        WARN("Attempt add entity hook %d -> but it was marked for deletion", index);
        return;
    }

    if (g_pHooks[index] != nullptr)
    {
        FERROR("Entity Hook listener<%d> new=[%s<%s>], old=[%s<%s>] is not nullptr.", index,
               pEntity->GetClassname(), pEntity->GetActualEHandle().ToInt(),
               g_pHooks[index]->GetClassname(), g_pHooks[index]->GetEntityHandle().ToInt());
        return;
    }

    g_pHooks[index] = new CHook(pEntity, defaultTransmit);
}

void UnhookEntity(CBaseEntity* pEntity)
{
    // NOTE 在外部使用🔒, 否则会循环等待

    const auto index = pEntity->GetEntityIndex();

    if (g_pHooks[index] == nullptr)
    {
        return;
    }

    delete g_pHooks[index];
    g_pHooks[index] = nullptr;
}

static bool TransmitManagerAddEntityHooks(CBaseEntity* pEntity, bool defaultTransmit)
{
    if (pEntity->IsPlayerPawn())
    {
        WARN("You can NOT hook PlayerPawn!");
        return false;
    }

    WLOCK;

    HookEntity(pEntity, defaultTransmit);

    return true;
}

static bool TransmitManagerRemoveEntHooks(CBaseEntity* pEntity)
{
    WLOCK;

    UnhookEntity(pEntity);

    return true;
}

static bool TransmitManagerIsEntityHooked(CBaseEntity* pEntity)
{
    WLOCK;

    const auto index = pEntity->GetEntityIndex();

    return g_pHooks[index] != nullptr;
}

static EntityIndex_t TransmitManagerGetEntityOwner(int index)
{
    RLOCK;

    if (g_pHooks[index] == nullptr)
    {
        return -2;
    }

    return g_pHooks[index]->GetOwner();
}

static bool TransmitManagerSetEntityOwner(EntityIndex_t index, EntityIndex_t owner)
{
    WLOCK;

    if (g_pHooks[index] != nullptr)
    {
        g_pHooks[index]->SetOwner(owner);
        return true;
    }

    return false;
}

static bool TransmitManagerGetEntityState(EntityIndex_t index, EntityIndex_t controllerIndex, bool state, int channel = -1)
{
    WLOCK;

    if (g_pHooks[index] == nullptr)
        return true;

    if (channel == -1)
        return g_pHooks[index]->CanSee(controllerIndex);

    return g_pHooks[index]->GetState(controllerIndex, std::clamp(channel, 0, MAX_CHANNEL));
}

static bool TransmitManagerSetEntityState(EntityIndex_t index, EntityIndex_t controllerIndex, bool state, int channel)
{
    WLOCK;

    if (g_pHooks[index] != nullptr)
    {
        g_pHooks[index]->SetSee(controllerIndex, state, channel);
        return true;
    }

    return false;
}

static bool TransmitManagerGetEntityBlock(EntityIndex_t index)
{
    WLOCK;

    if (g_pHooks[index] == nullptr)
    {
        // can see
        return false;
    }

    return g_pHooks[index]->GetBlockAll();
}

static bool TransmitManagerSetEntityBlock(EntityIndex_t index, bool val)
{
    WLOCK;

    if (g_pHooks[index] == nullptr)
    {
        // can see
        return false;
    }

    g_pHooks[index]->SetBlockAll(val);
    return true;
}

static bool TransmitManagerGetTempEntState(BlockTE_t type, PlayerSlot_t slot, bool state)
{
    if (type > BT_Count || type < 0)
        return false;

    RLOCK;

    return GetBlockTempEntState(type, slot);
}

static void TransmitManagerSetTempEntState(BlockTE_t type, PlayerSlot_t slot, bool state)
{
    if (type > BT_Count || type < 0)
        return;

    WLOCK;

    SetBlockTempEntState(type, slot, state);
}

static void TransmitManagerClearReceiverState(PlayerSlot_t slot)
{
    WLOCK;

    for (auto i = 1; i < MAX_ENTITY_COUNT; i++)
    {
        if (g_pHooks[i] == nullptr)
        {
            continue;
        }

        g_pHooks[i]->SetDefault(slot);
    }
}

static int32_t TransmitManagerGetWeaponFireBulletState(CBaseWeapon* pWeapon)
{
    if (!pWeapon)
        return FBS_None;

    return GetWeaponFireBulletState(pWeapon);
}

static void TransmitManagerSetWeaponFireBulletState(CBaseWeapon* pWeapon, FireBulletState_t state)
{
    if (!pWeapon)
        return;

    const auto hWeapon = GetEntityProtobufHandle(pWeapon);
    if (state > FBS_None)
    {
        s_fire_bullet_state[hWeapon] = state;
    }
    else
    {
        s_fire_bullet_state.erase(hWeapon);
    }
}

namespace natives::transmit
{
void Init()
{
    bridge::CreateNative("Transmit.AddEntityHooks", reinterpret_cast<void*>(TransmitManagerAddEntityHooks));
    bridge::CreateNative("Transmit.RemoveEntHooks", reinterpret_cast<void*>(TransmitManagerRemoveEntHooks));
    bridge::CreateNative("Transmit.IsEntityHooked", reinterpret_cast<void*>(TransmitManagerIsEntityHooked));

    bridge::CreateNative("Transmit.GetEntityState", reinterpret_cast<void*>(TransmitManagerGetEntityState));
    bridge::CreateNative("Transmit.SetEntityState", reinterpret_cast<void*>(TransmitManagerSetEntityState));
    bridge::CreateNative("Transmit.GetEntityBlock", reinterpret_cast<void*>(TransmitManagerGetEntityBlock));
    bridge::CreateNative("Transmit.SetEntityBlock", reinterpret_cast<void*>(TransmitManagerSetEntityBlock));
    bridge::CreateNative("Transmit.GetEntityOwner", reinterpret_cast<void*>(TransmitManagerGetEntityOwner));
    bridge::CreateNative("Transmit.SetEntityOwner", reinterpret_cast<void*>(TransmitManagerSetEntityOwner));

    bridge::CreateNative("Transmit.GetTempEntState", reinterpret_cast<void*>(TransmitManagerGetTempEntState));
    bridge::CreateNative("Transmit.SetTempEntState", reinterpret_cast<void*>(TransmitManagerSetTempEntState));

    bridge::CreateNative("Transmit.ClearReceiverState", reinterpret_cast<void*>(TransmitManagerClearReceiverState));

    bridge::CreateNative("Transmit.GetWeaponFireBulletState", reinterpret_cast<void*>(TransmitManagerGetWeaponFireBulletState));
    bridge::CreateNative("Transmit.SetWeaponFireBulletState", reinterpret_cast<void*>(TransmitManagerSetWeaponFireBulletState));
}
} // namespace natives::transmit

BeginMemberHookScope(ISource2GameEntities)
{
    DeclareMemberDetourHook(CheckTransmit, void, (ISource2GameEntities * pGameEntities, CCheckTransmitInfo * *ppInfoList, int infoCount, CBitVec<16384>& unionTransmitEdicts1, CBitVec<16384>& unionTransmitEdicts2, const Entity2Networkable_t** pNetworkables, const uint16_t* pEntityIndicies, uint32_t nEntities))
    {
#ifdef HOOK_EXTERN_TRANSMITMANAGER_ASSERT
        WARN("%10s: 0x%p\n" // ISource2GameEntities*
             "%10s: 0x%p\n" // CCheckTransmitInfo**
             "%10s: %d\n"   // int
             "%10s: 0x%p\n" // CBitVec<16384>&
                            // ReSharper disable once CommentTypo
             "%10s: 0x%p\n" // const Entity2Networkable_t**
             "%10s: 0x%p\n" // const uint16*
             "%10s: %d",    // int
             "this", pGameEntities,
             "ppInfoList", ppInfoList,
             "infoCount", infoCount,
             "unionTransmitEdicts", unionTransmitEdicts,
             "pNetworkables", pNetworkables,
             "pEntityIndicies", pEntityIndicies,
             "nEntities", nEntities);
#endif

        VPROF_MS_HOOK();

        CheckTransmit(pGameEntities, ppInfoList, infoCount, unionTransmitEdicts1, unionTransmitEdicts2, pNetworkables, pEntityIndicies, nEntities);

        RLOCK;

        const auto blockPawn = ms_transmit_block_dead_player_pawn->GetValue<bool>();
        const auto blockNull = ms_transmit_block_ownerless_pawn->GetValue<bool>();

        CUtlVector<CBaseEntity*> nullPawns;

        if (blockNull)
        {
            CCSPlayerPawnBase* pNullPawn = nullptr;
            while ((pNullPawn = g_pGameEntitySystem->FindByClassnameCast<CCSPlayerPawnBase*>(pNullPawn, "player")) != nullptr)
            {
                if (!pNullPawn->IsPlayerPawn() || !pNullPawn->IsPlayer())
                {
                    continue;
                }

                const CBaseHandle& hController = pNullPawn->m_hOriginalController();
                if (g_pGameEntitySystem->FindEntityByEHandle(hController) == nullptr)
                {
                    nullPawns.AddToTail(pNullPawn);
                }
            }
            pNullPawn = nullptr;
            while ((pNullPawn = g_pGameEntitySystem->FindByClassnameCast<CCSPlayerPawnBase*>(pNullPawn, "observer")) != nullptr)
            {
                if (!pNullPawn->IsPlayerPawn() || pNullPawn->IsPlayer())
                {
                    continue;
                }

                const CBaseHandle& hController = pNullPawn->m_hOriginalController();
                if (g_pGameEntitySystem->FindEntityByEHandle(hController) == nullptr)
                {
                    nullPawns.AddToTail(pNullPawn);
                }
            }
        }

        for (int x = 0; x < infoCount; x++)
        {
            const auto& pInfo = ppInfoList[x];

            const auto playerSlot = pInfo->GetPlayerSlot();
            const auto pClient    = sv->GetClient(playerSlot);
            if (!pClient || !pClient->IsInGame() || pClient->IsFakeClient())
                continue;

            const EntityIndex_t controllerIndex     = playerSlot + 1;
            const auto          pReceiverController = g_pGameEntitySystem->FindEntityByIndex<CCSPlayerController*>(controllerIndex);

            if (!pReceiverController)
                continue;

            const auto pReceiverPawn     = pReceiverController->GetPlayerPawn();
            const auto pReceiverObserver = pReceiverController->GetObserverPawn();
            const auto iReceiverPawn     = pReceiverPawn ? pReceiverPawn->GetEntityIndex() : INVALID_ENTITY_INDEX;
            const auto maxClients        = static_cast<int32_t>(gpGlobals->MaxClients);

            // Team UnAssigned
            if (pReceiverController->GetTeam() == TEAM_UNASSIGNED)
            {
                constexpr int64_t pendingDelay = TICK_BASE_VALUE(5);
                g_iBypassTick[playerSlot]      = std::max<int64_t>(g_iBypassTick[playerSlot], pendingDelay);
            }

            // LOOP Pawn
            if (g_iBypassTick[playerSlot] <= 0 && g_bEverSpawned[playerSlot] && !pInfo->m_bFullUpdate)
            {
                for (auto i = 1; i <= maxClients; i++)
                {
                    if (i == controllerIndex)
                        continue;

                    if (g_pHooks[i] == nullptr)
                        continue;

                    if (g_pHooks[i]->CanSee(controllerIndex))
                        continue;

                    const auto pSenderController = g_pGameEntitySystem->FindEntityByIndex<CCSPlayerController*>(i);
                    if (!pSenderController)
                        continue;

                    const auto pPawn = pSenderController->GetPlayerPawn();
                    if (!pPawn)
                        continue;

                    pInfo->m_pTransmitEntity->Clear(pPawn->GetEntityIndex());
                }

                if (blockPawn)
                {
                    for (auto i = 1; i <= maxClients; i++)
                    {
                        if (i == controllerIndex)
                            continue;

                        const auto pSenderController = g_pGameEntitySystem->FindEntityByIndex<CCSPlayerController*>(i);
                        if (!pSenderController)
                            continue;

                        if (const auto pObserver = pSenderController->GetObserverPawn())
                        {
                            pInfo->m_pTransmitEntity->Clear(pObserver->GetEntityIndex());
                        }

                        if (const auto pPlayer = pSenderController->GetPlayerPawn())
                        {
                            if (pPlayer->GetLifeState() != LIFE_ALIVE)
                            {
                                pInfo->m_pTransmitEntity->Clear(pPlayer->GetEntityIndex());
                            }
                        }
                    }
                }
            }

            // LOOP NullPawn
            if (blockNull)
            {
                for (auto i = 0; i < nullPawns.Count(); i++)
                {
                    const auto pPawn = nullPawns.Element(i);
                    if (pPawn == pReceiverPawn || pPawn == pReceiverObserver)
                        continue;

                    pInfo->m_pTransmitEntity->Clear(pPawn->GetEntityIndex());
                }
            }

            // LOOP Other Entities
            {
                for (auto i = 0u; i < nEntities; i++)
                {
                    const auto entity = static_cast<int32_t>(pEntityIndicies[i]);

                    if (!pInfo->m_pTransmitEntity->IsBitSet(entity) || entity <= maxClients)
                    {
                        continue;
                    }

                    if (entity == controllerIndex || entity == iReceiverPawn)
                    {
                        continue;
                    }

                    if (g_pHooks[entity] == nullptr)
                    {
                        continue;
                    }

                    // 这里不应该能操作Pawn
                    const auto pEntity = g_pGameEntitySystem->FindEntityByIndex(entity);
                    if (pEntity && pEntity->IsPlayerPawn())
                        continue;

                    if (!g_pHooks[entity]->CanSee(controllerIndex))
                    {
                        pInfo->m_pTransmitEntity->Clear(entity);
                        continue;
                    }

                    const auto owner = g_pHooks[entity]->GetOwner();
                    if (controllerIndex == owner)
                    {
                        continue;
                    }

                    if (g_pHooks[entity]->GetBlockAll())
                    {
                        pInfo->m_pTransmitEntity->Clear(entity);
                        continue;
                    }

                    if (owner != INVALID_ENTITY_INDEX && g_pHooks[owner] != nullptr && !g_pHooks[owner]->CanSee(controllerIndex))
                    {
                        pInfo->m_pTransmitEntity->Clear(entity);
                    }
                }
            }
        }
    }
}

// #define LOG_MUZZLE_EVENT

class TransmitEntityListener : public IEntityListener
{
public:
    void OnEntityCreated(CBaseEntity* pEntity) override
    {
        const auto index = pEntity->GetEntityIndex();
        if (!IsEntityIndexInRange(index))
            return;

        WLOCK;

        if (g_pHooks[index] != nullptr)
            FERROR("RemoveHook -> exists -> %d.%s<%u> -> old:%s<%u>",
                   index, pEntity->GetClassname(), pEntity->GetActualEHandle().ToInt(),
                   g_pHooks[index]->GetClassname(), g_pHooks[index]->GetEntityHandle().ToInt());

        UnhookEntity(pEntity);
    }
    void OnEntityDeleted(CBaseEntity* pEntity) override
    {
        s_fire_bullet_state.erase(GetEntityProtobufHandle(pEntity));

        const auto index = pEntity->GetEntityIndex();
        if (!IsEntityIndexInRange(index))
            return;

        WLOCK;
        UnhookEntity(pEntity);
    }
    void OnEntityFollowed(CBaseEntity* pEntity, CBaseEntity* pOwner) override {}
    void OnEntitySpawned(CBaseEntity* pEntity) override {}
} static s_listener;

void InstallTransmitHook()
{
    HOOK(ISource2GameEntities, CheckTransmit);

    g_pGameEntitySystem->AddListenerEntity(&s_listener);

    for (auto& i : g_bitsBlockTempEnt)
    {
        i = 0;
    }
    for (auto& b : g_bEverSpawned)
    {
        b = false;
    }

    g_pHookManager->Hook_ClientActivate(HookType_Post, [](PlayerSlot_t slot, const char* pszName, SteamId_t steamId) {
        SetBlockTempEntState(BT_FireBullets, slot, false);
        SetBlockTempEntState(BT_WorldDecals, slot, false);
        SetBlockTempEntState(BT_BloodEffect, slot, false);

        WLOCK;
        for (auto i = 1; i < MAX_ENTITY_COUNT; i++)
        {
            if (g_pHooks[i] == nullptr)
            {
                continue;
            }

            g_pHooks[i]->SetDefault(static_cast<int>(slot) + 1);
        }
    });
    g_pHookManager->Hook_GameDeactivate(HookType_Post, [] {
        for (PlayerSlot_t i = 0; i < CS_MAX_PLAYERS; i++)
        {
            SetBlockTempEntState(BT_FireBullets, i, false);
            SetBlockTempEntState(BT_WorldDecals, i, false);
            SetBlockTempEntState(BT_BloodEffect, i, false);
        }
        for (auto& a : g_iBypassTick)
        {
            a = TICK_BASE_VALUE(9999);
        }
        for (auto& b : g_bEverSpawned)
        {
            b = false;
        }

        WLOCK;

        s_fire_bullet_state.clear();

        for (auto i = 1; i < MAX_ENTITY_COUNT; i++)
        {
            if (g_pHooks[i] == nullptr)
            {
                continue;
            }

            delete g_pHooks[i];
            g_pHooks[i] = nullptr;
        }
    });
    g_pHookManager->Hook_ClientConnect(HookType_Post, [](PlayerSlot_t slot, const char* pszName, SteamId_t steamId, bool bFakeClient) {
        WLOCK;
        g_iBypassTick[slot]  = TICK_BASE_VALUE(9999);
        g_bEverSpawned[slot] = false;

        const EntityIndex_t index = slot + 1;

        for (auto i = 1; i < MAX_ENTITY_COUNT; i++)
        {
            if (g_pHooks[i] == nullptr)
            {
                continue;
            }

            if (g_pHooks[i]->GetOwner() == index)
            {
                g_pHooks[i]->SetOwner(INVALID_ENTITY_INDEX);
            }
        }

        if (g_pHooks[index] != nullptr)
        {
            delete g_pHooks[index];
            g_pHooks[index] = nullptr;
        }
    });
    g_pHookManager->Hook_ClientDisconnect(HookType_Post, [](PlayerSlot_t slot, int32_t reason, const char* name, SteamId_t steamId) {
        WLOCK;
        g_iBypassTick[slot]  = TICK_BASE_VALUE(9999);
        g_bEverSpawned[slot] = false;

        const auto index = slot + 1;
        if (g_pHooks[index] == nullptr)
        {
            return;
        }
        delete g_pHooks[index];
        g_pHooks[index] = nullptr;
    });
    g_pHookManager->Hook_ClientActivate(HookType_Post, [](PlayerSlot_t slot, const char* pszName, SteamId_t steamId) {
        WLOCK;
        g_iBypassTick[slot] = TICK_BASE_VALUE(10);
    });
    g_pHookManager->Hook_ClientFullyConnect(HookType_Post, [](PlayerSlot_t slot) {
        WLOCK;
        g_iBypassTick[slot] = TICK_BASE_VALUE(10);
    });
    g_pHookManager->Hook_GameFrame(HookType_Post, [](bool s, bool f, bool l) {
        WLOCK;
        for (PlayerSlot_t i = 0; i < CS_MAX_PLAYERS; i++)
        {
            g_iBypassTick[i]--;
        }
    });
    g_pHookManager->Hook_PlayerSpawned(HookType_Post, [](CCSPlayerPawn*, CServerSideClient* pClient) {
        g_bEverSpawned[pClient->GetSlot()] = true;
    });

    ms_transmit_block_dead_player_pawn = g_ConVarManager.CreateConVar("ms_transmit_block_dead_player_pawn", false, "Block transmit for dead player pawn.", FCVAR_RELEASE);
    ms_transmit_block_ownerless_pawn   = g_ConVarManager.CreateConVar("ms_transmit_block_ownerless_pawn", false, "Block transmit for ownerless player pawn.", FCVAR_RELEASE);
}

void TransmitCheckFireBullets(const NetworkReceiver_t* clients)
{
    *const_cast<uint64_t*>(clients) &= ~g_bitsBlockTempEnt[BT_FireBullets];
}

int32_t TransmitGetFireBulletState(uint32_t handle)
{
    const auto it = s_fire_bullet_state.find(handle);
    if (it == s_fire_bullet_state.end())
        return FBS_None;

    return it->second;
}

void TransmitCheckWorldDecals(const NetworkReceiver_t* clients)
{
    *const_cast<uint64_t*>(clients) &= ~g_bitsBlockTempEnt[BT_WorldDecals];
}

void TransmitCheckBloodEffect(const NetworkReceiver_t* clients)
{
    *const_cast<uint64_t*>(clients) &= ~g_bitsBlockTempEnt[BT_BloodEffect];
}

// Return True if the music is blocked.
bool TransmitCheckMapMusic(int slot)
{
    return GetBlockTempEntState(BT_MapMusic, static_cast<PlayerSlot_t>(slot));
}