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
#include "bridge/forwards/forward.h"
#include "global.h"
#include "hook/installer.h"
#include "memory/zydis_utility.h"
#include "module.h"
#include "sdkproxy.h"
#include "strtool.h"

#include "cstrike/component/PlayerPawnComponent.h"
#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/CGamePlayerEquip.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/ISchemaSystem.h"
#include "cstrike/schema.h"
#include "cstrike/type/CEconItemView.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/Variant.h"

#include <Zydis.h>
#include <safetyhook.hpp>

#include <algorithm>
#include <unordered_map>

#define FIX_PLAYER_EQUIP_MANUALLY

// #define HOOK_EXTERN_GIVENAMEDITEM_ASSERT

struct WeaponInfo_t
{
    int32_t       m_iItemDefinitionIndex;
    CStrikeTeam_t m_iTeamNum;
    GearSlot_t    m_eSlot;
    int32_t       m_nSlotPosition;

    WeaponInfo_t(int32_t index, CStrikeTeam_t team, GearSlot_t slot, int32_t position) :
        m_iItemDefinitionIndex(index), m_iTeamNum(team), m_eSlot(slot), m_nSlotPosition(position) {}

    WeaponInfo_t(int32_t index, uint8_t team, GearSlot_t slot, int32_t position) :
        m_iItemDefinitionIndex(index), m_iTeamNum(static_cast<CStrikeTeam_t>(team)), m_eSlot(slot), m_nSlotPosition(position) {}
};

