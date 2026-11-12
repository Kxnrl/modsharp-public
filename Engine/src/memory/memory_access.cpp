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