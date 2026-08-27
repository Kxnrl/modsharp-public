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
#include "module.h"

#include "cstrike/interface/ICvar.h"
#include "cstrike/interface/IGameSystem.h"
#include "cstrike/schema.h"
#include "cstrike/type/CCSScript.h"
#include "cstrike/type/CEntityClass.h"

#include <ranges>

class CBaseGameSystemFactory;

void FindCEntityIdentity_SetEntityName()
{
    const auto set_entity_name_functions = modules::server->FindAllFunctionsFromStringRefs({"CEntityIdentity::SetEntityName called, but there is no entity name string table pointer!\n"});
    if (set_entity_name_functions.empty()) [[unlikely]]
    {
        FatalError("Failed to find CEntityIdentity::SetEntityName");
        return;
    }

    const auto point_script_set_entity_name = modules::server->FindFunctionFromStringRefs({"SetEntityName",
                                                                                           "(name: string)"});
    if (!point_script_set_entity_name.IsValid())
    {
        FatalError("Failed to find CPointScript::SetEntityName");
        return;
    }

    const auto range = modules::server->GetFunctionRange(point_script_set_entity_name);
    if (!range)
    {
        FatalError("Failed to get function range for CPointScript::SetEntityName");
        return;
    }
    bool found = false;
    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
        if (instr.mnemonic == ZYDIS_MNEMONIC_CALL && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
        {
            uintptr_t call_dest = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
            for (const auto& func : set_entity_name_functions)
            {
                if (call_dest == func)
                {
                    found = true;

                    FLOG("Found CEntityIdentity::SetEntityName at server+0x%llx", func - modules::server->Base());
                    address::server::CEntityIdentity_SetEntityName = reinterpret_cast<address::server::CEntityIdentity_SetEntityName_t>(func);
                    return true;
                }
            }
        }
        return false;
    });
    if (!found)
        FatalError("Failed to find CEntityIdentity::SetEntityName call in CPointScript::SetEntityName");
}

void FindGameSystemFactory()
{
    const auto function_address = modules::server->FindFunctionFromStringRef("Game System %s is defined twice!\n");
    if (!function_address.IsValid()) [[unlikely]]
    {
        FatalError("Failed to find IGameSystem::InitAllSystems");
        return;
    }

    auto range = modules::server->GetFunctionRange(function_address);
    if (range == nullptr)
    {
        FatalError("Failed to get function range for IGameSystem::InitAllSystems");
        return;
    }

    int decode_count = 0;

    std::uintptr_t pending_addr = 0;
    ZydisRegister  pending_reg  = ZYDIS_REGISTER_NONE;

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        decode_count++;
        if (decode_count > 50)
        {
            FatalError("Found IGameSystem::InitAllSystems but failed to find instruction sequence within limit(50 times)");
            return true;
        }
        // mov reg, cs:CBaseGameSystemFactory::sm_pFirst
        if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE) && instr.operand_count_visible == 2)
        {
            const auto& dst = operands[0];
            const auto& src = operands[1];

            if (dst.type == ZYDIS_OPERAND_TYPE_REGISTER && src.type == ZYDIS_OPERAND_TYPE_MEMORY)
            {
                pending_reg  = dst.reg.value;
                pending_addr = ip + instr.length + src.mem.disp.value;
            }
        }
        // test reg, reg
        else if (pending_reg != ZYDIS_REGISTER_NONE && instr.mnemonic == ZYDIS_MNEMONIC_TEST && instr.operand_count_visible == 2)
        {
            const auto& op1 = operands[0];
            const auto& op2 = operands[1];

            if (op1.type == ZYDIS_OPERAND_TYPE_REGISTER && op1.reg.value == pending_reg && op2.type == ZYDIS_OPERAND_TYPE_REGISTER && op2.reg.value == pending_reg)
            {
                auto temp  = reinterpret_cast<CBaseGameSystemFactory**>(pending_addr);
                auto first = *temp;
                if (first == nullptr)
                {
                    WARN("Candidate at server+0x%llx rejected: factory pointer is null", pending_addr - modules::server->Base());
                    pending_reg  = ZYDIS_REGISTER_NONE;
                    pending_addr = 0;
                    return false;
                }

                if (!modules::server->IsPointerDerivedFrom(first->m_pInstance, "IGameSystem"))
                {
                    WARN("Candidate at server+0x%llx rejected: m_pInstance is not derived from IGameSystem", pending_addr - modules::server->Base());
                    pending_reg  = ZYDIS_REGISTER_NONE;
                    pending_addr = 0;
                    return false;
                }

                FLOG("Found CBaseGameSystemFactory::sm_ppFirst at sever+0x%llx", pending_addr - modules::server->Base());
                CBaseGameSystemFactory::sm_ppFirst = temp;
                return true;
            }

            pending_reg = ZYDIS_REGISTER_NONE;
        }
        else if (pending_reg != ZYDIS_REGISTER_NONE && instr.operand_count_visible > 0)
        {
            if (operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER && operands[0].reg.value == pending_reg && (operands[0].actions & ZYDIS_OPERAND_ACTION_WRITE))
            {
                pending_reg  = ZYDIS_REGISTER_NONE;
                pending_addr = 0;
            }
        }

        return false;
    });
}

void ResolveCEntityClassEntityListOffset()
{
    const auto find_by_classname = g_pGameData->GetAddress<std::uintptr_t>("CGameEntitySystem::FindByClassname");
    if (find_by_classname == 0) [[unlikely]]
    {
        FatalError("Failed to resolve CEntityClass entity list offset: CGameEntitySystem::FindByClassname is null");
        return;
    }

    const auto* wrapper_range = modules::server->GetFunctionRange(find_by_classname);
    if (!wrapper_range) [[unlikely]]
    {
        FatalError("Failed to get function range for CGameEntitySystem::FindByClassname");
        return;
    }

    std::uintptr_t next_by_classname = 0;
    ZydisUtility::ScanInstructions(wrapper_range->start, wrapper_range->end, [&](std::uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
        if (instr.opcode == 0xE8 && instr.mnemonic == ZYDIS_MNEMONIC_CALL)
        {
            if (const auto target = ZydisUtility::ResolveCallTarget(&instr, operands, ip))
            {
                next_by_classname = target;
            }
        }
        return false;
    });

    if (next_by_classname == 0) [[unlikely]]
    {
        FatalError("Failed to find CEntityIterator::NextByClassname call in CGameEntitySystem::FindByClassname");
        return;
    }

    const auto* iter_range = modules::server->GetFunctionRange(next_by_classname);
    if (!iter_range) [[unlikely]]
    {
        FatalError("Failed to get function range for CEntityIterator::NextByClassname");
        return;
    }

#ifdef PLATFORM_WINDOWS
    constexpr ZydisRegister kArg0Register = ZYDIS_REGISTER_RCX;
#else
    constexpr ZydisRegister kArg0Register = ZYDIS_REGISTER_RDI;
#endif

    ZydisRegister class_reg = ZYDIS_REGISTER_NONE;
    std::uint32_t offset    = 0;

    ZydisUtility::ScanInstructions(iter_range->start, iter_range->end, [&](std::uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) {
        if (instr.mnemonic != ZYDIS_MNEMONIC_MOV || instr.operand_count_visible != 2)
        {
            return false;
        }

        const auto& dst = operands[0];
        const auto& src = operands[1];

        if (dst.type != ZYDIS_OPERAND_TYPE_REGISTER || src.type != ZYDIS_OPERAND_TYPE_MEMORY || src.mem.index != ZYDIS_REGISTER_NONE)
        {
            return false;
        }

        const auto base_reg = ZydisUtility::GetBaseRegister(src.mem.base);

        if (class_reg == ZYDIS_REGISTER_NONE)
        {
            // mov class_reg, [arg0 + 0x20]
            if (base_reg == kArg0Register && src.mem.disp.has_displacement && src.mem.disp.value == 0x20)
            {
                class_reg = ZydisUtility::GetBaseRegister(dst.reg.value);
            }
            return false;
        }

        // mov dst, [class_reg + disp32]
        if (base_reg == class_reg && src.mem.disp.has_displacement && src.mem.disp.value > 0)
        {
            offset = static_cast<std::uint32_t>(src.mem.disp.value);
            return true;
        }

        return false;
    });

    if (offset == 0 || offset >= 0x2000) [[unlikely]]
    {
        FatalError("Failed to resolve CEntityClass entity list offset (got 0x%x)", offset);
        return;
    }

    FLOG("Found CEntityClass entity list head offset at 0x%x", offset);
    CEntityClass::sm_nEntityListHeadOffset = offset;
}

void FindCCSPlayerWeaponServices_DestroyWeapon()
{
    auto svr_mod = modules::server;

    uintptr_t target_call_addr = 0;
    bool      found            = false;

    auto functions            = svr_mod->FindAllFunctionsFromStringRefs({"DestroyWeapon", "Method %s.%s invoked with unrecognized 'this' value."});
    auto range                = functions.size() == 1 ? svr_mod->GetFunctionRange(functions.front()) : nullptr;
    auto weapon_base_typeinfo = svr_mod->GetTypeInfoFromName("CCSWeaponBase");

    if (functions.size() != 1)
        WARN("Failed to find CCSPointScript::DestroyWeapon, expected one function but got %zu.", functions.size());
    else if (range == nullptr)
        WARN("Failed to get function range for CCSPointScript::DestroyWeapon.");
    else if (!weapon_base_typeinfo.IsValid())
        WARN("Failed to find typeinfo for CCSWeaponBase.");
    else
    {
        enum class MatchState
        {
            SearchTypeInfo,
            SearchNullCheck,
            SearchServicesOffset,
            SearchTargetCall
        };

        auto state                 = MatchState::SearchTypeInfo;
        int  insn_since_last_state = 0;

        ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            uintptr_t abs_addr = 0;
            insn_since_last_state++;

            if (state != MatchState::SearchTypeInfo && insn_since_last_state > 30)
            {
                state                 = MatchState::SearchTypeInfo;
                insn_since_last_state = 0;
            }

            switch (state)
            {
            case MatchState::SearchTypeInfo: {
                if (instr.mnemonic == ZYDIS_MNEMONIC_LEA && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    if (ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip) == weapon_base_typeinfo)
                    {
                        state                 = MatchState::SearchNullCheck;
                        insn_since_last_state = 0;
                    }
                }
                break;
            }

            case MatchState::SearchNullCheck: {
                bool is_test = (instr.mnemonic == ZYDIS_MNEMONIC_TEST
                                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                                && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
                                && operands[0].reg.value == operands[1].reg.value);

                bool is_cmp_0 = (instr.mnemonic == ZYDIS_MNEMONIC_CMP
                                 && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                                 && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                                 && operands[1].imm.value.u == 0);

                if (is_test || is_cmp_0)
                {
                    state                 = MatchState::SearchServicesOffset;
                    insn_since_last_state = 0;
                }
                break;
            }

            case MatchState::SearchServicesOffset: {
                if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY)
                {
                    auto base_reg = operands[1].mem.base;
                    if (base_reg != ZYDIS_REGISTER_NONE
                        && base_reg != ZYDIS_REGISTER_RBP
                        && base_reg != ZYDIS_REGISTER_RSP
                        && operands[1].mem.disp.has_displacement)
                    {
                        state                 = MatchState::SearchTargetCall;
                        insn_since_last_state = 0;
                    }
                }
                break;
            }

            case MatchState::SearchTargetCall: {
                if (instr.mnemonic == ZYDIS_MNEMONIC_CALL && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    if (ZYAN_SUCCESS(ZydisCalcAbsoluteAddress(&instr, &operands[0], ip, &abs_addr)))
                    {
                        target_call_addr = abs_addr;
                        return true;
                    }
                }
                break;
            }
            }
            return false;
        });
    }

    AssignOrFallback(svr_mod, address::server::PlayerPawnWeaponServices_RemovePlayerItem, target_call_addr, "CCSPlayer_ItemServices::RemovePlayerItem");
}

