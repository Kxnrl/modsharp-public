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

#ifndef CORECLR_RUNTIMEPROTOBUFMESSAGE_H
#define CORECLR_RUNTIMEPROTOBUFMESSAGE_H

#include "global.h"

#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/IProtobufBinding.h"

#include <google/protobuf/message.h>

class CSharpNetworkData : public google::protobuf::Message
{
public:
    Message* New(google::protobuf::Arena* arena) const override
    {
        return nullptr;
    }

protected:
    google::protobuf::Metadata GetMetadata() const override
    {
        return {};
    }

public:
    bool IsInitialized() const override
    {
        return true;
    }

    int GetCachedSize() const override
    {
        return m_nSize;
    }

    uint8_t* _InternalSerialize(uint8_t* target, google::protobuf::io::EpsCopyOutputStream* stream) const override
    {
        memcpy(target, m_pData, m_nSize);
        target += m_nSize;
        return target;
    }

    size_t ByteSizeLong() const override
    {
        return m_nSize;
    }

    CSharpNetworkData(void* data, int size, INetworkMessageInternal* message) :
        m_nSize(size), m_pSerializable(message)
    {
        if (!data)
        {
            m_pData = data;
            return;
        }

        auto pMemory = AllocateMemory(size);
        memcpy(pMemory, data, size);
        m_pData = pMemory;
    }
    ~CSharpNetworkData() override
    {
        if (m_pData)
        {
            FreeMemory(m_pData);
        }
    }

    int GetSize() const { return m_nSize; }

    void* GetData() const { return m_pData; }

    INetworkMessageInternal* GetSerializable() const { return m_pSerializable; }

private:
    void*                    m_pData;
    int                      m_nSize;
    INetworkMessageInternal* m_pSerializable;
};

class CSharpNetworkDataWrapper : public CNetMessage, public CSharpNetworkData
{
    void* AsProto() const override
    {
        return (void*)static_cast<const CSharpNetworkData*>(this);
    }

    void* AsProto2() const override
    {
        return (void*)static_cast<const CSharpNetworkData*>(this);
    }

    INetworkMessageInternal* GetNetMessage() const override
    {
        return this->GetSerializable();
    }

    void* AllocAndCopy() const override
    {
        return new CSharpNetworkDataWrapper(this->GetData(), this->GetSize(), this->GetNetMessage());
    }

    int GetType() const override
    {
        return this->GetNetMessage()->GetNetMessageInfo()->m_MessageId;
    }

    const char* GetName() const override
    {
        return this->GetNetMessage()->GetNetMessageInfo()->m_pBinding->GetName();
    }

public:
    CSharpNetworkDataWrapper(void* data, int size, INetworkMessageInternal* message) :
        CSharpNetworkData(data, size, message)
    {
    }
};

class CSharpNetworkSerializable : public INetworkMessageInternal
{
public:
    CSharpNetworkSerializable(INetworkMessageInternal* pSerializable) :
        m_pSerializable(pSerializable) {}
    ~CSharpNetworkSerializable() override = default;

    const char*       GetUnscopedName() override { return m_pSerializable->GetUnscopedName(); }
    NetMessageInfo_t* GetNetMessageInfo() override { return m_pSerializable->GetNetMessageInfo(); }
    void              SetMessageId(int nMessageId) override { m_pSerializable->SetMessageId(nMessageId); }
    void              AddCategoryMask(int nMask, bool unk) override { m_pSerializable->AddCategoryMask(nMask, unk); }
    void              SwitchMode(NetworkValidationMode_t nMode) override { m_pSerializable->SwitchMode(nMode); }
    CNetMessage*      AllocateMessage() override
    {
        return new CSharpNetworkDataWrapper(nullptr, 0, m_pSerializable);
    }
    void DeallocateMessage(CNetMessage* pMsg)
    {
        const auto pData = reinterpret_cast<CSharpNetworkDataWrapper*>(pMsg);
        FreeMemory(pData);
    }
    CNetMessage* AllocateAndCopyConstructNetMessage(const CNetMessage* pOther)
    {
        const auto pProtobufData = reinterpret_cast<const CSharpNetworkDataWrapper*>(pOther);
        return new CSharpNetworkDataWrapper(pProtobufData->GetData(), pProtobufData->GetSize(), m_pSerializable);
    }

    bool Serialize(void* pBuf, const CNetMessage* pData) override { return m_pSerializable->Serialize(pBuf, pData); }

    bool Unserialize(void* pBuf, CNetMessage* pData) override { return m_pSerializable->Unserialize(pBuf, pData); }

private:
    INetworkMessageInternal* m_pSerializable;
};

INetworkMessageInternal* GetRuntimeProtobufSerializable(const char* name);

#endif