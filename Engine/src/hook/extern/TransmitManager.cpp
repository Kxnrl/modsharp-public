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
#include "cstrike/type/CEntityClass.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/VProf.h"

#include <algorithm>
#include <atomic>
#include <bit>
#include <mutex>
#include <shared_mutex>

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
constexpr int TRANSMIT_WORDS   = MAX_ENTITY_COUNT / 32;

static std::shared_mutex g_MutexHooks;

inline bool IsEntityIndexInRange(int index)
{
    return index > 0 && index < MAX_ENTITY_COUNT;
}

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

struct FireBulletSlot
{
    uint32_t          handle = INVALID_PACKED_HANDLE;
    FireBulletState_t state  = FBS_None;
};

static std::atomic<uint64_t>       g_bitsBlockTempEnt[BT_Count];
static std::atomic<FireBulletSlot> s_fire_bullet_state[MAX_ENTITY_COUNT];
static_assert(std::atomic<FireBulletSlot>::is_always_lock_free);

static CConVarBaseData* ms_transmit_block_dead_player_pawn = nullptr;
static CConVarBaseData* ms_transmit_block_ownerless_pawn   = nullptr;

[[nodiscard]] static bool IsPlayerSlotInRange(const PlayerSlot_t slot) noexcept
{
    return slot < CS_MAX_PLAYERS;
}

static FireBulletState_t GetWeaponFireBulletState(const CBaseEntity* pWeapon)
{
    if (!pWeapon)
        return FBS_None;

    const auto index = pWeapon->GetEntityIndex();
    if (!IsEntityIndexInRange(index))
        return FBS_None;

    const auto slot = s_fire_bullet_state[index].load(std::memory_order_relaxed);

    return slot.handle == pWeapon->GetActualEHandle().GetPackedValue() ? slot.state : FBS_None;
}

static void SetBlockTempEntState(const BlockTE_t type, const PlayerSlot_t slot, const bool state)
{
    if (!IsPlayerSlotInRange(slot))
        return;

    if (state)
        g_bitsBlockTempEnt[type].fetch_or(1ull << slot, std::memory_order_relaxed);
    else
        g_bitsBlockTempEnt[type].fetch_and(~(1ull << slot), std::memory_order_relaxed);
}

static bool GetBlockTempEntState(const BlockTE_t type, const PlayerSlot_t slot)
{
    if (!IsPlayerSlotInRange(slot))
        return false;

    return g_bitsBlockTempEnt[type].load(std::memory_order_relaxed) & (1ull << slot);
}

struct Entity2Networkable_t;

class CCheckTransmitInfo // sizeof = 584 (0x248)
{
    // +0  CDeltaEntityHeaderWriter (engine2.dll!WriteDeltaEntities)
    CBitVec<MAX_ENTITY_COUNT>* m_pTransmitEntity;

    // +8  CheckEntities => m_pNonTransmitEntity = Entities & ~m_pTransmitEntity
    //     :: CDeltaEntityNonTransmitHeaderWriter / ...Reader
    CBitVec<MAX_ENTITY_COUNT>* m_pNonTransmitEntity;
    // +16 PVS Delta => COutOfPVSDeltaEntityHeaderWriter
    [[maybe_unused]] CBitVec<MAX_ENTITY_COUNT>* m_pOutOfPvsEntity;
    // +24 HLTV/Replay (CSendJob_HltvSource) nullptr otherwise
    [[maybe_unused]] CBitVec<MAX_ENTITY_COUNT>* m_pHltvEntity;

    [[maybe_unused]] int32_t  m_nAreaCount;    // +32
    [[maybe_unused]] int32_t  m_nPad36;        // +36
    [[maybe_unused]] int32_t* m_pAreas;        // +40
    [[maybe_unused]] int32_t  m_nAreaCapacity; // +48
    [[maybe_unused]] int32_t  m_nPad52;        // +52
    [[maybe_unused]] int32_t  m_nPVSDwords;    // +56
    [[maybe_unused]] int32_t  m_nWorldIndex;   // +60
    [[maybe_unused]] uint32_t m_PVSBits[128];  // +64 .. +576

