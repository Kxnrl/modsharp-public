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

#ifndef CSTRIKE_TYPE_CNETWORKUTLVECTORBASE_H
#define CSTRIKE_TYPE_CNETWORKUTLVECTORBASE_H

#include "cstrike/type/CUtlVector.h"

#include <cstdint>

struct SchemaFieldData;

void NetworkVectorBaseStateChanged(void* pInstance, const SchemaFieldData* pData, int32_t nOffset, bool isStruct, uint32_t nArrayIndex);

template <class T>
class CNetworkUtlVectorBase : public CUtlVector<T>
{
};

template <typename T>
class CNetworkVectorBaseImpl
{
public:
    CNetworkVectorBaseImpl() = delete;

    CNetworkVectorBaseImpl(void* instance, const SchemaFieldData* data, int32_t nOffset, bool isStruct) :
        m_pInstance(instance),
        m_pData(data),
        m_nOffset(nOffset),
        m_bIsStruct(isStruct),
        m_pVector(reinterpret_cast<CNetworkUtlVectorBase<T>*>(reinterpret_cast<uintptr_t>(instance) + nOffset))
    {
    }

    [[nodiscard]] CNetworkUtlVectorBase<T>* GetVector() const { return m_pVector; }

    [[nodiscard]] int32_t  Count() const { return m_pVector->Count(); }
    [[nodiscard]] bool     IsValidIndex(int32_t i) const { return m_pVector->IsValidIndex(i); }
    [[nodiscard]] T&       Element(int32_t i) { return m_pVector->Element(i); }
    [[nodiscard]] const T& Element(int32_t i) const { return m_pVector->Element(i); }
    [[nodiscard]] T&       operator[](int32_t i) { return m_pVector->Element(i); }
    [[nodiscard]] const T& operator[](int32_t i) const { return m_pVector->Element(i); }
    [[nodiscard]] T*       Base() { return m_pVector->Base(); }

    [[nodiscard]] T* begin() { return m_pVector->begin(); }
    [[nodiscard]] T* end() { return m_pVector->end(); }

    int32_t AddToTail(const T& src)
    {
        const int32_t index = m_pVector->AddToTail(src);
        NotifyStateChanged(0xFFFFFFFF);
        NotifyStateChanged(static_cast<uint32_t>(index));
        return index;
    }

    void Set(int32_t index, const T& value)
    {
        m_pVector->Element(index) = value;
        NotifyStateChanged(static_cast<uint32_t>(index));
    }

    void Remove(int32_t index)
    {
        m_pVector->Remove(index);
        NotifyStateChanged(0xFFFFFFFF);
    }

    void FastRemove(int32_t index)
    {
        m_pVector->FastRemove(index);
        NotifyStateChanged(0xFFFFFFFF);
    }

    void RemoveAll()
    {
        m_pVector->RemoveAll();
        NotifyStateChanged(0xFFFFFFFF);
    }

private:
    void NotifyStateChanged(uint32_t nArrayIndex) const
    {
        NetworkVectorBaseStateChanged(m_pInstance, m_pData, m_nOffset, m_bIsStruct, nArrayIndex);
    }

    void*                     m_pInstance;
    const SchemaFieldData*    m_pData;
    int32_t                   m_nOffset;
    bool                      m_bIsStruct;
    CNetworkUtlVectorBase<T>* m_pVector;
};

#endif
