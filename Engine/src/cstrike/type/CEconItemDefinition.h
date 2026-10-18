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

#ifndef CSTRIKE_TYPE_CECON_ITEM_DEFINITION_H
#define CSTRIKE_TYPE_CECON_ITEM_DEFINITION_H

#include "definitions.h"

#include "cstrike/type/CUtlString.h"
#include "cstrike/type/CUtlSymbolLarge.h"
#include "cstrike/type/CUtlVector.h"

#include <cstdint>

class KeyValues;

#pragma pack(push, 1)
struct StaticAttrib_t
{
    uint16_t m_iDefIndex; // 0x0
private:
    char pad_2[0x6];

public:
    uint64_t m_Value;              // 0x8
    bool     m_bForceGcToGenerate; // 0x10
private:
    char pad_11[0x7];
}; // Size: 0x18

struct WeaponPaintableMaterial_t
{
    char  m_szName[0x80];         // 0x0
    char  m_szOrigMat[0x80];      // 0x80
    char  m_szFolderName[0x80];   // 0x100
    int   m_nViewmodelDim;        // 0x180
    int   m_nWorldDim;            // 0x184
    float m_flWeaponLength;       // 0x188
    float m_flUVScale;            // 0x18c
    bool  m_bBaseTextureOverride; // 0x190
    bool  m_bMirrorPattern;       // 0x191
private:
    char pad_192[0x2];
}; // Size: 0x194

struct CEconItemDefinition
{
    enum CapabilityFlags : uint32_t
    {
        Capability_Sticker  = 1048576,
        Capability_KeyChain = 8388608,
    };

