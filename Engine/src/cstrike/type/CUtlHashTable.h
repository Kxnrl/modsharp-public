// Ported from https://github.com/Wend4r/sourcesdk/blob/main/public/tier1/utlhashtable.h

#ifndef CSTRIKE_TYPE_CUTLHASHTABLE_H
#define CSTRIKE_TYPE_CUTLHASHTABLE_H

#include "cstrike/type/CUtlLeanVector.h"
#include "cstrike/type/UtlCommon.h"

#include <cstdint>
#include <cstring>
#include <type_traits>

inline int SmallestPowerOfTwoGreaterOrEqual(int x)
{
    if (x <= 0)
        return 1;
    x--;
    x |= x >> 1;
    x |= x >> 2;
    x |= x >> 4;
    x |= x >> 8;
    x |= x >> 16;
    return x + 1;
}

inline int LargestPowerOfTwoLessThanOrEqual(int x)
{
    if (x <= 0)
        return 0;
    x |= x >> 1;
    x |= x >> 2;
    x |= x >> 4;
    x |= x >> 8;
    x |= x >> 16;
    return x - (x >> 1);
}

using UtlHashtableHandle_t = unsigned int;

template <typename KeyT, typename ValueT = empty_t>
class CUtlHashtableEntry
{
public:
    using KVPair = CUtlKeyValuePair<KeyT, ValueT>;

    enum { INT16_STORAGE = (sizeof(KVPair) <= 2) };
    using storage_t = typename CTypeSelect<INT16_STORAGE, int16_t, int32_t>::type;

    enum : uint32_t
    {
        FLAG_FREE = INT16_STORAGE ? 0x8000u : 0x80000000u,
        FLAG_LAST = INT16_STORAGE ? 0x4000u : 0x40000000u,
        MASK_HASH = INT16_STORAGE ? 0x3FFFu : 0x3FFFFFFFu,
    };

    storage_t                     flags_and_hash;
    AlignedByteArray_t<1, KVPair> data;

    bool IsValid() const { return flags_and_hash >= 0; }
    void MarkInvalid() { flags_and_hash = static_cast<storage_t>(FLAG_FREE); }

    const KVPair* Raw() const { return reinterpret_cast<const KVPair*>(data.Base()); }
    KVPair*       Raw() { return reinterpret_cast<KVPair*>(data.Base()); }

    const KVPair* operator->() const { return Raw(); }
    KVPair*       operator->() { return Raw(); }

    void MoveDataFrom(CUtlHashtableEntry& src)
    {
        for (int i = 0; i < static_cast<int>(sizeof(data)); ++i)
            data.Base()[i] = src.data.Base()[i];
    }

    static uint32_t IdealIndex(int16_t h, uint32_t m)
    {
        uint32_t v = static_cast<uint16_t>(h) & MASK_HASH;
        v *= MASK_HASH + 2u;
        return v & m;
    }
    static uint32_t IdealIndex(int32_t h, uint32_t m)
    {
        return static_cast<uint32_t>(h) & m;
    }

    uint32_t IdealIndex(uint32_t slotmask) const
    {
        return IdealIndex(flags_and_hash, slotmask) | static_cast<uint32_t>(flags_and_hash >> 31);
    }
};

template <
    typename KeyT,
    typename ValueT        = empty_t,
    typename KeyHashT      = DefaultHashFunctor<KeyT>,
    typename KeyIsEqualT   = DefaultEqualFunctor<KeyT>,
    typename AlternateKeyT = typename ArgumentTypeInfo<KeyT>::Alt_t,
    typename TableT        = CUtlLeanVector<CUtlHashtableEntry<KeyT, ValueT>>>
class CUtlHashtable
{
public:
    using handle_t = UtlHashtableHandle_t;
    using entry_t  = CUtlHashtableEntry<KeyT, ValueT>;
    using KVPair   = CUtlKeyValuePair<KeyT, ValueT>;

