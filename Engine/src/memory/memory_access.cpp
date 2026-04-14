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

#include "memory_access.h"

#include <Zydis.h>
#include <safetyhook.hpp>

#ifndef PLATFORM_WINDOWS
#    include <sys/mman.h>
#else
#    ifndef WIN32_LEAN_AND_MEAN
#        define WIN32_LEAN_AND_MEAN
#endif
#    include <windows.h>
#endif

bool SetMemoryAccess(uint8_t* address, size_t size, uint8_t access, uint8_t* old_access)
{
    const bool read    = (access & MemoryAccess_Read) != 0;
    const bool write   = (access & MemoryAccess_Write) != 0;
    const bool execute = (access & MemoryAccess_Execute) != 0;

    safetyhook::VmAccess target_access{.read = read, .write = write, .execute = execute};
    const auto           result = safetyhook::vm_protect(address, size, target_access);

    if (!result.has_value())
        return false;

    if (old_access != nullptr)
    {
        uint32_t os_old_protect = result.value();

#ifdef PLATFORM_WINDOWS
        if (os_old_protect == PAGE_EXECUTE_READWRITE)
        {
            *old_access = MemoryAccess_Read | MemoryAccess_Write | MemoryAccess_Execute;
        }
        else if (os_old_protect == PAGE_EXECUTE_READ)
        {
            *old_access = MemoryAccess_Read | MemoryAccess_Execute;
        }
        else if (os_old_protect == PAGE_READWRITE)
        {
            *old_access = MemoryAccess_Read | MemoryAccess_Write;
        }
        else if (os_old_protect == PAGE_READONLY)
        {
            *old_access = MemoryAccess_Read;
        }
#else
        if (os_old_protect & PROT_READ) *old_access |= MemoryAccess_Read;
        if (os_old_protect & PROT_WRITE) *old_access |= MemoryAccess_Write;
        if (os_old_protect & PROT_EXEC) *old_access |= MemoryAccess_Execute;
#endif
    }

    return true;
}
uintptr_t ResolveCallTarget(ZydisDecoder* decoder, ZydisDecodedInstruction* instr, ZydisDecodedOperand* operands, uintptr_t current_ip)
{
    ZyanU64 raw_target = 0;
    if (!ZYAN_SUCCESS(ZydisCalcAbsoluteAddress(instr, &operands[0], current_ip, &raw_target)))
        return 0;

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
            if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(decoder, plt_cursor, ZYDIS_MAX_INSTRUCTION_LENGTH, &plt_instr, plt_operands))) break;

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