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

#ifndef CSTRIKE_TYPE_CECON_ITEM_DEFINITION_H
#define CSTRIKE_TYPE_CECON_ITEM_DEFINITION_H

#include "cstrike/type/CUtlVector.h"

#include <cstdint>

struct CItemSelectionCriteria;
struct CWeaponEconData;
class KeyValues;

#pragma pack(push, 1)
struct CEconItemDefinition
{
    enum CapabilityFlags : uint32_t
    {
        Capability_Sticker  = 1048576,
        Capability_KeyChain = 8388608,
    };

    void*    vtable;      // 0x0
    void*    m_pKVItem;   // 0x8
    uint16_t m_nDefIndex; // 0x10
private:
    char pad_12[0x1e];

public:
    bool m_bEnabled; // 0x30
private:
    char pad_31[0x7];

public:
    char*   m_szPrefab;                // 0x38
    uint8_t m_unMinItemLevel;          // 0x40
    uint8_t m_unMaxItemLevel;          // 0x41
    uint8_t m_nItemRarity;             // 0x42
    uint8_t m_nItemQuality;            // 0x43
    uint8_t m_nForcedItemQuality;      // 0x44
    uint8_t m_nDefaultDropItemQuality; // 0x45
    uint8_t m_nDefaultDropQuantity;    // 0x46
private:
    char pad_47[0x1];

public:
    CUtlVector<void*> m_vecStaticAttributes; // 0x48
    uint8_t           m_nPopularitySeed;     // 0x60
private:
    char pad_61[0x7];

public:
    void* m_pPortraitsKV; // 0x68
    // Metadata: utf8*
    char* m_pszItemBaseName; // 0x70
    bool  m_bProperName;     // 0x78
private:
    char pad_79[0x7];

public:
    // Metadata: utf8*
    char* m_pszItemTypeName; // 0x80
private:
    char pad_88[0x8];

public:
    // Metadata: utf8*
    char*    m_pszItemDesc;          // 0x90
    uint32_t m_iExpirationTimeStamp; // 0x98
    uint32_t m_iCreationTimeStamp;   // 0x9c
    // Metadata: utf8*
    char* m_pszInventoryModel; // 0xa0
    // Metadata: utf8*
    char* m_pszInventoryImage; // 0xa8
private:
    char pad_b0[0x18];

public:
    int m_iInventoryImagePosition[2]; // 0xc8
    int m_iInventoryImageSize[2];     // 0xd0
    // Metadata: utf8*
    char* m_pszBaseDisplayModel; // 0xd8
    bool  m_bLoadOnDemand;       // 0xe0
private:
    char pad_e1[0x1];

public:
    bool m_bHideBodyGroupsDeployedOnly; // 0xe2
private:
    char pad_e3[0x5];

public:
    // Metadata: utf8*
    char* m_pszWorldDisplayModel; // 0xe8
    // Metadata: utf8*
    char*    m_pszWorldExtraWearableModel; // 0xf0
    uint32_t m_iStickerSlots;              // 0xf8
private:
    char pad_fc[0x1c];

public:
    // Metadata: utf8*
    char* m_pszIconDefaultImage;  // 0x118
    bool  m_bAttachToHands;       // 0x120
    bool  m_bAttachToHandsVMOnly; // 0x121
    bool  m_bFlipViewModel;       // 0x122
    bool  m_bActAsWearable;       // 0x123
private:
    char pad_124[0x14];

public:
    uint32_t m_iItemType; // 0x138
private:
    char pad_13c[0x14];

public:
    // Metadata: utf8*
    char* m_pszBrassModelOverride; // 0x150
    // Metadata: utf8*
    char* m_pszZoomInSoundPath; // 0x158
    // Metadata: utf8*
    char* m_pszZoomOutSoundPath; // 0x160
private:
    char pad_168[0x10];

public:
    CapabilityFlags   m_nItemCapabilityFlags; // 0x178
    CUtlVector<void*> m_vecPaintData;         // 0x17c
private:
    char pad_194[0x4];

public:
    // Metadata: utf8*
    char* m_pszParticleFile; // 0x198
    // Metadata: utf8*
    char* m_pszParticleSnapshotFile; // 0x1a0
private:
    char pad_1a8[0x40];

public:
    // Metadata: utf8*
    char* m_pszItemClassname; // 0x1e8
    // Metadata: utf8*
    char* m_pszItemLogClassname; // 0x1f0
    // Metadata: utf8*
    char* m_pszItemIconClassname; // 0x1f8
    // Metadata: utf8*
    char* m_pszDefinitionName;       // 0x200
    bool  m_bHidden;                 // 0x208
    bool  m_bShouldShowInArmory;     // 0x209
    bool  m_bBaseItem;               // 0x20a
    bool  m_bFlexibleLoadoutDefault; // 0x20b
    bool  m_bImported;               // 0x20c
    bool  m_bOnePerAccountCDKEY;     // 0x20d
private:
    char pad_20e[0xa];

public:
    // Metadata: utf8*
    char* m_pszArmoryDesc; // 0x218
private:
    char pad_220[0x8];

public:
    // Metadata: utf8*
    char* m_pszArmoryRemap; // 0x228
    // Metadata: utf8*
    char* m_pszStoreRemap; // 0x230
    // Metadata: utf8*
    char* m_pszClassToken; // 0x238
    // Metadata: utf8*
    char*    m_pszSlotToken; // 0x240
    uint32_t m_iDropType;    // 0x248
private:
    char pad_24c[0x4];

public:
    // Metadata: utf8*
    char*    m_pszHolidayRestriction; // 0x250
    uint32_t m_iSubType;              // 0x258
    bool     m_bAllowedInThisMatch;   // 0x25c
private:
    char pad_25d[0x3];

public:
    CItemSelectionCriteria* m_pProxyCriteria;            // 0x260
    uint32_t                m_unEquipRegionMask;         // 0x268
    uint32_t                m_unEquipRegionConflictMask; // 0x26c
private:
    char pad_270[0x50];

public:
    bool m_bPublicItem;             // 0x2c0
    bool m_bIgnoreInCollectionView; // 0x2c1
private:
    char pad_2c2[0x6];

public:
    CWeaponEconData* m_pEconData; // 0x2c8
    // Metadata: utf8*
    char* m_pszItemType;         // 0x2d0
    int   m_iDefaultLoadoutSlot; // 0x2d8
private:
    char pad_2dc[0x4];

public:
    int   m_iItemGearSlotPosition;    // 0x2e0
    int   m_iAnimationSlot;           // 0x2e4
    char* m_pszPlayerDisplayModel[4]; // 0x2e8
    int   m_vbClassUsability;         // 0x308
    int   m_iLoadoutSlots[4];         // 0x30c
    bool  m_bIsSupplyCrate : 1;       // 0x31c
    bool  m_bItemSharesEquipSlot : 1; // 0x31c
private:
    bool pad_bitfield_31c_2 : 6;

public:
private:
    char pad_31d[0x2b];

public:
    // Metadata: utf8*
    char* m_pszVoicePrefix; // 0x348
    // Metadata: utf8*
    char* m_pszDefaultCheer; // 0x350
    // Metadata: utf8*
    char* m_pszDefaultDefeat; // 0x358
    // Metadata: utf8*
    char* m_pszAnimClass; // 0x360
}; // Size: 0x368
#pragma pack(pop)

#endif