void FindSetModel()
{
    auto svr_mod = modules::server;

    uintptr_t target_call_addr = 0;
    bool      found            = false;

    auto functions = svr_mod->FindAllFunctionsFromStringRefs({"weapons/models/defuser/defuser.vmdl", "defuser_dropped"});
    auto range     = functions.size() == 1 ? svr_mod->GetFunctionRange(functions.front()) : nullptr;

    if (functions.size() != 1)
        WARN("Failed to find CItemDefuser::Spawn, expected one function but got %zu.", functions.size());
    else if (range == nullptr)
        WARN("Failed to get function range for CItemDefuser::Spawn.");
    else
    {
        auto defuser_model_str = svr_mod->FindString("weapons/models/defuser/defuser.vmdl", false);

        int insn_since_last_state{};

        enum class MatchState
        {
            SearchModelPath,
            SearchTargetCall
        };
        auto state = MatchState::SearchModelPath;

        ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            insn_since_last_state++;
            if (state == MatchState::SearchTargetCall && insn_since_last_state > 6)
            {
                state                 = MatchState::SearchModelPath;
                insn_since_last_state = 0;
            }

            switch (state)
            {
            case MatchState::SearchModelPath:
                if (instr.mnemonic == ZYDIS_MNEMONIC_LEA && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    if (ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip) == defuser_model_str)
                    {
                        state                 = MatchState::SearchTargetCall;
                        insn_since_last_state = 0;
                    }
                }
                break;

            case MatchState::SearchTargetCall:
                if (instr.mnemonic == ZYDIS_MNEMONIC_CALL && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    target_call_addr = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                    return true;
                }
                break;
            }

            return false;
        });
    }

    AssignOrFallback(svr_mod, address::server::UTIL_SetModel, target_call_addr, "UTIL_SetModel");

    // CItemDefuser::Spawn is an override of CBaseEntity::Spawn.
    // Find its index in the CItemDefuser vtable to resolve the Spawn vfunc index.
    if (functions.size() == 1)
    {
        auto    spawn_addr  = functions.front();
        auto    vfuncs      = svr_mod->GetVFunctionsFromVTable("CItemDefuser");
        int32_t spawn_index = -1;

        for (int32_t i = 0; i < static_cast<int32_t>(vfuncs.size()); i++)
        {
            if (vfuncs[i] == spawn_addr)
            {
                spawn_index = i;
                break;
            }
        }

        try_overwrite_vfunc("CBaseEntity::Spawn", spawn_index);
    }
}

void FindCCSPlayerWeaponService_FindWeaponBySlot()
{
    auto svr_mod = modules::server;

    uintptr_t last_call_target = 0;

    auto functions            = svr_mod->FindAllFunctionsFromStringRefs({"FindWeaponBySlot", "Method %s.%s invoked with unrecognized 'this' value."});
    auto range                = functions.size() == 1 ? svr_mod->GetFunctionRange(functions.front()) : nullptr;
    auto weapon_base_typeinfo = svr_mod->GetTypeInfoFromName("CCSWeaponBase");

    if (functions.size() != 1)
        WARN("Failed to find CCSPointScript::FindWeaponBySlot, expected one function but got %zu.", functions.size());
    else if (range == nullptr)
        WARN("Failed to get function range for CCSPointScript::FindWeaponBySlot.");
    else if (!weapon_base_typeinfo.IsValid())
        WARN("Failed to find typeinfo for CCSWeaponBase.");
    else
    {
        int  insn_count_since_last_call = 0;
        bool found{};

        ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (instr.mnemonic == ZYDIS_MNEMONIC_CALL && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            {
                last_call_target = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                if (last_call_target != 0)
                    insn_count_since_last_call = 0;
            }
            else
            {
                insn_count_since_last_call++;
            }

            if (instr.mnemonic == ZYDIS_MNEMONIC_LEA && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            {
                if (ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip) == weapon_base_typeinfo)
                {
                    if (last_call_target != 0 && insn_count_since_last_call <= 10)
                    {
                        found = true;
                        return true;
                    }
                    WARN("Found lea reg, CCSWeaponBaseTypeInfo, but instruction count since last CALL instr is above 10");
                    last_call_target = 0;
                }
            }
            return false;
        });

        if (!found)
            last_call_target = 0;
    }

    AssignOrFallback(svr_mod, address::server::PlayerPawnWeaponServices_GetWeaponBySlot, last_call_target, "CCSPlayer_WeaponServices::GetWeaponBySlot");
}

void ResolveCCSPlayerPawnStateActive()
{
    auto svr_mod = modules::server;

    // CCSPlayerPawnStateActive::vfunc0 contains two consecutive relative CALLs:
    //   1st CALL -> CBaseEntity::SetMoveType
    //   2nd CALL -> CCollisionProperty::Update
    // Inside CCollisionProperty::Update, the first indirect vtable call (call [reg+disp])
    // gives the vfunc index for CBaseEntity::CollisionRulesChanged.

    uintptr_t set_move_type_addr    = 0;
    uintptr_t collision_update_addr = 0;

    auto vfuncs       = svr_mod->GetVFunctionsFromVTable("CCSPlayerPawnStateActive");
    auto vphys_vtable = svr_mod->GetVirtualTableByName("VPhysicsCollisionAttribute_t");

    if (!vfuncs.empty())
    {
        auto      func     = vfuncs.front();
        auto      range    = svr_mod->GetFunctionRange(func);
        uintptr_t scan_end = range ? range->end : (func + 256);

        // Track last immediate value loaded into the 2nd argument register
        // SetMoveType signature: SetMoveType(entity, MOVETYPE_STEP=2, 0)
        //   Linux:   mov esi, 2  then call
        //   Windows: mov dl, 2   then call
        bool seen_imm2 = false;

        // Helper: verify a call target is CCollisionProperty::Update by checking
        // for a RIP-relative LEA to VPhysicsCollisionAttribute_t vtable in its prologue.
        //
        // GetVirtualTableByName returns the address objects store, which on the Itanium ABI is
        // the vtable symbol + 0x10 (past offset-to-top and typeinfo). Older builds LEA'd that
        // address directly; newer ones LEA the vtable symbol and then `add rax, 10h`. Accept both.
        auto verify_collision_update = [&](uintptr_t call_dest) -> bool {
            if (!vphys_vtable.IsValid())
                return true; // can't verify, assume correct

            const uintptr_t vtable_ptr  = vphys_vtable.GetPtr();
            const uintptr_t vtable_base = vtable_ptr - 2 * sizeof(void*);

            auto      target_range = svr_mod->GetFunctionRange(call_dest);
            uintptr_t target_end   = target_range ? target_range->end : (call_dest + 64);
            bool      found_lea    = false;

            ZydisUtility::ScanInstructions(call_dest, target_end, [&](uintptr_t ip2, const ZydisDecodedInstruction& instr2, const ZydisDecodedOperand* ops2) -> bool {
                if (instr2.mnemonic == ZYDIS_MNEMONIC_LEA && (instr2.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    auto target = ZydisUtility::GetAbsoluteAddress(instr2, ops2[1], ip2);
                    if (target == vtable_ptr || target == vtable_base)
                    {
                        found_lea = true;
                        return true;
                    }
                }
                return false;
            });
            return found_lea;
        };

        ZydisUtility::ScanInstructions(func, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            // Detect "mov 2nd-arg-reg, 2"
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                && operands[1].imm.value.u == 2)
            {
                auto base = ZydisUtility::GetBaseRegister(operands[0].reg.value);
                if (base == ZYDIS_REGISTER_RSI || base == ZYDIS_REGISTER_RDX)
                    seen_imm2 = true;
            }

            if (instr.mnemonic == ZYDIS_MNEMONIC_CALL && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            {
                uintptr_t call_dest = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                if (set_move_type_addr == 0 && seen_imm2)
                {
                    set_move_type_addr = call_dest;
                }
                else if (set_move_type_addr != 0 && verify_collision_update(call_dest))
                {
                    collision_update_addr = call_dest;
                    return true;
                }
                seen_imm2 = false;
            }
            return false;
        });
    }
    else
    {
        WARN("No CCSPlayerPawnStateActive VFunc was found, falling back to GameData.");
    }

    AssignOrFallback(svr_mod, address::server::CBaseEntity_SetMoveType, set_move_type_addr, "CBaseEntity::SetMoveType");

    if (collision_update_addr != 0)
    {
        auto      update_range = svr_mod->GetFunctionRange(collision_update_addr);
        uintptr_t update_end   = update_range ? update_range->end : (collision_update_addr + 512);

        int32_t vcall_offset = -1;

        ZydisUtility::ScanInstructions(collision_update_addr, update_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (instr.mnemonic == ZYDIS_MNEMONIC_CALL
                && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                && operands[0].mem.disp.has_displacement
                && operands[0].mem.disp.value > 0
                && operands[0].mem.index == ZYDIS_REGISTER_NONE
                && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            {
                vcall_offset = static_cast<int32_t>(operands[0].mem.disp.value);
                return true;
            }
            return false;
        });

        auto vfunc_index = vcall_offset != -1 ? vcall_offset / static_cast<int32_t>(sizeof(void*)) : -1;
        try_overwrite_vfunc("CBaseEntity::CollisionRulesChanged", vfunc_index);
    }
    else
    {
        WARN("Failed to find CCollisionProperty::Update call in CCSPlayerPawnStateActive::vfunc0");
    }
}

