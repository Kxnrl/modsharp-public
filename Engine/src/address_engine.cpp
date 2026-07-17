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
#include "address_scan.h"
#include "logging.h"
#include "memory/zydis_utility.h"

void ResolveServerSideClientOffsets()
{
    auto engine_mod = modules::engine;

    auto vfuncs_CServerSideClientBase = engine_mod->GetVFunctionsFromVTable("CServerSideClientBase");
    auto vfuncs_CServerSideClient     = engine_mod->GetVFunctionsFromVTable("CServerSideClient");

    using address_scan::kThisReg;
    using address_scan::IsFieldMem;

    // Every vfunc referencing the string, not just the first. Once a function is inlined into a
    // second caller, its string ref appears in both and "the first vfunc that mentions it" starts
    // reading whichever the vtable happens to list first.
    auto find_vfunc_anchors = [&](const std::vector<uintptr_t>& vfuncs, const char* str_content, bool exact = false) {
        auto str = engine_mod->FindString(str_content, false, exact);
        if (!str.IsValid())
            WARN("GameData auto-resolve: string \"%s\" not found.", str_content);

        auto anchors = address_scan::AnchorVFunctions(engine_mod, vfuncs, str);
        if (anchors.empty() && str.IsValid())
            WARN("GameData auto-resolve: no vfunc references \"%s\".", str_content);

        return std::pair{str, anchors};
    };

    auto is_this_mem = [](const ZydisDecodedOperand& mem_op, ZydisRegister expected_base) {
        return IsFieldMem(mem_op, expected_base);
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

    // Offsets of every qword field on `this` that is loaded and then dereferenced at +0, i.e. used
    // as an object (vtable load ahead of a virtual call). A field merely passed to a function -
    // m_ConVars into KeyValues::GetInt - never gets dereferenced here, which is what separates
    // m_NetChannel from "the first qword this function happens to load".
    auto find_dereferenced_this_fields = [&](const CModule::FunctionEntry* range) {
        constexpr int kMaxUseGap = 8;

        std::vector<int32_t> results{};

        ZydisRegister pending_reg = ZYDIS_REGISTER_NONE;
        int32_t       pending_off = -1;
        int           pending_ttl = 0;

        scan_with_this(range, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
            if (pending_reg != ZYDIS_REGISTER_NONE)
            {
                if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                    && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                    && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[1].size == 64
                    && operands[1].mem.index == ZYDIS_REGISTER_NONE
                    && operands[1].mem.disp.value == 0
                    && ZydisUtility::GetBaseRegister(operands[1].mem.base) == pending_reg)
                {
                    if (std::ranges::find(results, pending_off) == results.end())
                        results.emplace_back(pending_off);

                    pending_reg = ZYDIS_REGISTER_NONE;
                }
                // A call clobbers it (and means it was only an argument), so does any write to it
                else if (--pending_ttl <= 0
                         || instr.mnemonic == ZYDIS_MNEMONIC_CALL
                         || (operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                             && (operands[0].actions & ZYDIS_OPERAND_ACTION_MASK_WRITE) != 0
                             && ZydisUtility::GetBaseRegister(operands[0].reg.value) == pending_reg))
                {
                    pending_reg = ZYDIS_REGISTER_NONE;
                }
            }

            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].size == 64
                && is_any_this_mem(operands[1], saved_this))
            {
                pending_reg = ZydisUtility::GetBaseRegister(operands[0].reg.value);
                pending_off = static_cast<int32_t>(operands[1].mem.disp.value);
                pending_ttl = kMaxUseGap;
            }

            return false; // collect every match
        });

        return results;
    };

    // --- m_NetChannel, m_Name, m_UserId (from SetRate) ---
    //
    // m_NetChannel is the qword field on this that gets used as an object:
    //
    //   mov  rdi, [this+58h]     ; m_NetChannel
    //   test rdi, rdi
    //   jz   short done
    //   mov  rax, [rdi]          ; vtable load  <- this is the proof
    //   call qword ptr [rax+28h]
    //
    // "First qword load from this" is not enough: SetRate also gets inlined into the vfunc that
    // reads "rate" out of the userinfo KeyValues, and there the first such load is m_ConVars (272),
    // which is only handed to KeyValues::GetInt and never dereferenced. Both copies of SetRate are
    // scanned and must agree.
    //
    // m_Name / m_UserId are the last qword load and last MOVZX word load before the format string.
    {
        auto [str, anchors] = find_vfunc_anchors(vfuncs_CServerSideClient, "Client %d '%s' setting rate to %d\n");

        ResolveVote net_channel{};
        ResolveVote name{};
        ResolveVote userid{};

        for (const auto* range : anchors)
        {
            VoteCandidates(net_channel, find_dereferenced_this_fields(range));

            int32_t last_qword_off   = -1;
            int32_t last_movzx_w_off = -1;
            int32_t name_off         = -1;
            int32_t userid_off       = -1;

            scan_with_this(range, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister saved_this) -> bool {
                if (saved_this == ZYDIS_REGISTER_NONE)
                    return false;

                // Track last qword load from saved_this (excluding m_NetChannel repeats)
                if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                    && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                    && operands[1].size == 64
                    && is_this_mem(operands[1], saved_this)
                    && static_cast<int32_t>(operands[1].mem.disp.value) != net_channel.Value())
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

            name.Add(name_off);
            userid.Add(userid_off);
        }

        try_overwrite_offset("CServerSideClient::m_NetChannel", net_channel);
        try_overwrite_offset("CServerSideClient::m_Name", name);
        try_overwrite_offset("CServerSideClient::m_UserId", userid);
    }

    // --- m_SteamId, m_ConVars, m_Slot (from Create) ---
    //
    // Pattern (both platforms):
    //   1. Before "userinfo" LEA: first qword store [this + disp] → m_SteamId
    //   2. After "userinfo" LEA: first qword store [this + disp] → m_ConVars
    //   3. Last dword load from [this + disp] in function → m_Slot
    {
        auto [str, anchors] = find_vfunc_anchors(vfuncs_CServerSideClient, "userinfo", true);

        ResolveVote steamid{};
        ResolveVote convars{};
        ResolveVote slot{};

        for (const auto* range : anchors)
        {
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

            steamid.Add(steamid_off);
            convars.Add(convars_off);
            slot.Add(slot_off);
        }

        try_overwrite_offset("CServerSideClient::m_SteamId", steamid);
        try_overwrite_offset("CServerSideClient::m_ConVars", convars);
        try_overwrite_offset("CServerSideClient::m_Slot", slot);
    }

    // --- m_SignonState (from ProcessMove) ---
    //
    // Primary: the IsActive() check is inlined, so the first CMP dword [this + disp], 6 wins.
    //   Windows: cmp dword ptr [rcx+64h], 6
    //   Linux:   cmp dword ptr [rdi+64h], 6
    //
    // Fallback: newer Linux builds outline it, leaving no CMP in the vfunc at all -
    //   call sub_523D30   ->   cmp dword ptr [rdi+64h], 6 ; setz al ; retn
    {
        auto [str, anchors] = find_vfunc_anchors(vfuncs_CServerSideClient, "Too many move messages");

        ResolveVote signon{};

        // Matches `cmp dword [this + disp], 6` -> disp, else -1.
        auto match_signon_cmp = [&](const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, ZydisRegister this_reg) -> int32_t {
            if (instr.mnemonic != ZYDIS_MNEMONIC_CMP
                || operands[0].size != 32
                || operands[1].type != ZYDIS_OPERAND_TYPE_IMMEDIATE
                || operands[1].imm.value.s != 6
                || !is_this_mem(operands[0], this_reg))
                return -1;

            return static_cast<int32_t>(operands[0].mem.disp.value);
        };

        for (const auto* range : anchors)
        {
            int32_t signon_off = -1;

            // Inlined IsActive(): CMP directly in the vfunc. Outlined: ScanWithLeaves follows the
            // relative call into the 8-byte `cmp dword [rdi+64h], 6 ; setz al ; retn` helper.
            address_scan::ScanWithLeaves(engine_mod, range->start, range->end, 0x10,
                                         [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                                             signon_off = match_signon_cmp(instr, operands, kThisReg);
                                             return signon_off != -1;
                                         });

            signon.Add(signon_off);
        }

        try_overwrite_offset("CServerSideClient::m_SignonState", signon);
    }

    // --- m_IsHLTV ---
    //
    // Pattern (both platforms):
    //   First CMP byte [this_reg + disp], 0 in the function
    //   Linux:   cmp byte ptr [rdi+142h], 0       (immediate)
    //   Windows: cmp [rcx+322], dil               (register zeroed by xor edi,edi)
    {
        auto [str, anchors] = find_vfunc_anchors(vfuncs_CServerSideClient, "TV client has no downstream TV sink\n");

        ResolveVote ishltv{};

        for (const auto* range : anchors)
        {
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

            ishltv.Add(ishlv_off);
        }

        try_overwrite_offset("CServerSideClient::m_IsHLTV", ishltv);
    }

    // --- m_FullyAuthenticated (from OnValidateAuthTicketResponse) ---
    //
    // The client's authentication flag is set on success: mov byte [client + 9FAh], 1. Note the
    // base is the looked-up *client*, not `this` (the steam auth handler), so this is not a
    // this-relative store.
    //
    // The old anchors ("STEAM USERID validated" + "SV: Canceling ...AuthTicketCanceled") stopped
    // co-locating once build 24116939 split the function - the intersection requires both in one
    // body and the Canceling log was rewritten out. "SV: OnValidateAuthTicketResponse duplicate
    // authentication" survives and is referenced by exactly this one function.
    //
    // The flag is the only `mov byte [reg + disp], 1` in the function on both builds, so collecting
    // distinct such offsets yields a single unambiguous candidate.
    if (auto func = engine_mod->FindFunctionFromStringRef("SV: OnValidateAuthTicketResponse duplicate authentication"); func.IsValid())
    {
        auto range = engine_mod->GetFunctionRange(func);

        ResolveVote fully_auth{};

        if (range != nullptr)
        {
            std::vector<int32_t> candidates{};

            ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                    && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[0].size == 8
                    && operands[0].mem.index == ZYDIS_REGISTER_NONE
                    && operands[0].mem.disp.has_displacement
                    && operands[0].mem.disp.value > 0
                    && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                    && operands[1].imm.value.u == 1
                    && address_scan::IsObjectBase(operands[0].mem.base))
                {
                    auto disp = static_cast<int32_t>(operands[0].mem.disp.value);
                    if (std::ranges::find(candidates, disp) == candidates.end())
                        candidates.emplace_back(disp);
                }

                return false;
            });

            VoteCandidates(fully_auth, candidates);
        }

        try_overwrite_offset("CServerSideClient::m_FullyAuthenticated", fully_auth);
    }

    // --- m_vecLoadedSpawnGroups ---
    //
    // CServerSideClientBase::OnSpawnGroupDeactivate drops the handle out of the vector:
    //
    //   Linux:    mov  edi, [rbx+178h]     ; m_Size
    //             test edi, edi
    //             jle  short done
    //             mov  rcx, [rbx+180h]     ; m_pMemory (m_Size + 8)
    //             ...
    //             cmp  eax, [rcx+rdx*4]    ; SpawnGroup_t is 4 bytes
    //
    //   Windows:  mov  edx, [rsi+178h]
    //             ...
    //             mov  rcx, [rsi+180h]
    //             cmp  [rcx], eax
    //             add  rcx, 4              ; same 4-byte stride, walked by pointer bump
    //
    // The same function also pushes a delayed call into CUtlVector<CDelayedCall*> at 0xB48, whose
    // {m_Size, m_pMemory} loads interleave with these across builds, so "first pair after the
    // string" latched onto 0xB48 (2888). SpawnGroup_t is 4 bytes and CDelayedCall* is 8, so the
    // element stride tells them apart - which is exactly what FindUtlVectors keys on. Requiring a
    // single 4-byte vector in the function also gives us the uniqueness needed to trust it.
    {
        auto str     = engine_mod->FindString("%s:  Not sending unload group to client '%s' due to not being sent\n", true);
        auto anchors = address_scan::AnchorFunctions(engine_mod, str);
        if (anchors.empty())
        {
            WARN("GameData auto-resolve: m_vecLoadedSpawnGroups anchor not found.");
        }

        ResolveVote spawn_groups{};
        for (const auto* range : anchors)
            VoteCandidates(spawn_groups, address_scan::FindUtlVectors(range->start, range->end, 4));

        try_overwrite_offset("CServerSideClient::m_vecLoadedSpawnGroups", spawn_groups);
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

            // Both are positional first-match heuristics (fast-path store / trivial getter), so they
            // are treated as cross-checks: they confirm gamedata and warn on disagreement rather than
            // silently overwriting a hand-verified value.
            ResolveVote delta_tick{};
            delta_tick.Add(delta_tick_primary);
            ResolveVote fake_client{};
            fake_client.Add(fake_client_off);

            try_overwrite_offset("CServerSideClient::m_nDeltaTick", delta_tick);
            try_overwrite_offset("CServerSideClient::m_FakeClient", fake_client);
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

            // m_ControllerEntityIndex is pinned structurally (slot load -> +1 -> store), so it can
            // overwrite; m_GameServer is just the first qword field and only cross-checks gamedata.
            ResolveVote ctrl_entity{};
            ctrl_entity.Add(ctrl_entity_off, /*unique*/ true);
            ResolveVote game_server{};
            game_server.Add(game_server_off);

            try_overwrite_offset("CServerSideClient::m_GameServer", game_server);
            try_overwrite_offset("CServerSideClient::m_ControllerEntityIndex", ctrl_entity);
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

        // The highest-offset trivial byte getter is m_PerfectWorld by construction; distinct getter
        // offsets can't tie, so the winner is unambiguous and may overwrite gamedata.
        ResolveVote perfect_world{};
        perfect_world.Add(perfect_world_off, /*unique*/ true);
        try_overwrite_offset("CServerSideClient::m_PerfectWorld", perfect_world);
    }
}

