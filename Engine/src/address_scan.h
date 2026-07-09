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

#ifndef MS_ROOT_ADDRESS_SCAN_H
#define MS_ROOT_ADDRESS_SCAN_H

#include "memory/zydis_utility.h"
#include "module.h"

#include <algorithm>
#include <cstdint>
#include <ranges>
#include <vector>

// Primitives shared by the gamedata auto-resolvers.
//
// Valve's compiler rearranges this code every build. Four things keep breaking positional scans,
// and each has a primitive here:
//
//   1. Basic blocks get reordered      -> match on structure (FindUtlVectors), never on "first hit
//                                         after the anchor".
//   2. A check gets outlined           -> ScanWithLeaves follows relative calls into leaf helpers.
//   3. A function gets inlined into N  -> AnchorFunctions returns every copy so the caller can
//      callers, duplicating the anchor    require them to agree (see ResolveVote).
//   4. A vtable is referenced as       -> MatchesVTableRef accepts both forms.
//      `lea base` + `add 0x10`
namespace address_scan
{

#ifdef PLATFORM_WINDOWS
inline constexpr ZydisRegister kThisReg = ZYDIS_REGISTER_RCX;
#else
inline constexpr ZydisRegister kThisReg = ZYDIS_REGISTER_RDI;
#endif

// A register usable as an object base: a real GPR, not the stack or the instruction pointer.
inline bool IsObjectBase(ZydisRegister reg)
{
    const auto base = ZydisUtility::GetBaseRegister(reg);
    return base != ZYDIS_REGISTER_NONE
           && base != ZYDIS_REGISTER_RSP
           && base != ZYDIS_REGISTER_RBP
           && base != ZYDIS_REGISTER_RIP;
}

// [base + disp] with a positive displacement and no index.
inline bool IsFieldMem(const ZydisDecodedOperand& op, ZydisRegister expected_base)
{
    return op.type == ZYDIS_OPERAND_TYPE_MEMORY
           && ZydisUtility::GetBaseRegister(op.mem.base) == expected_base
           && op.mem.index == ZYDIS_REGISTER_NONE
           && op.mem.disp.has_displacement
           && op.mem.disp.value > 0;
}

// Every distinct function that references `str`. A string that used to have one reference grows
// more as soon as its enclosing function is inlined somewhere, and picking refs.front() then
// silently switches which function you are reading.
inline std::vector<const CModule::FunctionEntry*> AnchorFunctions(CModule* mod, CAddress str)
{
    std::vector<const CModule::FunctionEntry*> ranges{};

    if (!str.IsValid())
        return ranges;

    for (const auto& ref : mod->GetReferenceRange(str))
    {
        const auto* range = mod->GetFunctionRange(ref.source_ip);
        if (range == nullptr)
            continue;

        if (std::ranges::none_of(ranges, [&](const auto* r) { return r->start == range->start; }))
            ranges.emplace_back(range);
    }

    return ranges;
}

// The subset of `vfuncs` whose bodies reference `str`.
inline std::vector<const CModule::FunctionEntry*> AnchorVFunctions(CModule* mod, const std::vector<uintptr_t>& vfuncs, CAddress str)
{
    std::vector<const CModule::FunctionEntry*> ranges{};

    if (!str.IsValid())
        return ranges;

    const auto refs = mod->GetReferenceRange(str);

    for (const uintptr_t vfunc : vfuncs)
    {
        const auto* range = mod->GetFunctionRange(vfunc);
        if (range == nullptr)
            continue;

        const bool referenced = std::ranges::any_of(refs, [&](const CModule::ReferenceEntry& ref) {
            return ref.source_ip > range->start && ref.source_ip < range->end;
        });

        if (referenced && std::ranges::none_of(ranges, [&](const auto* r) { return r->start == range->start; }))
            ranges.emplace_back(range);
    }

    return ranges;
}

// Scan [start, end); if the callback never fires, follow relative calls into leaf functions no
// bigger than `max_leaf_size` and scan those too. Leaves receive `this` in kThisReg.
//
// Newer builds keep outlining trivial predicates - CServerSideClient::IsActive() became an 8-byte
// `cmp dword [rdi+64h], 6 ; setz al ; retn` - so the pattern is simply not in the caller anymore.
template <typename Callback>
inline bool ScanWithLeaves(CModule* mod, uintptr_t start, uintptr_t end, size_t max_leaf_size, Callback callback)
{
    bool found = false;

    ZydisUtility::ScanInstructions(start, end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        found = callback(ip, instr, operands);
        return found;
    });