void ResolveCBaseEntity_IsWeapon()
{
    auto svr_mod = modules::server;

    auto func = svr_mod->FindFunctionFromStringRef("item_pickup_failed");
    if (!func.IsValid())
    {
        WARN("Failed to find item_pickup_failed reference.");
        return;
    }

    auto range = svr_mod->GetFunctionRange(func);
    if (range == nullptr)
    {
        WARN("Failed to get function range for item_pickup_failed function.");
        return;
    }

    // Pattern: cmp [reg+disp], 0x8000 → ... → call [reg+disp] (IsWeapon vtable call)
    // The first vtable call after the 0x8000 comparison is CBaseEntity::IsWeapon.
    int32_t vcall_off       = -1;
    bool    seen_8000_check = false;

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        if (!seen_8000_check
            && instr.mnemonic == ZYDIS_MNEMONIC_CMP
            && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
            && operands[1].imm.value.u == 0x8000)
        {
            seen_8000_check = true;
        }

        if (seen_8000_check
            && instr.mnemonic == ZYDIS_MNEMONIC_CALL
            && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
            && operands[0].mem.disp.has_displacement
            && operands[0].mem.disp.value > 0
            && operands[0].mem.index == ZYDIS_REGISTER_NONE
            && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
        {
            auto base = ZydisUtility::GetBaseRegister(operands[0].mem.base);
            if (base != ZYDIS_REGISTER_RSP
                && base != ZYDIS_REGISTER_RBP)
            {
                vcall_off = static_cast<int32_t>(operands[0].mem.disp.value);
                return true;
            }
        }
        return false;
    });

    auto vfunc_index = vcall_off != -1 ? vcall_off / static_cast<int32_t>(sizeof(void*)) : -1;
    try_overwrite_vfunc("CBaseEntity::IsWeapon", vfunc_index);
}

void ResolveCBaseEntityTeleport()
{
    auto svr_mod = modules::server;

    auto func = svr_mod->FindFunctionFromStringRef("Format: ent_teleport <entity name>\n");
    if (!func.IsValid())
    {
        WARN("Failed to find ent_teleport_callback.");
        return;
    }

    auto range = svr_mod->GetFunctionRange(func);
    if (range == nullptr)
    {
        WARN("Failed to get function range for ent_teleport_callback.");
        return;
    }

    // Scan for vtable calls and identify by surrounding context:
    //   IsPlayerController: call [reg+disp] followed by test al, al (bool return)
    //   Teleport: call [reg+disp] preceded by two xor'd registers (NULL args)
    int32_t is_player_controller_off = -1;
    int32_t teleport_off             = -1;
    int     xor_count                = 0;
    int32_t pending_vcall_off        = -1;

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        // Count XOR reg, reg (zeroing) instructions before vtable calls
        if (instr.mnemonic == ZYDIS_MNEMONIC_XOR
            && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
            && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
            && operands[0].reg.value == operands[1].reg.value)
        {
            xor_count++;
        }
        else if (instr.mnemonic != ZYDIS_MNEMONIC_MOV && instr.mnemonic != ZYDIS_MNEMONIC_LEA)
        {
            // Reset xor count on non-move instructions (except the vtable call itself)
            if (instr.mnemonic != ZYDIS_MNEMONIC_CALL)
                xor_count = 0;
        }

        // Check if previous instruction was a vtable call: test al, al → IsPlayerController
        /*
        mov     rax, [rbx]
        call    qword ptr [rax+548h] ; CBaseEntity::IsPlayerController
        test    al, al
        */
        if (pending_vcall_off != -1)
        {
            if (instr.mnemonic == ZYDIS_MNEMONIC_TEST
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
                && ZydisUtility::GetBaseRegister(operands[0].reg.value) == ZYDIS_REGISTER_RAX
                && operands[0].reg.value == operands[1].reg.value)
            {
                is_player_controller_off = pending_vcall_off;
            }
            pending_vcall_off = -1;
        }

        // Detect indirect vtable call: call [reg+disp]
        if (instr.mnemonic == ZYDIS_MNEMONIC_CALL
            && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
            && operands[0].mem.disp.has_displacement
            && operands[0].mem.disp.value > 0
            && operands[0].mem.index == ZYDIS_REGISTER_NONE)
        {
            auto base = ZydisUtility::GetBaseRegister(operands[0].mem.base);
            if (base != ZYDIS_REGISTER_RIP
                && base != ZYDIS_REGISTER_RSP
                && base != ZYDIS_REGISTER_RBP)
            {
                auto off          = static_cast<int32_t>(operands[0].mem.disp.value);
                pending_vcall_off = off;

                // Teleport: preceded by >= 2 xor zeroing (NULL, NULL args)
                /*
                mov     r10, [rbx]
                lea     rdx, [rsp+48h+var_18]
                xor     r9d, r9d
                xor     r8d, r8d
                mov     rcx, rbx
                call    qword ptr [r10+510h] ; CBaseEntity::Teleport
                */
                if (xor_count >= 2)
                    teleport_off = off;
            }
            xor_count = 0;
        }

        return false; // scan entire function
    });

    auto to_vfunc_index = [](int32_t off) -> int32_t {
        return off != -1 ? off / static_cast<int32_t>(sizeof(void*)) : -1;
    };

    try_overwrite_vfunc("CBaseEntity::IsPlayerController", to_vfunc_index(is_player_controller_off));
    try_overwrite_vfunc("CBaseEntity::Teleport", to_vfunc_index(teleport_off));
}

void ResolveCBaseEntity_GetEyePosition()
{
    auto svr_mod = modules::server;

    const auto point_script_get_eyeposition = svr_mod->FindFunctionFromStringRefs({"Method %s.%s invoked with incorrect 'this' value.",
                                                                                   "GetEyePosition",
                                                                                   "Calling %s.%s\n"});

    if (!point_script_get_eyeposition.IsValid())
    {
        WARN("Failed to find CPointScript::GetEyePosition.");
        return;
    }

    const auto range = svr_mod->GetFunctionRange(point_script_get_eyeposition);
    if (!range)
    {
        WARN("Failed to get function range for CPointScript::GetEyePosition.");
        return;
    }

    // CPointScript::GetEyePosition resolves an entity handle then calls GetEyePosition vtable.
    // Anchor: invalid handle check `cmp reg32, 0xFFFFFFFF` (INVALID_EHANDLE_INDEX).
    // After this check, entity resolution proceeds and the next vtable call is GetEyePosition.
    //   Linux:   cmp ecx, 0FFFFFFFFh → ... → call qword ptr [rax+5D0h]
    //   Windows: cmp edx, 0FFFFFFFFh → ... → call qword ptr [rax+5D8h]
    int32_t vcall_off          = -1;
    bool    seen_invalid_check = false;

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        // Detect cmp reg32, 0xFFFFFFFF (invalid entity handle check)
        if (!seen_invalid_check
            && instr.mnemonic == ZYDIS_MNEMONIC_CMP
            && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
            && operands[0].size == 32
            && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
            && static_cast<uint32_t>(operands[1].imm.value.u) == 0xFFFFFFFF)
        {
            seen_invalid_check = true;
        }

        // After invalid handle check, the next vtable call is GetEyePosition
        if (seen_invalid_check
            && instr.mnemonic == ZYDIS_MNEMONIC_CALL
            && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
            && operands[0].mem.disp.has_displacement
            && operands[0].mem.disp.value > 0
            && operands[0].mem.index == ZYDIS_REGISTER_NONE
            && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
        {
            auto base = ZydisUtility::GetBaseRegister(operands[0].mem.base);
            if (base != ZYDIS_REGISTER_RSP
                && base != ZYDIS_REGISTER_RBP)
            {
                vcall_off = static_cast<int32_t>(operands[0].mem.disp.value);
                return true;
            }
        }
        return false;
    });

    auto vfunc_index = vcall_off != -1 ? vcall_off / static_cast<int32_t>(sizeof(void*)) : -1;
    try_overwrite_vfunc("CBaseEntity::GetEyePosition", vfunc_index);
}

void ResolveCBaseEntity_ChangeTeam()
{
    // "Entity.ChangeTeam" is exposed to VScript, and its binding descriptor records the method's
    // pointer-to-member-function - from which the vtable index falls out directly. This is far more
    // stable than the old anchor (a per-method dispatch wrapper referencing "ChangeTeam" + two
    // generic format strings): build 24116939 genericized those wrappers so the method name now
    // lives only in the registration table, and the three-string intersection went empty.
    ResolveVote vote{};
    vote.Add(GetVScriptVirtualFunctionIndex("ChangeTeam"), /*unique*/ true);
    try_overwrite_vfunc("CBaseEntity::ChangeTeam", vote);
}

void ResolveCGameSceneNodeGetters()
{
    // CGameSceneNode::GetStudioModel (N) and GetSkeletonInstance (N+1) are adjacent vtable getters.
    // On the base CGameSceneNode both are `xor eax,eax ; ret` stubs; CSkeletonInstance overrides both
    // to `mov rax, this ; ret`. That adjacent (base-returns-0, override-returns-this) pair is unique
    // in the vtable across every build checked, which pins the indices structurally rather than by
    // absolute position.
    auto svr_mod = modules::server;

    const auto scene = svr_mod->GetVFunctionsFromVTable("CGameSceneNode");
    const auto skel  = svr_mod->GetVFunctionsFromVTable("CSkeletonInstance");

    // First instruction of `fn` matches (mnemonic, op0 reg, op1 reg-or-none); second is a return.
    auto is_getter = [](uintptr_t fn, ZydisMnemonic mnem, ZydisRegister op0_reg, ZydisRegister op1_reg) -> bool {
        ZydisDecodedInstruction instr{};
        ZydisDecodedOperand     ops[ZYDIS_MAX_OPERAND_COUNT]{};

        // Skip a CET endbr64 if the compiler emitted one.
        auto ip = fn;
        if (ZYAN_SUCCESS(ZydisDecoderDecodeFull(&ZydisUtility::DefaultDecoder, reinterpret_cast<const void*>(ip), ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, ops))
            && instr.mnemonic == ZYDIS_MNEMONIC_ENDBR64)
            ip += instr.length;

        if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&ZydisUtility::DefaultDecoder, reinterpret_cast<const void*>(ip), ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, ops)))
            return false;

        if (instr.mnemonic != mnem
            || ops[0].type != ZYDIS_OPERAND_TYPE_REGISTER
            || ZydisUtility::GetBaseRegister(ops[0].reg.value) != op0_reg)
            return false;

        if (op1_reg != ZYDIS_REGISTER_NONE
            && (ops[1].type != ZYDIS_OPERAND_TYPE_REGISTER || ZydisUtility::GetBaseRegister(ops[1].reg.value) != op1_reg))
            return false;

        ip += instr.length;
        if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&ZydisUtility::DefaultDecoder, reinterpret_cast<const void*>(ip), ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, ops)))
            return false;

        return instr.mnemonic == ZYDIS_MNEMONIC_RET;
    };

    // base: xor eax, eax ; ret  (return 0)
    auto returns_zero = [&](uintptr_t fn) { return is_getter(fn, ZYDIS_MNEMONIC_XOR, ZYDIS_REGISTER_RAX, ZYDIS_REGISTER_RAX); };
    // override: mov rax, this ; ret  (return this)
    auto returns_this = [&](uintptr_t fn) { return is_getter(fn, ZYDIS_MNEMONIC_MOV, ZYDIS_REGISTER_RAX, address_scan::kThisReg); };

    int32_t studio_model_idx = -1;

    const auto count = std::min(scene.size(), skel.size());
    for (size_t i = 0; i + 1 < count; ++i)
    {
        if (returns_zero(scene[i]) && returns_this(skel[i])
            && returns_zero(scene[i + 1]) && returns_this(skel[i + 1]))
        {
            studio_model_idx = static_cast<int32_t>(i);
            break;
        }
    }

    ResolveVote studio{};
    ResolveVote skeleton{};
    if (studio_model_idx != -1)
    {
        studio.Add(studio_model_idx, /*unique*/ true);
        skeleton.Add(studio_model_idx + 1, /*unique*/ true);
    }

    try_overwrite_vfunc("CGameSceneNode::GetStudioModel", studio);
    try_overwrite_vfunc("CGameSceneNode::GetSkeletonInstance", skeleton);
}

