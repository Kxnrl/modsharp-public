// This file modified from https://github.com/alliedmodders/hl2sdk/tree/cs2

#ifndef CSTRIKE_TYPE_CUTLVECTOR_H
#define CSTRIKE_TYPE_CUTLVECTOR_H

#include "logging.h"

#include "cstrike/type/CUtlMemory.h"

#include <cstring>
#include <memory>

template <class T, class A = CUtlMemory<T> >
class CUtlVector
{
    using CAllocator = A;

public:
    using ElemType_t = T;

    using value_type             = T;
    using size_type              = int32_t;
    using reference              = T&;
    using const_reference        = const T&;
    using pointer                = T*;
    using const_pointer          = const T*;
    using iterator               = T*;
    using const_iterator         = const T*;
    using reverse_iterator       = std::reverse_iterator<iterator>;
    using const_reverse_iterator = std::reverse_iterator<const_iterator>;

    iterator begin() noexcept
    {
        return m_Memory.Base();
    }

    const_iterator begin() const noexcept
    {
        return m_Memory.Base();
    }

    iterator end() noexcept
    {
        return m_Memory.Base() + m_Size;
    }

    const_iterator end() const noexcept
    {
        return m_Memory.Base() + m_Size;
    }

    const_iterator cbegin() const noexcept
    {
        return begin();
    }

    const_iterator cend() const noexcept
    {
        return end();
    }

    reverse_iterator rbegin() noexcept
    {
        return reverse_iterator(end());
    }

    const_reverse_iterator rbegin() const noexcept
    {
        return const_reverse_iterator(end());
    }

    reverse_iterator rend() noexcept
    {
        return reverse_iterator(begin());
    }

    const_reverse_iterator rend() const noexcept
    {
        return const_reverse_iterator(begin());
    }

    // constructor, destructor
    CUtlVector(int32_t growSize = 0, int32_t initSize = 0);
    CUtlVector(T* pMemory, int32_t allocationCount, int32_t numElements = 0);
    ~CUtlVector();

    // Copy the array.
    CUtlVector<T, A>& operator=(const CUtlVector<T, A>& other);

    // element access
    [[nodiscard]] T&       operator[](int32_t i);
    [[nodiscard]] const T& operator[](int32_t i) const;
    [[nodiscard]] T&       Element(int32_t i);
    [[nodiscard]] const T& Element(int32_t i) const;
    [[nodiscard]] T&       Head();
    [[nodiscard]] const T& Head() const;
    [[nodiscard]] T&       Tail();
    [[nodiscard]] const T& Tail() const;

    // Gets the base address (can change when adding elements!)
    [[nodiscard]] T*       Base() { return m_Memory.Base(); }
    [[nodiscard]] const T* Base() const { return m_Memory.Base(); }

    // Returns the number of elements in the vector
    [[nodiscard]] int32_t Count() const;

    // Is element index valid?
    [[nodiscard]] bool           IsValidIndex(int32_t i) const;
    [[nodiscard]] static int32_t InvalidIndex();

    // Adds an element, uses default constructor
    int32_t               AddToHead();
    int32_t               AddToTail();
    [[nodiscard]] T*      AddToTailGetPtr();
    [[nodiscard]] int32_t InsertBefore(int32_t index);
    [[nodiscard]] int32_t InsertAfter(int32_t index);

    // Adds an element, uses copy constructor
    int32_t               AddToHead(const T& src);
    int32_t               AddToTail(const T& src);
    [[nodiscard]] int32_t InsertBefore(int32_t index, const T& src);
    [[nodiscard]] int32_t InsertAfter(int32_t index, const T& src);

    // Makes sure we have enough memory allocated to store a requested # of elements
    void EnsureCapacity(int32_t num);

    void SetSize(int32_t size);

    // Element removal
    void FastRemove(int32_t index); // doesn't preserve order
    void Remove(int32_t index);     // preserves order, shifts elements
    void RemoveAll();               // doesn't deallocate memory

    // Memory deallocation
    void Purge();

    // Purges the list and calls delete on each element in it.
    void PurgeAndDeleteElements();

    [[nodiscard]] int32_t Find(const T& src) const;

protected:
    // Can't copy this unless we explicitly do it!
    CUtlVector(const CUtlVector& vec) :
        m_Size(0) {}

    // Grows the vector
    void GrowVector(int32_t num = 1);

    // Shifts elements....
    void ShiftElementsRight(int32_t elem, int32_t num = 1);
    void ShiftElementsLeft(int32_t elem, int32_t num = 1);

    int32_t    m_Size;
    CAllocator m_Memory;
};

