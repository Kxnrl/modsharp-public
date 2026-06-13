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
#include "address_resolve.h"
#include "logging.h"
#include "memory/zydis_utility.h"

void ResolveServerSideClientOffsets()
{
    auto engine_mod = modules::engine;

    auto vfuncs_CServerSideClientBase = engine_mod->GetVFunctionsFromVTable("CServerSideClientBase");
    auto vfuncs_CServerSideClient     = engine_mod->GetVFunctionsFromVTable("CServerSideClient");

    struct VFuncMatch
    {
        CAddress                      str;
        const CModule::FunctionEntry* range;
    };

    // Find a vfunc from vtable that references the given string, return string address + function range
    // skip: number of matches to skip (0 = first match, 1 = second match, ...)
    auto find_vfunc_by_str = [&](const std::vector<uintptr_t>& vfuncs, const char* str_content, bool exact = false, int skip = 0) -> std::optional<VFuncMatch> {
        auto str = engine_mod->FindString(str_content, false, exact);
        if (!str.IsValid())
        {
            WARN("GameData auto-resolve: string \"%s\" not found.", str_content);
            return std::nullopt;
        }

        auto refs    = engine_mod->GetReferenceRange(str);
        int  matched = 0;

        for (uintptr_t vfunc : vfuncs)
        {
            auto range = engine_mod->GetFunctionRange(vfunc);
            if (range == nullptr)
                continue;

            if (std::ranges::any_of(refs, [&](const CModule::ReferenceEntry& ref) {
                    return ref.source_ip > range->start && ref.source_ip < range->end;
                }))
            {
                if (matched++ < skip)
                    continue;

                return VFuncMatch{.str = str, .range = range};
            }
        }

        WARN("GameData auto-resolve: no vfunc references \"%s\" (skip=%d, found=%d).", str_content, skip, matched);
        return std::nullopt;
    };

#ifdef PLATFORM_WINDOWS
    constexpr auto kThisReg = ZYDIS_REGISTER_RCX;
#else
    constexpr auto kThisReg = ZYDIS_REGISTER_RDI;
#endif

    auto is_this_mem = [](const ZydisDecodedOperand& mem_op, ZydisRegister expected_base) {
        return mem_op.type == ZYDIS_OPERAND_TYPE_MEMORY
               && ZydisUtility::GetBaseRegister(mem_op.mem.base) == expected_base
               && mem_op.mem.index == ZYDIS_REGISTER_NONE
               && mem_op.mem.disp.has_displacement
               && mem_op.mem.disp.value > 0;
    };

    // Check if operand is [base + disp] where base is either original or saved this
    auto is_any_this_mem = [&](const ZydisDecodedOperand& mem_op, ZydisRegister saved_this) {
        return is_this_mem(mem_op, kThisReg)
               || (saved_this != ZYDIS_REGISTER_NONE && is_this_mem(mem_op, saved_this));
    };

    // Scan a vfunc with automatic this-pointer tracking.
    // Callback: (ip, instr, operands, saved_this_reg) -> bool (true to stop)
    auto scan_with_this = [&](const CModule::FunctionEntry* range, auto callback) {
        ZydisRegister saved_this = ZYDIS_REGISTER_NONE;

        ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (saved_this == ZYDIS_REGISTER_NONE
                && instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
                && ZydisUtility::GetBaseRegister(operands[1].reg.value) == kThisReg
                && !ZydisUtility::IsVolatileRegister(operands[0].reg.value))
            {
                saved_this = ZydisUtility::GetBaseRegister(operands[0].reg.value);
            }

            return callback(ip, instr, operands, saved_this);
        });
    };

    // --- m_NetChannel, m_Name, m_UserId (from SetRate) ---
    if (auto match = find_vfunc_by_str(vfuncs_CServerSideClient, "Client %d '%s' setting rate to %d\n"))
    {
        auto [str, range] = *match;

        int32_t net_channel_off  = -1;
        int32_t last_qword_off   = -1;
        int32_t last_movzx_w_off = -1;
        int32_t name_off         = -1;
        int32_t userid_off       = -1;

        scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
            // m_NetChannel: first qword load from this
            if (net_channel_off == -1
                && instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].size == 64
                && is_any_this_mem(operands[1], saved_this))
            {
                net_channel_off = static_cast<int32_t>(operands[1].mem.disp.value);
            }

            if (saved_this == ZYDIS_REGISTER_NONE)
                return false;

            // Track last qword load from saved_this (excluding m_NetChannel repeats)
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].size == 64
                && is_this_mem(operands[1], saved_this)
                && static_cast<int32_t>(operands[1].mem.disp.value) != net_channel_off)
            {
                last_qword_off = static_cast<int32_t>(operands[1].mem.disp.value);
            }

            // Track last MOVZX word load from saved_this
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOVZX
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].size == 16
                && is_this_mem(operands[1], saved_this))
            {
                last_movzx_w_off = static_cast<int32_t>(operands[1].mem.disp.value);
            }

            // When we hit the format string LEA, capture m_Name and m_UserId
            if (instr.mnemonic == ZYDIS_MNEMONIC_LEA && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            {
                if (ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip) == str.GetPtr())
                {
                    name_off   = last_qword_off;
                    userid_off = last_movzx_w_off;
                    return true;
                }
            }

            return false;
        });

        try_overwrite_offset("CServerSideClient::m_NetChannel", net_channel_off);
        try_overwrite_offset("CServerSideClient::m_Name", name_off);
        try_overwrite_offset("CServerSideClient::m_UserId", userid_off);
    }

    // --- m_SteamId, m_ConVars, m_Slot (from Create) ---
    //
    // Pattern (both platforms):
    //   1. Before "userinfo" LEA: first qword store [this + disp] → m_SteamId
    //   2. After "userinfo" LEA: first qword store [this + disp] → m_ConVars
    //   3. Last dword load from [this + disp] in function → m_Slot
    if (auto match = find_vfunc_by_str(vfuncs_CServerSideClient, "userinfo", true))
    {
        auto [str, range] = *match;

        int32_t steamid_off   = -1;
        int32_t convars_off   = -1;
        int32_t slot_off      = -1;
        bool    found_str_ref = false;

        scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
            if (saved_this == ZYDIS_REGISTER_NONE)
                return false;

            // Detect "userinfo" string ref
            if (!found_str_ref && instr.mnemonic == ZYDIS_MNEMONIC_LEA && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            {
                if (ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip) == str.GetPtr())
                    found_str_ref = true;
            }

            // Qword store to [this + disp]: m_SteamId (before str) / m_ConVars (after str)
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].size == 64
                && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
                && is_any_this_mem(operands[0], saved_this))
            {
                auto disp = static_cast<int32_t>(operands[0].mem.disp.value);

                if (!found_str_ref)
                {
                    if (steamid_off == -1)
                        steamid_off = disp;
                }
                else
                {
                    if (convars_off == -1)
                        convars_off = disp;
                }
            }

            // Track last dword load from [this + disp] → m_Slot
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].size == 32
                && is_any_this_mem(operands[1], saved_this))
            {
                slot_off = static_cast<int32_t>(operands[1].mem.disp.value);
            }

            return false; // scan entire function
        });

        try_overwrite_offset("CServerSideClient::m_SteamId", steamid_off);
        try_overwrite_offset("CServerSideClient::m_ConVars", convars_off);
        try_overwrite_offset("CServerSideClient::m_Slot", slot_off);
    }

    // --- m_SignonState (from ProcessMove) ---
    //
    // Pattern (both platforms):
    //   First CMP dword [this_reg + disp], 6 in the function
    //   Windows: cmp dword ptr [rcx+64h], 6
    //   Linux:   cmp dword ptr [rdi+64h], 6
    if (auto match = find_vfunc_by_str(vfuncs_CServerSideClient, "Too many move messages"))
    {
        auto [str, range] = *match;

        int32_t signon_off = -1;

        scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
            // First CMP [this + disp], 6
            if (instr.mnemonic == ZYDIS_MNEMONIC_CMP
                && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                && operands[1].imm.value.s == 6
                && operands[0].size == 32
                && is_any_this_mem(operands[0], saved_this))
            {
                signon_off = static_cast<int32_t>(operands[0].mem.disp.value);
                return true;
            }

            return false;
        });

        try_overwrite_offset("CServerSideClient::m_SignonState", signon_off);
    }

    // --- m_IsHLTV ---
    //
    // Pattern (both platforms):
    //   First CMP byte [this_reg + disp], 0 in the function
    //   Linux:   cmp byte ptr [rdi+142h], 0       (immediate)
    //   Windows: cmp [rcx+322], dil               (register zeroed by xor edi,edi)
    if (auto match = find_vfunc_by_str(vfuncs_CServerSideClient, "TV client has no downstream TV sink\n"))
    {
        auto [str, range] = *match;

        int32_t ishlv_off = -1;

        scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
            // First CMP byte [this + disp], 0 (immediate or zeroed register)
            if (instr.mnemonic == ZYDIS_MNEMONIC_CMP
                && operands[0].size == 8
                && is_any_this_mem(operands[0], saved_this))
            {
                ishlv_off = static_cast<int32_t>(operands[0].mem.disp.value);
                return true;
            }

            return false;
        });

        try_overwrite_offset("CServerSideClient::m_IsHLTV", ishlv_off);
    }

    // --- m_FullyAuthenticated (from SteamOnAuthenticatedUser) ---
    //
    // Pattern (both platforms): last MOV byte [reg + disp], 1 in the function.
    //   Windows: mov byte ptr [rbx+9FAh], 1
    //   Linux:   mov byte ptr [r12+9FAh], 1
    if (auto func = engine_mod->FindFunctionFromStringRefs({"\"%s<%i><%s><>\" STEAM USERID validated\n", "SV: Canceling k_EAuthSessionResponseAuthTicketCanceled for %s because %s.\n"}); func.IsValid())
    {
        auto range = engine_mod->GetFunctionRange(func);

        int32_t fully_auth_off = -1;

        if (range != nullptr)
        {
            ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                    && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[0].size == 8
                    && operands[0].mem.index == ZYDIS_REGISTER_NONE
                    && operands[0].mem.disp.has_displacement
                    && operands[0].mem.disp.value > 0
                    && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                    && operands[1].imm.value.u == 1)
                {
                    auto base = ZydisUtility::GetBaseRegister(operands[0].mem.base);
                    if (base != ZYDIS_REGISTER_NONE
                        && base != ZYDIS_REGISTER_RSP
                        && base != ZYDIS_REGISTER_RBP
                        && base != ZYDIS_REGISTER_RIP)
                    {
                        fully_auth_off = static_cast<int32_t>(operands[0].mem.disp.value);
                    }
                }

                return false;
            });
        }

        try_overwrite_offset("CServerSideClient::m_FullyAuthenticated", fully_auth_off);
    }

    // --- m_vecLoadedSpawnGroups ---
    //
    // Scan from string ref IP to skip unrelated vector accesses earlier in the function.
    {
        auto str = engine_mod->FindString("%s:  Not sending unload group to client '%s' due to not being sent\n", true);
        if (!str.IsValid())
        {
            WARN("GameData auto-resolve: m_vecLoadedSpawnGroups string not found.");
        }
        else
        {
            auto ref   = engine_mod->GetReferenceRange(str);
            auto range = ref.empty() ? nullptr : engine_mod->GetFunctionRange(ref.front().source_ip);

            int32_t spawn_groups_off = -1;

            if (range != nullptr)
            {
                ZydisRegister last_base_reg = ZYDIS_REGISTER_NONE;
                int64_t       last_disp     = 0;

                ZydisUtility::ScanInstructions(ref.front().source_ip, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    if (instr.mnemonic != ZYDIS_MNEMONIC_MOV
                        || operands[0].type != ZYDIS_OPERAND_TYPE_REGISTER
                        || operands[1].type != ZYDIS_OPERAND_TYPE_MEMORY
                        || operands[1].mem.index != ZYDIS_REGISTER_NONE
                        || !operands[1].mem.disp.has_displacement
                        || operands[1].mem.disp.value <= 0)
                        return false;

                    auto base = ZydisUtility::GetBaseRegister(operands[1].mem.base);
                    if (base == ZYDIS_REGISTER_NONE
                        || base == ZYDIS_REGISTER_RSP
                        || base == ZYDIS_REGISTER_RBP
                        || base == ZYDIS_REGISTER_RIP)
                        return false;

                    // Step 1: 32-bit load → candidate m_Size
                    if (operands[1].size == 32)
                    {
                        last_base_reg = base;
                        last_disp     = operands[1].mem.disp.value;
                    }
                    // Step 2: 64-bit load from same base, disp == last_disp + 8 → m_pMemory
                    else if (operands[1].size == 64
                             && base == last_base_reg
                             && operands[1].mem.disp.value == last_disp + 8)
                    {
                        spawn_groups_off = static_cast<int32_t>(last_disp);
                        return true;
                    }

                    return false;
                });
            }

            try_overwrite_offset("CServerSideClient::m_vecLoadedSpawnGroups", spawn_groups_off);
        }
    }

    // --- m_nDeltaTick, m_FakeClient ---
    //
    // m_nDeltaTick:
    //   Primary:  first MOV dword [this + disp], reg (not immediate) - fake-client fast path store.
    //   Fallback: from string ref, find MOV reg, [this + disp] -> CMP reg, -1.
    //   If both succeed but disagree -> WARN.
    //
    // m_FakeClient: first MOVZX byte [this + disp] in the function (inlined IsFakeClient).
    //   Linux:   movzx r15d, byte ptr [rdi+0A0h]
    //   Windows: vtable call (not inlined) - will fallback to gamedata.

    {
        if (auto func = engine_mod->FindFunctionFromStringRef("'%s' already awaiting full update\n"); func.IsValid())
        {
            auto range = engine_mod->GetFunctionRange(func);

            int32_t delta_tick_primary = -1;
            int32_t fake_client_off    = -1;

            if (range != nullptr)
            {
                // m_nDeltaTick primary: first MOV dword [this + disp], reg
                // Also detect first vtable call offset to resolve IsFakeClient
                int32_t first_vcall_off = -1;

                scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
                    if (delta_tick_primary == -1
                        && instr.mnemonic == ZYDIS_MNEMONIC_MOV
                        && operands[0].size == 32
                        && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
                        && is_any_this_mem(operands[0], saved_this))
                    {
                        delta_tick_primary = static_cast<int32_t>(operands[0].mem.disp.value);
                        return true;
                    }

                    // Detect first indirect vtable call: call [reg + offset] (exclude RIP-relative imports)
                    if (first_vcall_off == -1
                        && instr.mnemonic == ZYDIS_MNEMONIC_CALL
                        && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                        && operands[0].mem.disp.has_displacement
                        && operands[0].mem.disp.value > 0
                        && operands[0].mem.index == ZYDIS_REGISTER_NONE
                        && ZydisUtility::GetBaseRegister(operands[0].mem.base) != ZYDIS_REGISTER_RIP)
                    {
                        first_vcall_off = static_cast<int32_t>(operands[0].mem.disp.value);
                    }

                    return false;
                });

                // m_FakeClient: resolve the first vtable call (IsFakeClient), scan inside it
                if (first_vcall_off != -1)
                {
                    auto  vtable_index = first_vcall_off / static_cast<int32_t>(sizeof(void*));
                    auto& vfuncs       = vfuncs_CServerSideClientBase;

                    if (vtable_index >= 0 && vtable_index < static_cast<int32_t>(vfuncs.size()))
                    {
                        auto isfake_func  = vfuncs[vtable_index];
                        auto isfake_range = engine_mod->GetFunctionRange(isfake_func);

                        // IsFakeClient is a trivial getter (movzx byte + ret).
                        // Scan directly from the function address; if no range, cap at 32 bytes.
                        uintptr_t scan_end = isfake_range ? isfake_range->end : (isfake_func + 32);

                        ZydisUtility::ScanInstructions(isfake_func, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                            if (instr.mnemonic == ZYDIS_MNEMONIC_MOVZX
                                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                                && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
                                && operands[1].size == 8
                                && operands[1].mem.index == ZYDIS_REGISTER_NONE
                                && operands[1].mem.disp.has_displacement
                                && operands[1].mem.disp.value > 0)
                            {
                                fake_client_off = static_cast<int32_t>(operands[1].mem.disp.value);
                                return true;
                            }
                            // Stop at ret/int3
                            return instr.mnemonic == ZYDIS_MNEMONIC_RET || instr.mnemonic == ZYDIS_MNEMONIC_INT3;
                        });
                    }
                }
                // Fallback: IsFakeClient inlined (Linux devirtualization)
                // First MOVZX byte [this + disp] in the main function
                if (fake_client_off == -1)
                {
                    scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
                        if (instr.mnemonic == ZYDIS_MNEMONIC_MOVZX
                            && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                            && operands[1].size == 8
                            && is_any_this_mem(operands[1], saved_this))
                        {
                            fake_client_off = static_cast<int32_t>(operands[1].mem.disp.value);
                            return true;
                        }
                        return false;
                    });
                }
            }

            int32_t delta_tick_off = delta_tick_primary;
            try_overwrite_offset("CServerSideClient::m_nDeltaTick", delta_tick_off);
            try_overwrite_offset("CServerSideClient::m_FakeClient", fake_client_off);
        }
    }

    // --- m_ControllerEntityIndex ---
    //
    // Pattern: find SpawnPlayer via string ref, then find which vfunc calls it.
    // In the caller, look for: load [this + m_Slot] → ADD reg, 1 → store [this + disp].
    // That disp is m_ControllerEntityIndex.
    {
        auto spawn_player_ref   = engine_mod->FindFunctionFromStringRef("CServerSideClientBase::SpawnPlayer");
        auto spawn_player_range = spawn_player_ref.IsValid() ? engine_mod->GetFunctionRange(spawn_player_ref) : nullptr;
        if (spawn_player_range != nullptr)
        {
            int32_t ctrl_entity_off = -1;
            int32_t game_server_off = -1;

            int32_t slot_off = 0;
            g_pGameData->GetOffset("CServerSideClient::m_Slot", &slot_off);

            for (uintptr_t vfunc : vfuncs_CServerSideClient)
            {
                auto range = engine_mod->GetFunctionRange(vfunc);
                if (range == nullptr)
                    continue;

                bool calls_spawn = false;
                ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    // CALL or JMP (tail call) to SpawnPlayer
                    // Match by resolving target's function range to see if it overlaps SpawnPlayer
                    if ((instr.mnemonic == ZYDIS_MNEMONIC_CALL || instr.mnemonic == ZYDIS_MNEMONIC_JMP)
                        && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                    {
                        auto target       = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                        auto target_range = engine_mod->GetFunctionRange(target);
                        // Match if target IS SpawnPlayer, or target's range contains SpawnPlayer
                        if (target == spawn_player_range->start
                            || (target_range && target_range->start <= spawn_player_range->start && target_range->end >= spawn_player_range->end))
                        {
                            calls_spawn = true;
                            return true;
                        }
                    }
                    return false;
                });

                if (!calls_spawn)
                    continue;

                // Found the caller. Scan for:
                //   m_GameServer: first qword load [this + disp] (mov rbx, [rcx+50h])
                //   m_ControllerEntityIndex: load [this + m_Slot] → INC/ADD 1 → store [this + disp]
                ZydisRegister slot_load_reg = ZYDIS_REGISTER_NONE;

                scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
                    // m_GameServer: first qword load from [this + disp]
                    if (game_server_off == -1
                        && instr.mnemonic == ZYDIS_MNEMONIC_MOV
                        && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                        && operands[1].size == 64
                        && is_any_this_mem(operands[1], saved_this))
                    {
                        game_server_off = static_cast<int32_t>(operands[1].mem.disp.value);
                    }

                    // Detect load of m_Slot: MOV reg32, [this + slot_off]
                    if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                        && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                        && operands[1].size == 32
                        && is_any_this_mem(operands[1], saved_this)
                        && static_cast<int32_t>(operands[1].mem.disp.value) == slot_off)
                    {
                        slot_load_reg = ZydisUtility::GetBaseRegister(operands[0].reg.value);
                    }

                    // Detect ADD reg, 1 or INC reg on the slot register
                    // (no action needed - just ensures slot_load_reg tracks the +1'd value)
                    // Windows: inc eax    Linux: add eax, 1

                    // Detect store: MOV dword [this + disp], reg (where reg held m_Slot + 1)
                    if (slot_load_reg != ZYDIS_REGISTER_NONE
                        && instr.mnemonic == ZYDIS_MNEMONIC_MOV
                        && operands[0].size == 32
                        && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
                        && ZydisUtility::GetBaseRegister(operands[1].reg.value) == slot_load_reg
                        && is_any_this_mem(operands[0], saved_this)
                        && static_cast<int32_t>(operands[0].mem.disp.value) != slot_off)
                    {
                        ctrl_entity_off = static_cast<int32_t>(operands[0].mem.disp.value);
                        return true;
                    }

                    return false;
                });

                if (ctrl_entity_off != -1)
                    break;
            }

            try_overwrite_offset("CServerSideClient::m_GameServer", game_server_off);
            try_overwrite_offset("CServerSideClient::m_ControllerEntityIndex", ctrl_entity_off);
        }
    }

    // --- m_PerfectWorld ---
    //
    // Pattern: find a trivial byte getter in vtable — movzx eax, byte [this + disp] + ret.
    // m_PerfectWorld has the largest disp among all such getters (0x9F8 = 2552).
    // Scan both vtables to find it.
    {
        int32_t perfect_world_off = -1;

        auto scan_trivial_getter = [&](const std::vector<uintptr_t>& vfuncs) {
            for (uintptr_t vfunc : vfuncs)
            {
                auto      range    = engine_mod->GetFunctionRange(vfunc);
                uintptr_t scan_end = range ? range->end : (vfunc + 16);

                int32_t candidate  = -1;
                bool    is_trivial = false;
                int     insn_count = 0;

                ZydisUtility::ScanInstructions(vfunc, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    insn_count++;

                    // First instruction: movzx eax, byte [this + disp]
                    if (insn_count == 1
                        && instr.mnemonic == ZYDIS_MNEMONIC_MOVZX
                        && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                        && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
                        && operands[1].size == 8
                        && operands[1].mem.index == ZYDIS_REGISTER_NONE
                        && operands[1].mem.disp.has_displacement
                        && operands[1].mem.disp.value > 0)
                    {
                        candidate = static_cast<int32_t>(operands[1].mem.disp.value);
                        return false; // check next instruction
                    }

                    // Second instruction: ret
                    if (insn_count == 2 && candidate != -1 && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                    {
                        is_trivial = true;
                    }

                    return true; // stop
                });

                // Keep the one with the largest disp
                if (is_trivial && candidate > perfect_world_off)
                    perfect_world_off = candidate;
            }
        };

        scan_trivial_getter(vfuncs_CServerSideClientBase);
        scan_trivial_getter(vfuncs_CServerSideClient);

        try_overwrite_offset("CServerSideClient::m_PerfectWorld", perfect_world_off);
    }
}