void ResolveCBaseEntity_GetEyeAngles()
{
    auto svr_mod = modules::server;

    const auto point_script_get_eyeangles = svr_mod->FindFunctionFromStringRefs({"Method %s.%s invoked with incorrect 'this' value.",
                                                                                 "GetEyeAngles",
                                                                                 "Calling %s.%s\n"});

    if (!point_script_get_eyeangles.IsValid())
    {
        WARN("Failed to find CPointScript::GetEyeAngles.");
        return;
    }

    const auto range = svr_mod->GetFunctionRange(point_script_get_eyeangles);
    if (!range)
    {
        WARN("Failed to get function range for CPointScript::GetEyeAngles.");
        return;
    }

    // CPointScript::GetEyeAngles resolves an entity handle then calls GetEyeAngles vtable.
    // Anchor: invalid handle check `cmp reg32, 0xFFFFFFFF` (INVALID_EHANDLE_INDEX).
    // After this check, entity resolution proceeds and the next vtable call is GetEyeAngles.
    //   Linux:   cmp ecx, 0FFFFFFFFh → ... → call qword ptr [rax+5D0h]
    //   Windows: cmp edx, 0FFFFFFFFh → ... → call qword ptr [rax+5D8h]
    int32_t vcall_off          = -1;
    bool    seen_invalid_check = false;

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        // Detect cmp reg32, 0xFFFFFFFF (invalid entity handle check)
        if (!seen_invalid_check
            && instr.mnemonic == ZYDIS_MNEMONIC_CMP
            && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
            && operands[0].size == 32
            && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
            && static_cast<uint32_t>(operands[1].imm.value.u) == 0xFFFFFFFF)
        {
            seen_invalid_check = true;
        }

        // After invalid handle check, the next vtable call is GetEyeAngles
        if (seen_invalid_check
            && instr.mnemonic == ZYDIS_MNEMONIC_CALL
            && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
            && operands[0].mem.disp.has_displacement
            && operands[0].mem.disp.value > 0
            && operands[0].mem.index == ZYDIS_REGISTER_NONE
            && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
        {
            auto base = ZydisUtility::GetBaseRegister(operands[0].mem.base);
            if (base != ZYDIS_REGISTER_RSP
                && base != ZYDIS_REGISTER_RBP)
            {
                vcall_off = static_cast<int32_t>(operands[0].mem.disp.value);
                return true;
            }
        }
        return false;
    });

    auto vfunc_index = vcall_off != -1 ? vcall_off / static_cast<int32_t>(sizeof(void*)) : -1;
    try_overwrite_vfunc("CBaseEntity::GetEyeAngles", vfunc_index);
}

void ResolveCBaseEntity_AbsOrigin()
{
    auto svr_mod = modules::server;

    // OnC4Explode references "c4.explode" and calls both GetAbsOrigin and SetAbsOrigin as direct calls.
    // SetAbsOrigin: preceded by mulss/mulps with 0.6f constant (trace normal scaling).
    // GetAbsOrigin: returns const Vector&, caller reads [rax+8] (Z component) shortly after.
    // Note: call order differs between platforms (Windows: Set then Get, Linux: Get then Set).
    auto func = svr_mod->FindFunctionFromStringRef("c4.explode");
    if (!func.IsValid())
    {
        WARN("Failed to find OnC4Explode (string 'c4.explode').");
        return;
    }

    auto range = svr_mod->GetFunctionRange(func);
    if (!range)
    {
        WARN("Failed to get function range for OnC4Explode.");
        return;
    }

    uintptr_t get_abs_origin_addr = 0;
    uintptr_t set_abs_origin_addr = 0;

    // Verification:
    // GetAbsOrigin returns const Vector& → after the call, [rax+8] read (Z component).
    // SetAbsOrigin is preceded by the trace-hit position calculation which loads 0.6f
    //   and uses it in mulss/mulps to scale the trace normal. Detect by reading the
    //   actual float constant from the resolved RIP-relative address.
    //   Linux:  mulps  xmm0, cs:[0.6_packed]  → call SetAbsOrigin → jmp
    //   Windows: movss xmm2, cs:[0.6] → mulss ×3 → addss ×3 → call SetAbsOrigin
    uintptr_t     pending_call_target = 0;
    int           insn_after_call     = 0;
    bool          seen_mul_0_6        = false;
    constexpr int kMaxInsnLookahead   = 20;

    ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        // Verify pending GetAbsOrigin call: [rax+8] access within a few instructions
        if (pending_call_target != 0)
        {
            insn_after_call++;

            for (int i = 0; i < instr.operand_count; i++)
            {
                if (operands[i].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && ZydisUtility::GetBaseRegister(operands[i].mem.base) == ZYDIS_REGISTER_RAX
                    && operands[i].mem.index == ZYDIS_REGISTER_NONE
                    && operands[i].mem.disp.has_displacement
                    && operands[i].mem.disp.value == 8)
                {
                    get_abs_origin_addr = pending_call_target;
                    pending_call_target = 0;
                    break;
                }
            }

            if (insn_after_call >= kMaxInsnLookahead)
                pending_call_target = 0;
        }

        // Track 0.6f constant load used for trace normal scaling before SetAbsOrigin.
        //   Linux:  mulps  xmm0, cs:[0.6_packed]  (mul directly from memory)
        //   Windows: movss xmm2, cs:[0.6] → mulss xmm0, xmm2  (load then register mul)
        if ((instr.mnemonic == ZYDIS_MNEMONIC_MULSS || instr.mnemonic == ZYDIS_MNEMONIC_MULPS
             || instr.mnemonic == ZYDIS_MNEMONIC_MOVSS)
            && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
            && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
        {
            auto const_addr = ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip);
            if (const_addr != 0 && *reinterpret_cast<const uint32_t*>(const_addr) == 0x3F19999A) // 0.6f
                seen_mul_0_6 = true;
        }

        if (instr.mnemonic == ZYDIS_MNEMONIC_CALL && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
        {
            auto target = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);

            if (seen_mul_0_6 && set_abs_origin_addr == 0)
            {
                // Preceded by 0.6 scaling → this is SetAbsOrigin
                set_abs_origin_addr = target;
                seen_mul_0_6        = false;
            }
            else if (get_abs_origin_addr == 0)
            {
                // Candidate for GetAbsOrigin — verify with [rax+8] lookahead
                pending_call_target = target;
                insn_after_call     = 0;
            }
        }

        return get_abs_origin_addr != 0 && set_abs_origin_addr != 0;
    });

    AssignOrFallback(svr_mod, address::server::CBaseEntity_AbsOrigin, get_abs_origin_addr, "CBaseEntity::GetAbsOrigin", "CBaseEntity::AbsOrigin");
    AssignOrFallback(svr_mod, address::server::CBaseEntity_SetAbsOrigin, set_abs_origin_addr, "CBaseEntity::SetAbsOrigin");
}

void ResolveCBaseEntity_EventKill()
{
    auto svr_mod = modules::server;

    auto func = svr_mod->FindFunctionFromStringRefs({"#Cstrike_TitlesTXT_Hint_cannot_play_because_tk", "#Chat_SavePlayer_Savior", "#Chat_SavePlayer_Saved"});
    if (!func.IsValid())
    {
        WARN("Failed to find CCSPlayerPawn::Event_Kill");
        return;
    }

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CCSPlayerPawn");
    if (vfuncs.empty())
    {
        WARN("Cannot find vtable CCSPlayerPawn??");
        return;
    }

    int32_t event_kill_idx = -1;

    for (auto i = 0u; i < vfuncs.size(); i++)
    {
        if (vfuncs[i] == func)
        {
            event_kill_idx = i;
            break;
        }
    }

    try_overwrite_vfunc("CBaseEntity::Event_Killed", event_kill_idx);
}

void ResolveCBaseEntity_GetCenter()
{
    auto svr_mod = modules::server;

    // Step 1: Get schema offset of CBaseEntity::m_pCollision.
    auto collision_offset = schemas::GetOffset("CBaseEntity", "m_pCollision").offset;
    if (collision_offset == -1)
    {
        WARN("Failed to get schema offset for CBaseEntity::m_pCollision.");
        return;
    }

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CBaseEntity");
    if (vfuncs.empty())
    {
        WARN("Cannot find vtable CBaseEntity.");
        return;
    }

    auto get_abs_origin_addr = reinterpret_cast<uintptr_t>(address::server::CBaseEntity_AbsOrigin);
    if (get_abs_origin_addr == 0)
    {
        WARN("GetAbsOrigin not resolved, cannot find GetCenter.");
        return;
    }

    // Step 2: Find GetCenter vtable index.
    // GetCenter checks collision property, falls back to GetAbsOrigin if NULL.
    //   Windows: call [rax+disp] (vtable call to GetCollisionProperty) + call GetAbsOrigin
    //   Linux:   mov reg, [this+m_pCollision] (inlined getter) + call GetAbsOrigin
    // Match: (memory access [reg+m_pCollision] OR indirect vtable call) AND call GetAbsOrigin.
    // Linux version inlines collision property logic, making GetCenter much larger (~0x22B).
    constexpr uintptr_t kMaxFuncSize = 1024;

    int32_t get_center_idx = -1;

    for (auto i = 0u; i < vfuncs.size(); i++)
    {
        auto vfunc_range = svr_mod->GetFunctionRange(vfuncs[i]);

        uintptr_t scan_start = vfuncs[i];
        uintptr_t scan_end;

        if (vfunc_range)
        {
            if ((vfunc_range->end - vfunc_range->start) > kMaxFuncSize)
                continue;
            scan_end = vfunc_range->end;
        }
        else
        {
            // No function range available — use a bounded scan
            scan_end = scan_start + kMaxFuncSize;
        }

        bool has_collision_ref = false;
        bool has_call_get_abs  = false;

        ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            // Stop at RET if no function range (bounded scan)
            if (!vfunc_range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                return true;

            // Linux: inlined GetCollisionProperty → mov reg, [this+m_pCollision]
            for (int j = 0; j < instr.operand_count; j++)
            {
                if (operands[j].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[j].mem.disp.has_displacement
                    && operands[j].mem.disp.value == collision_offset
                    && operands[j].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    has_collision_ref = true;
                    break;
                }
            }

            if (instr.mnemonic == ZYDIS_MNEMONIC_CALL)
            {
                // Windows: vtable call to GetCollisionProperty → call [reg+disp]
                // The function loads vtable first (mov rax, [rcx]), then calls through it.
                if (operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[0].mem.disp.has_displacement
                    && operands[0].mem.disp.value > 0
                    && operands[0].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    auto base = ZydisUtility::GetBaseRegister(operands[0].mem.base);
                    if (base != ZYDIS_REGISTER_RIP && base != ZYDIS_REGISTER_RSP && base != ZYDIS_REGISTER_RBP)
                        has_collision_ref = true;
                }

                // Direct call to GetAbsOrigin (fallback path)
                if ((instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                    && ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip) == get_abs_origin_addr)
                {
                    has_call_get_abs = true;
                }
            }

            return has_collision_ref && has_call_get_abs;
        });

        if (has_collision_ref && has_call_get_abs)
        {
            get_center_idx = static_cast<int32_t>(i);
            break;
        }
    }

    try_overwrite_vfunc("CBaseEntity::GetCenter", get_center_idx);
}