template <class T, class A>
T& CUtlVector<T, A>::operator[](int32_t i)
{
    AssertBool(IsValidIndex(i));

    return m_Memory[i];
}

template <class T, class A>
const T& CUtlVector<T, A>::operator[](int32_t i) const
{
    AssertBool(IsValidIndex(i));

    return m_Memory[i];
}

template <class T, class A>
T& CUtlVector<T, A>::Element(int32_t i)
{
    AssertBool(IsValidIndex(i));

    return m_Memory[i];
}

template <class T, class A>
const T& CUtlVector<T, A>::Element(int32_t i) const
{
    AssertBool(IsValidIndex(i));

    return m_Memory[i];
}

template <class T, class A>
T& CUtlVector<T, A>::Head()
{
    AssertBool(IsValidIndex(0));

    return m_Memory[0];
}

template <class T, class A>
const T& CUtlVector<T, A>::Head() const
{
    AssertBool(IsValidIndex(0));

    return m_Memory[0];
}

template <class T, class A>
T& CUtlVector<T, A>::Tail()
{
    const auto index = Count() - 1;

    AssertBool(IsValidIndex(index));

    return m_Memory[index];
}

template <class T, class A>
const T& CUtlVector<T, A>::Tail() const
{
    const auto index = Count() - 1;

    AssertBool(IsValidIndex(index));

    return m_Memory[index];
}

template <typename T, class A>
inline CUtlVector<T, A>::CUtlVector(int32_t growSize, int32_t initSize) :
    m_Size(0), m_Memory(growSize, initSize)
{
}

template <typename T, class A>
inline CUtlVector<T, A>::CUtlVector(T* pMemory, int32_t allocationCount, int32_t numElements) :
    m_Size(numElements), m_Memory(pMemory, allocationCount)
{
}

template <typename T, class A>
inline CUtlVector<T, A>::~CUtlVector()
{
    Purge();
}

template <class T, class A>
CUtlVector<T, A>& CUtlVector<T, A>::operator=(const CUtlVector<T, A>& other)
{
    if (this == &other)
        return *this;

    const auto new_size = other.Count();

    if (new_size <= 0)
    {
        Purge();
        m_Size = 0;
        return *this;
    }

    const auto current_size             = m_Size;
    const auto current_allocation_count = m_Memory.NumAllocated();

    if (new_size <= current_allocation_count)
    {
        auto dest = m_Memory.Base();
        auto src  = other.m_Memory.Base();

        // if T is trivial to copy then just use memcpy to prevent overhead
        if constexpr (std::is_trivially_copyable_v<T>)
        {
            memcpy(dest, src, new_size * sizeof(T));
        }
        else
        {
            // reuse existing memory if new size is smaller or equal to the current size
            if (new_size <= current_size)
            {
                T* dest_end = std::copy(src, src + new_size, dest);

                // remove extra elements
                std::destroy_n(dest_end, current_size - new_size);
            }
            else // larger than current size but within the allocation count
            {
                std::copy(src, src + current_size, dest);

                // copy the new elements into the uninitialized part of the buffer
                std::uninitialized_copy(src + current_size, src + new_size, dest + current_size);
            }
        }

        m_Size = new_size;
    }
    else
    {
        EnsureCapacity(new_size);
        auto dest = m_Memory.Base();
        auto src  = other.m_Memory.Base();

        if constexpr (std::is_trivially_copyable_v<T>)
        {
            memcpy(dest, src, sizeof(T) * new_size);
        }
        else
        {
            std::uninitialized_copy(src, src + new_size, dest);
        }

        m_Size = new_size;
    }

    return *this;
}

template <class T, class A>
int32_t CUtlVector<T, A>::Count() const
{
    return m_Size;
}

template <class T, class A>
bool CUtlVector<T, A>::IsValidIndex(int32_t i) const
{
    return (i >= 0) && (i < m_Size);
}

template <class T, class A>
int32_t CUtlVector<T, A>::InvalidIndex()
{
    return -1;
}

template <class T, class A>
void CUtlVector<T, A>::GrowVector(int32_t num)
{
    if (m_Size + num > m_Memory.NumAllocated())
    {
        m_Memory.Grow(m_Size + num - m_Memory.NumAllocated());
    }

    m_Size += num;
}

template <class T, class A>
void CUtlVector<T, A>::ShiftElementsRight(int32_t elem, int32_t num)
{
    int32_t numToMove = m_Size - elem - num;
    if ((numToMove > 0) && (num > 0))
        memmove((void*)&Element(elem + num), (void*)&Element(elem), numToMove * sizeof(T));
}

