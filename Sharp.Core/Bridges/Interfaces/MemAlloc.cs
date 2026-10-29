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

using System;
using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared.Objects;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal partial class MemAlloc : NativeObject, IMemAlloc
{
    public partial nint AllocEx(nuint size);

    public partial nint ReAllocEx(nint pMem, nuint size);

    public partial void Free(nint pMem);

    public nint Alloc(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        return AllocEx((nuint) size);
    }

    public nint ReAlloc(nint pMem, int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        return ReAllocEx(pMem, (nuint) size);
    }
}