    int32_t m_nPlayerSlot; // +576
    bool    m_bFullUpdate; // +580

public:
    [[nodiscard]] inline PlayerSlot_t GetPlayerSlot() const
    {
        AssertBool(m_nPlayerSlot >= 0 && static_cast<uint32_t>(m_nPlayerSlot) < CS_MAX_PLAYERS);

        return static_cast<PlayerSlot_t>(m_nPlayerSlot);
    }

    [[nodiscard]] inline bool IsFullUpdate() const noexcept { return m_bFullUpdate; }

    [[nodiscard]] inline bool IsTransmitting(EntityIndex_t index) const noexcept { return m_pTransmitEntity->IsBitSet(index); }

    [[nodiscard]] inline uint32_t* TransmitBase() const noexcept { return m_pTransmitEntity->Base(); }

    [[nodiscard]] inline uint32_t* NonTransmitBase() const noexcept { return m_pNonTransmitEntity->Base(); }

    inline void BlockTransmit(const CBaseEntity* pEntity) const noexcept { BlockTransmit(pEntity->GetEntityIndex()); }

    inline void BlockTransmit(const EntityIndex_t index) const noexcept
    {
        if (m_pTransmitEntity->IsBitSet(index))
        {
            m_pTransmitEntity->Clear(index);
        }

        m_pNonTransmitEntity->Set(index);
    }

    CCheckTransmitInfo() = delete;
};
static_assert(sizeof(CCheckTransmitInfo) == 584);

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
        m_bitsAll(defaultTransmit ? ~0ull : 0ull),
        m_nOwnerEntity(INVALID_ENTITY_INDEX),
        m_bDefaultTransmit(defaultTransmit),
        m_bBlockAll(false),
        m_pszClassname(DumpString(pEntity->GetClassname()))
    {
        // can see by default
        for (auto& bits : m_bitsChannel)
        {
            bits = m_bitsAll;
        }
    }

    ~CHook()
    {
        delete[] m_pszClassname;
    }

public:
    [[nodiscard]] static bool IsClientIndexInRange(EntityIndex_t client) noexcept
    {
        return client >= 1 && client <= CS_MAX_PLAYERS;
    }

private:
    [[nodiscard]] static uint64_t ClientBit(EntityIndex_t client) noexcept
    {
        return 1ull << (client - 1);
    }

public:
    [[nodiscard]] bool CanSee(EntityIndex_t client) const noexcept
    {
        if (m_iEntityIndex == client)
            return true;

        if (!IsClientIndexInRange(client))
            return true;

        return (m_bitsAll & ClientBit(client)) != 0;
    }

    [[nodiscard]] uint64_t GetVisibleMask() const noexcept
    {
        return m_bitsAll;
    }

    bool SetSee(EntityIndex_t client, bool can, int channel)
    {
        if (!IsClientIndexInRange(client))
            return false;

        const auto bit = ClientBit(client);
        const auto old = m_bitsAll;

        if (channel == -1)
        {
            for (auto& bits : m_bitsChannel)
            {
                bits = can ? (bits | bit) : (bits & ~bit);
            }

            m_bitsAll = can ? (m_bitsAll | bit) : (m_bitsAll & ~bit);

            return m_bitsAll != old;
        }

        const auto c     = std::clamp(channel, 0, MAX_CHANNEL);
        m_bitsChannel[c] = can ? (m_bitsChannel[c] | bit) : (m_bitsChannel[c] & ~bit);

        if (!can)
        {
            m_bitsAll &= ~bit;

            return m_bitsAll != old;
        }

        uint64_t all = ~0ull;
        for (const auto bits : m_bitsChannel)
        {
            all &= bits;
        }

        m_bitsAll = (m_bitsAll & ~bit) | (all & bit);

        return m_bitsAll != old;
    }

    [[nodiscard]] bool GetState(EntityIndex_t client, int channel) const noexcept
    {
        if (!IsClientIndexInRange(client))
            return true;

        return (m_bitsChannel[std::clamp(channel, 0, MAX_CHANNEL)] & ClientBit(client)) != 0;
    }

    bool SetDefault(EntityIndex_t client)
    {
        return SetSee(client, m_bDefaultTransmit, -1);
    }

    [[nodiscard]] EntityIndex_t GetOwner() const
    {
        return m_nOwnerEntity;
    }

    bool SetOwner(EntityIndex_t owner)
    {
#ifdef TRACE
        if (m_iEntityIndex < CS_MAX_PLAYERS)
            LOG("SetOwner::%d.%s::(%d)\n", m_iEntityIndex, m_pszClassname, owner);
#endif
        if (m_nOwnerEntity == owner)
            return false;

        m_nOwnerEntity = owner;
        return true;
    }

    bool SetBlockAll(bool state)
    {
#ifdef TRACE
        if (m_iEntityIndex < CS_MAX_PLAYERS)
            LOG("SetBlockAll::%d.%s::(%s)\n", m_iEntityIndex, m_pszClassname, BOOLEAN(state));
#endif
        if (m_bBlockAll == state)
            return false;

        m_bBlockAll = state;
        return true;
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
    uint64_t      m_bitsChannel[MAX_CHANNEL + 1];
    uint64_t      m_bitsAll;
    EntityIndex_t m_nOwnerEntity;
    bool          m_bDefaultTransmit;
    bool          m_bBlockAll;
    const char*   m_pszClassname = nullptr;
};