static std::unordered_map<std::string, WeaponInfo_t> s_WeaponMap = {
    {"weapon_deagle",                {1, 0, GearSlot_t::GEAR_SLOT_PISTOL, 0}   },
    {"weapon_elite",                 {2, 0, GearSlot_t::GEAR_SLOT_PISTOL, 0}   },
    {"weapon_fiveseven",             {3, 3, GearSlot_t::GEAR_SLOT_PISTOL, 0}   },
    {"weapon_glock",                 {4, 2, GearSlot_t::GEAR_SLOT_PISTOL, 0}   },
    {"weapon_ak47",                  {7, 2, GearSlot_t::GEAR_SLOT_RIFLE, 0}    },
    {"weapon_aug",                   {8, 3, GearSlot_t::GEAR_SLOT_RIFLE, 0}    },
    {"weapon_awp",                   {9, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}    },
    {"weapon_famas",                 {10, 3, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_g3sg1",                 {11, 2, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_galilar",               {13, 2, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_m249",                  {14, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_m4a1",                  {16, 3, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_mac10",                 {17, 2, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_p90",                   {19, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_mp5sd",                 {23, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_ump45",                 {24, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_xm1014",                {25, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_bizon",                 {26, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_mag7",                  {27, 3, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_negev",                 {28, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_sawedoff",              {29, 2, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_tec9",                  {30, 2, GearSlot_t::GEAR_SLOT_PISTOL, 0}  },
    {"weapon_taser",                 {31, 0, GearSlot_t::GEAR_SLOT_KNIFE, 1}   },
    {"weapon_hkp2000",               {32, 3, GearSlot_t::GEAR_SLOT_PISTOL, 0}  },
    {"weapon_mp7",                   {33, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_mp9",                   {34, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_nova",                  {35, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_p250",                  {36, 0, GearSlot_t::GEAR_SLOT_PISTOL, 0}  },
    {"weapon_scar20",                {38, 3, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_sg556",                 {39, 2, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_ssg08",                 {40, 0, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_knifegg",               {41, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}   },
    {"weapon_knife",                 {42, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}   },
    {"weapon_flashbang",             {43, 0, GearSlot_t::GEAR_SLOT_GRENADES, 1}},
    {"weapon_hegrenade",             {44, 0, GearSlot_t::GEAR_SLOT_GRENADES, 0}},
    {"weapon_smokegrenade",          {45, 0, GearSlot_t::GEAR_SLOT_GRENADES, 2}},
    {"weapon_molotov",               {46, 0, GearSlot_t::GEAR_SLOT_GRENADES, 4}},
    {"weapon_decoy",                 {47, 0, GearSlot_t::GEAR_SLOT_GRENADES, 3}},
    {"weapon_incgrenade",            {48, 0, GearSlot_t::GEAR_SLOT_GRENADES, 4}},
    {"weapon_c4",                    {49, 0, GearSlot_t::GEAR_SLOT_C4, 0}      },
    {"weapon_knife_t",               {59, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}   },
    {"weapon_m4a1_silencer",         {60, 3, GearSlot_t::GEAR_SLOT_RIFLE, 0}   },
    {"weapon_usp_silencer",          {61, 3, GearSlot_t::GEAR_SLOT_PISTOL, 0}  },
    {"weapon_cz75a",                 {63, 0, GearSlot_t::GEAR_SLOT_PISTOL, 0}  },
    {"weapon_revolver",              {64, 0, GearSlot_t::GEAR_SLOT_PISTOL, 0}  },
    {"weapon_bayonet",               {500, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_css",             {503, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_flip",            {505, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_gut",             {506, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_karambit",        {507, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_m9_bayonet",      {508, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_tactical",        {509, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_falchion",        {512, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_survival_bowie",  {514, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_butterfly",       {515, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_push",            {516, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_cord",            {517, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_canis",           {518, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_ursus",           {519, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_gypsy_jackknife", {520, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_outdoor",         {521, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_stiletto",        {522, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_widowmaker",      {523, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_skeleton",        {525, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },
    {"weapon_knife_kukri",           {526, 0, GearSlot_t::GEAR_SLOT_KNIFE, 0}  },

    // Gears
    {"item_kevlar",                  {0, 0, GearSlot_t::GEAR_SLOT_INVALID, -1} },
    {"item_assaultsuit",             {0, 0, GearSlot_t::GEAR_SLOT_INVALID, -1} },
    {"item_defuser",                 {0, 0, GearSlot_t::GEAR_SLOT_INVALID, -1} },
    {"ammo_50ae",                    {0, 0, GearSlot_t::GEAR_SLOT_INVALID, -1} },
    /*{"item_heavyassaultsuit",        {0, 0, GearSlot_t::GEAR_SLOT_INVALID, -1} },*/
};

static volatile bool s_bGiveNamedItemIgnoredCEconItemView = false;

BeginMemberHookScope(CBasePlayerPawn)
{
    DeclareMemberDetourHook(FindMatchingWeaponsForTeamLoadout, void, (CBasePlayerPawn * pPawn, const char* pchName, int team, bool bMustBeTeamSpecific, void* unknown))
    {
        if (s_bGiveNamedItemIgnoredCEconItemView)
        {
            s_bGiveNamedItemIgnoredCEconItemView = false;
            return;
        }

        FindMatchingWeaponsForTeamLoadout(pPawn, pchName, team, bMustBeTeamSpecific, unknown);
    }
}

BeginMemberHookScope(CCSPlayer_ItemServices)
{
#ifdef PLATFORM_WINDOWS
    DeclareMemberDetourHook(GiveNamedItem, CBaseEntity*, (CCSPlayer_ItemServices * pItemServices, const char* pClassname, int64_t iSubType, void* pScriptItem, bool bForce, Vector* pOrigin))
    {
#    define CALL_GiveNamedItem() \
        GiveNamedItem(pItemServices, pClassname, iSubType, pScriptItem, bForce, pOrigin)
#    define CALL_GiveNamedItemChanged() \
        GiveNamedItem(pItemServices, classname, iSubType, pScriptItem, bForce, pOrigin)
#else
    DeclareMemberDetourHook(GiveNamedItem, CBaseEntity*, (CCSPlayer_ItemServices * pItemServices, const char* pClassname, int64_t a4, void* pScriptItem, bool bForced, Vector* pOrigin))
    {
#    define CALL_GiveNamedItem() \
        GiveNamedItem(pItemServices, pClassname, a4, pScriptItem, bForced, pOrigin)
#    define CALL_GiveNamedItemChanged() \
        GiveNamedItem(pItemServices, classname, a4, pScriptItem, bForced, pOrigin)
#endif
        s_bGiveNamedItemIgnoredCEconItemView = false;

        auto       bIgnore     = false;
        const auto pPawn       = pItemServices->GetPawn<CCSPlayerPawn*>();
        const auto pController = pPawn->GetController<CCSPlayerController*>();
        if (pController == nullptr)
        {
            WARN("pController is nullptr in GiveNamedItem -> [%s]", pClassname);
            return CALL_GiveNamedItem();
        }
        const auto pClient = sv->GetClient(pController->GetPlayerSlot());
        if (pClient == nullptr)
        {
            FatalError("pClient is nullptr in GiveNamedItem -> [%s]", pClassname);
        }
        if (!pClient->IsInGame())
        {
            WARN("pClient is not InGame in GiveNamedItem -> [%s]", pClassname);
            return CALL_GiveNamedItem();
        }

        char refClassname[64], classname[64];
        StrCopy(refClassname, sizeof(refClassname), pClassname);
        memcpy(classname, refClassname, sizeof(classname));

        const auto action = forwards::OnGiveNamedItemPre->Invoke(pClient, pController, pPawn, refClassname, &bIgnore);

        if (action == EHookAction::Ignored)
        {
            bIgnore = false;
            goto postHook;
        }

        if (action == EHookAction::SkipCallReturnOverride || action == EHookAction::ChangeParamReturnOverride || action == EHookAction::IgnoreParamReturnOverride)
        {
            FatalError("GiveNamedItem: unsupported hook action '%s'", EHookActionName(action));
        }

        if (action == EHookAction::ChangeParamReturnDefault)
        {
            if (bIgnore)
            {
                pScriptItem                          = nullptr;
                s_bGiveNamedItemIgnoredCEconItemView = bIgnore;
            }

            StrCopy(classname, sizeof(classname), refClassname);
        }

    postHook:

        const auto pWeapon = CALL_GiveNamedItemChanged();

        forwards::OnGiveNamedItemPost->Invoke(pClient, pController, pPawn, classname, bIgnore, action, pWeapon);

        return pWeapon;
    }

    DeclareMemberDetourHook(GiveGlove, void, (CCSPlayer_ItemServices * pItemServices))
    {
        GiveGlove(pItemServices);

        const auto pPawn = pItemServices->GetPawn<CCSPlayerPawn*>();
        if (pPawn == nullptr)
            return;

        const auto pController = pPawn->GetController<CCSPlayerController*>();
        if (pController == nullptr)
            return;

        const auto pClient = sv->GetClient(pController->GetPlayerSlot());
        if (pClient == nullptr)
            return;

        if (pClient->IsFakeClient() || !pClient->IsAuthenticated())
            return;

        forwards::OnGiveGloveItemPost->Invoke(pClient, pController, pPawn);
    }

    DeclareMemberDetourHook(CanAcquire, int, (CCSPlayer_ItemServices * pItemServices, CEconItemView * pEconItemView, int iAcquireMethod, int* pLimit))
    {
        const auto pPawn       = pItemServices->GetPawn<CCSPlayerPawn*>();
        const auto pController = pPawn->GetController<CCSPlayerController*>();
        if (pController == nullptr)
        {
            WARN("pController is nullptr!");
            return CanAcquire(pItemServices, pEconItemView, iAcquireMethod, pLimit);
        }
        const auto pClient = sv->GetClient(pController->GetPlayerSlot());
        if (pClient == nullptr)
        {
            FatalError("pClient is nullptr!");
        }
        if (!pClient->IsInGame())
        {
            WARN("pClient is not InGame!");
            return CanAcquire(pItemServices, pEconItemView, iAcquireMethod, pLimit);
        }

        const auto itemIndex = pEconItemView->m_iItemDefinitionIndex();

        auto acquireResult = CanAcquire(pItemServices, pEconItemView, iAcquireMethod, pLimit);
        auto proxyResult   = acquireResult;

        const auto action = forwards::OnCanAcquirePre->Invoke(pClient, pController, pPawn, itemIndex, iAcquireMethod, &proxyResult);

        if (action == EHookAction::Ignored)
            return acquireResult;

        if (action == EHookAction::SkipCallReturnOverride)
            return proxyResult;

        FatalError("CanAcquire: unsupported hook action '%s'", EHookActionName(action));
        return acquireResult;
    }
}

#ifdef FIX_PLAYER_EQUIP_MANUALLY

static int32_t s_nPlayerEquipWeaponsOffset = 0;

static void EquipPlayerItem(CCSPlayerPawn* pPlayer, CGamePlayerEquip* pEntity);
static void TriggerForPlayer(CGamePlayerEquip* pEntity, CCSPlayerPawn* pPlayer, const char* pszWeapon);

static int32_t ResolvePlayerEquipWeaponsOffset()
{
    const auto pClass = schemas::FindClassInfo("CGamePlayerEquip");
    AssertPtr(pClass);

    const auto pBases    = pClass->GetBaseClassSize() == 1 ? pClass->GetBaseClasses() : nullptr;
    const auto pBase     = pBases && pBases->m_nOffset == 0 ? pBases->m_pClass : nullptr;
    const auto pszBase   = pBase ? pBase->GetName() : "";
    const auto nBaseSize = pBase ? pBase->GetSize() : 0;

    if (strcmp(pszBase, "CRulePointEntity") != 0
        || pClass->GetFieldsSize() != 0
        || pClass->GetSize() - nBaseSize != static_cast<int32_t>(sizeof(CUtlVector<CUtlString>)))
    {
        FatalError("ResolvePlayerEquipWeaponsOffset: unexpected CGamePlayerEquip layout: bases=%d base='%s' fields=%d size=%d base_size=%d",
                   pClass->GetBaseClassSize(), pszBase, pClass->GetFieldsSize(), pClass->GetSize(), nBaseSize);
    }

    return nBaseSize;
}

static CGamePlayerEquip* ResolvePulsePlayerEquip(const void* pEntityArgument)
{
    if (!pEntityArgument)
        return nullptr;

    const auto pHandle = *reinterpret_cast<const CBaseHandle* const*>(static_cast<const char*>(pEntityArgument) + 8);
    if (!pHandle || !pHandle->IsValid())
        return nullptr;

    const auto pEntity = g_pGameEntitySystem->FindEntityByEHandle<CGamePlayerEquip*>(*pHandle);
    if (!pEntity)
        return nullptr;

    const auto pszClassname = pEntity->GetClassname();
    if (!pszClassname || strcasecmp(pszClassname, "game_player_equip") != 0)
        return nullptr;

    return pEntity;
}

static const char* ResolvePulseWeapon(const void* pEntityArgument)
{
    const auto ppszWeapon = *reinterpret_cast<const char* const* const*>(static_cast<const char*>(pEntityArgument) + 0x10);
    return ppszWeapon ? *ppszWeapon : nullptr;
}

static CCSPlayerPawn* ResolvePulsePlayer(const void* pContext)
{
    if (!pContext)
        return nullptr;

    const auto pValue = *reinterpret_cast<void* const*>(static_cast<const char*>(pContext) + 0x10);
    if (!pValue)
        return nullptr;

    const auto pVTable = *reinterpret_cast<void* const* const*>(pValue);
    if (!pVTable)
        return nullptr;

    using Getter          = void* (*)(void*);
    const auto pActivator = reinterpret_cast<CBaseEntity*>(reinterpret_cast<Getter>(pVTable[0])(pValue));
    reinterpret_cast<Getter>(pVTable[1])(pValue);
    if (!pActivator || !pActivator->IsPlayerPawn())
        return nullptr;

    const auto pPlayer = reinterpret_cast<CCSPlayerPawn*>(pActivator);
    return pPlayer->IsPlayer() && pPlayer->IsAlive() ? pPlayer : nullptr;
}

BeginMemberHookScope(CGamePlayerEquip)
{
    DeclareVirtualHook(Use, void, (CGamePlayerEquip * pEntity, int64_t* params))
    {
#    ifdef HOOK_EXTERN_GIVENAMEDITEM_ASSERT
        WARN("%10s: 0x%p\n" // CGamePlayerEquip*
             "%10s: 0x%p",  // uint64_t*
             "this", pEntity,
             "params", params);
#    endif

        const auto pCaller = reinterpret_cast<CBaseEntity*>(*params);

        if (!pCaller || !pCaller->IsPlayerPawn())
            return;

        const auto pPlayer = reinterpret_cast<CCSPlayerPawn*>(pCaller);

        if (!pPlayer->IsPlayer() || !pPlayer->IsAlive())
            return;

        EquipPlayerItem(pPlayer, pEntity);
    }

    DeclareVirtualHook(Touch, void, (CGamePlayerEquip * pEntity, CBaseEntity * pOther))
    {
#    ifdef HOOK_EXTERN_GIVENAMEDITEM_ASSERT
        WARN("%10s: 0x%p\n" // CGamePlayerEquip*
             "%10s: 0x%p",  // uint64_t*
             "this", pEntity,
             "pOther", pOther);
#    endif

        if (!pOther || !pOther->IsPlayerPawn())
            return;

        if (pEntity->m_spawnflags() & CGamePlayerEquip::SF_PLAYEREQUIP_USEONLY)
            return;

        const auto pPlayer = reinterpret_cast<CCSPlayerPawn*>(pOther);

        if (!pPlayer->IsPlayer() || !pPlayer->IsAlive())
            return;

        EquipPlayerItem(pPlayer, pEntity);
    }

    // The Pulse AllPlayers callback still calls this two-argument game function.
    DeclareMemberDetourHook(PulseTriggerForAllPlayers, void, (CGamePlayerEquip * pEntity, void*))
    {
        CCSPlayerPawn* pPlayer = nullptr;
        while ((pPlayer = g_pGameEntitySystem->FindByClassnameCast<CCSPlayerPawn*>(pPlayer, "player")) != nullptr)
        {
            if (pPlayer->IsPlayerPawn() && pPlayer->IsPlayer() && pPlayer->IsAlive())
                TriggerForPlayer(pEntity, pPlayer, nullptr);
        }
    }

    DeclareMemberDetourHook(PulseTriggerForActivatedPlayer, int32_t, (void*, void*, void*, void* pContext, void* pEntityArgument))
    {
        const auto pEntity = ResolvePulsePlayerEquip(pEntityArgument);
        if (!pEntity)
            return -2;

        if (const auto pPlayer = ResolvePulsePlayer(pContext))
            TriggerForPlayer(pEntity, pPlayer, ResolvePulseWeapon(pEntityArgument));
        return 0;
    }
}

static void StripSameSlotWeapons(CCSPlayerPawn* pPlayer, const WeaponInfo_t& info)
{
    if (info.m_eSlot == GearSlot_t::GEAR_SLOT_INVALID)
        return;

    CBaseWeapon* pWeapon = nullptr;
    while ((pWeapon = pPlayer->GetWeaponBySlot(info.m_eSlot, info.m_nSlotPosition)) != nullptr)
    {
        if (info.m_eSlot == GearSlot_t::GEAR_SLOT_GRENADES)
        {
            if (const auto ammoType = pWeapon->GetVData()->m_nPrimaryAmmoType(); ammoType >= 0 && ammoType < MAX_AMMO_TYPES)
                pPlayer->m_pWeaponServices()->SetAmmo(ammoType, 0);
        }

        pPlayer->RemovePlayerItem(pWeapon);
    }
}

static void GiveGrenade(CCSPlayerPawn* pPlayer, const char* pszName)
{
    const auto pGrenade = pPlayer->GiveNamedItem(pszName);
    if (pGrenade && !pGrenade->IsMarkedForDeletion() && pGrenade->GetOwner() != pPlayer)
        pGrenade->Kill();
}

// NOTE game_player_equip does not work in CStrike 2
// Now We impl that manually
// 这里全部照着CSGO的实现写!
static void EquipPlayerItem(CCSPlayerPawn* pPlayer, CGamePlayerEquip* pEntity)
{
    const auto flags = pEntity->GetSpawnFlags();

    if (flags & CGamePlayerEquip::SF_PLAYEREQUIP_STRIPFIRST)
    {
        pPlayer->RemoveAllItems(true);
    }

    const auto pWeapons = reinterpret_cast<const CUtlVector<CUtlString>*>(reinterpret_cast<uintptr_t>(pEntity) + s_nPlayerEquipWeaponsOffset);

    const auto team = pPlayer->GetTeam();

    for (const auto& weapon : *pWeapons)
    {
        const auto name = LowercaseString(weapon.Get());
        if (name.empty())
            continue;

        if (strcasecmp(name.c_str(), "ammo_50ae") == 0)
        {
            // HACK FIX for ammo_50AE
            const auto pPrimaryWeapon   = pPlayer->GetWeaponBySlot(GearSlot_t::GEAR_SLOT_RIFLE);
            const auto pSecondaryWeapon = pPlayer->GetWeaponBySlot(GearSlot_t::GEAR_SLOT_PISTOL);

            Variant_t pri{};
            pri.SetInt(99999);
            Variant_t sec{};
            sec.SetInt(99999);

            if (pPrimaryWeapon)
            {
                pPrimaryWeapon->AcceptInput("SetAmmoAmount", nullptr, nullptr, pri);
                pPrimaryWeapon->AcceptInput("SetReserveAmmoAmount", nullptr, nullptr, sec);
            }
            if (pSecondaryWeapon)
            {
                pSecondaryWeapon->AcceptInput("SetAmmoAmount", nullptr, nullptr, pri);
                pSecondaryWeapon->AcceptInput("SetReserveAmmoAmount", nullptr, nullptr, sec);
            }

            continue;
        }

        const auto data = s_WeaponMap.find(name);
        if (data != s_WeaponMap.end())
        {
            if (data->second.m_eSlot != GearSlot_t::GEAR_SLOT_INVALID)
            {
                if (flags & CGamePlayerEquip::SF_PLAYEREQUIP_ONLYSTRIPSAME)
                {
                    StripSameSlotWeapons(pPlayer, data->second);
                }

                if (data->second.m_eSlot == GearSlot_t::GEAR_SLOT_GRENADES)
                {
                    GiveGrenade(pPlayer, name.c_str());
                }
                else if (data->second.m_iTeamNum != TEAM_UNASSIGNED)
                {
                    if (data->second.m_iTeamNum != team)
                    {
                        pPlayer->TransientChangeTeam(data->second.m_iTeamNum);
                        pPlayer->GiveNamedItem(name.c_str());
                        pPlayer->TransientChangeTeam(team);
                    }
                    else
                    {
                        pPlayer->GiveNamedItem(name.c_str());
                    }
                }
                else
                {
                    pPlayer->GiveNamedItem(name.c_str());
                }
            }
            else
            {
                pPlayer->GiveNamedItem(name.c_str());
            }
        }
        else
        {
            WARN("game_player_equip: GiveNamedItem with unknown type '%s'\n", name.c_str());
        }
    }
}

static void TriggerForPlayer(CGamePlayerEquip* pEntity, CCSPlayerPawn* pPlayer, const char* pszWeapon)
{
    if (!pszWeapon || strnlen(pszWeapon, 5) <= 4 || strcasecmp(pszWeapon, "(null)") == 0) // 'weapon_' or 'item_'
    {
        EquipPlayerItem(pPlayer, pEntity);
        return;
    }

    const auto name = LowercaseString(pszWeapon);
    const auto data = s_WeaponMap.find(name);
    if (data == s_WeaponMap.end())
        return;

    const auto flags = pEntity->GetSpawnFlags();

    if (flags & CGamePlayerEquip::SF_PLAYEREQUIP_STRIPFIRST)
    {
        pPlayer->RemoveAllItems(true);
    }
    else if (flags & CGamePlayerEquip::SF_PLAYEREQUIP_ONLYSTRIPSAME)
    {
        StripSameSlotWeapons(pPlayer, data->second);
    }

    if (data->second.m_eSlot == GearSlot_t::GEAR_SLOT_GRENADES)
    {
        GiveGrenade(pPlayer, name.c_str());
    }
    else if (data->second.m_eSlot != GearSlot_t::GEAR_SLOT_INVALID)
    {
        const auto team = pPlayer->GetTeam();
        if (data->second.m_iTeamNum != TEAM_UNASSIGNED && data->second.m_iTeamNum != team)
        {
            pPlayer->TransientChangeTeam(data->second.m_iTeamNum);
            pPlayer->GiveNamedItem(name.c_str());
            pPlayer->TransientChangeTeam(team);
        }
        else
        {
            pPlayer->GiveNamedItem(name.c_str());
        }
    }
    else
    {
        pPlayer->GiveNamedItem(name.c_str());
    }
}

#endif

static void PatchGiveNamedItemLimit()
{
    static auto address = g_pGameData->GetAddress<uintptr_t>("CCSPlayer_ItemServices::GiveNamedItem");
    if (!address)
    {
        FatalError("Failed to find address for 'CCSPlayer_ItemServices::GiveNamedItem'");
    }

    auto V_stricmp_fast = modules::tier0->GetExportByName("V_stricmp_fast");
    if (!V_stricmp_fast.IsValid()) [[unlikely]]
    {
        FatalError("Failed to get V_stricmp_fast from tier0");
    }

    ZydisDecoder decoder{};
    if (ZYAN_FAILED(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
    {
        FatalError("Failed to initialize decoder");
    }

    ZydisDecodedInstruction instr{};
    ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

    bool     prev_was_stricmp     = false;
    uint8_t* pending_test_address = nullptr;

    constexpr int max_decode_times = 40;

    for (auto count = 0; count < max_decode_times; count++)
    {
        if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&decoder, reinterpret_cast<void*>(address), ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, operands))) [[unlikely]]
        {
            address += instr.length;
            continue;
        }

        /*
        call    cs:V_stricmp_fast
        test    eax, eax;        replace with xor eax, eax. forcing eax to 0
        jz      loc_180703391
        */
        if (pending_test_address && instr.meta.category == ZYDIS_CATEGORY_COND_BR)
        {
            std::array<uint8_t, ZYDIS_MAX_INSTRUCTION_LENGTH> buffer{};

            ZydisEncoderRequest req   = {};
            req.mnemonic              = ZYDIS_MNEMONIC_XOR;
            req.machine_mode          = ZYDIS_MACHINE_MODE_LONG_64;
            req.operand_count         = 2;
            req.operands[0].type      = ZYDIS_OPERAND_TYPE_REGISTER;
            req.operands[0].reg.value = ZYDIS_REGISTER_EAX;
            req.operands[1].type      = ZYDIS_OPERAND_TYPE_REGISTER;
            req.operands[1].reg.value = ZYDIS_REGISTER_EAX;

            ZyanUSize encoded_length = buffer.size();
            if (ZYAN_SUCCESS(ZydisEncoderEncodeInstruction(&req, buffer.data(), &encoded_length)))
            {
                if (encoded_length == 2)
                {
                    if (auto unprotect_guard = safetyhook::unprotect(pending_test_address, encoded_length))
                    {
                        memcpy(pending_test_address, buffer.data(), encoded_length);
                        FLOG("Successfully patched GiveNamedItem limit @ server+0x%llx", reinterpret_cast<uintptr_t>(pending_test_address) - modules::server->Base());
                    }
                    else
                    {
                        WARN("Failed to unprotect memory for patching GiveNamedItem");
                    }
                    return;
                }
                WARN("Encoder generated instruction length mismatch (Expected 2, got %d)", encoded_length);
            }
            else
            {
                WARN("Failed to encode XOR instruction");
            }

            return;
        }

        if (prev_was_stricmp && instr.mnemonic == ZYDIS_MNEMONIC_TEST && operands[0].reg.value == ZYDIS_REGISTER_EAX && operands[1].reg.value == ZYDIS_REGISTER_EAX)
        {
            pending_test_address = reinterpret_cast<uint8_t*>(address);
        }
        else if (instr.meta.category != ZYDIS_CATEGORY_COND_BR)
        {
            pending_test_address = nullptr;
        }

        if (instr.mnemonic == ZYDIS_MNEMONIC_CALL)
        {
            uintptr_t final_target = ZydisUtility::ResolveCallTarget(&instr, operands, address);
            prev_was_stricmp       = final_target == V_stricmp_fast;
        }
        else if (instr.mnemonic != ZYDIS_MNEMONIC_TEST)
        {
            prev_was_stricmp = false;
        }

        address += instr.length;
    }

    WARN("Failed to patch GiveNamedItemLimit after decoding %i times", max_decode_times);
}

void InstallGiveNamedItemHooks()
{
    HOOK(CCSPlayer_ItemServices, GiveNamedItem);
    HOOK(CCSPlayer_ItemServices, GiveGlove);
    HOOK(CCSPlayer_ItemServices, CanAcquire);

    HOOK(CBasePlayerPawn, FindMatchingWeaponsForTeamLoadout);

    PatchGiveNamedItemLimit();

#ifdef FIX_PLAYER_EQUIP_MANUALLY

    s_nPlayerEquipWeaponsOffset = ResolvePlayerEquipWeaponsOffset();

    HOOK(CGamePlayerEquip, PulseTriggerForAllPlayers, {.address = g_pGameData->GetAddress<void*>("CGamePlayerEquip::TriggerForAllPlayers")});
    HOOK(CGamePlayerEquip, PulseTriggerForActivatedPlayer);

    VHOOK(CGamePlayerEquip, Use, server, {.gamedata = "CBaseEntity::Use"});
    VHOOK(CGamePlayerEquip, Touch, server, {.gamedata = "CBaseEntity::Touch"});
#endif
}
