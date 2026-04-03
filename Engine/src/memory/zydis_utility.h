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

#ifndef MS_MEMORY_ZYDIS_H
#define MS_MEMORY_ZYDIS_H

#include <concepts>
#include <Zydis.h>

namespace ZydisUtility
{
inline const ZydisDecoder DefaultDecoder = []() {
    ZydisDecoder d{};
    ZydisDecoderInit(&d, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64);
    return d;
}();

inline ZydisRegister GetBaseRegister(ZydisRegister reg)
{
    if (reg == ZYDIS_REGISTER_NONE) return ZYDIS_REGISTER_NONE;

    return ZydisRegisterGetLargestEnclosing(ZYDIS_MACHINE_MODE_LONG_64, reg);
}

inline bool IsVolatileRegister(ZydisRegister reg)
{
    ZydisRegister base_reg = GetBaseRegister(reg);

    switch (base_reg)
    {
    case ZYDIS_REGISTER_RAX:
    case ZYDIS_REGISTER_RCX:
    case ZYDIS_REGISTER_RDX:
    case ZYDIS_REGISTER_R8:
    case ZYDIS_REGISTER_R9:
    case ZYDIS_REGISTER_R10:
    case ZYDIS_REGISTER_R11: return true;

#ifdef PLATFORM_LINUX
    case ZYDIS_REGISTER_RDI:
    case ZYDIS_REGISTER_RSI: return true;
#endif
    default: return false;
    }
}

inline uintptr_t ResolveCallTarget(const ZydisDecodedInstruction* instr, const ZydisDecodedOperand* operands, uintptr_t current_ip)
{
    ZyanU64 raw_target = 0;
    if (!ZYAN_SUCCESS(ZydisCalcAbsoluteAddress(instr, &operands[0], current_ip, &raw_target))) return 0;

    if (instr->opcode == 0xFF && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY)
    {
        return *reinterpret_cast<uintptr_t*>(raw_target);
    }

    if (instr->opcode == 0xE8)
    {
        uintptr_t target_addr = raw_target;

        ZydisDecodedInstruction plt_instr;
        ZydisDecodedOperand     plt_operands[ZYDIS_MAX_OPERAND_COUNT];
        auto                    plt_cursor = reinterpret_cast<uint8_t*>(target_addr);

        for (int i = 0; i < 3; i++)
        {
            if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&DefaultDecoder, plt_cursor, ZYDIS_MAX_INSTRUCTION_LENGTH, &plt_instr, plt_operands))) break;

            if (plt_instr.mnemonic == ZYDIS_MNEMONIC_JMP && plt_cursor[plt_instr.raw.modrm.offset] == 0x25 && plt_operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY)
            {
                ZyanU64 got_entry = 0;
                if (ZYAN_SUCCESS(ZydisCalcAbsoluteAddress(&plt_instr, &plt_operands[0], (ZyanU64)plt_cursor, &got_entry)))
                {
                    return *reinterpret_cast<uintptr_t*>(got_entry);
                }
            }

            if (plt_instr.mnemonic == ZYDIS_MNEMONIC_RET || plt_instr.mnemonic == ZYDIS_MNEMONIC_INT3) break;

            plt_cursor += plt_instr.length;
        }

        return target_addr;
    }

    return 0;
}

inline uintptr_t GetAbsoluteAddress(const ZydisDecodedInstruction& instr, const ZydisDecodedOperand& operand, uintptr_t ip)
{
    uintptr_t abs_addr = 0;
    if (ZYAN_SUCCESS(ZydisCalcAbsoluteAddress(&instr, &operand, ip, &abs_addr))) return abs_addr;
    return 0;
}

template <typename Callback> requires std::predicate<Callback, uintptr_t, const ZydisDecodedInstruction&, const ZydisDecodedOperand*>
inline void ScanInstructions(uintptr_t start, uintptr_t end, Callback callback)
{
    ZydisDecodedInstruction instr{};
    ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

    for (auto ip = start; ip < end;)
    {
        if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&DefaultDecoder, reinterpret_cast<const void*>(ip), ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, operands))) break;

        if (callback(ip, instr, operands)) break;

        ip += instr.length;
    }
}
} // namespace ZydisUtility

#endif  