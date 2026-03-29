// This file modified from https://github.com/alliedmodders/hl2sdk/tree/cs2

#ifndef CSTRIKE_TYPE_CUTLLEANVECTOR_H
#define CSTRIKE_TYPE_CUTLLEANVECTOR_H

#include "logging.h"

#include "cstrike/type/CMemAllocAllocator.h"

#include <algorithm>
#include <bit>
#include <cstdint>
#include <cstring>
#include <limits>
#include <memory>

inline int32_t CalcNewDoublingCount(int32_t old_count, int32_t requested_count, int32_t min_count, int32_t max_count)
{
    int32_t new_count = old_count;

    while (new_count < requested_count)
    {
        if (new_count < max_count / 2)
        {
            new_count *= 2;
            new_count = std::max(new_count, min_count);
        }
        else
        {
            new_count = max_count;
            break;
        }
    }

    return new_count;
}

template <class T, class I, class A>
class CUtlLeanVectorBase
{
    using CAllocator = A;

public:
    enum : I
    {
        EXTERNAL_BUFFER_MARKER = (I{1} << (std::numeric_limits<I>::digits - 1))
    };

    CUtlLeanVectorBase();
    CUtlLeanVectorBase(T* pMemory, I allocationCount, I numElements = 0);
    ~CUtlLeanVectorBase();

    // Gets the base address (can change when adding elements!)
    [[nodiscard]] T*       Base();
    [[nodiscard]] const T* Base() const;

    // Makes sure we have enough memory allocated to store a requested # of elements
    void EnsureCapacity(int32_t num, bool force = false);

    [[nodiscard]] bool IsExternallyAllocated() const { return (m_nAllocated & EXTERNAL_BUFFER_MARKER) != 0; }

    [[nodiscard]] int32_t NumAllocated() const { return (m_nAllocated & (~EXTERNAL_BUFFER_MARKER)); }

    // Element removal
    void RemoveAll(); // doesn't deallocate memory

    [[nodiscard]] bool IsIdxValid(I i) const { return (i >= 0) && (i < m_nCount); }

    // Memory deallocation
    void Purge();

#ifdef PLATFORM_WINDOWS
#    pragma warning(disable : 4201)
#endif

protected:
    struct
    {
        I  m_nCount;
        I  m_nAllocated;
        T* m_pElements;
    };

#ifdef PLATFORM_WINDOWS
#    pragma warning(default : 4201)
#endif
};
template <class T, class I, class A>
inline CUtlLeanVectorBase<T, I, A>::CUtlLeanVectorBase() :
    m_nCount(0), m_nAllocated(0), m_pElements(nullptr)
{
    EnsureCapacity(0);
}

template <class T, class I, class A>
inline CUtlLeanVectorBase<T, I, A>::CUtlLeanVectorBase(T* pMemory, I allocationCount, I numElements) :
    m_nCount(numElements), m_nAllocated(allocationCount | EXTERNAL_BUFFER_MARKER), m_pElements(pMemory)
{
}

template <class T, class I, class A>
inline CUtlLeanVectorBase<T, I, A>::~CUtlLeanVectorBase()
{
    Purge();
}

template <class T, class I, class A>
inline T* CUtlLeanVectorBase<T, I, A>::Base()
{
    return NumAllocated() ? m_pElements : nullptr;
}

template <class T, class I, class A>
inline const T* CUtlLeanVectorBase<T, I, A>::Base() const
{
    return NumAllocated() ? m_pElements : nullptr;
}

template <class T, class I, class A>
void CUtlLeanVectorBase<T, I, A>::EnsureCapacity(int32_t num, bool force)
{
    auto old_count = NumAllocated();
    if (num <= old_count)
        return;

    I min_count = (31 + sizeof(T)) / sizeof(T);
    I max_count = (std::numeric_limits<I>::max)();

    if (num > max_count)
    {
        return;
    }

    I new_allocate_cound = num;
    if (!force)
    {
        uint32_t required = std::max(num, min_count);
        if (static_cast<uint32_t>(max_count) < required)
        {
            new_allocate_cound = static_cast<uint32_t>(max_count);
        }
        else
        {
            uint32_t new_capacity = std::bit_ceil(required);
            new_allocate_cound    = std::min(new_capacity, static_cast<uint32_t>(max_count));
        }
    }

    T* new_ptr;
    if (IsExternallyAllocated())
    {
        new_ptr = static_cast<T*>(AllocateMemory(new_allocate_cound * sizeof(T)));
        memmove(static_cast<void*>(new_ptr), Base(), m_nCount * sizeof(T));
    }
    else
    {
        if (m_pElements != nullptr)
            new_ptr = static_cast<T*>(ReallocateMemory(m_pElements, new_allocate_cound * sizeof(T)));
        else
            new_ptr = static_cast<T*>(AllocateMemory(new_allocate_cound * sizeof(T)));
    }

    m_pElements  = new_ptr;
    m_nAllocated = new_allocate_cound;
}