void Resolve_CBaseEntity_Use_StartTouch_Touch_EndTouch()
{
    auto svr_mod = modules::server;

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CBaseEntity");
    if (vfuncs.empty())
        return;

    auto m_pfnUse_offset   = schemas::GetOffset("CBaseEntity", "m_pfnUse").offset;
    auto m_pfnTouch_offset = schemas::GetOffset("CBaseEntity", "m_pfnTouch").offset;

    if (m_pfnUse_offset <= 0 && m_pfnTouch_offset <= 0)
        return;

    constexpr uintptr_t kMaxFuncSize = 256;

    // Scan a vfunc for any memory operand with displacement matching target_off.
    auto vfunc_accesses_offset = [&](uint32_t i, int32_t target_off) -> bool {
        auto vfunc_range = svr_mod->GetFunctionRange(vfuncs[i]);
        auto scan_start  = vfuncs[i];
        auto scan_end    = vfunc_range ? vfunc_range->end : (scan_start + kMaxFuncSize);

        if (vfunc_range && (vfunc_range->end - vfunc_range->start) > kMaxFuncSize)
            return false;

        bool found = false;
        ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (!vfunc_range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                return true;

            for (int j = 0; j < instr.operand_count; j++)
            {
                if (operands[j].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[j].mem.disp.has_displacement
                    && static_cast<int32_t>(operands[j].mem.disp.value) == target_off
                    && operands[j].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    found = true;
                    return true;
                }
            }
            return false;
        });
        return found;
    };

    int32_t use_idx   = -1;
    int32_t touch_idx = -1;

    // Find Use — skip CEntityInstance vfuncs since Use is a CBaseEntity vfunc
    if (m_pfnUse_offset > 0)
    {
        auto entity_instance_start = static_cast<uint32_t>(svr_mod->GetVFunctionsFromVTable("CEntityInstance").size());

        for (auto i = entity_instance_start; i < vfuncs.size(); i++)
        {
            if (vfunc_accesses_offset(i, m_pfnUse_offset))
            {
                use_idx = static_cast<int32_t>(i);
                break;
            }
        }
    }

    // Touch is always after Use in the vtable
    if (m_pfnTouch_offset > 0 && use_idx != -1)
    {
        for (auto i = static_cast<uint32_t>(use_idx + 1); i < vfuncs.size(); i++)
        {
            if (vfunc_accesses_offset(i, m_pfnTouch_offset))
            {
                touch_idx = static_cast<int32_t>(i);
                break;
            }
        }
    }

    try_overwrite_vfunc("CBaseEntity::Use", use_idx);
    try_overwrite_vfunc("CBaseEntity::StartTouch", touch_idx > 0 ? touch_idx - 1 : -1);
    try_overwrite_vfunc("CBaseEntity::Touch", touch_idx);
    try_overwrite_vfunc("CBaseEntity::EndTouch", touch_idx > 0 ? touch_idx + 1 : -1);
}

void ResolveCEntityInstance_GetDynamicBinding()
{
    auto svr_mod = modules::server;

    // GetDynamicBinding returns a pointer to a binding struct.
    // The binding struct has the class name string at +0x8:
    //   [+0x0] = 0 (or runtime data)
    //   [+0x8] = pointer to "CEntityInstance"
    //
    // The function references the binding via RIP-relative LEA/MOV:
    //   Linux:  lea rax, [rip+binding_ptr] → mov rdi, [rax] → jmp helper  (indirect)
    //           or: lea rdi, [rip+binding]  → jmp helper                   (direct)
    //   Windows: mov rdx, cs:[rip+binding_ptr] → call helper               (indirect)
    //
    // Scan CEntityInstance vtable from the end (GetDynamicBinding is near the bottom).

    auto str_addr = svr_mod->FindString("CEntityInstance", true, true);
    if (!str_addr.IsValid())
    {
        WARN("Failed to find string 'CEntityInstance'.");
        return;
    }

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CEntityInstance");
    if (vfuncs.empty())
    {
        WARN("Cannot find vtable CEntityInstance.");
        return;
    }

    constexpr uintptr_t kMaxFuncSize = 64;

    auto check_binding_name = [&](uintptr_t addr) -> bool {
        if (!svr_mod->IsInModule(addr + 8))
            return false;

        // Direct: addr is the binding struct itself, [addr+8] == str_addr
        auto name_ptr = *reinterpret_cast<uintptr_t*>(addr + 8);
        if (name_ptr == str_addr.GetPtr())
            return true;

        // Indirect: addr is a pointer to the binding struct
        auto binding = *reinterpret_cast<uintptr_t*>(addr);
        if (binding != 0 && svr_mod->IsInModule(binding) && svr_mod->IsInModule(binding + 8))
        {
            name_ptr = *reinterpret_cast<uintptr_t*>(binding + 8);
            if (name_ptr == str_addr.GetPtr())
                return true;
        }

        return false;
    };

    int32_t dynamic_binding_idx = -1;

    for (auto i = static_cast<int32_t>(vfuncs.size()) - 1; i >= 0; i--)
    {
        auto vfunc_range = svr_mod->GetFunctionRange(vfuncs[i]);

        uintptr_t scan_start = vfuncs[i];
        uintptr_t scan_end;

        if (vfunc_range)
        {
            if ((vfunc_range->end - vfunc_range->start) > kMaxFuncSize)
                continue;
            scan_end = vfunc_range->end;
        }
        else
        {
            scan_end = scan_start + kMaxFuncSize;
        }

        bool found = false;

        ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (!vfunc_range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                return true;

            // Look for RIP-relative LEA or MOV that loads a data address
            if ((instr.mnemonic == ZYDIS_MNEMONIC_LEA || instr.mnemonic == ZYDIS_MNEMONIC_MOV)
                && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY)
            {
                auto resolved = ZydisUtility::GetAbsoluteAddress(instr, operands[1], ip);
                if (resolved != 0 && svr_mod->IsInModule(resolved) && check_binding_name(resolved))
                {
                    found = true;
                    return true;
                }
            }
            return false;
        });

        if (found)
        {
            dynamic_binding_idx = i;
            break;
        }
    }

    try_overwrite_vfunc("CEntityInstance::GetDynamicBinding", dynamic_binding_idx);
}

void ResolveCBaseEntity_Precache()
{
    auto svr_mod = modules::server;

    auto str = svr_mod->FindString("weapons/models/elite/weapon_pist_elite.vmdl", false, true);
    if (!str.IsValid())
    {
        WARN("Failed to find string \"weapons/models/elite/weapon_pist_elite.vmdl\"");
        return;
    }

    auto references = svr_mod->GetReferenceRange(str);

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CWeaponElite");
    if (vfuncs.empty())
    {
        FERROR("Failed to find vtable CWeaponElite??");
        return;
    }

    int32_t precache_idx = -1;

    for (const auto& ref : references)
    {
        auto* range = svr_mod->GetFunctionRange(ref.source_ip);
        if (!range)
            continue;

        for (int32_t i = 0; i < static_cast<int32_t>(vfuncs.size()); i++)
        {
            if (vfuncs[i] == range->start)
            {
                precache_idx = i;
                break;
            }
        }

        if (precache_idx != -1)
            break;
    }

    try_overwrite_vfunc("CBaseEntity::Precache", precache_idx);
}