void ResolveNetworkGameServerOffsets()
{
    auto engine_mod = modules::engine;

    using address_scan::kThisReg;

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
            auto        range = engine_mod->GetFunctionRange(func);
            ResolveVote server_state{};

            if (range != nullptr)
            {
                int32_t server_state_off = -1;
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
                server_state.Add(server_state_off);
            }

            try_overwrite_offset("CNetworkGameServer::m_ServerState", server_state);
        }
    }

    // --- CNetworkGameServer::m_vecClients ---
    //
    // Pattern: find com_cmd_users via "<slot:userid:\"name\">\n" string ref.
    //
    // Older builds inline the vector walk:
    //   CUtlVector layout: 32-bit m_Size at [base + disp], 64-bit m_pMemory at [base + disp + 8].
    //   Linux:   [r14+250h] (size), [r14+258h] (ptr)
    //   Windows: [rdi+250h] (size), [rdi+258h] (ptr)
    //
    // Newer ones call out-of-line accessors, so com_cmd_users holds no vector access at all:
    //   CNetworkGameServer::GetClientCount():  mov eax, [this+248h] ; retn
    //
    // Prefer the leaf getter - a two-instruction `mov eax, [this + disp]; ret` cannot be
    // confused with anything else - and fall back to the inlined pair.
    {
        auto str = engine_mod->FindString("<slot:userid:\"name\">\n", false);
        if (str.IsValid())
        {
            auto refs  = engine_mod->GetReferenceRange(str);
            auto range = refs.empty() ? nullptr : engine_mod->GetFunctionRange(refs.front().source_ip);

            ResolveVote vec_clients{};

            // Primary: com_cmd_users calls GetClientCount(), a `mov eax, [this+X] ; retn` leaf. That
            // leaf getter is the single most stable place to read m_Size's offset from, so it may
            // overwrite gamedata.
            if (range != nullptr)
            {
                ZydisUtility::ScanInstructions(refs.front().source_ip, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    if (instr.mnemonic != ZYDIS_MNEMONIC_CALL)
                        return false;

                    auto target = ZydisUtility::ResolveCallTarget(&instr, operands, ip);
                    if (target == 0)
                        return false;

                    if (auto disp = address_scan::LeafFieldGetter(engine_mod, target); disp != -1)
                    {
                        vec_clients.Add(disp, /*unique*/ true);
                        return true;
                    }

                    return false;
                });
            }

            // Fallback: builds that inline the vector walk (notably Windows). m_vecClients holds
            // pointers, so its stride is 8. The walk is a bounds-checked loop that hoists the m_Size
            // load ahead of the m_pMemory load, so use a wide pair gap; com_cmd_users touches only
            // this one vector, so the stride check alone keeps it unambiguous.
            if (vec_clients.Empty() && range != nullptr)
                VoteCandidates(vec_clients, address_scan::FindUtlVectors(refs.front().source_ip, range->end, 8, /*max_pair_gap*/ 64));

            try_overwrite_offset("CNetworkGameServer::m_vecClients", vec_clients);
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

            // Positional (last qword load before the string LEA), so cross-check only.
            ResolveVote map_name{};
            map_name.Add(map_name_off);
            try_overwrite_offset("CNetworkGameServer::m_MapName", map_name);
        }
    }
}