    using KeyArg_t   = typename ArgumentTypeInfo<KeyT>::Arg_t;
    using ValueArg_t = typename ArgumentTypeInfo<ValueT>::Arg_t;
    using KeyAlt_t   = typename ArgumentTypeInfo<AlternateKeyT>::Arg_t;
    using Element_t  = typename KVPair::ValueReturn_t;

    static constexpr bool kHasAltKey = !std::is_same_v<KeyArg_t, KeyAlt_t>;

    enum : handle_t { INVALID_HANDLE = (handle_t)-1 };
    static handle_t InvalidHandle() { return INVALID_HANDLE; }

protected:
    enum : uint32_t
    {
        FLAG_FREE = entry_t::FLAG_FREE,
        FLAG_LAST = entry_t::FLAG_LAST,
        MASK_HASH = entry_t::MASK_HASH,
    };

    TableT      m_table;
    int         m_nUsed;
    int         m_nTableSize;
    int         m_nMinSize;
    bool        m_bSizeLocked;
    KeyIsEqualT m_eq;
    KeyHashT    m_hash;

public:
    explicit CUtlHashtable(int minimumSize = 32)
        : m_nUsed(0), m_nTableSize(0)
        , m_nMinSize(minimumSize < 8 ? 8 : minimumSize)
        , m_bSizeLocked(false)
    {
        InitTable();
    }

    ~CUtlHashtable() { RemoveAll(); }

    int  Count() const { return m_nUsed; }
    int  TableSize() const { return m_nTableSize; }

    bool IsValidHandle(handle_t idx) const
    {
        return (unsigned)idx < (unsigned)m_nTableSize && m_table[idx].IsValid();
    }

    KeyHashT&          GetHashRef()       { return m_hash; }
    KeyIsEqualT&       GetEqualRef()      { return m_eq; }
    const KeyHashT&    GetHashRef() const { return m_hash; }
    const KeyIsEqualT& GetEqualRef() const { return m_eq; }

    const KeyT& Key(handle_t idx) const { return m_table[idx]->m_key; }

    const Element_t& Element(handle_t idx) const { return m_table[idx]->GetValue(); }
    Element_t&       Element(handle_t idx)       { return m_table[idx]->GetValue(); }

    const Element_t& operator[](handle_t idx) const { return m_table[idx]->GetValue(); }
    Element_t&       operator[](handle_t idx)       { return m_table[idx]->GetValue(); }

    const Element_t* GetPtr(KeyArg_t k) const
    {
        handle_t h = Find(k);
        return h != INVALID_HANDLE ? &Element(h) : nullptr;
    }
    Element_t* GetPtr(KeyArg_t k)
    {
        handle_t h = Find(k);
        return h != INVALID_HANDLE ? &Element(h) : nullptr;
    }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    const Element_t* GetPtr(KeyAlt_t k) const
    {
        handle_t h = Find(k);
        return h != INVALID_HANDLE ? &Element(h) : nullptr;
    }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    Element_t* GetPtr(KeyAlt_t k)
    {
        handle_t h = Find(k);
        return h != INVALID_HANDLE ? &Element(h) : nullptr;
    }

    handle_t FirstHandle() const { return NextHandle((handle_t)-1); }

    handle_t NextHandle(handle_t start) const
    {
        const entry_t* table = m_table.Base();
        for (int i = (int)start + 1; i < m_nTableSize; ++i)
        {
            if (table[i].IsValid())
                return (handle_t)i;
        }
        return INVALID_HANDLE;
    }

    handle_t Find(KeyArg_t k) const
    {
        return DoLookup<KeyArg_t>(k, m_hash(k), nullptr);
    }
    handle_t Find(KeyArg_t k, unsigned int hash) const
    {
        return DoLookup<KeyArg_t>(k, hash, nullptr);
    }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    handle_t Find(KeyAlt_t k) const
    {
        return DoLookup<KeyAlt_t>(k, m_hash(k), nullptr);
    }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    handle_t Find(KeyAlt_t k, unsigned int hash) const
    {
        return DoLookup<KeyAlt_t>(k, hash, nullptr);
    }