void ResolveCCSPlayerWeaponServices()
{
    auto svr_mod = modules::server;

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CCSPlayer_WeaponServices");

    // CSPlayerPawn::SwitchToWeapon --> CCSPlayer_WeaponServices::SelectItem
    {
        auto str = svr_mod->FindString("SwitchToWeapon", true, true);

        int32_t   select_item_idx  = -1;
        uintptr_t switch_to_weapon = 0;

        if (str.IsValid())
        {
            auto references = svr_mod->GetReferenceRange(str);
            for (const auto& ref : references)
            {
                auto* range = svr_mod->GetFunctionRange(ref.source_ip);
                if (!range)
                    continue;

                auto method_str = svr_mod->FindString("Method %s.%s invoked with incorrect 'this' value.", true);
                if (!method_str.IsValid())
                    continue;

                auto method_refs    = svr_mod->GetReferenceRange(method_str);
                bool has_method_ref = std::ranges::any_of(method_refs, [&](const CModule::ReferenceEntry& mref) {
                    return mref.source_ip >= range->start && mref.source_ip < range->end;
                });

                if (has_method_ref)
                {
                    switch_to_weapon = range->start;
                    break;
                }
            }
        }

        if (switch_to_weapon)
        {
            auto range      = svr_mod->GetFunctionRange(switch_to_weapon);
            auto scan_start = switch_to_weapon;
            auto scan_end   = range ? range->end : scan_start + 512;

            // Track vtable loads: mov reg, [other_reg] (no disp = loading vtable ptr from object)
            // Then match: call [vtable_reg + disp] to find the vcall dispatch
            ZydisRegister last_vtable_reg = ZYDIS_REGISTER_NONE;

            ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                // mov reg, [reg2] — vtable load pattern (no index, no/zero displacement)
                if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                    && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                    && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[1].mem.index == ZYDIS_REGISTER_NONE
                    && (!operands[1].mem.disp.has_displacement || operands[1].mem.disp.value == 0)
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    last_vtable_reg = ZydisUtility::GetBaseRegister(operands[0].reg.value);
                }
                // Clear tracking when that register is overwritten by something else
                else if (last_vtable_reg != ZYDIS_REGISTER_NONE
                         && instr.operand_count > 0
                         && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                         && (operands[0].actions & ZYDIS_OPERAND_ACTION_WRITE)
                         && ZydisUtility::GetBaseRegister(operands[0].reg.value) == last_vtable_reg)
                {
                    last_vtable_reg = ZYDIS_REGISTER_NONE;
                }

                // call [vtable_reg + disp]
                if (instr.mnemonic == ZYDIS_MNEMONIC_CALL
                    && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[0].mem.disp.has_displacement
                    && operands[0].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                    && last_vtable_reg != ZYDIS_REGISTER_NONE
                    && ZydisUtility::GetBaseRegister(operands[0].mem.base) == last_vtable_reg)
                {
                    auto disp = operands[0].mem.disp.value;
                    if (disp > 0 && (disp % sizeof(void*)) == 0)
                    {
                        select_item_idx = static_cast<int32_t>(disp / sizeof(void*));
                        return true;
                    }
                }
                return false;
            });
        }

        try_overwrite_vfunc("CCSPlayer_WeaponServices::SelectItem", select_item_idx);
    }

    // CBaseWeapon::Deploy and CBaseWeapon::Holster
    // SelectItem calls Deploy multiple times and Holster (= Deploy + 1 vfunc) with arg2=0.
    // Scan SelectItem for weapon vcall dispatches; the most frequent offset is Deploy.
    {
        int32_t deploy_idx  = -1;
        int32_t holster_idx = -1;

        auto select_item_vfunc_idx = g_pGameData->GetVFunctionIndex("CCSPlayer_WeaponServices::SelectItem");
        if (select_item_vfunc_idx >= 0 && static_cast<uint32_t>(select_item_vfunc_idx) < vfuncs.size())
        {
            auto select_item_addr = vfuncs[select_item_vfunc_idx];
            auto range            = svr_mod->GetFunctionRange(select_item_addr);
            auto scan_start       = select_item_addr;
            auto scan_end         = range ? range->end : scan_start + 512;

            // Collect vcall offsets: call/jmp [reg + disp] where reg came from [other_reg] (vtable load)
            std::unordered_map<int64_t, int> vcall_counts;

            ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                // call/jmp [reg + disp] (indirect vcall through vtable)
                if ((instr.mnemonic == ZYDIS_MNEMONIC_CALL || instr.mnemonic == ZYDIS_MNEMONIC_JMP)
                    && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[0].mem.disp.has_displacement
                    && operands[0].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    auto base = ZydisUtility::GetBaseRegister(operands[0].mem.base);
                    if (base != ZYDIS_REGISTER_RSP && base != ZYDIS_REGISTER_RBP)
                    {
                        auto disp = operands[0].mem.disp.value;
                        if (disp > 0 && (disp % sizeof(void*)) == 0)
                            vcall_counts[disp]++;
                    }
                }

                // Linux tail call: mov rax, [reg + disp]; ... jmp rax
                // Handled by counting the memory load above when it appears as call target
                // But for mov rax, [rax+disp] followed by jmp rax, the jmp rax won't be counted.
                // We also track mov rax, [rax+disp] patterns separately.
                if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                    && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                    && ZydisUtility::GetBaseRegister(operands[0].reg.value) == ZYDIS_REGISTER_RAX
                    && operands[1].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[1].mem.disp.has_displacement
                    && operands[1].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    auto base = ZydisUtility::GetBaseRegister(operands[1].mem.base);
                    if (base == ZYDIS_REGISTER_RAX)
                    {
                        auto disp = operands[1].mem.disp.value;
                        if (disp > 0 && (disp % sizeof(void*)) == 0)
                            vcall_counts[disp]++;
                    }
                }

                return false;
            });

            // Deploy is the vcall offset that appears most frequently
            int64_t deploy_disp = -1;
            int     max_count   = 0;
            for (const auto& [disp, count] : vcall_counts)
            {
                if (count > max_count)
                {
                    max_count   = count;
                    deploy_disp = disp;
                }
            }

            if (deploy_disp > 0)
            {
                deploy_idx  = static_cast<int32_t>(deploy_disp / sizeof(void*));
                holster_idx = deploy_idx + 1;
            }
        }

        try_overwrite_vfunc("CBaseWeapon::Deploy", deploy_idx);
        try_overwrite_vfunc("CBaseWeapon::Holster", holster_idx);
    }

    // CCSPlayer_WeaponServices::DropWeapon
    // DropWeapon accesses m_bDroppedNearBuyZone on the weapon being dropped.
    // Scan vfuncs for a CMP instruction referencing that schema offset.
    {
        auto    drop_near_buyzone_offset = schemas::GetOffset("CCSWeaponBase", "m_bDroppedNearBuyZone").offset;
        int32_t drop_weapon_idx          = -1;

        for (auto i = 0u; i < vfuncs.size(); i++)
        {
            auto range      = svr_mod->GetFunctionRange(vfuncs[i]);
            auto scan_start = vfuncs[i];
            auto scan_end   = range ? range->end : scan_start + 512;

            bool found = false;
            ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                if (!range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                    return true;

                if (instr.mnemonic == ZYDIS_MNEMONIC_CMP
                    && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[0].mem.disp.has_displacement
                    && operands[0].mem.disp.value == drop_near_buyzone_offset
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    found = true;
                    return true;
                }
                return false;
            });

            if (found)
            {
                drop_weapon_idx = static_cast<int32_t>(i);
                break;
            }
        }

        try_overwrite_vfunc("CCSPlayer_WeaponServices::DropWeapon", drop_weapon_idx);
    }

    // CCSPlayer_WeaponServices::CanUse
    // CanUse references the ConVar "mp_weaponstay".
    {
        int32_t can_use_idx = -1;

        auto convar_ptr = g_pCVar->FindConVarIterator("mp_weaponstay");
        if (convar_ptr)
        {
            auto ptr_to_cvar = svr_mod->FindPtr(reinterpret_cast<uintptr_t>(convar_ptr));
            if (ptr_to_cvar.IsValid())
            {
                auto references = svr_mod->GetReferenceRange(ptr_to_cvar);
                for (const auto& ref : references)
                {
                    auto* range = svr_mod->GetFunctionRange(ref.source_ip);
                    if (!range)
                        continue;

                    for (int32_t i = 0; i < static_cast<int32_t>(vfuncs.size()); i++)
                    {
                        if (vfuncs[i] == range->start)
                        {
                            can_use_idx = i;
                            break;
                        }
                    }

                    if (can_use_idx != -1)
                        break;
                }
            }
        }

        try_overwrite_vfunc("CCSPlayer_WeaponServices::CanUse", can_use_idx);
    }

    // CCSPlayer_WeaponServices::SwitchWeapon
    // SwitchWeapon references the string "active_weapon" via LEA.
    {
        auto    str               = svr_mod->FindString("active_weapon", true, true);
        int32_t switch_weapon_idx = -1;

        if (str.IsValid())
        {
            auto references = svr_mod->GetReferenceRange(str);

            for (const auto& ref : references)
            {
                auto* range = svr_mod->GetFunctionRange(ref.source_ip);
                if (!range)
                    continue;

                for (int32_t i = 0; i < static_cast<int32_t>(vfuncs.size()); i++)
                {
                    if (vfuncs[i] == range->start)
                    {
                        switch_weapon_idx = i;
                        break;
                    }
                }

                if (switch_weapon_idx != -1)
                    break;
            }
        }

        try_overwrite_vfunc("CCSPlayer_WeaponServices::SwitchWeapon", switch_weapon_idx);
    }
}

void ResolveCCSPlayerItemServices()
{
    auto svr_mod = modules::server;

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CCSPlayer_ItemServices");

    auto give_named_item_address = g_pGameData->GetAddress<void*>("CCSPlayer_ItemServices::GiveNamedItem");

    // CCSPlayer_ItemServices::GiveNamedItem
    // First vfunc that contains a direct call to GiveNamedItem.
    {
        int32_t give_named_item_idx = -1;

        if (give_named_item_address)
        {
            auto target = reinterpret_cast<uintptr_t>(give_named_item_address);

            for (auto i = 0u; i < vfuncs.size(); i++)
            {
                auto range      = svr_mod->GetFunctionRange(vfuncs[i]);
                auto scan_start = vfuncs[i];
                auto scan_end   = range ? range->end : scan_start + 512;

                bool found = false;
                ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    if (!range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                        return true;

                    if ((instr.mnemonic == ZYDIS_MNEMONIC_CALL || instr.mnemonic == ZYDIS_MNEMONIC_JMP) && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                    {
                        if (ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip) == target)
                        {
                            found = true;
                            return true;
                        }
                    }
                    return false;
                });

                if (found)
                {
                    give_named_item_idx = static_cast<int32_t>(i);
                    break;
                }
            }
        }

        try_overwrite_vfunc("CCSPlayer_ItemServices::GiveNamedItem", give_named_item_idx);
    }

    // CCSPlayer_ItemServices::RemoveAllItems
    // RemoveAllItems checks m_bHasDefuser: cmp byte [reg + m_bHasDefuser_offset], 0
    {
        auto    has_defuser_offset   = schemas::GetOffset("CCSPlayer_ItemServices", "m_bHasDefuser").offset;
        int32_t remove_all_items_idx = -1;

        if (has_defuser_offset > 0)
        {
            for (auto i = 0u; i < vfuncs.size(); i++)
            {
                auto range      = svr_mod->GetFunctionRange(vfuncs[i]);
                auto scan_start = vfuncs[i];
                auto scan_end   = range ? range->end : scan_start + 512;

                bool found = false;
                ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
                    if (!range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                        return true;

                    // cmp byte [reg + m_bHasDefuser], 0
                    if (instr.mnemonic == ZYDIS_MNEMONIC_CMP
                        && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                        && operands[0].mem.disp.has_displacement
                        && static_cast<int32_t>(operands[0].mem.disp.value) == has_defuser_offset
                        && operands[0].mem.index == ZYDIS_REGISTER_NONE
                        && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                        && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                        && operands[1].imm.value.u == 0)
                    {
                        found = true;
                        return true;
                    }
                    return false;
                });

                if (found)
                {
                    remove_all_items_idx = static_cast<int32_t>(i);
                    break;
                }
            }
        }

        try_overwrite_vfunc("CCSPlayer_ItemServices::RemoveAllItems", remove_all_items_idx);
    }
}

