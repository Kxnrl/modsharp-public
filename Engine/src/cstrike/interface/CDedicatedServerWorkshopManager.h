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

#ifndef CSTRIKE_TYPE_DSWORKSHOPMGR_H
#define CSTRIKE_TYPE_DSWORKSHOPMGR_H

#include "cstrike/type/CUtlMap.h"
#include "cstrike/type/CUtlVector.h"

#include <cstddef>

struct WorkshopMapInfo_t;
struct RequestedMap_t;

struct WorkshopMap_t
{
    const char* m_pName;
    uint64_t    m_nPublishFileId;
};

class CDedicatedServerWorkshopManager
{
    void* vtable; // 0x0
public:
    // Metadata: utf8*
    char* m_pszGameSystemName; // 0x8
private:
    void* m_pUGCAddonPathResolver; // 0x10
private:
    struct CSteamWorksCallback
    {
        void*    vtable;           // 0x0
        uint32_t m_nCallbackFlags; // 0x8
        uint32_t m_iCallback;      // 0xc
        void*    m_pObj;           // 0x10
        void*    m_Func;           // 0x18
    }; // Size: 0x20

    char                pad_18[0x8];
    CSteamWorksCallback m_cbDownloadItemResult; // 0x20
    char                pad_40[0x8];
    CSteamWorksCallback m_cbSteamServerConnected; // 0x48
    char                pad_68[0x8];

public:
    CUtlOrderedMap<uint64_t, RequestedMap_t*>    m_requestedMaps;                    // 0x70
    CUtlOrderedMap<uint64_t, WorkshopMapInfo_t*> m_mapLoadedWorkshopMaps;            // 0x98
    CUtlOrderedMap<uint64_t, bool>               m_mapStatus;                        // 0xc0
    uint64_t                                     m_nRequestedSharedFileId;           // 0xe8
    uint64_t                                     m_nRequestedCollectionSharedFileId; // 0xf0
    bool                                         m_bInitialized;                     // 0xf8

    bool AddWorkshopMap(uint64_t sharedFileId, const char* mapName, const char* path);
    bool WorkshopMapExists(uint64_t sharedFileId) const;
    bool RemoveWorkshopMap(uint64_t sharedFileId);
    void ListWorkshopMaps(CUtlVector<WorkshopMap_t>* list);
};

#ifdef PLATFORM_WINDOWS
static_assert(offsetof(CDedicatedServerWorkshopManager, m_mapLoadedWorkshopMaps) == 0x98);
static_assert(sizeof(CDedicatedServerWorkshopManager) == 0x100);
#endif

#endif