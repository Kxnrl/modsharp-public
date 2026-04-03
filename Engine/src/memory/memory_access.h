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

#ifndef MS_MEMORY_MEMORY_ACCESS_H
#define MS_MEMORY_MEMORY_ACCESS_H

#include <cstddef>
#include <cstdint>

enum MemoryAccess : uint8_t
{
    MemoryAccess_Read    = 1,
    MemoryAccess_Write   = 2,
    MemoryAccess_Execute = 4,
};

constexpr uint8_t g_nReadOnlyAccess         = MemoryAccess_Read;
constexpr uint8_t g_nReadExecuteAccess      = MemoryAccess_Read | MemoryAccess_Execute;
constexpr uint8_t g_nReadWriteAccess        = MemoryAccess_Read | MemoryAccess_Write;
constexpr uint8_t g_nReadWriteExecuteAccess = MemoryAccess_Read | MemoryAccess_Write | MemoryAccess_Execute;

bool SetMemoryAccess(uint8_t* address, size_t size, uint8_t access, uint8_t* old_access);

#endif