void ResolveCCSPlayerPawn_IsPlayer()
{
    auto svr_mod = modules::server;

    auto CCSPlayerPawn_vfuncs   = svr_mod->GetVFunctionsFromVTable("CCSPlayerPawn");
    auto CBasePlayerPawn_vfuncs = svr_mod->GetVFunctionsFromVTable("CBasePlayerPawn");

    if (CCSPlayerPawn_vfuncs.empty() || CBasePlayerPawn_vfuncs.empty())
    {
        WARN("No vfuncs was found for either CCSPlayerPawn or CBasePlayerPawn");
        return;
    }

    // IsPlayer() is a 2-instruction stub: mov eax/al, 1; retn
    int32_t is_player_idx = -1;

    for (auto i = CBasePlayerPawn_vfuncs.size(); i < CCSPlayerPawn_vfuncs.size(); i++)
    {
        auto start      = CCSPlayerPawn_vfuncs[i];
        bool is_mov_1   = false;
        bool is_ret     = false;
        int  insn_count = 0;

        ZydisUtility::ScanInstructions(start, start + 16, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            insn_count++;

            if (insn_count == 1
                && instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && ZydisUtility::GetBaseRegister(operands[0].reg.value) == ZYDIS_REGISTER_RAX
                && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                && operands[1].imm.value.u == 1)
            {
                is_mov_1 = true;
            }
            else if (insn_count == 2 && instr.mnemonic == ZYDIS_MNEMONIC_RET)
            {
                is_ret = true;
            }

            return insn_count >= 2;
        });

        if (is_mov_1 && is_ret)
        {
            is_player_idx = static_cast<int32_t>(i);
            break;
        }
    }

    try_overwrite_vfunc("CCSPlayerPawnBase::IsPlayer", is_player_idx);
}

void ResolveCBasePlayerController_CanHearAndReadChatFrom()
{
    auto svr_mod = modules::server;

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CBasePlayerController");
    if (vfuncs.empty())
    {
        WARN("No vfuncs was found for CBasePlayerController??");
        return;
    }

    auto offset = schemas::GetOffset("CBasePlayerController", "m_iIgnoreGlobalChat").offset;
    if (offset <= 0)
    {
        WARN("Cannot find offset for CBasePlayerController->m_iIgnoreGlobalChat");
        return;
    }

    // Find the last vfunc that accesses m_iIgnoreGlobalChat (search from end)
    int32_t result_idx = -1;

    for (auto i = static_cast<int32_t>(vfuncs.size()) - 1; i >= 0; i--)
    {
        auto range      = svr_mod->GetFunctionRange(vfuncs[i]);
        auto scan_start = vfuncs[i];
        auto scan_end   = range ? range->end : scan_start + 256;

        bool found = false;
        ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (!range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                return true;

            for (int j = 0; j < instr.operand_count; j++)
            {
                if (operands[j].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[j].mem.disp.has_displacement
                    && static_cast<int32_t>(operands[j].mem.disp.value) == offset
                    && operands[j].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
                {
                    found = true;
                    return true;
                }
            }
            return false;
        });

        if (found)
        {
            result_idx = i;
            break;
        }
    }

    try_overwrite_vfunc("CBasePlayerController::CanHearAndReadChatFrom", result_idx);
}

void ResolveCCSPlayerController_RoundRespawn()
{
    auto svr_mod = modules::server;

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CCSPlayerController");
    if (vfuncs.empty())
    {
        WARN("No vfuncs was found for CCSPlayerController??");
        return;
    }

    auto damage_service_offset = schemas::GetOffset("CCSPlayerController", "m_pDamageServices").offset;
    if (damage_service_offset <= 0)
    {
        WARN("Cannot find offset for CCSPlayerController->m_pDamageServices");
        return;
    }

    auto controlled_bot_this_round_offset = schemas::GetOffset("CCSPlayerController", "m_bHasControlledBotThisRound").offset;
    if (controlled_bot_this_round_offset <= 0)
    {
        WARN("Cannot find offset for CCSPlayerController->m_bHasControlledBotThisRound");
        return;
    }

    auto team_changed_offset = schemas::GetOffset("CCSPlayerController", "m_bTeamChanged").offset;
    if (team_changed_offset <= 0)
    {
        WARN("Cannot find offset for CCSPlayerController->m_bTeamChanged");
        return;
    }

    // RoundRespawn accesses all three: m_pDamageServices, m_bHasControlledBotThisRound, m_bTeamChanged
    int32_t result_idx = -1;

    auto accesses_offset = [](const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands, int32_t target) -> bool {
        for (int j = 0; j < instr.operand_count; j++)
        {
            if (operands[j].type == ZYDIS_OPERAND_TYPE_MEMORY
                && operands[j].mem.disp.has_displacement
                && static_cast<int32_t>(operands[j].mem.disp.value) == target
                && operands[j].mem.index == ZYDIS_REGISTER_NONE
                && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
            {
                return true;
            }
        }
        return false;
    };

    for (auto i = static_cast<int32_t>(vfuncs.size()) - 1; i >= 0; i--)
    {
        auto range      = svr_mod->GetFunctionRange(vfuncs[i]);
        auto scan_start = vfuncs[i];
        auto scan_end   = range ? range->end : scan_start + 512;

        bool has_damage       = false;
        bool has_controlled   = false;
        bool has_team_changed = false;

        ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (!range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                return true;

            if (!has_damage && accesses_offset(instr, operands, damage_service_offset))
                has_damage = true;
            if (!has_controlled && accesses_offset(instr, operands, controlled_bot_this_round_offset))
                has_controlled = true;
            if (!has_team_changed && accesses_offset(instr, operands, team_changed_offset))
                has_team_changed = true;

            return has_damage && has_controlled && has_team_changed;
        });

        if (has_damage && has_controlled && has_team_changed)
        {
            result_idx = i;
            break;
        }
    }

    try_overwrite_vfunc("CCSPlayerController::RoundRespawn", result_idx);
}

void ResolveCBasePlayerPawn_CommitSuicide()
{
    auto svr_mod = modules::server;

    auto vfuncs = svr_mod->GetVFunctionsFromVTable("CBasePlayerPawn");
    if (vfuncs.empty())
    {
        WARN("No vfuncs was found for CBasePlayerPawn??");
        return;
    }

    constexpr uint32_t flt_5_0 = 0x40A00000;
    constexpr uint32_t flt_1_0 = 0x3F800000;

    auto next_suicide_time_offset = schemas::GetOffset("CBasePlayerPawn", "m_fNextSuicideTime").offset;
    if (next_suicide_time_offset <= 0)
    {
        WARN("Cannot find offset for CBasePlayerPawn->m_fNextSuicideTime");
        return;
    }

    // CommitSuicide accesses m_fNextSuicideTime and loads float constants 5.0 and 1.0
    int32_t result_idx = -1;

    for (auto i = static_cast<int32_t>(vfuncs.size()) - 1; i >= 0; i--)
    {
        auto range      = svr_mod->GetFunctionRange(vfuncs[i]);
        auto scan_start = vfuncs[i];
        auto scan_end   = range ? range->end : scan_start + 512;

        bool has_suicide_time = false;
        bool has_flt_5        = false;
        bool has_flt_1        = false;

        ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            if (!range && instr.mnemonic == ZYDIS_MNEMONIC_RET)
                return true;

            for (int j = 0; j < instr.operand_count; j++)
            {
                // Check memory operand with displacement for m_fNextSuicideTime
                if (operands[j].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && operands[j].mem.disp.has_displacement
                    && operands[j].mem.index == ZYDIS_REGISTER_NONE
                    && !(instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                    && static_cast<int32_t>(operands[j].mem.disp.value) == next_suicide_time_offset)
                {
                    has_suicide_time = true;
                }

                // Float constants loaded via RIP-relative (e.g. movss xmm, [rip+disp])
                if (operands[j].type == ZYDIS_OPERAND_TYPE_MEMORY
                    && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                    && operands[j].mem.base == ZYDIS_REGISTER_RIP)
                {
                    auto target = ip + instr.length + operands[j].mem.disp.value;
                    auto val    = *reinterpret_cast<const uint32_t*>(target);
                    if (val == flt_5_0) has_flt_5 = true;
                    if (val == flt_1_0) has_flt_1 = true;
                }

                // Float constants as immediate
                if (operands[j].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
                {
                    auto val = static_cast<uint32_t>(operands[j].imm.value.u);
                    if (val == flt_5_0) has_flt_5 = true;
                    if (val == flt_1_0) has_flt_1 = true;
                }
            }

            return has_suicide_time && has_flt_5 && has_flt_1;
        });

        if (has_suicide_time && has_flt_5 && has_flt_1)
        {
            result_idx = i;
            break;
        }
    }

    try_overwrite_vfunc("CBasePlayerPawn::CommitSuicide", result_idx);
}

void ResolveCCSPlayerPawn_SetDefaultGloves()
{
    auto svr_mod = modules::server;

    uintptr_t addr{};

    constexpr std::string_view token_str = "first_or_third_person";

    constexpr uint32_t token = MurmurHash2(token_str, MURMURHASH_SEED);
    static_assert(token == 0x3C74EB85, "Token for first_or_third_person mismatched");

    auto token_address = svr_mod->FindData(reinterpret_cast<const uint8_t*>(&token), sizeof(uint32_t), false);
    if (token_address.IsValid())
    {
        auto references = svr_mod->GetReferenceRange(token_address);

        std::unordered_set<uintptr_t> sets{};

        ZydisDecodedInstruction instr{};
        ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

        for (auto [target, source_ip] : references)
        {
            if (auto entry = svr_mod->GetFunctionRange(source_ip))
            {
                if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&ZydisUtility::DefaultDecoder,
                                                         reinterpret_cast<const void*>(source_ip),
                                                         ZYDIS_MAX_INSTRUCTION_LENGTH,
                                                         &instr, operands)))
                {
                    continue;
                }

                bool is_read_only_reference = false;

                for (uint8_t i = 0; i < instr.operand_count; ++i)
                {
                    const auto& operand = operands[i];

                    if (operand.type == ZYDIS_OPERAND_TYPE_MEMORY)
                    {
                        bool is_read  = false;
                        bool is_write = false;

                        switch (operand.actions)
                        {
                        case ZYDIS_OPERAND_ACTION_READ:
                        case ZYDIS_OPERAND_ACTION_CONDREAD:
                            is_read = true;
                            break;
                        case ZYDIS_OPERAND_ACTION_WRITE:
                        case ZYDIS_OPERAND_ACTION_CONDWRITE:
                            is_write = true;
                            break;
                        case ZYDIS_OPERAND_ACTION_READWRITE:
                        case ZYDIS_OPERAND_ACTION_READ_CONDWRITE:
                        case ZYDIS_OPERAND_ACTION_CONDREAD_WRITE:
                            is_read  = true;
                            is_write = true;
                            break;
                        default: break;
                        }

                        if (is_read && !is_write)
                        {
                            is_read_only_reference = true;
                            break;
                        }
                    }
                }

                if (is_read_only_reference)
                {
                    sets.insert(entry->start);
                }
            }
        }

        auto size = sets.size();
        if (size != 1)
        {
            WARN("Expected to have one function that references token '%s' but got %zu, falling back to signature", token_str.data(), size);
        }
        else
        {
            addr = *sets.begin();
        }
    }

    AssignOrFallback(svr_mod, address::server::CCSPlayerPawn_SetDefaultGloves, addr, "CCSPlayerPawn::SetDefaultGloves");
}

