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

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 64)]
public unsafe ref struct CTraceFilter
{
    [FieldOffset(0)]
    public CTraceFilterVTableDescriptor* Vtable;

    [FieldOffset(8)]
    public RnQueryShapeAttr QueryAttribute;

    [FieldOffset(56)]
    public bool m_bIterateEntities;
}

public unsafe struct CTraceFilterVTableDescriptor
{
    public delegate* unmanaged<nint, void>                Deconstructor;
    public delegate* unmanaged<CTraceFilter*, nint, bool> ShouldHitEntity;
}