    if (found)
        return true;

    ZydisUtility::ScanInstructions(start, end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        if (instr.mnemonic != ZYDIS_MNEMONIC_CALL || !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            return false;

        const auto target = ZydisUtility::ResolveCallTarget(&instr, operands, ip);
        if (target == 0)
            return false;

        const auto* leaf = mod->GetFunctionRange(target);
        if (leaf == nullptr || leaf->end - leaf->start > max_leaf_size)
            return false;

        ZydisUtility::ScanInstructions(leaf->start, leaf->end, [&](uintptr_t lip, const ZydisDecodedInstruction& linstr, const ZydisDecodedOperand* lops) -> bool {
            found = callback(lip, linstr, lops);
            return found;
        });

        return found;
    });

    return found;
}

// Offsets of every CUtlVector<T> in [start, end) whose elements are `elem_size` bytes wide.
//
//   mov  r32, [base + X]        ; m_Size
//   test r32, r32
//   jle  done
//   mov  r64, [base + X + 8]    ; m_pMemory
//   ...
//   cmp  eax, [r64 + idx*4]     ; GCC indexes by element size
//   add  r64, 4                 ; MSVC bumps the pointer by it
//
// The element stride is what separates a CUtlVector<int> from a CUtlVector<T*> sitting on the same
// object, which no amount of "first pair wins" ordering can do. Returns every distinct offset so
// the caller can reject an ambiguous function outright.
//
// max_pair_gap bounds how far the m_pMemory load may trail the m_Size load. The default 8 assumes
// the tight `size; test; jle; ptr` idiom and keeps unrelated {dwordX, qwordX+8} coincidences from
// pairing. Raise it when the access sits in a bounds-checked loop that hoists the size load well
// ahead of the pointer load (com_cmd_users walking m_vecClients); the stride check still guarantees
// correctness, so a wide gap only risks ambiguity, never a wrong offset.
inline std::vector<int32_t> FindUtlVectors(uintptr_t start, uintptr_t end, uint8_t elem_size, int max_pair_gap = 8)
{
    // Instructions tolerated between m_pMemory and its first index.
    constexpr int kMaxStrideGap = 16;

    std::vector<int32_t> results{};

    ZydisRegister size_base = ZYDIS_REGISTER_NONE;
    int64_t       size_disp = 0;
    int           size_ttl  = 0;

    ZydisRegister mem_reg  = ZYDIS_REGISTER_NONE;
    int64_t       mem_disp = 0;
    int           mem_ttl  = 0;

    auto commit = [&](int64_t disp) {
        const auto value = static_cast<int32_t>(disp);
        if (std::ranges::find(results, value) == results.end())
            results.emplace_back(value);
    };

    ZydisUtility::ScanInstructions(start, end, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        if (size_ttl > 0)
            size_ttl--;

        // Pending m_pMemory: prove the element stride. Padding does not count against the budget.
        if (mem_reg != ZYDIS_REGISTER_NONE && instr.mnemonic != ZYDIS_MNEMONIC_NOP)
        {
            if (mem_ttl-- <= 0)
            {
                mem_reg = ZYDIS_REGISTER_NONE;
            }
            else if (instr.mnemonic == ZYDIS_MNEMONIC_ADD
                     && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                     && ZydisUtility::GetBaseRegister(operands[0].reg.value) == mem_reg
                     && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                     && operands[1].imm.value.u == elem_size)
            {
                commit(mem_disp);
                mem_reg = ZYDIS_REGISTER_NONE;
            }
            else
            {
                for (uint8_t i = 0; i < instr.operand_count_visible; i++)
                {
                    const auto& op = operands[i];

                    // [m_pMemory + index * scale]: the scale is the element size
                    if (op.type == ZYDIS_OPERAND_TYPE_MEMORY
                        && op.mem.index != ZYDIS_REGISTER_NONE
                        && ZydisUtility::GetBaseRegister(op.mem.base) == mem_reg)
                    {
                        if (op.mem.scale == elem_size)
                            commit(mem_disp);

                        mem_reg = ZYDIS_REGISTER_NONE;
                        break;
                    }

                    // m_pMemory clobbered before it was ever indexed
                    if (op.type == ZYDIS_OPERAND_TYPE_REGISTER
                        && (op.actions & ZYDIS_OPERAND_ACTION_MASK_WRITE) != 0
                        && ZydisUtility::GetBaseRegister(op.reg.value) == mem_reg)
                    {
                        mem_reg = ZYDIS_REGISTER_NONE;
                        break;
                    }
                }
            }
        }

        if (instr.mnemonic != ZYDIS_MNEMONIC_MOV
            || operands[0].type != ZYDIS_OPERAND_TYPE_REGISTER
            || operands[1].type != ZYDIS_OPERAND_TYPE_MEMORY
            || operands[1].mem.index != ZYDIS_REGISTER_NONE
            || !operands[1].mem.disp.has_displacement
            || operands[1].mem.disp.value <= 0
            || !IsObjectBase(operands[1].mem.base))
            return false;

        const auto base = ZydisUtility::GetBaseRegister(operands[1].mem.base);

        // Step 1: 32-bit load -> candidate m_Size
        if (operands[1].size == 32)
        {
            size_base = base;
            size_disp = operands[1].mem.disp.value;
            size_ttl  = max_pair_gap;
        }
        // Step 2: 64-bit load from the same base at m_Size + 8 -> m_pMemory
        else if (operands[1].size == 64
                 && size_ttl > 0
                 && base == size_base
                 && operands[1].mem.disp.value == size_disp + 8)
        {
            mem_reg   = ZydisUtility::GetBaseRegister(operands[0].reg.value);
            mem_disp  = size_disp;
            mem_ttl   = kMaxStrideGap;
            size_base = ZYDIS_REGISTER_NONE;
        }

        return false; // never stop early - collect every match so ambiguity is visible
    });

    return results;
}

