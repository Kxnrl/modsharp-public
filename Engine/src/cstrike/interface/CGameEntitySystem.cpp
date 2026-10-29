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
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "platform.h"

#include "cstrike/cstrike.h"
#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/IGameResourceServiceServer.h"
#include "cstrike/type/CEntityClass.h"
#include "cstrike/type/CEntityKeyValues.h"
#include "cstrike/type/CUtlSymbolLarge.h"
#include "cstrike/type/Variant.h"

#include <cstdint>
#include <cstring>
#include <vector>

CGameEntitySystem* g_pGameEntitySystem;

namespace
{
[[nodiscard]] inline bool EqualAscii(unsigned char lhs, unsigned char rhs) noexcept
{
    return lhs == rhs || ((lhs ^ rhs) == 0x20 && static_cast<unsigned int>((lhs | 0x20) - 'a') < 26);
}

[[nodiscard]] inline bool EqualsIgnoreCase(const char* lhs, const char* rhs) noexcept
{
    for (;; ++lhs, ++rhs)
    {
        if (!EqualAscii(*lhs, *rhs))
            return false;
        if (*lhs == 0)
            return true;
    }
}

[[nodiscard]] constexpr uint64_t FoldWord(uint64_t word) noexcept
{
    constexpr uint64_t high  = 0x8080808080808080ull;
    const uint64_t     ascii = word & ~high;
    const uint64_t     upper = (ascii + 0x3F3F3F3F3F3F3F3Full) & ~(ascii + 0x2525252525252525ull) & ~word & high;
    return word | (upper >> 2);
}

[[nodiscard]] inline bool EqualsIgnoreCase(const char* lhs, const char* rhs, size_t length) noexcept
{
    if (length < 8)
        return EqualsIgnoreCase(lhs, rhs);

    for (size_t at = 0;; at += 8)
    {
        if (at + 8 > length)
            at = length - 8;

        uint64_t a, b;
        std::memcpy(&a, lhs + at, 8);
        std::memcpy(&b, rhs + at, 8);
        if (a != b && FoldWord(a) != FoldWord(b))
            return false;
        if (at + 8 == length)
            return true;
    }
}

[[nodiscard]] MS_FORCEINLINE bool SegmentEquals(const char* name, const char* segment, size_t length) noexcept
{
    for (size_t i = 0; i < length; ++i)
    {
        const auto expected = static_cast<unsigned char>(segment[i]);
        const auto actual   = static_cast<unsigned char>(name[i]);
        if (expected != actual && (expected == '?' ? actual == 0 : !EqualAscii(expected, actual)))
            return false;
    }
    return true;
}

class FirstByteFilter
{
public:
    explicit FirstByteFilter(const char* pattern) noexcept
    {
        const auto first = static_cast<unsigned char>(*pattern);
        m_fold           = first == '*' || first == '?' ? 0xFF : static_cast<unsigned int>((first | 0x20) - 'a') < 26 ? 0x20 :
                                                                                                                        0;
        m_expect         = static_cast<unsigned char>(first | m_fold);
    }

    [[nodiscard]] bool MayMatch(const char* name) const noexcept { return (static_cast<unsigned char>(*name) | m_fold) == m_expect; }

private:
    unsigned char m_fold;
    unsigned char m_expect;
};

struct MatchAny
{
    [[nodiscard]] bool MayMatch(const char*) const noexcept { return true; }
    [[nodiscard]] bool operator()(const char*) const noexcept { return true; }
};

class SuffixPattern
{
public:
    SuffixPattern(const char* tail, size_t length) noexcept
        : m_tail(tail), m_length(length)
    {
        const auto covered = length < 8 ? length : 8;
        for (size_t i = 0; i < covered; ++i)
        {
            const auto byte = static_cast<unsigned char>(tail[length - covered + i]);
            const auto lane = (8 - covered + i) * 8;
            if (byte != '?')
            {
                m_word |= uint64_t{byte} << lane;
                m_mask |= uint64_t{0xFF} << lane;
            }
        }
        m_word = FoldWord(m_word);
    }

    [[nodiscard]] bool MayMatch(const char*) const noexcept { return true; }