    bool HasElement(KeyArg_t k) const { return INVALID_HANDLE != Find(k); }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    bool HasElement(KeyAlt_t k) const { return INVALID_HANDLE != Find(k); }

    handle_t Insert(KeyArg_t k)
    {
        return DoInsert<KeyArg_t>(k, m_hash(k));
    }
    handle_t Insert(KeyArg_t k, ValueArg_t v, bool* pDidInsert = nullptr)
    {
        return DoInsert<KeyArg_t>(k, v, m_hash(k), pDidInsert);
    }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    handle_t Insert(KeyAlt_t k)
    {
        return DoInsert<KeyAlt_t>(k, m_hash(k));
    }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    handle_t Insert(KeyAlt_t k, ValueArg_t v, bool* pDidInsert = nullptr)
    {
        return DoInsert<KeyAlt_t>(k, v, m_hash(k), pDidInsert);
    }

    bool Remove(KeyArg_t k) { return DoRemove<KeyArg_t>(k, m_hash(k)) >= 0; }
    template <bool B = kHasAltKey, typename = std::enable_if_t<B>>
    bool Remove(KeyAlt_t k) { return DoRemove<KeyAlt_t>(k, m_hash(k)) >= 0; }

    handle_t RemoveAndAdvance(handle_t idx)
    {
        int hole = DoRemove<KeyArg_t>(m_table[idx]->m_key,
                                      m_table[idx].flags_and_hash & MASK_HASH);
        if (hole <= (int)idx)
            return NextHandle(idx);
        else
            return idx;
    }

    void RemoveAll()
    {
        int used = m_nUsed;
        if (used != 0)
        {
            entry_t* table = m_table.Base();
            for (int i = m_nTableSize - 1; i >= 0; --i)
            {
                if (table[i].IsValid())
                {
                    table[i].MarkInvalid();
                    table[i].Raw()->~KVPair();
                    if (--used == 0)
                        break;
                }
            }
            m_nUsed = 0;
        }
    }

    void Purge()
    {
        RemoveAll();
        m_table.Purge();
        m_nTableSize = 0;
    }

    void Reserve(int expected)
    {
        if (expected > m_nUsed)
            DoRealloc(expected * 4 / 3);
    }

    void Compact(bool bMinimal)
    {
        DoRealloc(bMinimal ? m_nUsed : (m_nUsed * 4 / 3));
    }

    void SetSizeLocked(bool bLocked) { m_bSizeLocked = bLocked; }

protected:
    void InitTable()
    {
        if (m_table.Count() > 0)
        {
            m_nTableSize = LargestPowerOfTwoLessThanOrEqual(m_table.Count());
            for (int i = 0; i < m_nTableSize; ++i)
                m_table[i].MarkInvalid();
        }
    }

    template <typename KeyParamT>
    handle_t DoLookupSlow(KeyParamT x, unsigned int h) const
    {
        const entry_t* table = m_table.Base();

        for (int i = 0; i < m_nTableSize; ++i)
        {
            if (!table[i].IsValid())
                continue;
            if (((table[i].flags_and_hash ^ (typename entry_t::storage_t)h) & MASK_HASH) != 0)
                continue;
            if (m_eq(table[i]->m_key, x))
                return (handle_t)i;
        }

        for (int i = 0; i < m_nTableSize; ++i)
        {
            if (table[i].IsValid() && m_eq(table[i]->m_key, x))
                return (handle_t)i;
        }

        return INVALID_HANDLE;
    }

