/* 
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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

#include "global.h"

#include "cstrike/interface/IMemAlloc.h"

inline void* AllocUnattributed(size_t nSize)
{
    return g_pMemAlloc->IndirectAlloc(nSize);
}

#ifdef PLATFORM_WINDOWS
void* __cdecl operator new(size_t nSize)
{
    return AllocUnattributed(nSize);
}

void* __cdecl operator new(size_t nSize, int nBlockUse, const char* pFileName, int nLine)
{
    return AllocUnattributed(nSize);
}

void __cdecl operator delete(void* pMem)
{
    g_pMemAlloc->Free(pMem);
}

void* __cdecl operator new[](size_t nSize)
{
    return AllocUnattributed(nSize);
}

void* __cdecl operator new[](size_t nSize, int nBlockUse, const char* pFileName, int nLine)
{
    return AllocUnattributed(nSize);
}

void __cdecl operator delete[](void* pMem)
{
    g_pMemAlloc->Free(pMem);
}
#endif

void* AllocateMemory(size_t size)
{
    return g_pMemAlloc->Alloc(size);
}

void* ReallocateMemory(void* mem, size_t size)
{
    return g_pMemAlloc->Realloc(mem, size);
}

void FreeMemory(void* mem)
{
    g_pMemAlloc->Free(mem);
}

std::size_t GetMemorySize(void* mem)
{
    return g_pMemAlloc->GetSize(mem);
}