    [[nodiscard]] MS_FORCEINLINE bool operator()(const char* name) const noexcept
    {
        const auto length = std::strlen(name);
        if (length < m_length)
            return false;
        if (length < 8)
            return SegmentEquals(name + length - m_length, m_tail, m_length);

        uint64_t last;
        std::memcpy(&last, name + length - 8, 8);
        return (FoldWord(last) & m_mask) == m_word && (m_length <= 8 || SegmentEquals(name + length - m_length, m_tail, m_length - 8));
    }

private:
    const char* m_tail;
    size_t      m_length;
    uint64_t    m_word = 0;
    uint64_t    m_mask = 0;
};

class ContainsPattern
{
public:
    ContainsPattern(const char* segment, size_t length) noexcept
        : m_segment(segment), m_length(length), m_first(segment)
    {
    }

    [[nodiscard]] bool MayMatch(const char*) const noexcept { return true; }

    [[nodiscard]] MS_FORCEINLINE bool operator()(const char* name) const noexcept
    {
        for (; *name != 0; ++name)
        {
            if (m_first.MayMatch(name) && SegmentEquals(name, m_segment, m_length))
                return true;
        }
        return false;
    }

private:
    const char*     m_segment;
    size_t          m_length;
    FirstByteFilter m_first;
};

class WildcardPattern
{
public:
    explicit WildcardPattern(const char* pattern) noexcept
        : m_pattern(pattern), m_first(pattern)
    {
    }

    [[nodiscard]] bool MayMatch(const char* name) const noexcept { return m_first.MayMatch(name); }

    [[nodiscard]] MS_FORCEINLINE bool operator()(const char* name) const noexcept
    {
        if (!m_first.MayMatch(name))
            return false;

        const char* segment = m_pattern;
        for (;; ++segment, ++name)
        {
            const auto expected = static_cast<unsigned char>(*segment);
            if (expected == '*')
                break;

            const auto actual = static_cast<unsigned char>(*name);
            if (expected == actual)
            {
                if (expected == 0)
                    return true;
            }
            else if (expected == '?' ? actual == 0 : !EqualAscii(expected, actual))
                return false;
        }
        return segment[1] == 0 || MatchStars(segment, name);
    }

private:
    [[nodiscard]] static bool MatchStars(const char* segment, const char* name) noexcept
    {
        const char* lastStar  = segment;
        bool        hasMiddle = false;
        const char* p         = segment;
        for (; *p != 0; ++p)
        {
            if (*p == '*')
            {
                hasMiddle |= p != segment && p[-1] != '*';
                lastStar = p;
            }
        }
        const auto tailLength = static_cast<size_t>(p - lastStar - 1);

        const auto* end = name + std::strlen(name);
        if (static_cast<size_t>(end - name) < tailLength || !SegmentEquals(end - tailLength, lastStar + 1, tailLength))
            return false;

        end -= tailLength;
        while (hasMiddle)
        {
            while (*segment == '*')
                ++segment;
            if (segment > lastStar)
                break;

            size_t length = 0;
            while (segment[length] != '*')
                ++length;
            const FirstByteFilter first(segment);
            for (;; ++name)
            {
                if (static_cast<size_t>(end - name) < length)
                    return false;
                if (first.MayMatch(name) && SegmentEquals(name, segment, length))
                    break;
            }
            name += length;
            segment += length;
        }
        return true;
    }

    const char*     m_pattern;
    FirstByteFilter m_first;
};

template <typename Visit>
decltype(auto) WithMatcher(const char* pattern, Visit&& visit)
{
    if (*pattern != '*')
        return visit(WildcardPattern(pattern));

    const char* segment = pattern;
    while (*segment == '*')
        ++segment;
    if (*segment == 0)
        return visit(MatchAny{});

    const char* end = segment;
    while (*end != 0 && *end != '*')
        ++end;
    if (*end == 0)
        return visit(SuffixPattern(segment, static_cast<size_t>(end - segment)));

    const char* rest = end;
    while (*rest == '*')
        ++rest;
    if (*rest == 0)
        return visit(ContainsPattern(segment, static_cast<size_t>(end - segment)));
    return visit(WildcardPattern(pattern));
}

class VerdictMemo
{
public:
    template <typename Matches>
    [[nodiscard]] MS_FORCEINLINE bool operator()(const char* name, const Matches& matches) noexcept
    {
        const auto slot = static_cast<size_t>((reinterpret_cast<uintptr_t>(name) * 0x9E3779B97F4A7C15ull) >> 56);
        const auto bit  = uint64_t{1} << (slot & 63);
        if ((m_filled[slot >> 6] & bit) == 0 || m_names[slot] != name)
        {
            m_filled[slot >> 6] |= bit;
            m_names[slot]    = name;
            m_verdicts[slot] = matches(name);
        }
        return m_verdicts[slot];
    }

