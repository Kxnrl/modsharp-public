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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 72)]
public unsafe ref struct CTraceFilter
{
    private static readonly nint IterateEntitiesOffset =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? 0x3A : 0x40;

    [FieldOffset(0)]
    public CTraceFilterVirtualTableDescriptor* Vtable;

    [FieldOffset(8)]
    public RnQueryShapeAttr QueryAttribute;

    private ref byte Base
    {
        get
        {
            fixed (void* ptr = &this)
            {
                return ref Unsafe.AsRef<byte>(ptr);
            }
        }
    }

    /// <remarks>
    ///     On Linux it lies in the tail padding of <see cref="QueryAttribute" />, so write it after assigning
    ///     <see cref="QueryAttribute" /> as a whole
    /// </remarks>
    public bool m_bIterateEntities
    {
        get => Unsafe.ReadUnaligned<bool>(ref Unsafe.AddByteOffset(ref Base, IterateEntitiesOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,      IterateEntitiesOffset), value);
    }
}

public unsafe struct CTraceFilterVirtualTableDescriptor
{
    public delegate* unmanaged<nint, void>                Deconstructor;
    public delegate* unmanaged<CTraceFilter*, nint, bool> ShouldHitEntity;
}
