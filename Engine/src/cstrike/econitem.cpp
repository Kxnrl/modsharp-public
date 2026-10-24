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
#include "consts.h"
#include "module.h"
#include "strtool.h"

#include "bridge/adapter.h"
#include "bridge/natives/EconItemSchemaNative.h"

#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/type/CEconItemDefinition.h"
#include "cstrike/type/CEconItemView.h"
#include "cstrike/type/CPaintKit.h"
#include "cstrike/type/CUtlMap.h"
#include "cstrike/type/CUtlVector.h"

static CUtlVector<void*>      g_vecEconItemDefinitions;
static CUtlVector<CPaintKit*> g_vecPaintKit;

static std::unordered_map<int, bool> s_mapPaintKitRequiredLegacyModel;

static void InitItemDefinitions()
{
    const CAddress econ_item_schema           = address::server::GetEconItemSchema();
    const auto     sorted_item_definition_map = econ_item_schema.Offset(8).Dereference(1).Offset(0x120).As<CUtlMap<int32_t, CEconItemDefinition*, uint32_t>*>();

    for (auto it = 0u; it < sorted_item_definition_map->Count(); ++it)
    {
        const auto definition = sorted_item_definition_map->Element(it);
        if (!definition) [[unlikely]]
            continue;

        g_vecEconItemDefinitions.AddToTail(definition);
    }
}

static void InitPaintKit()
{
    const CAddress econ_item_schema = address::server::GetEconItemSchema();
    const auto     paint_kit_map    = econ_item_schema.Offset(8).Dereference(1).Offset(0x298).As<CUtlMap<int32_t, CPaintKit*, uint32_t>*>();

    for (auto it = 0u; it < paint_kit_map->Count(); ++it)
    {
        auto paint_kit = paint_kit_map->Element(it);
        if (!paint_kit) [[unlikely]]
            continue;

        g_vecPaintKit.AddToTail(paint_kit);

        const auto legacy = CAddress(paint_kit).Offset(174).Get<bool>();
        s_mapPaintKitRequiredLegacyModel.emplace(paint_kit->m_nIndex, legacy);
    }
}

static bool IsUsingLegacyModel(int index)
{
    const auto kit = s_mapPaintKitRequiredLegacyModel.find(index);
    if (kit == s_mapPaintKitRequiredLegacyModel.end())
        return false;

    return kit->second;
}

void InitEconItemSchema()
{
    InitItemDefinitions();
    InitPaintKit();
}

static CUtlVector<void*>* GetEconItemDefinitions()
{
    return &g_vecEconItemDefinitions;
}

static CUtlVector<CPaintKit*>* GetPaintKits()
{
    return &g_vecPaintKit;
}

static bool UpdateItemAttributes(CBaseWeapon* pEntity,
                                 uint32_t     accountId,
                                 const char*  pNameTag,
                                 int          nPaint,
                                 int          nPattern,
                                 float        flWear,
                                 int          nSticker0,
                                 float        flSticker0,
                                 int          nSticker1,
                                 float        flSticker1,
                                 int          nSticker2,
                                 float        flSticker2,
                                 int          nSticker3,
                                 float        flSticker3)
{
    if (!pEntity->IsWeapon())
        return false;

    if (nPaint < 0)
        return false;

    const auto pManager = pEntity->GetAttributeManager();
    if (!pManager)
        return false;

    const auto pItem = pManager->GetItem();
    if (!pItem)
        return false;

    const auto attributes = pItem->m_NetworkedDynamicAttributes();

    // paint
    attributes->SetOrAddAttributeValueByName("set item texture prefab", static_cast<float>(nPaint));

    // wear
    attributes->SetOrAddAttributeValueByName("set item texture wear", flWear);

    // pattern
    attributes->SetOrAddAttributeValueByName("set item texture seed", static_cast<float>(nPattern));

    // StatTrak
    // attributes->SetOrAddAttributeValueByName("kill eater", *reinterpret_cast<float*>(nStatTrak));
    // attributes->SetOrAddAttributeValueByName("kill eater score type", 0);

#define STICKER(i)                                                                                                               \
    do                                                                                                                           \
    {                                                                                                                            \
        if (nSticker##i)                                                                                                         \
        {                                                                                                                        \
            attributes->SetOrAddAttributeValueByName(FString("sticker slot %d id", i), *reinterpret_cast<float*>(&nSticker##i)); \
            attributes->SetOrAddAttributeValueByName(FString("sticker slot %d wear", i), (flSticker##i) + 0.0001f);              \
        }                                                                                                                        \
    } while (0)

    // stickers
    STICKER(0);
    STICKER(1);
    STICKER(2);
    STICKER(3);

    // name tag
    if (pNameTag)
    {
        pItem->SetCustomName(pNameTag);
    }

    static int iIDHigh = 16384;
    pItem->m_iItemIDHigh(iIDHigh++);
    pItem->m_iItemIDLow(-1);
    pItem->m_iAccountID(accountId);

    const auto nSlot = pEntity->GetSlot();

    if ((nSlot == GearSlot_t::GEAR_SLOT_RIFLE || nSlot == GearSlot_t::GEAR_SLOT_PISTOL) && IsUsingLegacyModel(nPaint))
    {
        pEntity->SetBodyGroupByName("body", 1);
    }

    return true;
}

namespace natives::econitemschema
{
void Init()
{
    bridge::CreateNative("Econ.GetItemDefinitions", reinterpret_cast<void*>(GetEconItemDefinitions));
    bridge::CreateNative("Econ.GetPaintKits", reinterpret_cast<void*>(GetPaintKits));

    bridge::CreateNative("Econ.UpdateItemAttributes", reinterpret_cast<void*>(UpdateItemAttributes));
}
} // namespace natives::econitemschema