void ResolveNetworkGameServerOffsets()
{
    auto engine_mod = modules::engine;

    // --- CNetworkGameServer::m_ServerState ---
    //
    // Pattern: find the function via "Paused: %s" string ref.
    //   First CMP dword [this + disp], imm in the function.
    //   Linux:   cmp dword ptr [rdi+38h], 2
    //   Windows: cmp dword ptr [rcx+38h], 3
    {
        auto func = engine_mod->FindFunctionFromStringRef("Paused: %s");
        if (func.IsValid())
        {
            auto    range            = engine_mod->GetFunctionRange(func);
            int32_t server_state_off = -1;

            if (range != nullptr)
            {
                ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    if (instr.mnemonic == ZYDIS_MNEMONIC_CMP
                        && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                        && operands[0].size == 32
                        && operands[0].mem.index == ZYDIS_REGISTER_NONE
                        && operands[0].mem.disp.has_displacement
                        && operands[0].mem.disp.value > 0
                        && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
                    {
                        server_state_off = static_cast<int32_t>(operands[0].mem.disp.value);
                        return true;
                    }
                    return false;
                });
            }

            try_overwrite_offset("CNetworkGameServer::m_ServerState", server_state_off);
        }
    }

    // --- CNetworkGameServer::m_vecClients ---
    //
    // Pattern: find com_cmd_users via "<slot:userid:\"name\">\n" string ref.
    //   CUtlVector layout: 32-bit m_Size at [base + disp], 64-bit m_pMemory at [base + disp + 8].
    //   Linux:   [r14+250h] (size), [r14+258h] (ptr)
    //   Windows: [rdi+250h] (size), [rdi+258h] (ptr)
    {
        auto str = engine_mod->FindString("<slot:userid:\"name\">\n", false);
        if (str.IsValid())
        {
            auto refs  = engine_mod->GetReferenceRange(str);
            auto range = refs.empty() ? nullptr : engine_mod->GetFunctionRange(refs.front().source_ip);

            int32_t vec_clients_off = -1;

            if (range != nullptr)
            {
                ZydisRegister last_base_reg = ZYDIS_REGISTER_NONE;
                int64_t       last_disp     = 0;

                ZydisUtility::ScanInstructions(refs.front().source_ip, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    if (instr.mnemonic != ZYDIS_MNEMONIC_MOV
                        || operands[0].type != ZYDIS_OPERAND_TYPE_REGISTER
                        || operands[1].type != ZYDIS_OPERAND_TYPE_MEMORY
                        || operands[1].mem.index != ZYDIS_REGISTER_NONE
                        || !operands[1].mem.disp.has_displacement
                        || operands[1].mem.disp.value <= 0)
                        return false;

                    auto base = ZydisUtility::GetBaseRegister(operands[1].mem.base);
                    if (base == ZYDIS_REGISTER_NONE
                        || base == ZYDIS_REGISTER_RSP
                        || base == ZYDIS_REGISTER_RBP
                        || base == ZYDIS_REGISTER_RIP)
                        return false;

                    if (operands[1].size == 32)
                    {
                        last_base_reg = base;
                        last_disp     = operands[1].mem.disp.value;
                    }
                    else if (operands[1].size == 64
                             && base == last_base_reg
                             && operands[1].mem.disp.value == last_disp + 8)
                    {
                        vec_clients_off = static_cast<int32_t>(last_disp);
                        return true;
                    }

                    return false;
                });
            }

            try_overwrite_offset("CNetworkGameServer::m_vecClients", vec_clients_off);
        }
    }

    // --- CNetworkGameServer::m_MapName ---
    //
    // Pattern: find via "map/mapname" string ref.
    //   Last qword load [this + disp] before the "map/mapname" LEA.
    //   Windows: mov rax, [rdi+150h]   then   lea rdx, "map/mapname"
    //   Linux:   mov rdx, [r12+150h]   then   lea rsi, "map/mapname"
    {
        auto str = engine_mod->FindString("map/mapname", false, true);
        if (str.IsValid())
        {
            auto func  = engine_mod->FindAllFunctionsFromStringRefs({"map/mapname", "::ExecGameTypeCfg"});
            auto range = func.empty() ? nullptr : (func[0] > 0 ? engine_mod->GetFunctionRange(func[0]) : nullptr);

            int32_t map_name_off   = -1;
            int32_t last_qword_off = -1;

            if (range != nullptr)
            {
                ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    // Track qword loads from [non-stack reg + disp]
                    if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                        && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                        && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
                        && operands[1].size == 64
                        && operands[1].mem.index == ZYDIS_REGISTER_NONE
                        && operands[1].mem.disp.has_displacement
                        && operands[1].mem.disp.value > 0)
                    {
                        auto base = ZydisUtility::GetBaseRegister(operands[1].mem.base);
                        if (base != ZYDIS_REGISTER_NONE
                            && base != ZYDIS_REGISTER_RSP
                            && base != ZYDIS_REGISTER_RBP
                            && base != ZYDIS_REGISTER_RIP)
                        {
                            last_qword_off = static_cast<int32_t>(operands[1].mem.disp.value);
                        }
                    }

                    // When we hit the "map/mapname" LEA, capture
                    if (instr.mnemonic == ZYDIS_MNEMONIC_LEA && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                    {
                        if (ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip) == str.GetPtr())
                        {
                            map_name_off = last_qword_off;
                            return true;
                        }
                    }

                    return false;
                });
            }

            try_overwrite_offset("CNetworkGameServer::m_MapName", map_name_off);
        }
    }
}
