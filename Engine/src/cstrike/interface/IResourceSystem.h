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

#ifndef CSTRIKE_INTERFACE_RESOURCESYSTEM_H
#define CSTRIKE_INTERFACE_RESOURCESYSTEM_H

#include "murmurhash.h"
#include "cstrike/type/CBufferString.h"

#include <cstdint>

enum ResourceStatus_t
{
    RESOURCE_STATUS_UNKNOWN = 0,
    RESOURCE_STATUS_KNOWN_BUT_NOT_RESIDENT,
    RESOURCE_STATUS_PARTIALLY_RESIDENT,
    RESOURCE_STATUS_RESIDENT,
};

using ResourceId_t   = uint64_t;
using ResourceType_t = uint64_t;


struct CResourceNameTyped
{
    CFixedBufferString<200> m_szResourcePath;
    ResourceId_t            m_nResourceId;   // 0xd0
    ResourceType_t          m_nResourceType; // 0xd8

    static CResourceNameTyped Create(std::string_view path)
    {
        char   normalized[200] = {};
        size_t len             = 0;

        for (char c : path)
        {
            if (len >= 199) break;

            if (c == '\\')
            {
                normalized[len++] = '/';
            }
            else if (c >= 'A' && c <= 'Z')
            {
                normalized[len++] = c + ('a' - 'A');
            }
            else
            {
                normalized[len++] = c;
            }
        }
        normalized[len] = '\0';

        std::string_view normalized_view(normalized, len);
        if (len == 0 || normalized[0] == '/')
            return {
                .m_szResourcePath = {"", false},
                .m_nResourceId    = 0,
                .m_nResourceType  = 0
            };

        size_t         slash_pos = normalized_view.find_last_of('/');
        size_t         dot_pos   = normalized_view.find_last_of('.');
        ResourceType_t type_hash = 0;

        if (dot_pos != std::string_view::npos && (slash_pos == std::string_view::npos || dot_pos > slash_pos) && dot_pos + 1 < len)
        {
            size_t ext_len = 0;
            for (size_t i = dot_pos + 1; i < len; ++i)
            {
                if (normalized[i] == '_')
                {
                    break;
                }

                if (ext_len >= 8)
                {
                    type_hash = 0;
                    break;
                }
                uint64_t byte_val = static_cast<uint8_t>(normalized[i]);
                type_hash |= (byte_val << (ext_len * 8));
                ext_len++;
            }
        }

        return {
            .m_szResourcePath = {normalized, false},
            .m_nResourceId    = MurmurHash64B(normalized, static_cast<int>(len), MURMURHASH_RESOURCE_SEED),
            .m_nResourceType  = (type_hash)
        };
    }
};
static_assert(sizeof(CResourceNameTyped) == 0xe0, "CResourceNameTyped size mismatch");

class IResourceSystem
{
public:
    ResourceStatus_t GetResourceStatus(const CResourceNameTyped& resource);
};

#endif