    [[nodiscard]] bool operator()(const char*, const MatchAny&) noexcept { return true; }

private:
    uint64_t    m_filled[4] = {};
    const char* m_names[256];
    bool        m_verdicts[256];
};

int32_t s_identityNextOffset = 0;

[[nodiscard]] inline CEntityIdentity* Link(const CEntityIdentity* identity, int32_t offset)
{
    return *reinterpret_cast<CEntityIdentity* const*>(reinterpret_cast<uintptr_t>(identity) + offset);
}

template <typename Matches>
MS_FORCEINLINE CBaseEntity* FindNext(CEntityIdentity* identity, int32_t next, const Matches& matches)
{
    for (; identity != nullptr; identity = Link(identity, next))
    {
        if (!identity->IsMarkedForDeletion() && matches(identity->GetClassname()))
            return identity->GetBaseEntity();
    }
    return nullptr;
}

template <typename Matches>
int32_t CollectAll(CEntityIdentity* identity, int32_t next, const Matches& matches, CBaseEntity** buffer, int32_t capacity)
{
    VerdictMemo memo;
    int32_t     total = 0;
    for (; identity != nullptr; identity = Link(identity, next))
    {
        const char* name = identity->GetClassname();
        if (identity->IsMarkedForDeletion() || !matches.MayMatch(name) || !memo(name, matches))
            continue;

        auto* entity = identity->GetBaseEntity();
        if (entity == nullptr)
            break;

        if (buffer != nullptr && total < capacity)
            buffer[total] = entity;
        ++total;
    }
    return total;
}

MS_NOINLINE CBaseEntity* NextByShape(CEntityIdentity* start, const char* pattern)
{
    return WithMatcher(pattern, [&](const auto& matches) { return FindNext(start, s_identityNextOffset, matches); });
}

CBaseEntity* NextByClassname(const CGameEntitySystem* system, const CEntityIdentity* cursor, const char* pattern)
{
    if (pattern == nullptr)
        return cursor != nullptr ? cursor->GetBaseEntity() : nullptr;
    if (system == nullptr)
        return nullptr;

    auto* start = cursor != nullptr ? Link(cursor, s_identityNextOffset) : system->GetGlobalEntityListHead();
    if (*pattern != '*')
        return FindNext(start, s_identityNextOffset, WildcardPattern(pattern));
    if (pattern[1] == 0)
        return FindNext(start, s_identityNextOffset, MatchAny{});
    return NextByShape(start, pattern);
}

int32_t CollectByClassname(const CGameEntitySystem* system, const char* pattern, CBaseEntity** buffer, int32_t capacity)
{
    if (system == nullptr || pattern == nullptr)
        return 0;

    return WithMatcher(pattern, [&](const auto& matches) {
        return CollectAll(system->GetGlobalEntityListHead(), s_identityNextOffset, matches, buffer, capacity);
    });
}

CBaseEntity* FindNextEntityInClass(CEntityIdentity* identity)
{
    CBaseEntity* result = nullptr;
    CEntityClass::ForEachIdentityFrom(identity, [&result](CEntityIdentity* candidate) {
        result = candidate->GetBaseEntity();
        return false;
    });
    return result;
}

int32_t EnumerateEntityClass(const CEntityClass* klass, CBaseEntity** buffer, int32_t capacity)
{
    if (klass == nullptr)
        return 0;

    int32_t total = 0;
    klass->ForEachEntity([&](CBaseEntity* entity) {
        if (buffer != nullptr && total < capacity)
            buffer[total] = entity;
        ++total;
    });
    return total;
}

int32_t CollectInClass(const CEntityClass* klass, const char* classname, CBaseEntity** buffer, int32_t capacity)
{
    const WildcardPattern matches(classname);
    VerdictMemo           memo;
    int32_t               total = 0;
    klass->ForEachIdentity([&](CEntityIdentity* identity) {
        if (!memo(identity->GetClassname(), matches))
            return;

        if (buffer != nullptr && total < capacity)
            buffer[total] = identity->GetBaseEntity();
        ++total;
    });
    return total;
}

class ClassIndex
{
    struct Slot
    {
        const char*   name;
        size_t        length;
        CEntityClass* klass;
    };

public:
    void Clear()
    {
        m_slots.clear();
        m_size       = 0;
        m_nullTaken  = false;
        m_emptyTaken = false;
    }

