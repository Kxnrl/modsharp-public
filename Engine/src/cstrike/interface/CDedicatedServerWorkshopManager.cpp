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

#include "logging.h"
#include "steamproxy.h"

#include "cstrike/interface/CDedicatedServerWorkshopManager.h"

#include "cstrike/type/CBufferString.h"
#include "cstrike/type/CUtlMap.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/CUtlVector.h"

#include <steamworks/isteamugc.h>

struct WorkshopMapInfo_t
{
    CFixedBufferString<200> m_szFilePath;
    CUtlString              m_szMapName;
    WorkshopMapInfo_t() = default;
    WorkshopMapInfo_t(const char* path, const char* mapName)
    {
        m_szFilePath = path;
        m_szMapName  = mapName;
    }
};

struct RequestedMap_t
{
    int32_t           m_nDownloadStatus;
    SteamUGCDetails_t m_details;
};

bool CDedicatedServerWorkshopManager::AddWorkshopMap(uint64_t sharedFileId, const char* mapName, const char* path)
{
    m_mapLoadedWorkshopMaps.EnsureCapacity(1);

    if (m_mapLoadedWorkshopMaps.Find(sharedFileId) != m_mapLoadedWorkshopMaps.InvalidIndex())
    {
        FERROR("Workshop map \"%s\" with id %lld already exists", mapName, sharedFileId);
        return false;
    }

    const auto index = m_mapLoadedWorkshopMaps.Insert(sharedFileId, new WorkshopMapInfo_t{path, mapName});
    return index != m_mapLoadedWorkshopMaps.InvalidIndex();
}

bool CDedicatedServerWorkshopManager::WorkshopMapExists(uint64_t sharedFileId) const
{
    return m_mapLoadedWorkshopMaps.Find(sharedFileId) != m_mapLoadedWorkshopMaps.InvalidIndex();
}

bool CDedicatedServerWorkshopManager::RemoveWorkshopMap(uint64_t sharedFileId)
{
    return m_mapLoadedWorkshopMaps.Remove(sharedFileId);
}

void CDedicatedServerWorkshopManager::ListWorkshopMaps(CUtlVector<WorkshopMap_t>* list)
{
    const auto count = static_cast<int32_t>(m_mapLoadedWorkshopMaps.Count());
    for (auto it = 0; it < count; it++)
    {
        const auto  fileId  = m_mapLoadedWorkshopMaps.Key(it);
        const auto& element = m_mapLoadedWorkshopMaps.Element(it);

        list->AddToTail(WorkshopMap_t{.m_pName = element->m_szMapName.Get(), .m_nPublishFileId = fileId});
    }
}