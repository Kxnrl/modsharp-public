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
#include "global.h"
#include "manager/HookManager.h"
#include "memory/memory_access.h"
#include "module.h"
#include "sdkproxy.h"
#include "strtool.h"
#include "vhook/hook.h"

#include "cstrike/component/PlayerPawnComponent.h"
#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/CGamePlayerEquip.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/type/CEconItemView.h"
#include "cstrike/type/CEntityKeyValues.h"
#include "cstrike/type/CEntityPrecacheContext.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/Variant.h"

#include <Zydis.h>
#include <safetyhook.hpp>

#include <algorithm>
#include <ranges>
#include <unordered_map>

#ifdef PLATFORM_WINDOWS
#    define FIX_PLAYER_EQUIP_MANUALLY
#endif

// #define HOOK_EXTERN_GIVENAMEDITEM_ASSERT

struct WeaponInfo_t
{
    int32_t       m_iItemDefinitionIndex;
    CStrikeTeam_t m_iTeamNum;
    GearSlot_t    m_eSlot;

    WeaponInfo_t(int32_t index, CStrikeTeam_t team, GearSlot_t slot) :
        m_iItemDefinitionIndex(index), m_iTeamNum(team), m_eSlot(slot) {}

    WeaponInfo_t(int32_t index, uint8_t team, GearSlot_t slot) :
        m_iItemDefinitionIndex(index), m_iTeamNum(static_cast<CStrikeTeam_t>(team)), m_eSlot(slot) {}
};

static std::unordered_map<uint32_t, std::vector<std::string>> s_gamePlayerEquipMap;