    [[nodiscard]] size_t Size() const { return m_size; }

    [[nodiscard]] bool Contains(const char* name) const
    {
        if (name == nullptr)
            return m_nullTaken;
        if (*name == 0)
            return m_emptyTaken;
        return Find(name) != nullptr;
    }

    [[nodiscard]] CEntityClass* Find(const char* name) const
    {
        if (name == nullptr || *name == 0 || m_slots.empty())
            return nullptr;

        const auto length = std::strlen(name);
        const auto mask   = m_slots.size() - 1;
        for (auto i = Hash(name, length) & mask;; i = (i + 1) & mask)
        {
            const auto& slot = m_slots[i];
            if (slot.klass == nullptr)
                return nullptr;
            if (slot.length == length && EqualsIgnoreCase(slot.name, name, length))
                return slot.klass;
        }
    }

    void Insert(const char* name, CEntityClass* klass)
    {
        if (name == nullptr)
            m_nullTaken = true;
        else if (*name == 0)
            m_emptyTaken = true;
        else if (klass != nullptr && Find(name) == nullptr)
        {
            if ((m_size + 1) * 2 > m_slots.size())
                Grow();
            Place({name, std::strlen(name), klass});
            ++m_size;
        }
    }

private:
    [[nodiscard]] static size_t Hash(const char* name, size_t length)
    {
        uint64_t head = 0, tail = 0;
        if (length >= 8)
        {
            std::memcpy(&head, name, 8);
            std::memcpy(&tail, name + length - 8, 8);
        }
        else
        {
            for (size_t i = 0; i < length; ++i)
                head |= uint64_t{static_cast<unsigned char>(name[i])} << (i * 8);
        }

        constexpr uint64_t fold = 0x2020202020202020ull;
        auto               hash = ((head | fold) * 0x9E3779B97F4A7C15ull) ^ ((tail | fold) + length);
        hash *= 0xD6E8FEB86659FD93ull;
        return static_cast<size_t>(hash ^ (hash >> 32));
    }

    void Grow()
    {
        std::vector<Slot> previous(m_slots.empty() ? 16 : m_slots.size() * 2);
        previous.swap(m_slots);
        for (const auto& slot : previous)
        {
            if (slot.klass != nullptr)
                Place(slot);
        }
    }

    void Place(const Slot& value)
    {
        const auto mask = m_slots.size() - 1;
        auto       i    = Hash(value.name, value.length) & mask;
        while (m_slots[i].klass != nullptr)
            i = (i + 1) & mask;
        m_slots[i] = value;
    }

    std::vector<Slot> m_slots;
    size_t            m_size       = 0;
    bool              m_nullTaken  = false;
    bool              m_emptyTaken = false;
};

ClassIndex s_classesByName;
ClassIndex s_classesByCppClass;
ClassIndex s_registeredByCppClass;
} // namespace

void InitGameEntitySystem()
{
    CGameEntitySystem::Init();
}

void CGameEntitySystem::Init()
{
    g_pGameEntitySystem                    = g_pGameResourceServiceServer->GetGameEntitySystem();
    s_identityNextOffset                   = CEntityIdentity::GetNextOffset();
    CEntityIdentity::sm_nNextByClassOffset = CEntityIdentity::GetNextByClassOffset();
    CacheEntityClasses();
}

