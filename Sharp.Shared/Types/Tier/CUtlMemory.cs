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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sharp.Shared.Types.Tier;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct CUtlMemory<T> : IDisposable
    where T : unmanaged
{
    private T*  _memory;
    private int _allocationCount;
    private int _growSize;

    private const int ExternalConstBufferMarker = 1 << 30;
    private const int ExternalBufferMarker      = 1 << 31;

    public int  AllocationCount       => _allocationCount;
    public bool IsReadOnly            => (_growSize & ExternalConstBufferMarker)                          != 0;
    public bool IsExternallyAllocated => (_growSize & (ExternalConstBufferMarker | ExternalBufferMarker)) != 0;

    public CUtlMemory(int growSize, int initAllocationCount)
    {
        _allocationCount = initAllocationCount;
        _growSize        = growSize & ~(ExternalConstBufferMarker | ExternalBufferMarker);

        if (_allocationCount > 0)
        {
            _memory = (T*) MemoryAllocator.Alloc((nuint) (_allocationCount * Unsafe.SizeOf<T>()));
        }
    }

    public void Dispose()
    {
        Purge();
    }

    public T* Base()
        => _memory;

    public Span<T> AsSpan()
        => new (_memory, _allocationCount);

    public ReadOnlySpan<T> AsReadOnlySpan()
        => new (_memory, _allocationCount);

    public ref T this[long index] => ref _memory[index];

    private int CalcNewAllocationCount(int nAllocationCount, int nGrowSize, int nNewSize, int nBytesItem)
    {
        if (nGrowSize > 0)
        {
            nAllocationCount = (1 + ((nNewSize - 1) / nGrowSize)) * nGrowSize;
        }
        else
        {
            if (nAllocationCount == 0)
            {
                // Compute an allocation which is at least as big as a cache line...
                nAllocationCount = (31 + nBytesItem) / nBytesItem;

                // If the requested amount is larger then compute an allocation which
                // is exactly the right size. Otherwise we can end up with wasted memory
                // when CUtlVector::EnsureCount(n) is called.
                if (nAllocationCount < nNewSize)
                {
                    nAllocationCount = nNewSize;
                }
            }

            while (nAllocationCount < nNewSize)
            {
                var nNewAllocationCount = (nAllocationCount * 9) / 8; // 12.5 %

                if (nNewAllocationCount > nAllocationCount)
                {
                    nAllocationCount = nNewAllocationCount;
                }
                else
                {
                    nAllocationCount *= 2;
                }
            }
        }

        return nAllocationCount;
    }

    public void Purge()
    {
        if (IsExternallyAllocated || _memory is null)
        {
            return;
        }

        MemoryAllocator.Free(_memory);
        _memory          = null;
        _allocationCount = 0;
    }

    public void Purge(int numElements)
    {
        if (numElements < 0)
        {
            return;
        }

        if (numElements == 0)
        {
            Purge();

            return;
        }

        if (IsReadOnly)
        {
            return;
        }

        if (numElements == _allocationCount)
        {
            return;
        }

        if (_memory == null)
        {
            return;
        }

        _memory = (T*) MemoryAllocator.VectorMemory_Alloc(_memory,
                                                          !IsExternallyAllocated,
                                                          numElements     * Unsafe.SizeOf<T>(),
                                                          AllocationCount * Unsafe.SizeOf<T>());

        if (IsExternallyAllocated)
        {
            _growSize &= ~(ExternalConstBufferMarker | ExternalBufferMarker);
        }

        _allocationCount = numElements;
    }

    public void EnsureCapacity(int num)
    {
        if (AllocationCount >= num || IsReadOnly)
        {
            return;
        }

        _memory = (T*) MemoryAllocator.VectorMemory_Alloc(_memory,
                                                          !IsExternallyAllocated,
                                                          num             * Unsafe.SizeOf<T>(),
                                                          AllocationCount * Unsafe.SizeOf<T>());

        if (IsExternallyAllocated)
        {
            _growSize &= ~(ExternalConstBufferMarker | ExternalBufferMarker);
        }

        _allocationCount = num;
    }

    public void Grow(int num)
    {
        if (num <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(num));
        }

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Cannot grow a read-only buffer");
        }

        if ((long) _allocationCount + num > int.MaxValue)
        {
            throw new OutOfMemoryException("Cannot allocate more than 2GB of memory");
        }

        // Make sure we have at least numallocated + num allocations.
        // Use the grow rules specified for this memory (in m_nGrowSize)
        var nAllocationRequested = _allocationCount + num;

        var nNewAllocationCount = CalcNewAllocationCount(_allocationCount,
                                                         _growSize
                                                         & ~(ExternalConstBufferMarker | ExternalBufferMarker),
                                                         nAllocationRequested,
                                                         Unsafe.SizeOf<T>());

        // if m_nAllocationRequested wraps index type I, recalculate
        /*if (nNewAllocationCount < nAllocationRequested)
        {
            if (nNewAllocationCount == 0 && nNewAllocationCount - 1 >= nAllocationRequested)
            {
                --nNewAllocationCount; // deal w/ the common case of m_nAllocationCount == MAX_USHORT + 1
            }
            else
            {
                while (nNewAllocationCount < nAllocationRequested)
                {
                    nNewAllocationCount = (nNewAllocationCount + nAllocationRequested) / 2;
                }
            }
        }*/

        _memory = (T*) MemoryAllocator.VectorMemory_Alloc(_memory,
                                                          !IsExternallyAllocated,
                                                          nNewAllocationCount * Unsafe.SizeOf<T>(),
                                                          _allocationCount    * Unsafe.SizeOf<T>());

        if (IsExternallyAllocated)
        {
            _growSize &= ~(ExternalConstBufferMarker | ExternalBufferMarker);
        }

        _allocationCount = nNewAllocationCount;
    }
}
