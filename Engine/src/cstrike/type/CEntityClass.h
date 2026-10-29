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

#ifndef CSTRIKE_TYPE_ENTITYCLASS_H
#define CSTRIKE_TYPE_ENTITYCLASS_H

#include "cstrike/type/CEntityIdentity.h"

#include <cstdint>
#include <type_traits>
#include <utility>

class CEntityInstance;
class CEntityClass;

struct CEntityClassInfo
{
    static constexpr uint32_t kNotSpawnableFlag = 1U << 0;

    const char*       m_pszClassname;
    const char*       m_pszCppClassname;
    uint32_t          m_nFlags;
    CEntityClass*     m_pClass;
    CEntityClassInfo* m_pBaseClassInfo;
    void*             m_pSchemaBinding;
    void*             m_pDataDescMap;
};
static_assert(sizeof(CEntityClassInfo) == 0x38);

class CEntityClass
{
public:
    using ThinkFunc = void (*)(CEntityInstance*);
    using NameToFuncCb = ThinkFunc (*)(const char*);

    CEntityClass() = delete;

    static constexpr uint32_t kAliasFlag = 1U << 1;

    inline static uint32_t       sm_nEntityListHeadOffset = 0;
    inline static CEntityClass** sm_ppFirst               = nullptr;
    inline static uint32_t       sm_nNextOffset           = 0;
    inline static uint32_t       sm_nClassInfoOffset      = 0;
    inline static uint32_t       sm_nFlagsOffset          = 0;
    inline static uint32_t       sm_nNameToThinkFuncOffset = 0;

    [[nodiscard]] static CEntityClass* GetFirst()
    {
        if (sm_ppFirst == nullptr)
            return nullptr;

        auto* first = *sm_ppFirst;
        return reinterpret_cast<uintptr_t>(first) == ~uintptr_t{0} ? nullptr : first;
    }

    [[nodiscard]] CEntityClass* GetNext() const
    {
        if (sm_nNextOffset == 0)
            return nullptr;

        return *reinterpret_cast<CEntityClass* const*>(reinterpret_cast<uintptr_t>(this) + sm_nNextOffset);
    }

    [[nodiscard]] const CEntityClassInfo* GetInfo() const
    {
        if (sm_nClassInfoOffset == 0)
            return nullptr;

        return *reinterpret_cast<CEntityClassInfo* const*>(reinterpret_cast<uintptr_t>(this) + sm_nClassInfoOffset);
    }

    [[nodiscard]] bool IsAlias() const
    {
        if (sm_nFlagsOffset == 0)
            return false;

        return (*reinterpret_cast<const uint32_t*>(reinterpret_cast<uintptr_t>(this) + sm_nFlagsOffset) & kAliasFlag) != 0;
    }

    [[nodiscard]] ThinkFunc GetThinkFunc(const char* name) const
    {
        if (name == nullptr || sm_nNameToThinkFuncOffset == 0)
            return nullptr;

        const auto resolver = *reinterpret_cast<NameToFuncCb const*>(reinterpret_cast<uintptr_t>(this) + sm_nNameToThinkFuncOffset);
        return resolver != nullptr ? resolver(name) : nullptr;
    }

    [[nodiscard]] CEntityIdentity* GetEntityListHead() const
    {
        return *reinterpret_cast<CEntityIdentity* const*>(reinterpret_cast<const uintptr_t>(this) + sm_nEntityListHeadOffset);
    }

    template <typename Fn>
    static void ForEachIdentityFrom(CEntityIdentity* pIdentity, Fn&& fn)
    {
        const auto next = CEntityIdentity::sm_nNextByClassOffset;
        for (; pIdentity; pIdentity = CEntityIdentity::GetNextByClass(pIdentity, next))
        {
            if (pIdentity->IsMarkedForDeletion() || pIdentity->GetBaseEntity() == nullptr)
                continue;

            if constexpr (std::is_void_v<std::invoke_result_t<Fn&, CEntityIdentity*>>)
                fn(pIdentity);
            else if (!fn(pIdentity))
                return;
        }
    }

    template <typename Fn>
    void ForEachIdentity(Fn&& fn) const
    {
        ForEachIdentityFrom(GetEntityListHead(), std::forward<Fn>(fn));
    }

    template <typename Fn>
    void ForEachEntity(Fn&& fn) const
    {
        ForEachIdentity([&fn](CEntityIdentity* pIdentity) { return fn(pIdentity->GetBaseEntity()); });
    }
};

#endif