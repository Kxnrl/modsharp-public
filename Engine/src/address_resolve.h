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

#ifndef MS_ROOT_ADDRESS_RESOLVER_H
#define MS_ROOT_ADDRESS_RESOLVER_H

#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "module.h"

#include <Zydis.h>

#include <format>
#include <string>
#include <utility>
#include <vector>

#define RESOLVE_GAMEDATA_ADDRESS(name, variable) \
    (variable) = g_pGameData->GetAddress<decltype(variable)>(name)

// Confidence-gated result of a scan.
//
// A resolver may derive the same answer several independent ways - once per inlined copy of the
// anchor function - and may know whether its structural predicate matched exactly once inside a
// scan rather than being the first of many plausible hits.
//
// Only a corroborated value may overwrite hand-verified gamedata. A lone "first thing that looked
// right" match is treated as a cross-check: it can confirm gamedata, never replace it. Every
// auto-resolve bug we have hit (m_vecLoadedSpawnGroups=2888, m_NetChannel=272) was an
// uncorroborated positional guess silently installed over a correct value.
class ResolveVote
{
public:
    // Record one independent derivation. `unique` means the predicate matched exactly once in
    // its scan, so this is not a first-match-wins guess.
    void Add(int32_t value, bool unique = false)
    {
        if (value == -1)
            return;

        _unique |= unique;

        for (auto& [v, n] : _values)
        {
            if (v == value)
            {
                ++n;
                return;
            }
        }

        _values.emplace_back(value, 1);
    }

    [[nodiscard]] bool    Empty() const { return _values.empty(); }
    [[nodiscard]] bool    Ambiguous() const { return _values.size() > 1; }
    [[nodiscard]] int32_t Value() const { return _values.size() == 1 ? _values[0].first : -1; }

    // Trustworthy enough to overwrite gamedata: a single value, and either the predicate was
    // unique within its scan or two independent derivations agreed on it.
    [[nodiscard]] bool Corroborated() const
    {
        return _values.size() == 1 && (_unique || _values[0].second >= 2);
    }

    [[nodiscard]] std::string Describe() const
    {
        std::string desc;
        for (const auto& [v, n] : _values)
        {
            if (!desc.empty())
                desc += ", ";
            desc += std::format("{}(x{})", v, n);
        }
        return desc;
    }

private:
    std::vector<std::pair<int32_t, int>> _values{};
    bool                                 _unique{false};
};

// Feed a set of candidate offsets into a vote. Exactly one distinct candidate is unambiguous within
// its scan and marked unique; several distinct ones mean the predicate no longer pins the field.
inline void VoteCandidates(ResolveVote& vote, const std::vector<int32_t>& candidates)
{
    if (candidates.size() == 1)
    {
        vote.Add(candidates.front(), /*unique*/ true);
    }
    else
    {
        for (auto value : candidates)
            vote.Add(value);
    }
}

inline void try_overwrite_vfunc(const char* name, int32_t resolved)
{
    if (resolved == -1)
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    int current = 0;
    if (g_pGameData->GetVFunctionIndex(name, &current) && current != resolved)
    {
        WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (mismatch!).", name, resolved, current);
    }

    g_pGameData->OverwriteVFuncIndex(name, resolved);
}

inline void try_overwrite_offset(const char* name, int32_t resolved)
{
    if (resolved == -1)
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    int32_t current = 0;
    if (g_pGameData->GetOffset(name, &current) && current != resolved)
    {
        WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (mismatch!).", name, resolved, current);
    }

    g_pGameData->OverwriteOffset(name, resolved);
}

inline void try_overwrite_address(const char* name, uintptr_t resolved)
{
    if (resolved == 0)
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    g_pGameData->OverwriteAddress(name, resolved);
}

// Confidence-gated overload. Refuses to install a value derived from conflicting evidence, and
// lets an uncorroborated scan confirm gamedata without being able to overwrite it.
inline void try_overwrite_offset(const char* name, const ResolveVote& vote)
{
    if (vote.Empty())
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    if (vote.Ambiguous())
    {
        WARN("GameData auto-resolve: %s is ambiguous [%s], keeping gamedata.", name, vote.Describe().c_str());
        return;
    }

    const auto resolved = vote.Value();

    int32_t current = 0;
    if (g_pGameData->GetOffset(name, &current) && current != resolved)
    {
        if (!vote.Corroborated())
        {
            WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (uncorroborated, keeping gamedata).", name, resolved, current);
            return;
        }

        WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (mismatch!).", name, resolved, current);
    }

    g_pGameData->OverwriteOffset(name, resolved);
}