static std::unordered_map<std::string, WeaponInfo_t> s_WeaponMap = {
    {"weapon_deagle",                {1, 0, GearSlot_t::GEAR_SLOT_PISTOL}   },
    {"weapon_elite",                 {2, 0, GearSlot_t::GEAR_SLOT_PISTOL}   },
    {"weapon_fiveseven",             {3, 3, GearSlot_t::GEAR_SLOT_PISTOL}   },
    {"weapon_glock",                 {4, 2, GearSlot_t::GEAR_SLOT_PISTOL}   },
    {"weapon_ak47",                  {7, 2, GearSlot_t::GEAR_SLOT_RIFLE}    },
    {"weapon_aug",                   {8, 3, GearSlot_t::GEAR_SLOT_RIFLE}    },
    {"weapon_awp",                   {9, 0, GearSlot_t::GEAR_SLOT_RIFLE}    },
    {"weapon_famas",                 {10, 3, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_g3sg1",                 {11, 2, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_galilar",               {13, 2, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_m249",                  {14, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_m4a1",                  {16, 3, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_mac10",                 {17, 2, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_p90",                   {19, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_mp5sd",                 {23, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_ump45",                 {24, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_xm1014",                {25, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_bizon",                 {26, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_mag7",                  {27, 3, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_negev",                 {28, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_sawedoff",              {29, 2, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_tec9",                  {30, 2, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_taser",                 {31, 0, GearSlot_t::GEAR_SLOT_KNIFE}   },
    {"weapon_hkp2000",               {32, 3, GearSlot_t::GEAR_SLOT_PISTOL}  },
    {"weapon_mp7",                   {33, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_mp9",                   {34, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_nova",                  {35, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_p250",                  {36, 0, GearSlot_t::GEAR_SLOT_PISTOL}  },
    {"weapon_scar20",                {38, 3, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_sg556",                 {39, 2, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_ssg08",                 {40, 0, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_knifegg",               {41, 0, GearSlot_t::GEAR_SLOT_KNIFE}   },
    {"weapon_knife",                 {42, 0, GearSlot_t::GEAR_SLOT_KNIFE}   },
    {"weapon_flashbang",             {43, 0, GearSlot_t::GEAR_SLOT_GRENADES}},
    {"weapon_hegrenade",             {44, 0, GearSlot_t::GEAR_SLOT_GRENADES}},
    {"weapon_smokegrenade",          {45, 0, GearSlot_t::GEAR_SLOT_GRENADES}},
    {"weapon_molotov",               {46, 0, GearSlot_t::GEAR_SLOT_GRENADES}},
    {"weapon_decoy",                 {47, 0, GearSlot_t::GEAR_SLOT_GRENADES}},
    {"weapon_incgrenade",            {48, 0, GearSlot_t::GEAR_SLOT_GRENADES}},
    {"weapon_c4",                    {49, 0, GearSlot_t::GEAR_SLOT_C4}      },
    {"weapon_knife_t",               {59, 0, GearSlot_t::GEAR_SLOT_KNIFE}   },
    {"weapon_m4a1_silencer",         {60, 3, GearSlot_t::GEAR_SLOT_RIFLE}   },
    {"weapon_usp_silencer",          {61, 3, GearSlot_t::GEAR_SLOT_PISTOL}  },
    {"weapon_cz75a",                 {63, 0, GearSlot_t::GEAR_SLOT_PISTOL}  },
    {"weapon_revolver",              {64, 0, GearSlot_t::GEAR_SLOT_PISTOL}  },
    {"weapon_bayonet",               {500, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_css",             {503, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_flip",            {505, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_gut",             {506, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_karambit",        {507, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_m9_bayonet",      {508, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_tactical",        {509, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_falchion",        {512, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_survival_bowie",  {514, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_butterfly",       {515, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_push",            {516, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_cord",            {517, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_canis",           {518, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_ursus",           {519, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_gypsy_jackknife", {520, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_outdoor",         {521, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_stiletto",        {522, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_widowmaker",      {523, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_skeleton",        {525, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },
    {"weapon_knife_kukri",           {526, 0, GearSlot_t::GEAR_SLOT_KNIFE}  },

    // Gears
    {"item_kevlar",                  {0, 0, GearSlot_t::GEAR_SLOT_INVALID}  },
    {"item_assaultsuit",             {0, 0, GearSlot_t::GEAR_SLOT_INVALID}  },
    {"item_defuser",                 {0, 0, GearSlot_t::GEAR_SLOT_INVALID}  },
    {"ammo_50ae",                    {0, 0, GearSlot_t::GEAR_SLOT_INVALID}  },
    /*{"item_heavyassaultsuit",        {0, 0, GearSlot_t::GEAR_SLOT_INVALID}  },*/
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
            WARN("pController is nullptr!");
            return CALL_GiveNamedItem();
        }
        const auto pClient = sv->GetClient(pController->GetPlayerSlot());
        if (pClient == nullptr)
        {
            FatalError("pClient is nullptr!");
        }
        if (!pClient->IsInGame())
        {
            WARN("pClient is not InGame!");
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
            FatalError("Not impl yet!");
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

        FatalError("Not implemented");
        return 0;
    }
}

#ifdef FIX_PLAYER_EQUIP_MANUALLY

static bool EquipPlayerItem(CBasePlayerPawn* pPlayer, CGamePlayerEquip* pEntity);

BeginMemberHookScope(CGamePlayerEquip)
{
    DeclareVirtualHook(Precache, void, (CGamePlayerEquip * pEntity, const CEntityPrecacheContext* pContext))
    {
        const auto pHammerId = pContext->m_pKeyValues->FindKeyValues("hammerUniqueId");
        if (!pHammerId)
            return Precache(pEntity, pContext);

        const auto kv = pContext->m_pKeyValues;

        auto list = std::vector<std::string>();

        for (auto i = 0; i < CGamePlayerEquip::MAX_EQUIPMENTS_SIZE; i++)
        {
            char key[32];
            snprintf(key, sizeof(key), "weapon%d", i);
            const auto val = kv->GetString(key);
            if (val && strlen(val) > 1)
            {
                list.emplace_back(val);
            }
        }

        const auto hEntity = MurmurHash2Lowercase(pHammerId->GetString(), MURMURHASH_SEED);

        if (!list.empty())
            s_gamePlayerEquipMap[hEntity] = list;

        Precache(pEntity, pContext);
    }

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

        if (EquipPlayerItem(reinterpret_cast<CBasePlayerPawn*>(pCaller), pEntity))
            return;

        Use(pEntity, params);
    }

    DeclareVirtualHook(Touch, void, (CGamePlayerEquip * pEntity, CBaseEntity * pOther))
    {
#    ifdef HOOK_EXTERN_GIVENAMEDITEM_ASSERT
        WARN("%10s: 0x%p\n" // CGamePlayerEquip*
             "%10s: 0x%p",  // uint64_t*
             "this", pEntity,
             "pOther", pOther);
#    endif

        if (!pOther)
            return;

        if (pEntity->m_spawnflags() & CGamePlayerEquip::SF_PLAYEREQUIP_USEONLY)
            return;

        if (!pOther->IsPlayerPawn())
            return;

        if (EquipPlayerItem(reinterpret_cast<CBasePlayerPawn*>(pOther), pEntity))
            return;

        Touch(pEntity, pOther);
    }

    DeclareMemberDetourHook(InputTriggerForAllPlayers, void, (CGamePlayerEquip * pEntity, InputData_t * pInput))
    {
        auto           handle  = false;
        CCSPlayerPawn* pPlayer = nullptr;
        while ((pPlayer = (g_pGameEntitySystem->FindByClassnameCast<CCSPlayerPawn*>(pPlayer, "player"))) != nullptr)
        {
            if (!pPlayer->IsPlayerPawn())
                continue;

            if (!pPlayer->IsAlive())
                continue;

            if (EquipPlayerItem(pPlayer, pEntity))
                handle = true;
        }

        if (handle)
            return;

        InputTriggerForAllPlayers(pEntity, pInput);
    }

    DeclareMemberDetourHook(InputTriggerForActivatedPlayer, void, (CGamePlayerEquip * pEntity, InputData_t * pInput))
    {
        if (!pInput->pActivator || !pInput->pActivator->IsPlayerPawn())
            return;

        const auto pszWeapon = pInput->value.AutoCastString();
        if (!pszWeapon || strnlen(pszWeapon, 5) <= 4) // 'weapon_' or 'item_'
        {
            // Fallback to default use
            EquipPlayerItem(reinterpret_cast<CBasePlayerPawn*>(pInput->pActivator), pEntity);
            return;
        }

        const auto pPlayer = reinterpret_cast<CCSPlayerPawn*>(pInput->pActivator);

        const auto pController = pPlayer->GetController<CCSPlayerController*>();
        if (!pController)
            return;

        const auto data = s_WeaponMap.find(pszWeapon);
        if (data == s_WeaponMap.end())
            return;

        const auto flags = pEntity->GetSpawnFlags();

        if (flags & CGamePlayerEquip::SF_PLAYEREQUIP_STRIPFIRST)
        {
            pPlayer->RemoveAllItems(true);
        }
        else if (flags & CGamePlayerEquip::SF_PLAYEREQUIP_ONLYSTRIPSAME)
        {
            // 手雷就不收
            if (data->second.m_eSlot != GearSlot_t::GEAR_SLOT_GRENADES && data->second.m_eSlot != GearSlot_t::GEAR_SLOT_INVALID)
            {
                CBaseWeapon* pWeapon = nullptr;
                while ((pWeapon = pPlayer->GetWeaponBySlot(data->second.m_eSlot)) != nullptr)
                {
                    pPlayer->RemovePlayerItem(pWeapon);
                }
            }
        }

        if (data->second.m_eSlot != GearSlot_t::GEAR_SLOT_INVALID)
        {
            const auto team = pPlayer->GetTeam();
            if (data->second.m_iTeamNum != team)
            {
                pPlayer->TransientChangeTeam(data->second.m_iTeamNum);
                pPlayer->GiveNamedItem(pszWeapon);
                pPlayer->TransientChangeTeam(team);
            }
            else
            {
                pPlayer->GiveNamedItem(pszWeapon);
            }
        }
        else
        {
            pPlayer->GiveNamedItem(pszWeapon);
        }
    }
}

// NOTE game_player_equip does not work in CStrike 2
// Now We impl that manually
// 这里全部照着CSGO的实现写!
static bool EquipPlayerItem(CBasePlayerPawn* pPlayer, CGamePlayerEquip* pEntity)
{
    const auto pController = pPlayer->GetController<CCSPlayerController*>();
    if (!pController)
        return false;

    const auto flags = pEntity->GetSpawnFlags();

    if (flags & CGamePlayerEquip::SF_PLAYEREQUIP_STRIPFIRST)
    {
        pPlayer->RemoveAllItems(true);
    }

    const auto pszHammerId = pEntity->GetHammerId();
    if (!pszHammerId || !pszHammerId[0])
        return false;

    const auto key = MurmurHash2Lowercase(pszHammerId, MURMURHASH_SEED);
    const auto map = s_gamePlayerEquipMap.find(key);
    if (map == s_gamePlayerEquipMap.end())
    {
        // WARN("CGamePlayerEquip (%d.%s) is missing weapons!\n", pEntity->GetEntityIndex(), pName);
        return false;
    }

    const auto team = pPlayer->GetTeam();

    for (const auto& name : map->second)
    {
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
                pPrimaryWeapon->AcceptInput("SetAmmoAmount", nullptr, nullptr, pri, 0);
                pPrimaryWeapon->AcceptInput("SetReserveAmmoAmount", nullptr, nullptr, sec, 0);
            }
            if (pSecondaryWeapon)
            {
                pSecondaryWeapon->AcceptInput("SetAmmoAmount", nullptr, nullptr, pri, 0);
                pSecondaryWeapon->AcceptInput("SetReserveAmmoAmount", nullptr, nullptr, sec, 0);
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
                    // 手雷就不重复发了
                    if (data->second.m_eSlot == GearSlot_t::GEAR_SLOT_GRENADES)
                    {
                        auto alreadyHas = false;
                        if (const auto m_hMyWeapons = pPlayer->m_pWeaponServices()->m_hMyWeapons())
                        {
                            auto index = m_hMyWeapons->Count() - 1;
                            for (; index >= 0; index--)
                            {
                                if (const auto hWeapon = m_hMyWeapons->Element(index); hWeapon.IsValid())
                                {
                                    if (const auto pWeapon = reinterpret_cast<CBaseWeapon*>(g_pGameEntitySystem->FindEntityByEHandle(hWeapon)))
                                    {
                                        if (strcasecmp(pWeapon->GetClassname(), name.c_str()) == 0)
                                        {
                                            alreadyHas = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (alreadyHas)
                        {
                            continue;
                        }
                    }
                    // 其他的还是一律先strip
                    else
                    {
                        CBaseWeapon* pWeapon = nullptr;
                        while ((pWeapon = pPlayer->GetWeaponBySlot(data->second.m_eSlot)) != nullptr)
                        {
                            pPlayer->RemovePlayerItem(pWeapon);
                        }
                    }
                }

                if (data->second.m_iTeamNum != TEAM_UNASSIGNED)
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

    return true;
}

#endif

#ifdef FIX_WEAPON_ECON
class LocalEntityListener : public IEntityListener
{
    void OnEntitySpawned(CBaseEntity* pEntity) override
    {
        if (strncasecmp(pEntity->GetClassname(), "weapon_", 7) == 0)
        {
            const auto pWeapon  = reinterpret_cast<CEconEntity*>(pEntity);
            const auto pManager = pWeapon->GetAttributeManager();
            if (!pManager)
                return;

            const auto pItem = pManager->GetItem();
            if (!pItem)
                return;

            if (pItem->m_bInitialized())
                return;

            const auto data = s_WeaponMap.find(pEntity->GetClassname());
            if (data == s_WeaponMap.end())
                FatalError("Invalid Weapon Classname: %s", pEntity->GetClassname());

            if (data->second.m_iItemDefinitionIndex == 0)
                return;

            static auto pHammerId = *entList->AllocPooledString("this is map created weapon!");
            static auto nOffset   = schemas::GetOffset("CBaseEntity", "m_sUniqueHammerID");

            const auto m_sUniqueHammerID = reinterpret_cast<CUtlString*>(reinterpret_cast<uintptr_t>(pEntity) + nOffset);
            m_sUniqueHammerID->Set(pHammerId);

            pItem->m_bInitialized(true);
            pItem->m_iItemDefinitionIndex(data->second.m_iItemDefinitionIndex);

            WARN("Fix Weapon %d.%s CEconEntity!", pEntity->GetEntityIndex(), pEntity->GetClassname());
        }
    }

    void OnEntityCreated(CBaseEntity* pEntity) override {}
    void OnEntityDeleted(CBaseEntity* pEntity) override {}

    void OnEntityFollowed(CBaseEntity* pEntity, CBaseEntity* pOwner) override {}
} static s_EntityListener;

#endif

static void PatchGiveNamedItemLimit()
{
    auto address = g_pGameData->GetAddress<uint8_t*>("CCSPlayer_ItemServices::GiveNamedItem");
    if (!address)
        return;

    ZydisDecoder decoder{};
    if (ZYAN_FAILED(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
    {
        FatalError("Failed to initialize decoder");
    }

    ZydisDecodedInstruction instr{};
    ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

    bool found_test_eax = false;

    for (auto count = 0; count < 40; count++)
    {
        if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&decoder, address, ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, operands)))
        {
            address += instr.length;
            continue;
        }

        if (found_test_eax)
        {
#ifdef PLATFORM_WINDOWS
            if (instr.mnemonic == ZYDIS_MNEMONIC_JZ)
            {
                SetMemoryAccess(address, instr.length, g_nReadWriteExecuteAccess);
                auto op = &operands[0];
                if (instr.opcode_map == ZYDIS_OPCODE_MAP_DEFAULT)
                {
                    *address = 0xEB;
                }
                else
                {
                    std::array<uint8_t, 5> bytes;
                    bytes[0]           = 0xE9;
                    int32_t new_offset = static_cast<int32_t>(op->imm.value.s) + 1;
                    memcpy(&bytes[1], &new_offset, sizeof(new_offset));
                    memcpy(address, bytes.data(), bytes.size());

                    *(address + 5) = 0x90; // NOP opcode
                }
                FLOG("Successfully patched GiveNamedItem limit @ server+0x%llx", reinterpret_cast<uintptr_t>(address) - modules::server->Base());
                SetMemoryAccess(address, instr.length, g_nReadExecuteAccess);

                return;
            }
#else
            if (instr.mnemonic == ZYDIS_MNEMONIC_JNZ)
            {
                SetMemoryAccess(address, instr.length, g_nReadWriteExecuteAccess);

                memset(address, 0x90, instr.length);
                FLOG("Successfully patched GiveNamedItem limit @ server+0x%llx", reinterpret_cast<uintptr_t>(address) - modules::server->Base());
                SetMemoryAccess(address, instr.length, g_nReadExecuteAccess);
                return;
            }
#endif
        }
        else if (instr.mnemonic == ZYDIS_MNEMONIC_TEST && instr.operand_count_visible == 2 && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER && operands[0].reg.value == ZYDIS_REGISTER_EAX && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER && operands[1].reg.value == ZYDIS_REGISTER_EAX)
        {
            found_test_eax = true;
        }

        address += instr.length;
    }

    WARN("Failed to patch GiveNamedItemLimit, found_test_eax: %s", found_test_eax ? "true" : "false");
}

void InstallGiveNamedItemHooks()
{
    InstallMemberDetourAutoSig(CCSPlayer_ItemServices, GiveNamedItem);
    InstallMemberDetourAutoSig(CCSPlayer_ItemServices, GiveGlove);
    InstallMemberDetourAutoSig(CCSPlayer_ItemServices, CanAcquire);

    InstallMemberDetourAutoSig(CBasePlayerPawn, FindMatchingWeaponsForTeamLoadout);

    PatchGiveNamedItemLimit();

#ifdef FIX_PLAYER_EQUIP_MANUALLY

    InstallVirtualHookManualWithVTableAuto(CGamePlayerEquip, Precache, server, "CBaseEntity::Precache");
    InstallMemberDetourAutoSig(CGamePlayerEquip, InputTriggerForAllPlayers);
    InstallMemberDetourAutoSig(CGamePlayerEquip, InputTriggerForActivatedPlayer);

    InstallVirtualHookManualWithVTableAuto(CGamePlayerEquip, Use, server, "CBaseEntity::Use");
    InstallVirtualHookManualWithVTableAuto(CGamePlayerEquip, Touch, server, "CBaseEntity::Touch");

    g_pHookManager->Hook_GameShutdown(HookType_Post, [] {
        for (auto& val : s_gamePlayerEquipMap | std::views::values)
        {
            val.clear();
        }
        s_gamePlayerEquipMap.clear();
    });
#endif

#ifdef FIX_WEAPON_ECON
    entList->AddListenerEntity(&s_EntityListener);
#endif
}