template <class T, class A>
void CUtlVector<T, A>::ShiftElementsLeft(int32_t elem, int32_t num)
{
    int32_t numToMove = m_Size - elem - num;
    if ((numToMove > 0) && (num > 0))
    {
        memmove((void*)&Element(elem), (void*)&Element(elem + num), numToMove * sizeof(T));
    }
}

//-----------------------------------------------------------------------------
// Adds an element, uses default constructor
//-----------------------------------------------------------------------------
template <typename T, class A>
inline int32_t CUtlVector<T, A>::AddToHead()
{
    return InsertBefore(0);
}

template <typename T, class A>
inline int32_t CUtlVector<T, A>::AddToTail()
{
    return InsertBefore(m_Size);
}

template <typename T, class A>
inline T* CUtlVector<T, A>::AddToTailGetPtr()
{
    return &Element(AddToTail());
}

template <typename T, class A>
inline int32_t CUtlVector<T, A>::InsertAfter(int32_t index)
{
    return InsertBefore(index + 1);
}

template <typename T, class A>
int32_t CUtlVector<T, A>::InsertBefore(int32_t index)
{
    GrowVector();
    ShiftElementsRight(index);
    std::construct_at(m_Memory.Base() + index);
    return index;
}

//-----------------------------------------------------------------------------
// Adds an element, uses copy constructor
//-----------------------------------------------------------------------------
template <typename T, class A>
inline int32_t CUtlVector<T, A>::AddToHead(const T& src)
{
    return InsertBefore(0, src);
}

template <typename T, class A>
inline int32_t CUtlVector<T, A>::AddToTail(const T& src)
{
    return InsertBefore(m_Size, src);
}

template <typename T, class A>
inline int32_t CUtlVector<T, A>::InsertAfter(int32_t index, const T& src)
{
    return InsertBefore(index + 1, src);
}

template <typename T, class A>
int32_t CUtlVector<T, A>::InsertBefore(int32_t index, const T& src)
{
    GrowVector();
    ShiftElementsRight(index);
    std::construct_at(m_Memory.Base() + index, src);
    return index;
}

//-----------------------------------------------------------------------------
// Makes sure we have enough memory allocated to store a requested # of elements
//-----------------------------------------------------------------------------
template <typename T, class A>
void CUtlVector<T, A>::EnsureCapacity(int32_t num)
{
    m_Memory.EnsureCapacity(num);
}

template <typename T, class A>
void CUtlVector<T, A>::SetSize(int32_t size)
{
    RemoveAll();
    GrowVector(size);

    for (int32_t i = 0; i < size; i++)
    {
        std::construct_at(m_Memory.Base() + i);
    }
}

//-----------------------------------------------------------------------------
// Element removal
//-----------------------------------------------------------------------------
template <typename T, class A>
void CUtlVector<T, A>::FastRemove(int32_t index)
{
    std::destroy_at(&Element(index));

    if (m_Size > 0)
    {
        if (index != m_Size - 1)
            memcpy(&Element(index), &Element(m_Size - 1), sizeof(T));
        --m_Size;
    }
}

template <typename T, class A>
void CUtlVector<T, A>::Remove(int32_t index)
{
    std::destroy_at(&Element(index));

    ShiftElementsLeft(index);
    --m_Size;
}

template <typename T, class A>
void CUtlVector<T, A>::RemoveAll()
{
    for (int32_t i = m_Size; --i >= 0;)
    {
        std::destroy_at(&Element(i));
    }

    m_Size = 0;
}

//-----------------------------------------------------------------------------
// Memory deallocation
//-----------------------------------------------------------------------------

template <typename T, class A>
inline void CUtlVector<T, A>::Purge()
{
    RemoveAll();
    m_Memory.Purge();
}

template <typename T, class A>
inline void CUtlVector<T, A>::PurgeAndDeleteElements()
{
    for (int32_t i = 0; i < m_Size; i++)
    {
        delete Element(i);
    }
    Purge();
}

template <class T, class A>
int32_t CUtlVector<T, A>::Find(const T& src) const
{
    for (auto i = 0; i < Count(); ++i)
    {
        if (Element(i) == src)
            return i;
    }
    return -1;
}

template <class T, size_t MAX_SIZE>
class CUtlVectorFixedGrowable : public CUtlVector<T, CUtlMemoryFixedGrowable<T, MAX_SIZE> >
{
    using BaseClass = CUtlVector<T, CUtlMemoryFixedGrowable<T, MAX_SIZE> >;

public:
    // constructor, destructor
    CUtlVectorFixedGrowable(int32_t growSize = 0) :
        BaseClass(growSize, MAX_SIZE) {}
};
#endif