inline void try_overwrite_vfunc(const char* name, const ResolveVote& vote)
{
    if (vote.Empty())
    {
        WARN("GameData auto-resolve: failed to resolve %s.", name);
        return;
    }

    if (vote.Ambiguous())
    {
        WARN("GameData auto-resolve: %s is ambiguous [%s], keeping gamedata.", name, vote.Describe().c_str());
        return;
    }

    const auto resolved = vote.Value();

    int current = 0;
    if (g_pGameData->GetVFunctionIndex(name, &current) && current != resolved)
    {
        if (!vote.Corroborated())
        {
            WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (uncorroborated, keeping gamedata).", name, resolved, current);
            return;
        }

        WARN("GameData auto-resolve: %s resolved=%d, gamedata=%d (mismatch!).", name, resolved, current);
    }

    g_pGameData->OverwriteVFuncIndex(name, resolved);
}

template <typename T>
static void AssignOrFallback(CModule* mod, T& target_ptr, uintptr_t found_addr, const char* func_name, const char* gamedata_key = nullptr)
{
    const char* key = gamedata_key ? gamedata_key : func_name;

    auto mod_name = mod ? mod->ModuleName() : std::string_view{"<unknown>"};

    if (found_addr != 0)
    {
        FLOG("Found %s at %.*s+0x%llx", func_name, static_cast<int>(mod_name.size()), mod_name.data(), found_addr - mod->Base());
#ifdef DEBUG
        uintptr_t gamedata_addr = 0;
        if (g_pGameData->GetAddress(key, gamedata_addr) && gamedata_addr != 0)
        {
            if (gamedata_addr != found_addr)
            {
                WARN("GameData auto-resolve: %s resolved=%.*s+0x%llx, gamedata=%.*s+0x%llx (mismatch!).",
                     key,
                     static_cast<int>(mod_name.size()), mod_name.data(), found_addr - mod->Base(),
                     static_cast<int>(mod_name.size()), mod_name.data(), gamedata_addr - mod->Base());
            }
            else
            {
                FLOG("GameData auto-resolve: %s resolved matches gamedata at %.*s+0x%llx.", key, static_cast<int>(mod_name.size()), mod_name.data(), found_addr - mod->Base());
            }
        }
#endif

        target_ptr = reinterpret_cast<T>(found_addr);

        try_overwrite_address(key, found_addr);
    }
    else
    {
        target_ptr = g_pGameData->GetAddress<T>(key);

        if (target_ptr == nullptr)
        {
            FatalError("Failed to find %s (Both Scanner and GameData failed)", func_name);
        }
        else
        {
            FLOG("Fallback: Loaded %s from GameData.\n", func_name);
        }
    }
}

// address_server.cpp
void FindCEntityIdentity_SetEntityName();
void FindGameSystemFactory();
void FindCCSPlayerWeaponServices_DestroyWeapon();
void FindSetModel();
void FindCCSPlayerWeaponService_FindWeaponBySlot();
void ResolveCCSPlayerPawnStateActive();
void ResolveCBaseEntity_IsWeapon();
void ResolveCBaseEntityTeleport();
void ResolveCBaseEntity_GetEyeAngles();
void ResolveCBaseEntity_GetEyePosition();
void ResolveCBaseEntity_ChangeTeam();
void ResolveCGameSceneNodeGetters();
void ResolveCBaseEntity_AbsOrigin();
void ResolveCBaseEntity_EventKill();
void ResolveCBaseEntity_GetCenter();
void Resolve_CBaseEntity_Use_StartTouch_Touch_EndTouch();
void ResolveCEntityInstance_GetDynamicBinding();
void ResolveCBaseEntity_Precache();
void ResolveCCSPlayerWeaponServices();
void ResolveCCSPlayerItemServices();
void ResolveCCSPlayerPawn_IsPlayer();
void ResolveCBasePlayerController_CanHearAndReadChatFrom();
void ResolveCCSPlayerController_RoundRespawn();
void ResolveCBasePlayerPawn_CommitSuicide();
void ResolveCCSPlayerPawn_SetDefaultGloves();
void ResolveCEntityClassEntityListOffset();
void ResolveCBasePlayerPawn_SnapViewAngles();
void ResolveCreateTriggerInternal();

// address_engine.cpp
void ResolveServerSideClientOffsets();
void ResolveNetworkGameServerOffsets();

#endif // MS_ROOT_ADDRESS_RESOLVER_H