void CGameEntitySystem::CacheEntityClasses()
{
    s_classesByName.Clear();
    s_classesByCppClass.Clear();
    s_registeredByCppClass.Clear();

    for (auto* klass = CEntityClass::GetFirst(); klass != nullptr; klass = klass->GetNext())
    {
        const auto* info = klass->GetInfo();
        if (info == nullptr || klass->IsAlias())
            continue;

        const auto* name = info->m_pszClassname;
        if (s_registeredByCppClass.Contains(info->m_pszCppClassname) || (name != nullptr && s_classesByName.Contains(name)))
            continue;

        s_registeredByCppClass.Insert(info->m_pszCppClassname, klass);
        if (name != nullptr)
            s_classesByName.Insert(name, klass);
    }

    for (auto* klass = CEntityClass::GetFirst(); klass != nullptr; klass = klass->GetNext())
    {
        const auto* info = klass->GetInfo();
        if (info == nullptr || !klass->IsAlias() || info->m_pszClassname == nullptr || *info->m_pszClassname == 0
            || info->m_pBaseClassInfo == nullptr || info->m_pBaseClassInfo->m_pClass == nullptr)
            continue;

        const auto* baseInfo = info->m_pBaseClassInfo->m_pClass->GetInfo();
        if (baseInfo == nullptr)
            continue;

        if (auto* target = s_registeredByCppClass.Find(baseInfo->m_pszCppClassname))
            s_classesByName.Insert(info->m_pszClassname, target);
    }

    size_t total = 0, redirected = 0;
    for (auto* klass = CEntityClass::GetFirst(); klass != nullptr; klass = klass->GetNext())
    {
        const auto* info = klass->GetInfo();
        if (info == nullptr)
            continue;

        ++total;
        const auto* resolved = s_classesByName.Find(info->m_pszClassname);
        redirected += resolved != nullptr && resolved != klass;
        s_classesByCppClass.Insert(info->m_pszCppClassname, klass);
    }

    FLOG("Cached %zu entity classes (%zu classnames, %zu CPP classnames, %zu alias redirects)",
         total, s_classesByName.Size(), s_classesByCppClass.Size(), redirected);
}

CBaseEntity* CGameEntitySystem::FindEntityByEHandle(const CBaseHandle& handle)
{
    if (!handle.IsValid())
        return nullptr;

    const auto entity = FindEntityByIndex(handle.GetEntryIndex());
    if (!entity)
        return nullptr;

    const auto a = entity->GetActualEHandle().ToInt();
    const auto b = handle.ToInt();
    if (a != b)
        return nullptr;

    return entity;
}

CBaseEntity* CGameEntitySystem::FindEntityByIndex(EntityIndex_t index)
{
    return address::server::CGameEntitySystem_FindEntityByIndex(this, index);
}

const matrix3x4_t& CGameEntitySystem::GetSpawnOriginOffset() const
{
    AssertPtr(this);
    return address::server::CGameEntitySystem_GetSpawnOriginOffset(this);
}

CUtlSymbolLarge CGameEntitySystem::AllocPooledString(const char* pStr) const
{
    AssertPtr(this);
    CUtlSymbolLarge symbol;
    address::server::CGameEntitySystem_AllocPooledString(&symbol, pStr);
    return symbol;
}

CEntityIdentity* CGameEntitySystem::GetGlobalEntityListHead() const
{
    if (sm_nGlobalListHeadOffset == 0)
        return nullptr;
    return *reinterpret_cast<CEntityIdentity* const*>(reinterpret_cast<uintptr_t>(this) + sm_nGlobalListHeadOffset);
}

CBaseEntity* CGameEntitySystem::FindByClassname(CBaseEntity* startEntity, const char* classname)
{
    auto* identity = startEntity != nullptr ? startEntity->GetEntityIdentity() : nullptr;
    return NextByClassname(g_pGameEntitySystem, identity, classname);
}

int32_t CGameEntitySystem::FindAllByClassname(const char* classname, CBaseEntity** buffer, int32_t capacity) const
{
    if (classname != nullptr && std::strpbrk(classname, "*?") == nullptr)
    {
        if (const auto* klass = s_classesByName.Find(classname))
            return CollectInClass(klass, classname, buffer, capacity);
    }

    return CollectByClassname(g_pGameEntitySystem, classname, buffer, capacity);
}

CBaseEntity* CGameEntitySystem::FindByCppClass(CBaseEntity* startEntity, const char* cppClass)
{
    if (cppClass == nullptr || *cppClass == '\0')
        return nullptr;

    if (startEntity != nullptr)
    {
        auto*       identity = startEntity->GetEntityIdentity();
        auto*       klass    = identity != nullptr ? identity->GetEntityClass() : nullptr;
        const auto* info     = klass != nullptr ? klass->GetInfo() : nullptr;
        if (info != nullptr && info->m_pszCppClassname != nullptr
            && EqualsIgnoreCase(cppClass, info->m_pszCppClassname))
            return FindNextEntityInClass(identity->GetNextByClass());
        return nullptr;
    }

    auto* klass = FindEntityClassByCppClass(cppClass);
    return klass != nullptr ? FindNextEntityInClass(klass->GetEntityListHead()) : nullptr;
}

