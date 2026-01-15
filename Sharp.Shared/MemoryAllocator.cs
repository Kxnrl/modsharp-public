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
using Sharp.Shared.Types.Tier;

namespace Sharp.Shared;

/// <summary>
///     Provides direct access to the Source Engine's internal memory allocator.
/// </summary>
/// <remarks>
///     <b>DANGER: HIGH RISK API</b>
///     <br />
///     This class interacts directly with the Game's Native Heap.
///     <list type="bullet">
///         <item>
///             <description>
///                 Memory allocated here <b>MUST</b> be freed using <see cref="Free(void*)" />. Do NOT use
///                 <see cref="System.Runtime.InteropServices.Marshal.FreeHGlobal(nint)" />.
///             </description>
///         </item>
///         <item>
///             <description>You cannot pass pointers created here to standard .NET marshalling without manual management.</description>
///         </item>
///         <item>
///             <description>
///                 Use this only when you need to pass raw memory ownership <b>to</b> the game engine.
///             </description>
///         </item>
///     </list>
/// </remarks>
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

    /// <summary>
    ///     Allocates a block of memory on the native game heap.
    /// </summary>
    /// <param name="size">The size of the memory block, in bytes.</param>
    /// <returns>A pointer to the allocated memory, or null if allocation failed.</returns>
    public static void* Alloc(nuint size)
        => MemAlloc_AllocFunc(size);

    /// <summary>
    ///     Reallocates a block of memory on the native game heap, preserving its content.
    /// </summary>
    /// <param name="ptr">Pointer to the existing memory block.</param>
    /// <param name="size">The new size in bytes.</param>
    /// <returns>A pointer to the reallocated memory (which may have moved), or null if failed.</returns>
    public static void* Realloc(void* ptr, nuint size)
        => MemAlloc_ReallocFunc(ptr, size);

    /// <summary>
    ///     Frees a memory block previously allocated by <see cref="Alloc" /> or <see cref="Realloc" />.
    /// </summary>
    /// <param name="ptr">The pointer to free. If null, this method does nothing.</param>
    public static void Free(void* ptr)
        => MemAlloc_FreeFunc(ptr);

    /// <summary>
    ///     Gets the size of a memory block allocated on the native game heap.
    /// </summary>
    public static nuint GetSize(void* ptr)
        => MemAlloc_GetSizeFunc(ptr);

    /// <summary>
    ///     Copies bytes from a source to a destination. Handles overlapping memory regions correctly.
    /// </summary>
    public static void MemMove(void* dest, void* src, nuint count)
        => V_tier0_memmove(dest, src, count);

    /// <summary>
    ///     Internal allocator used specifically for Source Engine's <see cref="CUtlVector{T}" /> growth strategy.
    ///     <br />
    ///     You likely do not need this unless you are manually implementing <see cref="CUtlVector{T}" /> logic.
    /// </summary>
    public static void* VectorMemory_Alloc(void* pMem, bool bRealloc, int nNewSize, int nOldSize)
        => UtlVectorMemory_Alloc(pMem, bRealloc, nNewSize, nOldSize);
}
