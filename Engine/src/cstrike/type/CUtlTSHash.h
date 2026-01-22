// This file modified from https://github.com/alliedmodders/hl2sdk/tree/cs2

#ifndef CSTRIKE_TYPE_TSHASH_H
#define CSTRIKE_TYPE_TSHASH_H

#include <cstdint>

class CUtlMemoryPool
{
public:
    [[nodiscard]] int32_t Count() const
    {
        return nBlocksAllocated;
    }

    [[nodiscard]] int32_t PeakCount() const
    {
        return nPeakAlloc;
    }

    [[nodiscard]] int32_t BlockSize() const
    {
        return nBlockSize;
    }

protected:
    int32_t nBlockSize;       // 0x0
    int32_t nBlocksPerBlob;   // 0x4
    int32_t nGrowMode;        // 0x8
    int32_t nBlocksAllocated; // 0xc
    int32_t nPeakAlloc;       // 0x10

    uint16_t nAlignment; // 0x14
    uint16_t nNumBlobs;  // 0x16

    uint8_t pad_18[0x8];     // 0x18
    void*   pHeadOfFreeList; // 0x20
    uint8_t pad_28[0x38];    // 0x28
};

using UtlTSHashHandle_t = uintptr_t;

inline uint32_t HashIntConventional1(const int32_t n)
{
    uint32_t hash = 0xAAAAAAAA + (n & 0xFF);
    hash          = (hash << 5) + hash + ((n >> 8) & 0xFF);
    hash          = (hash << 5) + hash + ((n >> 16) & 0xFF);
    hash          = (hash << 5) + hash + ((n >> 24) & 0xFF);

    return hash;
}

template <int32_t nBucketCount, class tKey = uintptr_t>
class CUtlTSHashGenericHash
{
public:
    [[nodiscard]] static int32_t Hash(const tKey& Key, int32_t nBucketMask)
    {
        int32_t nHash = HashIntConventional1(uintptr_t(Key));
        if (nBucketCount <= UINT16_MAX)
        {
            nHash ^= (nHash >> 16);
        }

        if (nBucketCount <= UINT8_MAX)
        {
            nHash ^= (nHash >> 8);
        }

        return (nHash & nBucketMask);
    }

    [[nodiscard]] static bool Compare(const tKey& lhs, const tKey& rhs)
    {
        return lhs == rhs;
    }
};

template <class tElement, int32_t nBucketCount, class tKey = uintptr_t, class tHashFuncs = CUtlTSHashGenericHash<nBucketCount, tKey>, int32_t nAlignment = 0>
class CUtlTSHash
{
    static constexpr int32_t nBucketMask = nBucketCount - 1;

public:
    static constexpr UtlTSHashHandle_t InvalidHandle()
    {
        return static_cast<UtlTSHashHandle_t>(0);
    }

    [[nodiscard]] UtlTSHashHandle_t Find(tKey uiKey)
    {
        int32_t                 iBucket    = tHashFuncs::Hash(uiKey, nBucketCount);
        const HashBucket_t&     hashBucket = aBuckets[iBucket];
        const UtlTSHashHandle_t hHash      = Find(uiKey, hashBucket.pFirst, nullptr);
        return hHash ? hHash : Find(uiKey, hashBucket.pFirstUncommited, hashBucket.pFirst);
    }

    [[nodiscard]] int32_t Count() const
    {
        return EntryMemory.Count();
    }

    [[nodiscard]] int32_t GetElements(int32_t nFirstElement, int32_t nCount, UtlTSHashHandle_t* pHandles) const
    {
        int32_t nIndex = 0;
        for (int32_t nBucketIndex = 0; nBucketIndex < nBucketCount; nBucketIndex++)
        {
            const HashBucket_t& hashBucket = aBuckets[nBucketIndex];

            HashFixedData_t* pElement = hashBucket.pFirstUncommited;
            for (; pElement; pElement = pElement->GetNext())
            {
                if (--nFirstElement >= 0)
                    continue;

                pHandles[nIndex++] = reinterpret_cast<UtlTSHashHandle_t>(pElement);

                if (nIndex >= nCount)
                    return nIndex;
            }
        }

        return nIndex;
    }

    [[nodiscard]] tElement Element(UtlTSHashHandle_t hHash)
    {
        return ((HashFixedData_t*)(hHash))->GetData();
    }

    [[nodiscard]] const tElement& Element(UtlTSHashHandle_t hHash) const
    {
        return reinterpret_cast<HashFixedData_t*>(hHash)->Data;
    }

    [[nodiscard]] tElement& operator[](UtlTSHashHandle_t hHash)
    {
        return reinterpret_cast<HashFixedData_t*>(hHash)->Data;
    }

    [[nodiscard]] const tElement& operator[](UtlTSHashHandle_t hHash) const
    {
        return reinterpret_cast<HashFixedData_t*>(hHash)->Data;
    }

    [[nodiscard]] tKey GetID(UtlTSHashHandle_t hHash) const
    {
        return reinterpret_cast<HashFixedData_t*>(hHash)->uiKey;
    }

private:
    template <typename tData>
    struct HashFixedDataInternal_t
    {
        tKey                            uiKey;
        HashFixedDataInternal_t<tData>* pNext;
        tData                           Data;

        tData GetData()
        {
            return *reinterpret_cast<tData*>(reinterpret_cast<uintptr_t>(this) + (0x10));
        }

        HashFixedDataInternal_t<tData>* GetNext()
        {
            return *reinterpret_cast<HashFixedDataInternal_t<tData>**>(reinterpret_cast<uintptr_t>(this) + (0x8));
        }
    };

    using HashFixedData_t = HashFixedDataInternal_t<tElement>;

    struct HashBucket_t
    {
    private:
        [[maybe_unused]] uint8_t pad0[0x8];

#ifndef PLATFORM_WINDOWS
        [[maybe_unused]] uint8_t pad8[0x30];
#endif
    public:
        HashFixedData_t* pFirst;
        HashFixedData_t* pFirstUncommited;
    };

    UtlTSHashHandle_t Find(tKey uiKey, HashFixedData_t* pFirstElement, HashFixedData_t* pLastElement)
    {
        for (HashFixedData_t* pElement = pFirstElement; pElement != pLastElement; pElement = pElement->pNext)
        {
            if (tHashFuncs::Compare(pElement->uiKey, uiKey))
                return reinterpret_cast<UtlTSHashHandle_t>(pElement);
        }

        return InvalidHandle();
    }

    CUtlMemoryPool EntryMemory;
    HashBucket_t   aBuckets[nBucketCount];
    bool           bNeedsCommit;
    uint8_t        pad[0xf];
};
#endif