static CHook*  g_pHooks[MAX_ENTITY_COUNT];
static int64_t g_iBypassTick[CS_MAX_PLAYERS];
static bool    g_bEverSpawned[CS_MAX_PLAYERS];

static CBitVec<MAX_ENTITY_COUNT> g_HookedMask;

static std::atomic<uint32_t> g_HookVersion{1};

static inline void InvalidateBlockPlan() noexcept
{
    g_HookVersion.fetch_add(1, std::memory_order_release);
}

struct BlockPlan
{
    alignas(64) uint32_t blockMask[CS_MAX_PLAYERS][TRANSMIT_WORDS];
    alignas(64) uint32_t allowMask[CS_MAX_PLAYERS][TRANSMIT_WORDS];
    alignas(64) uint32_t blockAllMask[TRANSMIT_WORDS];

    uint32_t activeWords[TRANSMIT_WORDS / 32];

    uint64_t senderVisible[CS_MAX_PLAYERS + 1];
    bool     senderHooked[CS_MAX_PLAYERS + 1];

    bool blockAny[CS_MAX_PLAYERS];
    bool allowAny[CS_MAX_PLAYERS];
    bool blockAllAny;

    uint32_t      version     = 0;
    EntityIndex_t maxClients  = -1;
    EntityIndex_t senderCount = 0;

    void Reset() noexcept
    {
        memset(blockMask, 0, sizeof(blockMask));
        memset(allowMask, 0, sizeof(allowMask));
        memset(blockAllMask, 0, sizeof(blockAllMask));
        memset(activeWords, 0, sizeof(activeWords));
        memset(senderVisible, 0xFF, sizeof(senderVisible));
        memset(senderHooked, 0, sizeof(senderHooked));
        memset(blockAny, 0, sizeof(blockAny));
        memset(allowAny, 0, sizeof(allowAny));
        blockAllAny = false;
    }
} static g_BlockPlan;

struct SenderCache
{
    CCSPlayerController* pController;
    CCSPlayerPawn*       pPlayerPawn;
    CCSObserverPawn*     pObserverPawn;
    uint64_t             visibleMask;
    bool                 bHooked;
    bool                 bPlayerAlive;
};

template <typename Fn>
static void ForEachOfClass(const CEntityClass* pClass, Fn&& fn)
{
    if (pClass == nullptr)
    {
        return;
    }

    for (auto* pIdentity = pClass->GetEntityListHead(); pIdentity; pIdentity = pIdentity->m_pNextByClass())
    {
        if (pIdentity->IsMarkedForDeletion())
        {
            continue;
        }

        if (auto* pEntity = pIdentity->GetBaseEntity())
        {
            fn(pEntity);
        }
    }
}

template <typename Fn>
static void ForEachHooked(Fn&& fn)
{
    const uint32_t* words = g_HookedMask.Base();

    for (std::size_t w = 0; w < g_HookedMask.GetNumDWords(); w++)
    {
        uint32_t bits = words[w];

        while (bits)
        {
            const auto b = static_cast<uint32_t>(std::countr_zero(bits));
            bits &= bits - 1;
            fn(static_cast<EntityIndex_t>(w * 32 + b));
        }
    }
}