CBaseEntity* CGameEntitySystem::FindByName(CBaseEntity* startEntity, const char* name)
{
    return address::server::CGameEntitySystem_FindByName(this, startEntity, name, nullptr, nullptr, nullptr, nullptr);
}

CBaseEntity* CGameEntitySystem::FindInSphere(CBaseEntity* startEntity, Vector* center, float radius)
{
    return address::server::CGameEntitySystem_FindInSphere(this, startEntity, center, radius);
}

CBaseEntity* CGameEntitySystem::SpawnEntityFromKeyValuesSync(const char* classname, const KeyValuesVariantItem* items, int count) const
{
    const auto entity = address::server::CreateEntityByName(classname, -1);
    if (entity == nullptr)
        return nullptr;

    CEntityKeyValues* pKeyValues = CEntityKeyValues::Create();

    for (int i = 0; i < count; i++)
    {
        const auto& item = items[i];

        switch (item.Value.type)
        {
        case KeyValuesVariantValueItemType_Bool:
            pKeyValues->SetBool(item.Key, item.Value.bValue);
            break;
        case KeyValuesVariantValueItemType_Int32:
            pKeyValues->SetInt(item.Key, item.Value.i32Value);
            break;
        case KeyValuesVariantValueItemType_Float:
            pKeyValues->SetFloat(item.Key, item.Value.flValue);
            break;
        case KeyValuesVariantValueItemType_String:
            pKeyValues->SetString(item.Key, item.Value.szValue);
            break;
        case KeyValuesVariantValueItemType_Pointer:
            pKeyValues->SetPointer(item.Key, item.Value.pValue);
            break;
        case KeyValuesVariantValueItemType_UInt64:
            pKeyValues->SetUInt64(item.Key, item.Value.u64Value);
            break;
        default:
            FatalError("Not supported KeyValuesVariantValueItemType");
            break;
        }
    }

    if (auto classname_kv = pKeyValues->FindOrCreateKeyValues("classname"))
    {
        classname_kv->SetString(classname);
    }

    EntitySpawnInfo_t info;
    info.m_pEntityKeyValues = pKeyValues;
    info.m_pIdentity        = entity->GetEntityIdentity();

    const auto& matrix = GetSpawnOriginOffset();

    g_pGameResourceServiceServer->PrecacheEntitiesAndConfirmResourcesAreLoaded(entity->GetSpawnGroup(), 1, info, matrix);

    entity->DispatchSpawn(pKeyValues);

    return entity;
}

CBaseEntity* CGameEntitySystem::CreateEntityByName(const char* classname) const
{
    return address::server::CreateEntityByName(classname, -1);
}

void CGameEntitySystem::AddEntityIOEvent(CBaseEntity* pEntity, const char* pInputName, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue, float flDelay)
{
    return address::server::CGameEntitySystem_AddEntityIOEvent(this, pEntity, pInputName, pActivator, pCaller, pValue, flDelay, nullptr, nullptr);
}

CEntityClass* CGameEntitySystem::FindEntityClassByName(const char* classname) const
{
    if (auto* klass = s_classesByName.Find(classname))
        return klass;

    return s_registeredByCppClass.Find(classname);
}

CEntityClass* CGameEntitySystem::FindEntityClassByCppClass(const char* cppClass) const
{
    return s_classesByCppClass.Find(cppClass);
}

int32_t CGameEntitySystem::EnumerateByCppClass(const char* cppClass, CBaseEntity** buffer, int32_t capacity) const
{
    return EnumerateEntityClass(FindEntityClassByCppClass(cppClass), buffer, capacity);
}

void CGameEntitySystem::AddListenerEntity(IEntityListener* pListener)
{
    return address::server::CGameEntitySystem_AddListenerEntity(this, pListener);
}

void CGameEntitySystem::RemoveListenerEntity(IEntityListener* pListener)
{
    return address::server::CGameEntitySystem_RemoveListenerEntity(this, pListener);
}
