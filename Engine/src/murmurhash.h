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

#ifndef MS_ROOT_MURMURHASH_H
#define MS_ROOT_MURMURHASH_H

#include <cstdint>
#include <cstring>
#include <string>
#include <string_view>

static constexpr uint32_t MURMURHASH_SEED            = 0x31415926;
static constexpr uint32_t MURMURHASH_SOUNDEVENT_SEED = 0x53524332;
static constexpr uint32_t MURMURHASH_SOUNDSTACK_SEED = 0x50524748;
static constexpr uint32_t MURMURHASH_SEED_MODSHARP   = 0x11451419;

constexpr uint32_t MurmurHash2(std::string_view key, uint32_t seed)
{
    // 'm' and 'r' are mixing constants generated offline.
    // They're not really 'magic', they just happen to work well.

    constexpr uint32_t m = 0x5bd1e995;
    constexpr int      r = 24;

    // Initialize the hash to a 'random' value

    uint32_t h = seed ^ static_cast<uint32_t>(key.length());

    // Mix 4 bytes at a time into the hash

    const char* data = key.data();
    size_t      len  = key.length();
    size_t      i    = 0;

    while (len >= 4)
    {
        uint32_t k = static_cast<uint32_t>(static_cast<unsigned char>(data[i]))
                     | (static_cast<uint32_t>(static_cast<unsigned char>(data[i + 1])) << 8)
                     | (static_cast<uint32_t>(static_cast<unsigned char>(data[i + 2])) << 16)
                     | (static_cast<uint32_t>(static_cast<unsigned char>(data[i + 3])) << 24);

        k *= m;
        k ^= k >> r;
        k *= m;

        h *= m;
        h ^= k;

        i += 4;
        len -= 4;
    }

    // Handle the last few bytes of the input array

    switch (len)
    {
    case 3: h ^= static_cast<uint32_t>(static_cast<unsigned char>(data[i + 2])) << 16;
        [[fallthrough]];
    case 2: h ^= static_cast<uint32_t>(static_cast<unsigned char>(data[i + 1])) << 8;
        [[fallthrough]];
    case 1:
        h ^= static_cast<uint32_t>(static_cast<unsigned char>(data[i]));
        h *= m;
    };

    // Do a few final mixes of the hash to ensure the last few
    // bytes are well-incorporated.

    h ^= h >> 13;
    h *= m;
    h ^= h >> 15;

    return h;
}

constexpr uint32_t MurmurHash2Lowercase(std::string_view key, uint32_t seed)
{
    constexpr auto to_lower = [](char c) {
        return (c >= 'A' && c <= 'Z') ? (c - 'A' + 'a') : c;
    };

    // 'm' and 'r' are mixing constants generated offline.
    // They're not really 'magic', they just happen to work well.

    constexpr uint32_t m = 0x5bd1e995;
    constexpr int      r = 24;

    // Initialize the hash to a 'random' value

    uint32_t h = seed ^ static_cast<uint32_t>(key.length());

    // Mix 4 bytes at a time into the hash

    const char* data = key.data();
    size_t      len  = key.length();
    size_t      i    = 0;

    while (len >= 4)
    {
        uint32_t k = static_cast<uint32_t>(static_cast<unsigned char>(to_lower(data[i])))
                     | (static_cast<uint32_t>(static_cast<unsigned char>(to_lower(data[i + 1]))) << 8)
                     | (static_cast<uint32_t>(static_cast<unsigned char>(to_lower(data[i + 2]))) << 16)
                     | (static_cast<uint32_t>(static_cast<unsigned char>(to_lower(data[i + 3]))) << 24);

        k *= m;
        k ^= k >> r;
        k *= m;

        h *= m;
        h ^= k;

        i += 4;
        len -= 4;
    }

    // Handle the last few bytes of the input array

    switch (len)
    {
    case 3: h ^= static_cast<uint32_t>(static_cast<unsigned char>(to_lower(data[i + 2]))) << 16;
        [[fallthrough]];
    case 2: h ^= static_cast<uint32_t>(static_cast<unsigned char>(to_lower(data[i + 1]))) << 8;
        [[fallthrough]];
    case 1:
        h ^= static_cast<uint32_t>(static_cast<unsigned char>(to_lower(data[i])));
        h *= m;
    };

    // Do a few final mixes of the hash to ensure the last few
    // bytes are well-incorporated.

    h ^= h >> 13;
    h *= m;
    h ^= h >> 15;

    return h;
}

inline uint32_t MurmurHash2ConcatLowercase(std::string_view str1, std::string_view str2, uint32_t seed)
{
    auto len1 = str1.size();
    auto len2 = str2.size();
    auto total_len = len1 + 1 + len2;

    constexpr std::size_t MAX_STACK_BUFFER_SIZE = 128;

    if (total_len <= MAX_STACK_BUFFER_SIZE)
    {
        char buffer[MAX_STACK_BUFFER_SIZE];
        char* p = buffer;

        memcpy(p, str1.data(), len1);
        p += len1;
        *p++ = '.';
        memcpy(p, str2.data(), len2);
        return MurmurHash2Lowercase({buffer, total_len}, seed);
    }

    std::string buffer{};
    buffer.reserve(total_len);
    buffer.append(str1);
    buffer.push_back('.');
    buffer.append(str2);

    return MurmurHash2Lowercase(buffer, seed);
}
#endif