static uint64_t ComputeEntityBlockedMask(const CHook* pHook)
{
    const uint64_t notVisible = ~pHook->GetVisibleMask();

    uint64_t conditional = pHook->GetBlockAll() ? ~0ull : 0ull;

    const auto owner = pHook->GetOwner();
    if (owner != INVALID_ENTITY_INDEX)
    {
        if (IsEntityIndexInRange(owner) && g_pHooks[owner] != nullptr)
        {
            conditional |= ~g_pHooks[owner]->GetVisibleMask();
        }

        if (owner >= 1 && owner <= CS_MAX_PLAYERS)
        {
            conditional &= ~(1ull << (owner - 1));
        }
    }

    return notVisible | conditional;
}

static void HookEntity(CBaseEntity* pEntity, bool defaultTransmit)
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
        FERROR("Entity Hook listener<%d> new=[%s<%u>], old=[%s<%u>] is not nullptr.", index,
               pEntity->GetClassname(), pEntity->GetActualEHandle().ToInt(),
               g_pHooks[index]->GetClassname(), g_pHooks[index]->GetEntityHandle().ToInt());
        return;
    }

    g_pHooks[index] = new CHook(pEntity, defaultTransmit);
    g_HookedMask.Set(index);

    InvalidateBlockPlan();
}

static void DestroyHook(EntityIndex_t index)
{
    if (!IsEntityIndexInRange(index))
    {
        return;
    }

    g_HookedMask.Clear(index);

    if (g_pHooks[index] == nullptr)
    {
        return;
    }

    delete g_pHooks[index];
    g_pHooks[index] = nullptr;

    InvalidateBlockPlan();
}

static void UnhookEntity(CBaseEntity* pEntity)
{
    // NOTE 在外部使用🔒, 否则会循环等待

    const auto index = pEntity->GetEntityIndex();

    if (!IsEntityIndexInRange(index))
    {
        return;
    }

    DestroyHook(index);
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
    const auto index = pEntity->GetEntityIndex();

    if (!IsEntityIndexInRange(index))
    {
        return false;
    }

    RLOCK;

    return g_pHooks[index] != nullptr;
}

static EntityIndex_t TransmitManagerGetEntityOwner(int index)
{
    if (!IsEntityIndexInRange(index))
    {
        return -2;
    }

    RLOCK;

    if (g_pHooks[index] == nullptr)
    {
        return -2;
    }

    return g_pHooks[index]->GetOwner();
}

static bool TransmitManagerSetEntityOwner(EntityIndex_t index, EntityIndex_t owner)
{
    if (!IsEntityIndexInRange(index))
    {
        return false;
    }

    WLOCK;

    if (g_pHooks[index] != nullptr)
    {
        if (g_pHooks[index]->SetOwner(owner))
            InvalidateBlockPlan();

        return true;
    }

    return false;
}

static bool TransmitManagerGetEntityState(EntityIndex_t index, EntityIndex_t controllerIndex, int channel)
{
    if (!IsEntityIndexInRange(index))
        return true;

    RLOCK;

    if (g_pHooks[index] == nullptr)
        return true;

    if (channel == -1)
        return g_pHooks[index]->CanSee(controllerIndex);

    return g_pHooks[index]->GetState(controllerIndex, std::clamp(channel, 0, MAX_CHANNEL));
}

static bool TransmitManagerSetEntityState(EntityIndex_t index, EntityIndex_t controllerIndex, bool state, int channel)
{
    if (!IsEntityIndexInRange(index) || !CHook::IsClientIndexInRange(controllerIndex))
    {
        return false;
    }

    WLOCK;

    if (g_pHooks[index] != nullptr)
    {
        if (g_pHooks[index]->SetSee(controllerIndex, state, channel))
            InvalidateBlockPlan();

        return true;
    }

    return false;
}

static bool TransmitManagerGetEntityBlock(EntityIndex_t index)
{
    if (!IsEntityIndexInRange(index))
    {
        return false;
    }

    RLOCK;

    if (g_pHooks[index] == nullptr)
    {
        // can see
        return false;
    }

    return g_pHooks[index]->GetBlockAll();
}

static bool TransmitManagerSetEntityBlock(EntityIndex_t index, bool val)
{
    if (!IsEntityIndexInRange(index))
    {
        return false;
    }

    WLOCK;

    if (g_pHooks[index] == nullptr)
    {
        // can see
        return false;
    }

    if (g_pHooks[index]->SetBlockAll(val))
        InvalidateBlockPlan();

    return true;
}