    template <typename KeyParamT>
    handle_t DoLookup(KeyParamT x, unsigned int h, handle_t* pPreviousInChain) const
    {
        if (m_nUsed == 0)
            return INVALID_HANDLE;

        const entry_t* table    = m_table.Base();
        unsigned int   slotmask = (unsigned int)(m_nTableSize - 1);
        unsigned int   chainid  = entry_t::IdealIndex((typename entry_t::storage_t)h, slotmask);

        unsigned int idx = chainid;
        if (table[idx].IdealIndex(slotmask) != chainid)
            return pPreviousInChain ? INVALID_HANDLE : DoLookupSlow<KeyParamT>(x, h);

        handle_t     lastIdx  = INVALID_HANDLE;
        unsigned int startIdx = idx;
        do
        {
            if (table[idx].IdealIndex(slotmask) == chainid)
            {
                if (((table[idx].flags_and_hash ^ (typename entry_t::storage_t)h) & MASK_HASH) == 0
                    && m_eq(table[idx]->m_key, x))
                {
                    if (pPreviousInChain)
                        *pPreviousInChain = lastIdx;
                    return (handle_t)idx;
                }

                if (table[idx].flags_and_hash & FLAG_LAST)
                    return pPreviousInChain ? INVALID_HANDLE : DoLookupSlow<KeyParamT>(x, h);

                lastIdx = (handle_t)idx;
            }
            idx = (idx + 1) & slotmask;
        } while (idx != startIdx);

        return pPreviousInChain ? INVALID_HANDLE : DoLookupSlow<KeyParamT>(x, h);
    }

    void BumpEntry(unsigned int idx)
    {
        entry_t*     table    = m_table.Base();
        unsigned int slotmask = (unsigned int)(m_nTableSize - 1);

        unsigned int new_fah = table[idx].flags_and_hash & (FLAG_LAST | MASK_HASH);
        unsigned int chainid = entry_t::IdealIndex(
            (typename entry_t::storage_t)new_fah, slotmask);

        int newIdx = (int)chainid;
        for (;; newIdx = (newIdx + 1) & slotmask)
        {
            if (table[newIdx].IdealIndex(slotmask) == chainid)
            {
                if (table[newIdx].flags_and_hash & FLAG_LAST)
                {
                    table[newIdx].flags_and_hash &= ~(typename entry_t::storage_t)FLAG_LAST;
                    new_fah |= FLAG_LAST;
                }
                continue;
            }
            if (table[newIdx].IsValid())
                continue;
            break;
        }

        if (table[idx].flags_and_hash & FLAG_LAST)
        {
            int scan = (idx + slotmask) & slotmask;
            while (scan != newIdx)
            {
                if (table[scan].IdealIndex(slotmask) == chainid)
                {
                    table[scan].flags_and_hash |= (typename entry_t::storage_t)FLAG_LAST;
                    new_fah &= ~(typename entry_t::storage_t)FLAG_LAST;
                    break;
                }
                scan = (scan + slotmask) & slotmask;
            }
        }

        table[newIdx].flags_and_hash = (typename entry_t::storage_t)new_fah;
        table[newIdx].MoveDataFrom(table[idx]);
        table[idx].MarkInvalid();
    }

    int DoInsertUnconstructed(unsigned int h, bool allowGrow)
    {
        if (allowGrow && !m_bSizeLocked)
        {
            int newSize = m_nUsed + 4;
            if (newSize * 4 > m_nTableSize * 3)
                DoRealloc(newSize * 4 / 3);
        }

        entry_t*     table    = m_table.Base();
        unsigned int slotmask = (unsigned int)(m_nTableSize - 1);
        unsigned int new_fah  = FLAG_LAST | (h & MASK_HASH);
        unsigned int idx      = entry_t::IdealIndex((typename entry_t::storage_t)h, slotmask);

        if (table[idx].IdealIndex(slotmask) == idx)
        {
            new_fah &= ~FLAG_LAST;
            BumpEntry(idx);
        }
        else if (table[idx].IsValid())
        {
            BumpEntry(idx);
        }

        table[idx].flags_and_hash = (typename entry_t::storage_t)new_fah;
        ++m_nUsed;
        return (int)idx;
    }

    template <typename KeyParamT>
    handle_t DoInsert(KeyParamT k, unsigned int h)
    {
        handle_t idx = DoLookup<KeyParamT>(k, h, nullptr);
        if (idx == INVALID_HANDLE)
        {
            idx = (handle_t)DoInsertUnconstructed(h, true);
            new (m_table[idx].Raw()) KVPair(k);
        }
        return idx;
    }

