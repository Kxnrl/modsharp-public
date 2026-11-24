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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sharp.Shared.Types.Tier;

[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 24)]
public unsafe struct CUtlVector<T> : IDisposable, IEnumerable<T> where T : unmanaged
{
    private int           _size;
    private CUtlMemory<T> _memory;

    public int Size  => _size;
    public int Count => _size;

    public CUtlVector(int growSize = 0, int initSize = 0)
        => _memory = new CUtlMemory<T>(growSize, initSize);

    public void Dispose()
    {
        RemoveAll();
    }

    public Span<T> AsSpan()
        => _memory.AsSpan()[.._size];

    public ReadOnlySpan<T> AsReadOnlySpan()
        => _memory.AsReadOnlySpan()[.._size];

    public T* Base()
        => _memory.Base();

    public ref T this[long index] => ref Element(index);

    public ref T Element(long index)
    {
        if (index >= _size || index < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        return ref _memory[index];
    }

    public Enumerator GetEnumerator()
        => new (this);

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable<T>) this).GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => GetEnumerator();

    private void GrowVector(int num)
    {
        if (_size + num > _memory.AllocationCount)
        {
            _memory.Grow((_size + num) - _memory.AllocationCount);
        }

        _size += num;
    }

    public void Add(T item)
        => Add(ref item);

    public void Add(ref readonly T item)
        => Insert(_size, in item);

    private void Insert(int index, T item)
        => Insert(index, ref item);

    private void Insert(int index, ref readonly T item)
    {
        if (index < 0 || index > _size)
        {
            throw new ArgumentOutOfRangeException();
        }

        GrowVector(1);
        ShiftElementsRight(index);
        _memory[index] = item;
    }

    public ref T AddToTailGetRef()
    {
        var newIndex = _size;
        GrowVector(1);

        return ref _memory[newIndex];
    }

    public T* AddToTailGetPtr()
    {
        var newIndex = _size;
        GrowVector(1);

        return (T*) Unsafe.AsPointer(ref _memory[newIndex]);
    }

    private void ShiftElementsRight(int index, int num = 1)
    {
        if (_size == 0 || num == 0)
        {
            throw new InvalidOperationException();
        }

        var numToMove = _size - index - num;

        if (numToMove > 0 && num > 0)
        {
            var pSource = Unsafe.AsPointer(ref _memory[index]);
            var pDest   = Unsafe.AsPointer(ref _memory[index + num]);
            var size    = (nuint) (numToMove * Unsafe.SizeOf<T>());

            MemoryAllocator.MemMove(pDest, pSource, size);
        }
    }

    private void ShiftElementsLeft(int index, int num = 1)
    {
        if (_size == 0 || num <= 0)
        {
            throw new InvalidOperationException();
        }

        var numToMove = _size - index - num;

        if (numToMove > 0)
        {
            var pSource      = Unsafe.AsPointer(ref _memory[index + num]);
            var pDestination = Unsafe.AsPointer(ref _memory[index]);
            var size         = (nuint) (numToMove * Unsafe.SizeOf<T>());

            MemoryAllocator.MemMove(pDestination, pSource, size);
        }
    }

    public void Remove(int index)
    {
        if ((uint) index >= (uint) _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ShiftElementsLeft(index);
        --_size;
        _memory[_size] = default;
    }

    /// <summary>
    ///     Frees the unmanaged memory pointed to by each element, then clears and deallocates the vector's own memory.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method MUST only be used when <typeparamref name="T" /> is a pointer type,
    ///         this includes:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Native pointer types (e.g., <see cref="IntPtr" />, <c>void*</c>).</description>
    ///             </item>
    ///             <item>
    ///                 <description>The <see cref="Pointer{T}" />  struct, as its only field is a pointer.</description>
    ///             </item>
    ///         </list>
    ///         Using this on a vector of other types (like <see cref="int" /> or a different struct) will cause heap
    ///         corruption and crash the application.
    ///     </para>
    /// </remarks>
    public void PurgeAndDeleteElements()
    {
        var memoryPtr = (void**) _memory.Base();

        for (var i = 0; i < _size; i++)
        {
            var ptr = memoryPtr[i];

            if (ptr is null)
            {
                continue;
            }

            MemoryAllocator.Free(ptr);
        }

        RemoveAll();
        _memory.Purge();
    }

    public void Purge()
    {
        RemoveAll();
        _memory.Purge();
    }

    public void RemoveAll()
    {
        for (var i = _size - 1; i >= 0; i--)
        {
            _memory[i] = default;
        }

        _size = 0;
    }

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly CUtlVector<T> _vector;
        private          int           _index;

        public Enumerator(CUtlVector<T> vector)
        {
            _vector = vector;
            _index  = -1;
        }

        public bool MoveNext()
        {
            _index++;

            return _index < _vector.Size;
        }

        public void Reset()
        {
            _index = -1;
        }

        public T Current => _vector[_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}