static bool TransmitManagerGetTempEntState(BlockTE_t type, PlayerSlot_t slot)
{
    if (type >= BT_Count || type < 0 || !IsPlayerSlotInRange(slot))
        return false;

    return GetBlockTempEntState(type, slot);
}

static bool TransmitManagerSetTempEntState(BlockTE_t type, PlayerSlot_t slot, bool state)
{
    if (type >= BT_Count || type < 0 || !IsPlayerSlotInRange(slot))
        return false;

    SetBlockTempEntState(type, slot, state);

    return true;
}

static void TransmitManagerClearReceiverState(EntityIndex_t receiverIndex)
{
    WLOCK;

    bool changed = false;
    ForEachHooked([receiverIndex, &changed](EntityIndex_t index) { changed |= g_pHooks[index]->SetDefault(receiverIndex); });

    if (changed)
        InvalidateBlockPlan();
}

static NetworkReceiver_t ComputeBypassMask()
{
    NetworkReceiver_t mask = 0;

    for (PlayerSlot_t i = 0; i < CS_MAX_PLAYERS; i++)
    {
        if (g_iBypassTick[i] > 0 || !g_bEverSpawned[i])
        {
            mask |= BASE_RECEIVER_MAGIC << i;
        }
    }

    return mask;
}

static NetworkReceiver_t ComputePawnReceiver(CCSPlayerPawnBase* pPawn, bool bPlayerPawn)
{
    auto* pController = pPawn->GetOriginalController<CCSPlayerController*>();

    const auto controllerIndex = pController != nullptr ? pController->GetEntityIndex() : INVALID_ENTITY_INDEX;
    const bool bHasSelf        = CHook::IsClientIndexInRange(controllerIndex);

    const NetworkReceiver_t selfBit = bHasSelf ? BASE_RECEIVER_MAGIC << (controllerIndex - 1) : 0;

    NetworkReceiver_t visible = ~0ull;

    if (bHasSelf)
    {
        if (bPlayerPawn)
        {
            RLOCK;

            if (const auto* pHook = g_pHooks[controllerIndex])
            {
                visible = pHook->GetVisibleMask() | selfBit;
            }
        }

        if (ms_transmit_block_dead_player_pawn->GetValue<bool>()
            && (!bPlayerPawn || pPawn->GetLifeState() != LIFE_ALIVE))
        {
            visible = selfBit;
        }

        visible |= ComputeBypassMask();
    }

    if (pController == nullptr && ms_transmit_block_ownerless_pawn->GetValue<bool>())
    {
        visible = selfBit;
    }

    return visible;
}

static NetworkReceiver_t TransmitManagerGetEntityReceiver(EntityIndex_t index)
{
    if (!IsEntityIndexInRange(index))
    {
        return ~0ull;
    }

    if (gpGlobals == nullptr)
    {
        return ~0ull;
    }

    const auto maxClients = static_cast<EntityIndex_t>(gpGlobals->MaxClients);

    if (index > maxClients)
    {
        RLOCK;

        if (g_pHooks[index] != nullptr)
        {
            return ~ComputeEntityBlockedMask(g_pHooks[index]);
        }
    }

    auto* pEntity = g_pGameEntitySystem->FindEntityByIndex<CBaseEntity*>(index);

    if (pEntity == nullptr)
    {
        return ~0ull;
    }

    if (index <= maxClients)
    {
        auto* pController = pEntity->ToPlayerController();

        if (pController == nullptr)
        {
            return ~0ull;
        }

        auto* pPlayerPawn = pController->GetPlayerPawn();

        return pPlayerPawn != nullptr ? ComputePawnReceiver(pPlayerPawn, true) : ~0ull;
    }

    if (pEntity->IsPlayerPawn())
    {
        auto* pPawn = reinterpret_cast<CCSPlayerPawnBase*>(pEntity);

        return ComputePawnReceiver(pPawn, pPawn->IsPlayer());
    }

    return ~0ull;
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

    const auto index = pWeapon->GetEntityIndex();
    if (!IsEntityIndexInRange(index))
        return;

    const auto slot = state > FBS_None ? FireBulletSlot{pWeapon->GetActualEHandle().GetPackedValue(), state} : FireBulletSlot{};

    s_fire_bullet_state[index].store(slot, std::memory_order_relaxed);
}

