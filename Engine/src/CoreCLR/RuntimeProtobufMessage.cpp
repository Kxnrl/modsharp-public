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

#include "global.h"
#include "murmurhash.h"

#include "CoreCLR/RuntimeProtobufMessage.h"

static std::unordered_map<uint32_t, INetworkMessageInternal*> s_CachedCSharpNetworkSerializable;

INetworkMessageInternal* GetRuntimeProtobufSerializable(const char* name)
{
    const auto hash = MurmurHash2(name, MURMURHASH_SEED);

    if (const auto it = s_CachedCSharpNetworkSerializable.find(hash); it != s_CachedCSharpNetworkSerializable.end())
    {
        return it->second;
    }

    const auto net = g_pNetworkMessages->FindNetworkMessagePartial(name);

    if (net == nullptr)
        return nullptr;

    const auto csharpNet = new CSharpNetworkSerializable(net);

    auto [it, inserted] = s_CachedCSharpNetworkSerializable.try_emplace(hash, csharpNet);

    if (!inserted)
    {
        delete csharpNet;
        return it->second;
    }

    return csharpNet;
}