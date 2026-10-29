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

#ifndef CSTRIKE_TYPE_RESOURCE_SYSTEM_H
#define CSTRIKE_TYPE_RESOURCE_SYSTEM_H

#include <climits>
#include <cstddef>
#include <cstdint>

using ResourceBlockId_t = uint32_t;

// 一般情况下用这个就好
inline constexpr ResourceBlockId_t g_ResourceBlockId_Data = 0x41544144;
inline constexpr ResourceBlockId_t g_ResourceBlockId_Flc  = 0x49434C46;

template <typename T>
struct CResourceArrayBase
{
    int32_t  m_nOffset;
    uint32_t m_nCount;
};

template <typename T>
struct CResourcePointerBase
{
    int32_t m_nOffset;
};

struct CResourceBlockInfo
{
    ResourceBlockId_t m_nBlockType{};
    char*             m_pBlockData{};
    int               m_nSize{};
};

struct ResourceBlockEntry_t
{
    ResourceBlockId_t          m_nBlockType;
    CResourcePointerBase<void> m_pBlockData;
    uint32_t                   m_nBlockSize;
};

struct ResourceFileHeader_t
{
    uint32_t                                 m_nNonStreamingSize;
    uint16_t                                 m_nHeaderVersion;
    uint16_t                                 m_nResourceVersion;
    CResourceArrayBase<ResourceBlockEntry_t> m_ResourceBlocks;
};

static bool Resource_FindBlockInfo(const void* buffer, size_t bufferSize, ResourceBlockId_t block_id, CResourceBlockInfo& out_block_info)
{
    if (!buffer || bufferSize < sizeof(ResourceFileHeader_t))
    {
        return false;
    }

    const auto* header = static_cast<const ResourceFileHeader_t*>(buffer);
    if (header->m_nHeaderVersion != 12 || header->m_ResourceBlocks.m_nCount == 0)
    {
        return false;
    }

    const auto* base = static_cast<const char*>(buffer);

    const auto entriesPos = (reinterpret_cast<const char*>(&header->m_ResourceBlocks.m_nOffset) - base) + static_cast<int64_t>(header->m_ResourceBlocks.m_nOffset);
    const auto entriesEnd = static_cast<uint64_t>(entriesPos) + static_cast<uint64_t>(header->m_ResourceBlocks.m_nCount) * sizeof(ResourceBlockEntry_t);
    if (entriesPos < 0 || entriesEnd > bufferSize)
    {
        return false;
    }

    const auto* entries = reinterpret_cast<const ResourceBlockEntry_t*>(base + entriesPos);

    for (uint32_t i = 0; i < header->m_ResourceBlocks.m_nCount; ++i)
    {
        const ResourceBlockEntry_t& entry = entries[i];

        if (entry.m_nBlockType != block_id)
        {
            continue;
        }

        if (entry.m_nBlockSize > static_cast<uint32_t>(INT_MAX))
        {
            return false;
        }

        const char* pBlockData = nullptr;

        if (entry.m_pBlockData.m_nOffset != 0)
        {
            const auto dataPos = (reinterpret_cast<const char*>(&entry.m_pBlockData) - base) + static_cast<int64_t>(entry.m_pBlockData.m_nOffset);
            if (dataPos < 0 || static_cast<uint64_t>(dataPos) + entry.m_nBlockSize > bufferSize)
            {
                return false;
            }

            pBlockData = base + dataPos;
        }
        else if (entry.m_nBlockSize != 0)
        {
            return false;
        }

        out_block_info.m_nBlockType = entry.m_nBlockType;
        out_block_info.m_nSize      = static_cast<int>(entry.m_nBlockSize);
        out_block_info.m_pBlockData = const_cast<char*>(pBlockData);

        return true;
    }

    return false;
}
#endif