template <class T, class I, class A>
void CUtlLeanVectorBase<T, I, A>::RemoveAll()
{
    T*       pElement = Base();
    const T* pEnd     = &pElement[m_nCount];
    while (pElement != pEnd)
    {
        std::destroy_at(pElement++);
    }

    m_nCount = 0;
}

//-----------------------------------------------------------------------------
// Memory deallocation
//-----------------------------------------------------------------------------
template <class T, class I, class A>
inline void CUtlLeanVectorBase<T, I, A>::Purge()
{
    RemoveAll();

    if (!IsExternallyAllocated())
    {
        if (NumAllocated() > 0)
        {
            FreeMemory(m_pElements);
            m_pElements = nullptr;
        }

        m_nAllocated = 0;
    }
}

template <class B, class T, class I>
class CUtlLeanVectorImpl : public B
{
    using BaseClass = B;

public:
    CUtlLeanVectorImpl() :
        BaseClass() {}
    CUtlLeanVectorImpl(T* pMemory, I allocationCount, I numElements = 0) :
        BaseClass(pMemory, allocationCount, numElements) {}
    ~CUtlLeanVectorImpl() {}

    CUtlLeanVectorImpl(const CUtlLeanVectorImpl& vec) = delete;

    // Copy the array.
    CUtlLeanVectorImpl& operator=(const CUtlLeanVectorImpl& other);

    class Iterator_t
    {
    public:
        Iterator_t(I i) :
            index(i) {}
        I index;

        bool operator==(const Iterator_t it) const { return index == it.index; }
        bool operator!=(const Iterator_t it) const { return index != it.index; }
    };

    [[nodiscard]] Iterator_t First() const { return Iterator_t(IsIdxValid(0) ? 0 : InvalidIndex()); }
    [[nodiscard]] Iterator_t Next(const Iterator_t it) const { return Iterator_t(IsIdxValid(it.index + 1) ? it.index + 1 : InvalidIndex()); }
    [[nodiscard]] I          GetIndex(const Iterator_t it) const { return it.index; }
    [[nodiscard]] bool       IsIdxAfter(I i, const Iterator_t it) const { return i > it.index; }
    [[nodiscard]] bool       IsValidIterator(const Iterator_t it) const { return IsIdxValid(it.index); }
    [[nodiscard]] Iterator_t InvalidIterator() const { return Iterator_t(InvalidIndex()); }

    [[nodiscard]] bool IsIdxValid(int32_t i) const { return i >= 0 && i < Count(); }

    [[nodiscard]] static int32_t InvalidIndex() { return -1; }

    // element access
    [[nodiscard]] T&       operator[](int32_t i);
    [[nodiscard]] const T& operator[](int32_t i) const;
    [[nodiscard]] T&       Element(int32_t i);
    [[nodiscard]] const T& Element(int32_t i) const;
    [[nodiscard]] T&       Head();
    [[nodiscard]] const T& Head() const;
    [[nodiscard]] T&       Tail();
    [[nodiscard]] const T& Tail() const;

    [[nodiscard]] int32_t Count() const;

    [[nodiscard]] T*      AddToTailGetPtr();
    [[nodiscard]] int32_t AddToTail();
    [[nodiscard]] int32_t AddToTail(const T& src);

    [[nodiscard]] int32_t AddMultipleToTail(int32_t count);

    void SetSize(int32_t size);
    void SetCount(int32_t count);

    void EnsureCount(int32_t count);

    void Remove(int32_t index);
};

template <class B, class T, class I>
CUtlLeanVectorImpl<B, T, I>& CUtlLeanVectorImpl<B, T, I>::operator=(const CUtlLeanVectorImpl<B, T, I>& other)
{
    if (this == &other)
        return *this;

    int32_t nCount = other.Count();
    SetSize(nCount);

    T*       pDest = this->Base();
    const T* pSrc  = other.Base();
    const T* pEnd  = &pSrc[nCount];

    while (pSrc != pEnd)
        *(pDest++) = *(pSrc++);

    return *this;
}

template <class B, class T, class I>
T& CUtlLeanVectorImpl<B, T, I>::operator[](int32_t i)
{
    AssertBool(IsIdxValid(i));

    return this->Base()[i];
}

template <class B, class T, class I>
const T& CUtlLeanVectorImpl<B, T, I>::operator[](int32_t i) const
{
    AssertBool(IsIdxValid(i));

    return this->Base()[i];
}

template <class B, class T, class I>
T& CUtlLeanVectorImpl<B, T, I>::Element(int32_t i)
{
    AssertBool(IsIdxValid(i));

    return this->Base()[i];
}