// `mov eax, [this + X] ; retn` -> X, else -1.
//
// Outlining is not only a hazard: a two-instruction leaf accessor is the single most stable thing
// a field offset can be read from. CNetworkGameServer::GetClientCount() is exactly this.
inline int32_t LeafFieldGetter(CModule* mod, uintptr_t func, uint16_t operand_size = 32, size_t max_size = 0x10)
{
    const auto* range = mod->GetFunctionRange(func);
    if (range == nullptr || range->end - range->start > max_size)
        return -1;

    int32_t disp  = -1;
    int     index = 0;

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        if (instr.mnemonic == ZYDIS_MNEMONIC_ENDBR64)
            return false;

        if (index++ == 0)
        {
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && ZydisUtility::GetBaseRegister(operands[0].reg.value) == ZYDIS_REGISTER_RAX
                && operands[1].size == operand_size
                && IsFieldMem(operands[1], kThisReg))
            {
                disp = static_cast<int32_t>(operands[1].mem.disp.value);
                return false;
            }

            return true;
        }

        // Anything but an immediate `ret` means this leaf does more than read one field
        if (instr.mnemonic != ZYDIS_MNEMONIC_RET)
            disp = -1;

        return true;
    });

    return disp;
}

// True if a RIP-relative LEA target names `vtable_ptr`.
//
// GetVirtualTableByName returns the address objects store, which on the Itanium ABI is the vtable
// symbol + 0x10 (past offset-to-top and typeinfo). Older GCC LEA'd that directly; newer builds LEA
// the symbol and then `add rax, 10h`. MSVC always names the pointer itself.
inline bool MatchesVTableRef(uintptr_t target, uintptr_t vtable_ptr)
{
    return target == vtable_ptr || target == vtable_ptr - 2 * sizeof(void*);
}

} // namespace address_scan

#endif // MS_ROOT_ADDRESS_SCAN_H