void ResolveCBasePlayerPawn_SnapViewAngles()
{
    auto svr_mod = modules::server;

    constexpr std::string_view usage_substr = "Usage:  setang pitch yaw";
    auto                       usage_str    = svr_mod->FindData(
        reinterpret_cast<const uint8_t*>(usage_substr.data()),
        usage_substr.size(),
        false);
    if (!usage_str.IsValid()) [[unlikely]]
    {
        WARN("Failed to find 'Usage:  setang pitch yaw ...' string.");
        return;
    }

    auto refs = svr_mod->GetReferenceRange(usage_str);
    if (refs.empty())
    {
        WARN("No references to 'Usage:  setang pitch yaw' string.");
        return;
    }

    // Collect unique containing functions from the references
    std::unordered_set<uintptr_t> handler_candidates;
    for (const auto& ref : refs)
    {
        auto entry = svr_mod->GetFunctionRange(ref.source_ip);
        if (entry)
            handler_candidates.insert(entry->start);
    }

    if (handler_candidates.size() != 1)
    {
        WARN("Expected 1 handler for setang, got %zu.", handler_candidates.size());
        return;
    }

    auto handler_addr  = *handler_candidates.begin();
    auto handler_range = svr_mod->GetFunctionRange(handler_addr);
    if (!handler_range)
    {
        WARN("Failed to get function range for setang handler.");
        return;
    }

    FLOG("Found setang handler at server+0x%llx", handler_addr - svr_mod->Base());

    // Collect every RELATIVE CALL target in the handler body
    std::vector<uintptr_t> call_targets;

    ZydisUtility::ScanInstructions(handler_range->start, handler_range->end, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        if (instr.mnemonic == ZYDIS_MNEMONIC_CALL
            && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
            && operands[0].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
        {
            auto target = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
            if (target != 0 && svr_mod->IsInModule(target))
                call_targets.push_back(target);
        }
        return false;
    });

    if (call_targets.empty())
    {
        WARN("No relative call targets found in setang handler.");
        return;
    }

    // Verification: the real SnapViewAngles writes to the "v_angle" QAngle field
    // (and also reads from it — it saves the old value to v_anglePrevious).
    // Most other functions called from the handler do not touch v_angle at all.
    auto v_angle_offset = schemas::GetOffset("CBasePlayerPawn", "v_angle").offset;
    if (v_angle_offset <= 0)
    {
        WARN("Failed to get schema offset for CBasePlayerPawn::v_angle.");
        return;
    }

    uintptr_t snap_view_angles_addr = 0;

    for (auto target : call_targets)
    {
        auto      target_range = svr_mod->GetFunctionRange(target);
        uintptr_t scan_start   = target;
        uintptr_t scan_end     = target_range ? target_range->end : (target + 512);

        bool reads_v_angle  = false;
        bool writes_v_angle = false;

        ZydisUtility::ScanInstructions(scan_start, scan_end, [&](uintptr_t ip2, const ZydisDecodedInstruction& instr2, const ZydisDecodedOperand* operands2) -> bool {
            for (int i = 0; i < instr2.operand_count; i++)
            {
                if (operands2[i].type != ZYDIS_OPERAND_TYPE_MEMORY)
                    continue;

                auto& mem = operands2[i].mem;
                if (!mem.disp.has_displacement)
                    continue;

                if (static_cast<int32_t>(mem.disp.value) != v_angle_offset)
                    continue;

                if (mem.index != ZYDIS_REGISTER_NONE)
                    continue;

                // Exclude RIP-relative accesses (global data, not this pointer)
                auto base = ZydisUtility::GetBaseRegister(mem.base);
                if (base == ZYDIS_REGISTER_RIP || base == ZYDIS_REGISTER_RSP || base == ZYDIS_REGISTER_RBP)
                    continue;

                if (operands2[i].actions & ZYDIS_OPERAND_ACTION_READ)
                    reads_v_angle = true;
                if (operands2[i].actions & ZYDIS_OPERAND_ACTION_WRITE)
                    writes_v_angle = true;
            }

            return false;
        });

        if (reads_v_angle && writes_v_angle)
        {
            snap_view_angles_addr = target;
            break;
        }
    }

    AssignOrFallback(svr_mod, address::server::CBasePlayerPawn_SnapViewAngles,
                     snap_view_angles_addr, "CBasePlayerPawn::SnapViewAngles");
}

void ResolveCreateTriggerInternal()
{
    if (address::server::CreateTriggerInternal != nullptr) return;

    auto svr_mod = modules::server;

    auto str_addr = svr_mod->FindString("Script_CreateTrigger", false, true);
    if (!str_addr.IsValid())
    {
        WARN("Failed to find string 'Script_CreateTrigger'");
        return;
    }

    auto ptr_addr = svr_mod->FindPtr(str_addr.GetPtr());
    if (!ptr_addr.IsValid())
    {
        WARN("Failed to find pointer to 'Script_CreateTrigger' string");
        return;
    }

    auto wrapper_func = *reinterpret_cast<uintptr_t*>(ptr_addr.GetPtr() + 0x38);
    if (!svr_mod->IsInModule(wrapper_func))
    {
        WARN("Function pointer at Script_CreateTrigger descriptor+0x38 is not in module");
        return;
    }

    uintptr_t target_call_addr = 0;

    ZydisUtility::ScanInstructions(wrapper_func, wrapper_func + 256, [&](uintptr_t ip, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
        if (instr.mnemonic == ZYDIS_MNEMONIC_CALL
            && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
            && operands[0].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
        {
            auto target = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
            if (target != 0 && target != wrapper_func)
            {
                target_call_addr = target;
                return true;
            }
        }
        return false;
    });

    AssignOrFallback(svr_mod, address::server::CreateTriggerInternal, target_call_addr, "CreateTriggerInternal");
}

// CBaseTrigger::PassesTriggerFilters is the first virtual CBaseTrigger declares, so its index is
// exactly the length of its base's vtable. CBaseTrigger appends 8 slots and nothing else in that
// block touches m_spawnflags - four are nullsubs, two are small helpers. Both facts held on server
// builds 19747060, 21411853, 23333587, 23669931 and 24116939 on Windows and Linux; the 24116939
// index shift (win 273 -> 269) came entirely from CBaseModelEntity shedding four vfuncs.
//
// The scan must stay inside the trigger block. On Linux a base-class vfunc (the trigger's Spawn,
// which ORs bit 1 into m_spawnflags) reads the field too, so a whole-vtable scan is unique on
// Windows only.
void ResolveCBaseTrigger_PassesTriggerFilters()
{
    static_assert(ZYDIS_REGISTER_R15 - ZYDIS_REGISTER_RAX == 15, "Zydis GPR64 enum is no longer contiguous");

    const auto svr_mod = modules::server;

    const auto trigger = svr_mod->GetVFunctionsFromVTable("CBaseTrigger");
    if (trigger.empty())
    {
        WARN("Failed to get CBaseTrigger vtable.");
        return;
    }

    // CBaseToggle declares no virtuals of its own, so CBaseModelEntity yields the same bound and
    // stands in if CBaseToggle's RTTI ever disappears.
    auto base_size = svr_mod->GetVFunctionsFromVTable("CBaseToggle").size();
    if (base_size == 0)
        base_size = svr_mod->GetVFunctionsFromVTable("CBaseModelEntity").size();

    if (base_size == 0 || base_size >= trigger.size())
    {
        WARN("CBaseTrigger vtable (%zu) does not extend its base (%zu).", trigger.size(), base_size);
        return;
    }

    const auto spawnflags_offset = schemas::GetOffset("CBaseEntity", "m_spawnflags").offset;
    if (spawnflags_offset <= 0)
    {
        WARN("Failed to get schema offset for CBaseEntity::m_spawnflags.");
        return;
    }

    const auto reg_bit = [](ZydisRegister reg) -> uint16_t {
        const auto base = ZydisUtility::GetBaseRegister(reg);
        if (base < ZYDIS_REGISTER_RAX || base > ZYDIS_REGISTER_R15)
            return 0;
        return static_cast<uint16_t>(1u << (base - ZYDIS_REGISTER_RAX));
    };

    // `this` is copied out of the argument register on entry (rsi on Windows, r12 on Linux) and only
    // the first of the four reads goes through kThisReg, so the copies have to be followed.
    const auto reads_spawnflags = [&](const uintptr_t vfunc) {
        const auto* range = svr_mod->GetFunctionRange(vfunc);
        if (range == nullptr)
            return false;

        uint16_t this_regs = reg_bit(address_scan::kThisReg);
        bool     found     = false;

        ZydisUtility::ScanInstructions(range->start, range->end, [&](uintptr_t, const ZydisDecodedInstruction& instr, const ZydisDecodedOperand* operands) -> bool {
            for (uint8_t i = 0; i < instr.operand_count_visible; i++)
            {
                const auto& op = operands[i];

                if (op.type == ZYDIS_OPERAND_TYPE_MEMORY
                    && op.mem.index == ZYDIS_REGISTER_NONE
                    && op.mem.disp.has_displacement
                    && static_cast<int32_t>(op.mem.disp.value) == spawnflags_offset
                    && (this_regs & reg_bit(op.mem.base)) != 0)
                {
                    found = true;
                    return true;
                }
            }

            // Propagate `this` through register-to-register moves, drop it on any other write.
            if (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].type == ZYDIS_OPERAND_TYPE_REGISTER
                && operands[1].size == 64
                && (this_regs & reg_bit(operands[1].reg.value)) != 0)
            {
                this_regs |= reg_bit(operands[0].reg.value);
                return false;
            }

            for (uint8_t i = 0; i < instr.operand_count_visible; i++)
            {
                const auto& op = operands[i];

                if (op.type == ZYDIS_OPERAND_TYPE_REGISTER && (op.actions & ZYDIS_OPERAND_ACTION_MASK_WRITE) != 0)
                    this_regs &= static_cast<uint16_t>(~reg_bit(op.reg.value));
            }

            return false;
        });

        return found;
    };

    std::vector<int32_t> candidates{};
    for (auto i = base_size; i < trigger.size(); i++)
    {
        if (reads_spawnflags(trigger[i]))
            candidates.emplace_back(static_cast<int32_t>(i));
    }

    // Two independent derivations: the lone m_spawnflags reader in the trigger block, and the first
    // slot past the base vtable. Agreement makes the vote corroborated and lets it beat gamedata.
    ResolveVote vote{};
    VoteCandidates(vote, candidates);
    vote.Add(static_cast<int32_t>(base_size));

    try_overwrite_vfunc("CBaseTrigger::PassesTriggerFilters", vote);
}