static void RebuildBlockMask(EntityIndex_t maxClients, BlockPlan& plan)
{
    AssertBool(g_nMainThreadId == static_cast<uint64_t>(GetCurrentThreadId()));

    plan.Reset();

    plan.maxClients  = maxClients;
    plan.senderCount = std::min<EntityIndex_t>(maxClients, CS_MAX_PLAYERS);

    for (EntityIndex_t i = 1; i <= plan.senderCount; i++)
    {
        const auto* pHook     = g_pHooks[i];
        plan.senderHooked[i]  = pHook != nullptr;
        plan.senderVisible[i] = pHook != nullptr ? pHook->GetVisibleMask() : ~0ull;
    }

    ForEachHooked([maxClients, &plan](EntityIndex_t index) {
        if (index <= maxClients)
        {
            return;
        }

        const uint64_t blocked = ComputeEntityBlockedMask(g_pHooks[index]);

        if (blocked == 0)
        {
            return;
        }

        const auto     word = static_cast<uint32_t>(index) >> 5;
        const uint32_t bit  = 1u << (static_cast<uint32_t>(index) & 31);

        plan.activeWords[word >> 5] |= 1u << (word & 31);

        if (blocked == ~0ull)
        {
            plan.blockAllMask[word] |= bit;
            plan.blockAllAny = true;
            return;
        }

        if (std::popcount(blocked) > CS_MAX_PLAYERS / 2)
        {
            plan.blockAllMask[word] |= bit;
            plan.blockAllAny = true;

            uint64_t remain = ~blocked;
            while (remain)
            {
                const auto slot = static_cast<uint32_t>(std::countr_zero(remain));
                remain &= remain - 1;
                plan.allowMask[slot][word] |= bit;
                plan.allowAny[slot] = true;
            }

            return;
        }

        uint64_t remain = blocked;
        while (remain)
        {
            const auto slot = static_cast<uint32_t>(std::countr_zero(remain));
            remain &= remain - 1;
            plan.blockMask[slot][word] |= bit;
            plan.blockAny[slot] = true;
        }
    });
}