template <class B, class T, class I>
const T& CUtlLeanVectorImpl<B, T, I>::Element(int32_t i) const
{
    AssertBool(IsIdxValid(i));

    return this->Base()[i];
}

template <class B, class T, class I>
T& CUtlLeanVectorImpl<B, T, I>::Head()
{
    AssertBool(IsIdxValid(0));

    return this->Base()[0];
}

template <class B, class T, class I>
const T& CUtlLeanVectorImpl<B, T, I>::Head() const
{
    AssertBool(IsIdxValid(0));

    return this->Base()[0];
}

template <class B, class T, class I>
T& CUtlLeanVectorImpl<B, T, I>::Tail()
{
    const auto index = this->m_nCount - 1;

    AssertBool(IsIdxValid(index));

    return this->Base()[index];
}

template <class B, class T, class I>
const T& CUtlLeanVectorImpl<B, T, I>::Tail() const
{
    const auto index = this->m_nCount - 1;

    AssertBool(IsIdxValid(index));

    return this->Base()[index];
}

template <class B, class T, class I>
int32_t CUtlLeanVectorImpl<B, T, I>::Count() const
{
    return this->m_nCount;
}

template <class B, class T, class I>
T* CUtlLeanVectorImpl<B, T, I>::AddToTailGetPtr()
{
    this->EnsureCapacity(this->m_nCount + 1);
    T* pBase = this->Base();
    std::construct_at(&pBase[this->m_nCount]);
    return &pBase[this->m_nCount++];
}

template <class B, class T, class I>
int32_t CUtlLeanVectorImpl<B, T, I>::AddToTail()
{
    this->EnsureCapacity(this->m_nCount + 1);
    T* pBase = this->Base();
    std::construct_at(&pBase[this->m_nCount]);
    return this->m_nCount++;
}

template <class B, class T, class I>
int32_t CUtlLeanVectorImpl<B, T, I>::AddToTail(const T& src)
{
    this->EnsureCapacity(this->m_nCount + 1);
    T* pBase = this->Base();
    std::construct_at(&pBase[this->m_nCount], src);
    return this->m_nCount++;
}

template <class B, class T, class I>
int32_t CUtlLeanVectorImpl<B, T, I>::AddMultipleToTail(int32_t count)
{
    const I old_count = this->m_nCount;

    if (count <= 0)
    {
        return old_count;
    }

    const I max_size = std::numeric_limits<I>::max();

    if (static_cast<I>(count) > (max_size - old_count))
    {
        return 0;
    }

    const I new_count = old_count + count;
    this->EnsureCapacity(new_count);

    T* const base_ptr = this->Base();
    std::uninitialized_default_construct(&base_ptr[old_count], &base_ptr[new_count]);

    this->m_nCount = new_count;

    return old_count;
}

template <class B, class T, class I>
void CUtlLeanVectorImpl<B, T, I>::SetCount(int32_t new_count)
{
    if (new_count < 0)
    {
        return;
    }

    const I old_count = this->m_nCount;

    if (new_count == old_count)
    {
        return;
    }

    if (new_count > old_count)
    {
        this->EnsureCapacity(new_count);

        T* const base_ptr = this->Base();
        std::uninitialized_default_construct(&base_ptr[old_count], &base_ptr[new_count]);
    }
    else
    {
        T* const base_ptr = this->Base();
        std::destroy(&base_ptr[new_count], &base_ptr[old_count]);
    }

    this->m_nCount = new_count;
}

template <class B, class T, class I>
inline void CUtlLeanVectorImpl<B, T, I>::SetSize(int32_t size)
{
    SetCount(size);
}

template <class B, class T, class I>
void CUtlLeanVectorImpl<B, T, I>::EnsureCount(int32_t count)
{
    if (Count() < count)
    {
        AddMultipleToTail(count - Count());
    }
}

template <class B, class T, class I>
void CUtlLeanVectorImpl<B, T, I>::Remove(int32_t index)
{
    if (index < 0 || index >= this->m_nCount)
    {
        return;
    }

    T* const base_ptr  = this->Base();
    T*       dest      = &base_ptr[index];
    T*       src_begin = &base_ptr[index + 1];
    T*       src_end   = &base_ptr[this->m_nCount];

    if constexpr (std::is_trivially_copyable_v<T>)
    {
        std::size_t num_to_move = src_end - src_begin;
        if (num_to_move > 0)
        {
            memmove(dest, src_begin, num_to_move * sizeof(T));
        }
    }
    else
    {
        std::move(src_begin, src_end, dest);
    }

    --this->m_nCount;
    std::destroy_at(&base_ptr[this->m_nCount]);
}

template <class T, typename I = int32_t, class A = CMemAllocAllocator>
class CUtlLeanVector : public CUtlLeanVectorImpl<CUtlLeanVectorBase<T, I, A>, T, I>
{
public:
};

#endif