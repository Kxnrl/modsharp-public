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

using System.Runtime.InteropServices;

namespace Sharp.Shared;

/// <summary>
///     如果你不知道你在做什么, 你就不要用这个 <br />
///     <remarks>手贱可能会导致意想不到的结果</remarks>
/// </summary>
public static unsafe class MemoryAllocator
{
    [DllImport("tier0", CallingConvention = CallingConvention.Cdecl)]
    private static extern void* MemAlloc_AllocFunc(nuint size);

    [DllImport("tier0", CallingConvention = CallingConvention.Cdecl)]
    private static extern void MemAlloc_FreeFunc(void* ptr);

    [DllImport("tier0", CallingConvention = CallingConvention.Cdecl)]
    private static extern void* MemAlloc_ReallocFunc(void* ptr, nuint size);

    [DllImport("tier0", CallingConvention = CallingConvention.Cdecl)]
    private static extern nuint MemAlloc_GetSizeFunc(void* ptr);

    [DllImport("tier0", CallingConvention = CallingConvention.Cdecl)]
    private static extern void V_tier0_memmove(void* dest, void* src, nuint count);

    [DllImport("tier0", CallingConvention = CallingConvention.Cdecl)]
    private static extern void* UtlVectorMemory_Alloc(void* pMem, bool bRealloc, int nNewSize, int nOldSize);

    public static void* Alloc(nuint size)
        => MemAlloc_AllocFunc(size);

    public static void* Realloc(void* ptr, nuint size)
        => MemAlloc_ReallocFunc(ptr, size);

    public static void Free(void* ptr)
        => MemAlloc_FreeFunc(ptr);

    public static nuint GetSize(void* ptr)
        => MemAlloc_GetSizeFunc(ptr);

    public static void MemMove(void* dest, void* src, nuint count)
        => V_tier0_memmove(dest, src, count);

    public static void* VectorMemory_Alloc(void* pMem, bool bRealloc, int nNewSize, int nOldSize)
        => UtlVectorMemory_Alloc(pMem, bRealloc, nNewSize, nOldSize);
}