    void*      vtable;      // 0x0
    KeyValues* m_pKVItem;   // 0x8
    uint16_t   m_nDefIndex; // 0x10
private:
    char pad_12[0x6];

public:
    CUtlVector<uint16_t> m_vecAssociatedItems; // 0x18
    bool                 m_bEnabled;           // 0x30
private:
    char pad_31[0xf];

public:
    uint8_t m_unMinIlevel;          // 0x40
    uint8_t m_unMaxIlevel;          // 0x41
    uint8_t m_nItemRarity;          // 0x42
    uint8_t m_nItemQuality;         // 0x43
    uint8_t m_nForcedItemQuality;   // 0x44
    uint8_t m_nDefaultDropQuality;  // 0x45
    uint8_t m_nDefaultDropQuantity; // 0x46
private:
    char pad_47[0x1];

public:
    CUtlVector<StaticAttrib_t> m_vecAttributes;   // 0x48
    uint8_t                    m_nPopularitySeed; // 0x60
private:
    char pad_61[0x7];

public:
    KeyValues*  m_pPortraits;  // 0x68
    const char* m_pszItemName; // 0x70
    bool        m_bPropername; // 0x78
private:
    char pad_79[0x7];

public:
    const char* m_pszItemTypeName; // 0x80
    uint32_t    m_unItemTypeID;    // 0x88
private:
    char pad_8c[0x4];

public:
    const char* m_pszItemDescription; // 0x90
    uint32_t    m_iExpirationDate;    // 0x98
    uint32_t    m_iCreationDate;      // 0x9c
    const char* m_pszModelInventory;  // 0xa0
    const char* m_pszImageInventory;  // 0xa8
private:
    char pad_b0[0x70];

public:
    CUtlVector<const char*> m_vecImageInventoryOverlay; // 0x120
    int                     m_iImageInventoryPos[2];    // 0x138
    int                     m_iImageInventorySize[2];   // 0x140
    CUtlString              m_pszModelPlayer;           // 0x148
    bool                    m_bLoadondemand;            // 0x150
private:
    char pad_151[0x1];

public:
    bool m_bHideBodygroupsDeployedOnly; // 0x152
private:
    char pad_153[0x5];

public:
    CUtlString  m_pszModelWorld;    // 0x158
    const char* m_pszExtraWearable; // 0x160
    uint32_t    m_iStickers;        // 0x168
    uint32_t    m_iKeychains;       // 0x16c
private:
    char pad_170[0x18];

public:
    const char* m_pszIconDefaultImage;  // 0x188
    bool        m_bAttachToHands;       // 0x190
    bool        m_bAttachToHandsVmOnly; // 0x191
    bool        m_bFlipViewmodel;       // 0x192
    bool        m_bActAsWearable;       // 0x193
private:
    char pad_194[0x24];

public:
    uint32_t m_iItemType; // 0x1b8
private:
    char pad_1bc[0x4];

public:
    const char* m_pszBrassEjectModel; // 0x1c0
    const char* m_pszZoomInSound;     // 0x1c8
    const char* m_pszZoomOutSound;    // 0x1d0
    void*       m_pTool;              // 0x1d8
private:
    char pad_1e0[0x8];

public:
    CapabilityFlags m_nCapabilities; // 0x1e8
private:
    char pad_1ec[0x4];

public:
    int32_t m_nSoundMaterial;        // 0x1f0
    bool    m_bDisableStyleSelector; // 0x1f4
private:
    char pad_1f5[0x3];

public:
    const char* m_pszParticleFile;     // 0x1f8
    const char* m_pszParticleSnapshot; // 0x200
    const char* m_pszLootListName;     // 0x208
private:
    char pad_210[0x18];

public:
    CUtlVector<WeaponPaintableMaterial_t> m_vecPaintData; // 0x228
private:
    char pad_240[0x8];

public:
    const char* m_pszItemClass;            // 0x248
    const char* m_pszItemLogname;          // 0x250
    const char* m_pszItemIconname;         // 0x258
    const char* m_pszName;                 // 0x260
    bool        m_bOwnerOnly;              // 0x268
    bool        m_bHidden;                 // 0x269
    bool        m_bShowInArmory;           // 0x26a
    bool        m_bBaseitem;               // 0x26b
    bool        m_bFlexibleLoadoutDefault; // 0x26c
    bool        m_bOnePerAccountCdkey;     // 0x26d
private:
    char pad_26e[0xa];

public:
    const char* m_pszArmoryDesc; // 0x278
private:
    char pad_280[0x8];

public:
    const char* m_pszArmoryRemap;  // 0x288
    const char* m_pszStoreRemap;   // 0x290
    const char* m_pszClassTokenId; // 0x298
    const char* m_pszSlotTokenId;  // 0x2a0
    int32_t     m_iDropType;       // 0x2a8
private:
    char pad_2ac[0x4];

public:
    const char* m_pszHolidayRestriction; // 0x2b0
    int32_t     m_iSubtype;              // 0x2b8
private:
    char pad_2bc[0xc];

public:
    uint32_t m_unEquipRegionMask;         // 0x2c8
    uint32_t m_unEquipRegionConflictMask; // 0x2cc
private:
    char pad_2d0[0x50];

public:
    bool m_bPublicItem;             // 0x320
    bool m_bIgnoreInCollectionView; // 0x321
private:
    char pad_322[0xe];

public:
    CUtlSymbolLarge m_pszFlexibleLoadoutCategory; // 0x330
    int32_t         m_iFlexibleLoadoutSlot;       // 0x338
    int32_t         m_iItemGearSlot;              // 0x33c
    int32_t         m_iItemGearSlotPosition;      // 0x340
    int32_t         m_iAnimSlot;                  // 0x344
    CUtlSymbolLarge m_pszModelPlayerPerClass[4];  // 0x348
    uint32_t        m_vbClassUsability;           // 0x368
    int32_t         m_iLoadoutSlots[4];           // 0x36c
    bool            m_bIsSupplyCrate : 1;         // 0x37c
    bool            m_bItemSharesEquipSlot : 1;   // 0x37c
private:
    bool pad_bitfield_37c_2 : 6;
    char pad_37d[0x2b];

public:
    const char* m_pszVoPrefix;      // 0x3a8
    const char* m_pszDefaultCheer;  // 0x3b0
    const char* m_pszDefaultDefeat; // 0x3b8
    const char* m_pszAnimClass;     // 0x3c0
}; // Size: 0x3c8
#pragma pack(pop)

static_assert(sizeof(StaticAttrib_t) == 0x18);
static_assert(sizeof(WeaponPaintableMaterial_t) == 0x194);
static_assert(sizeof(CEconItemDefinition) == 0x3C8);

#endif
