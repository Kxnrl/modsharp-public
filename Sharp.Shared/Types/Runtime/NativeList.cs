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

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sharp.Shared.Types.Runtime;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct NativeList<T> where T : unmanaged
{
    private readonly T* Memory;

    public readonly int Count;

    public ref T this[int index] => ref Memory[index];

    /// <summary>
    ///     Note: The Enumerator yields copies by value. Use the indexer for by-reference access.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }
}