static void ApplyBlockPlan(const BlockPlan& plan, PlayerSlot_t slot, uint32_t* __restrict pTransmit, uint32_t* __restrict pNonTransmit) noexcept
{
    const uint32_t* __restrict pAll   = plan.blockAllMask;
    const uint32_t* __restrict pAllow = plan.allowMask[slot];
    const uint32_t* __restrict pOwn   = plan.blockMask[slot];

    const bool useAllow = plan.allowAny[slot];
    const bool useOwn   = plan.blockAny[slot];

    for (uint32_t g = 0; g < TRANSMIT_WORDS / 32; g++)
    {
        uint32_t bits = plan.activeWords[g];

        while (bits)
        {
            const auto w = g * 32 + static_cast<uint32_t>(std::countr_zero(bits));
            bits &= bits - 1;

            uint32_t block = pAll[w];

            if (useAllow)
                block &= ~pAllow[w];

            if (useOwn)
                block |= pOwn[w];

            const uint32_t old = pTransmit[w];
            pNonTransmit[w] |= old & block;
            pTransmit[w] = old & ~block;
        }
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
    bridge::CreateNative("Transmit.GetEntityReceiver", reinterpret_cast<void*>(TransmitManagerGetEntityReceiver));

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

        VPROF_MS_HOOK_SCOPE("CheckTransmit::PostProcess");

        const auto blockPawn  = ms_transmit_block_dead_player_pawn->GetValue<bool>();
        const auto blockNull  = ms_transmit_block_ownerless_pawn->GetValue<bool>();
        const auto maxClients = static_cast<EntityIndex_t>(gpGlobals->MaxClients);

        {
            RLOCK;

            const auto version = g_HookVersion.load(std::memory_order_relaxed);

            if (g_BlockPlan.version != version || g_BlockPlan.maxClients != maxClients)
            {
                RebuildBlockMask(maxClients, g_BlockPlan);
                g_BlockPlan.version = version;
            }
        }

        const auto senderCount = g_BlockPlan.senderCount;

        SenderCache senders[CS_MAX_PLAYERS + 1] = {};

        for (EntityIndex_t i = 1; i <= senderCount; i++)
        {
            senders[i].bHooked     = g_BlockPlan.senderHooked[i];
            senders[i].visibleMask = g_BlockPlan.senderVisible[i];

            if (!blockPawn && !senders[i].bHooked)
            {
                continue;
            }

            auto* pController = g_pGameEntitySystem->FindEntityByIndex<CCSPlayerController*>(i);
            if (!pController)
            {
                continue;
            }

            auto* pPlayerPawn = pController->GetPlayerPawn();

            senders[i].pController   = pController;
            senders[i].pPlayerPawn   = pPlayerPawn;
            senders[i].pObserverPawn = pController->GetObserverPawn();
            senders[i].bPlayerAlive  = pPlayerPawn != nullptr && pPlayerPawn->GetLifeState() == LIFE_ALIVE;
        }

        CUtlVector<CBaseEntity*> nullPawns;

        if (blockNull)
        {
            static const CEntityClass* s_pPlayerClass   = nullptr;
            static const CEntityClass* s_pObserverClass = nullptr;

            if (s_pPlayerClass == nullptr)
            {
                s_pPlayerClass = g_pGameEntitySystem->FindEntityClassByName("player");
            }

            if (s_pObserverClass == nullptr)
            {
                s_pObserverClass = g_pGameEntitySystem->FindEntityClassByName("observer");
            }

            const auto collect = [&](const CEntityClass* pClass, bool wantPlayer) {
                ForEachOfClass(pClass, [&](CBaseEntity* pEntity) {
                    const auto pPawn = reinterpret_cast<CCSPlayerPawnBase*>(pEntity);
                    if (!pPawn->IsPlayerPawn() || pPawn->IsPlayer() != wantPlayer)
                    {
                        return;
                    }

                    if (g_pGameEntitySystem->FindEntityByEHandle(pPawn->m_hOriginalController()) != nullptr)
                    {
                        return;
                    }

                    nullPawns.AddToTail(pPawn);
                });
            };

            collect(s_pPlayerClass, true);
            collect(s_pObserverClass, false);
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

            // Team UnAssigned
            if (pReceiverController->GetTeam() == TEAM_UNASSIGNED)
            {
                constexpr int64_t pendingDelay = TICK_BASE_VALUE(5);
                g_iBypassTick[playerSlot]      = std::max<int64_t>(g_iBypassTick[playerSlot], pendingDelay);
            }

            // LOOP Pawn
            if (g_iBypassTick[playerSlot] <= 0 && g_bEverSpawned[playerSlot] && !pInfo->IsFullUpdate())
            {
                for (EntityIndex_t i = 1; i <= senderCount; i++)
                {
                    if (i == controllerIndex)
                        continue;

                    const auto& sender = senders[i];
                    if (sender.pController == nullptr)
                        continue;

                    const bool bSenderVisible = (sender.visibleMask >> (controllerIndex - 1)) & 1;
                    if (sender.bHooked && !bSenderVisible && sender.pPlayerPawn != nullptr)
                    {
                        pInfo->BlockTransmit(sender.pPlayerPawn);
                    }

                    if (blockPawn)
                    {
                        if (sender.pObserverPawn != nullptr)
                        {
                            pInfo->BlockTransmit(sender.pObserverPawn);
                        }

                        if (sender.pPlayerPawn != nullptr && !sender.bPlayerAlive)
                        {
                            pInfo->BlockTransmit(sender.pPlayerPawn);
                        }
                    }
                }
            }

            // LOOP NullPawn
            for (int32_t i = 0; i < nullPawns.Count(); i++)
            {
                auto* pPawn = nullPawns.Element(i);
                if (pPawn == pReceiverPawn || pPawn == pReceiverObserver)
                    continue;

                pInfo->BlockTransmit(pPawn);
            }

            // LOOP Other Entities
            if (g_BlockPlan.blockAny[playerSlot] || g_BlockPlan.blockAllAny)
            {
                auto* pTransmit    = pInfo->TransmitBase();
                auto* pNonTransmit = pInfo->NonTransmitBase();

                const auto     selfWord = static_cast<uint32_t>(controllerIndex) >> 5;
                const uint32_t selfBit  = 1u << (static_cast<uint32_t>(controllerIndex) & 31);
                const uint32_t selfT    = pTransmit[selfWord] & selfBit;
                const uint32_t selfN    = pNonTransmit[selfWord] & selfBit;

                const bool     bKeepPawn = IsEntityIndexInRange(iReceiverPawn);
                const auto     pawnWord  = bKeepPawn ? static_cast<uint32_t>(iReceiverPawn) >> 5 : 0u;
                const uint32_t pawnBit   = bKeepPawn ? 1u << (static_cast<uint32_t>(iReceiverPawn) & 31) : 0u;
                const uint32_t pawnT     = pTransmit[pawnWord] & pawnBit;
                const uint32_t pawnN     = pNonTransmit[pawnWord] & pawnBit;

                ApplyBlockPlan(g_BlockPlan, playerSlot, pTransmit, pNonTransmit);

                if (bKeepPawn)
                {
                    pTransmit[pawnWord]    = (pTransmit[pawnWord] & ~pawnBit) | pawnT;
                    pNonTransmit[pawnWord] = (pNonTransmit[pawnWord] & ~pawnBit) | pawnN;
                }

                pTransmit[selfWord]    = (pTransmit[selfWord] & ~selfBit) | selfT;
                pNonTransmit[selfWord] = (pNonTransmit[selfWord] & ~selfBit) | selfN;
            }
        }
    }
}

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
        const auto index = pEntity->GetEntityIndex();
        if (!IsEntityIndexInRange(index))
            return;

        s_fire_bullet_state[index].store(FireBulletSlot{}, std::memory_order_relaxed);

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
        i.store(0, std::memory_order_relaxed);
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

        const EntityIndex_t receiverIndex = static_cast<int>(slot) + 1;

        bool changed = false;
        ForEachHooked([receiverIndex, &changed](EntityIndex_t index) { changed |= g_pHooks[index]->SetDefault(receiverIndex); });

        if (changed)
            InvalidateBlockPlan();
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

        for (auto& s : s_fire_bullet_state)
        {
            s.store(FireBulletSlot{}, std::memory_order_relaxed);
        }

        WLOCK;

        ForEachHooked([](EntityIndex_t index) {
            delete g_pHooks[index];
            g_pHooks[index] = nullptr;
        });

        g_HookedMask.ClearAll();

        InvalidateBlockPlan();
    });
    g_pHookManager->Hook_ClientConnect(HookType_Post, [](PlayerSlot_t slot, const char* pszName, SteamId_t steamId, bool bFakeClient) {
        WLOCK;
        g_iBypassTick[slot]  = TICK_BASE_VALUE(9999);
        g_bEverSpawned[slot] = false;

        const EntityIndex_t index = slot + 1;

        bool changed = false;
        ForEachHooked([index, &changed](EntityIndex_t hooked) {
            if (g_pHooks[hooked]->GetOwner() == index)
            {
                changed |= g_pHooks[hooked]->SetOwner(INVALID_ENTITY_INDEX);
            }
        });

        if (changed)
            InvalidateBlockPlan();

        DestroyHook(index);
    });
    g_pHookManager->Hook_ClientDisconnect(HookType_Post, [](PlayerSlot_t slot, int32_t reason, const char* name, SteamId_t steamId) {
        WLOCK;
        g_iBypassTick[slot]  = TICK_BASE_VALUE(9999);
        g_bEverSpawned[slot] = false;

        DestroyHook(slot + 1);
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
    *const_cast<uint64_t*>(clients) &= ~g_bitsBlockTempEnt[BT_FireBullets].load(std::memory_order_relaxed);
}

int32_t TransmitGetFireBulletState(uint32_t handle)
{
    const auto index = CBaseHandle::FromPackedValue(handle).GetEntryIndex();
    if (!IsEntityIndexInRange(index))
        return FBS_None;

    const auto slot = s_fire_bullet_state[index].load(std::memory_order_relaxed);

    return slot.handle == handle ? slot.state : FBS_None;
}

void TransmitCheckWorldDecals(const NetworkReceiver_t* clients)
{
    *const_cast<uint64_t*>(clients) &= ~g_bitsBlockTempEnt[BT_WorldDecals].load(std::memory_order_relaxed);
}

void TransmitCheckBloodEffect(const NetworkReceiver_t* clients)
{
    *const_cast<uint64_t*>(clients) &= ~g_bitsBlockTempEnt[BT_BloodEffect].load(std::memory_order_relaxed);
}

// Return True if the music is blocked.
bool TransmitCheckMapMusic(int slot)
{
    return GetBlockTempEntState(BT_MapMusic, static_cast<PlayerSlot_t>(slot));
}