    template <typename KeyParamT>
    handle_t DoInsert(KeyParamT k, ValueArg_t v, unsigned int h, bool* pDidInsert)
    {
        handle_t idx = DoLookup<KeyParamT>(k, h, nullptr);
        if (idx == INVALID_HANDLE)
        {
            idx = (handle_t)DoInsertUnconstructed(h, true);
            new (m_table[idx].Raw()) KVPair(k, v);
            if (pDidInsert) *pDidInsert = true;
        }
        else
        {
            if (pDidInsert) *pDidInsert = false;
        }
        return idx;
    }

    template <typename KeyParamT>
    int DoRemove(KeyParamT x, unsigned int h)
    {
        unsigned int slotmask = (unsigned int)(m_nTableSize - 1);
        handle_t     previous = INVALID_HANDLE;
        int          idx      = (int)DoLookup<KeyParamT>(x, h, &previous);
        if (idx < 0)
            return -1;

        enum { FAKEFLAG_ROOT = 1 };
        int nLastAndRootFlags = m_table[idx].flags_and_hash & FLAG_LAST;
        nLastAndRootFlags |= ((unsigned int)idx == m_table[idx].IdealIndex(slotmask)) ? FAKEFLAG_ROOT : 0;

        m_table[idx].MarkInvalid();
        m_table[idx].Raw()->~KVPair();
        --m_nUsed;

        if (nLastAndRootFlags == (int)FLAG_LAST)
        {
            m_table[previous].flags_and_hash |= (typename entry_t::storage_t)FLAG_LAST;
        }

        if (nLastAndRootFlags == FAKEFLAG_ROOT)
        {
            unsigned int chainid = entry_t::IdealIndex((typename entry_t::storage_t)h, slotmask);
            unsigned int nextIdx = (unsigned int)idx;
            while (true)
            {
                nextIdx = (nextIdx + 1) & slotmask;
                if (m_table[nextIdx].IdealIndex(slotmask) == chainid)
                    break;
            }

            m_table[idx].flags_and_hash = m_table[nextIdx].flags_and_hash;
            m_table[idx].MoveDataFrom(m_table[nextIdx]);
            m_table[nextIdx].MarkInvalid();
            return (int)nextIdx;
        }

        return idx;
    }

    void DoRealloc(int size)
    {
        size = SmallestPowerOfTwoGreaterOrEqual(
            m_nMinSize > size ? m_nMinSize : size);

        int nOldSize = m_nTableSize;
        int nOldUsed = m_nUsed;

        entry_t* pOldCopy = nullptr;
        if (nOldSize > 0 && nOldUsed > 0)
        {
            pOldCopy = static_cast<entry_t*>(::operator new(
                static_cast<size_t>(nOldSize) * sizeof(entry_t)));
            std::memcpy(pOldCopy, m_table.Base(), static_cast<size_t>(nOldSize) * sizeof(entry_t));
        }

        m_table.EnsureCount(size);

        while (size <= INT32_MAX / 2)
        {
            int newSize = size * 2;
            if (newSize > m_table.Count())
                break;
            size = newSize;
        }

        m_nTableSize = size;
        entry_t* pNewBase = m_table.Base();

        for (int i = 0; i < m_table.Count(); ++i)
            pNewBase[i].MarkInvalid();

        m_nUsed = 0;
        if (pOldCopy)
        {
            int nLeftToMove = nOldUsed;
            for (int i = nOldSize - 1; i >= 0 && nLeftToMove > 0; --i)
            {
                if (pOldCopy[i].IsValid())
                {
                    int newIdx = DoInsertUnconstructed(
                        (unsigned int)pOldCopy[i].flags_and_hash, false);
                    pNewBase[newIdx].MoveDataFrom(pOldCopy[i]);
                    --nLeftToMove;
                }
            }
            ::operator delete(pOldCopy);
        }
    }
};

#endif
