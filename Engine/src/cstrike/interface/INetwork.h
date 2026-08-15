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

#ifndef CSTRIKE_INTERFACE_NETWORK_H
#define CSTRIKE_INTERFACE_NETWORK_H

#include "cstrike/interface/IAppSystem.h"
#include "cstrike/interface/INetChannel.h"

#include <cstdint>

class IRecipientFilter;
class CNetMessage;
struct NetMessageInfo_t;

enum NetworkValidationMode_t
{

};

enum NetworkSerializationMode_t
{
    NET_SERIALIZATION_MODE_0       = 0x0,
    NET_SERIALIZATION_MODE_1       = 0x1,
    NET_SERIALIZATION_MODE_COUNT   = 0x2,
    NET_SERIALIZATION_MODE_DEFAULT = 0x0,
    NET_SERIALIZATION_MODE_SERVER  = 0x0,
    NET_SERIALIZATION_MODE_CLIENT  = 0x1,
};

class INetworkMessageInternal
{
public:
    virtual ~INetworkMessageInternal() = default;

    virtual const char*       GetUnscopedName()   = 0;
    virtual NetMessageInfo_t* GetNetMessageInfo() = 0;

    virtual void SetMessageId(unsigned short nMessageId) = 0;

    virtual void AddCategoryMask(int nMask, bool) = 0;

    virtual void SwitchMode(NetworkValidationMode_t nMode) = 0;

    virtual CNetMessage* AllocateMessage() = 0;

    // Calls to INetworkMessages::SerializeMessageInternal
    virtual bool Serialize(void* pBuf, const CNetMessage* pData) = 0;
    // Calls to INetworkMessages::UnserializeMessageInternal
    virtual bool Unserialize(void* pBuf, CNetMessage* pData) = 0;
};

template <typename PROTO_TYPE>
class CNetMessagePB;

class CNetMessage
{
public:
    virtual ~CNetMessage() {}

    // Returns the underlying proto object
    virtual void* AsProto() const  = 0;
    virtual void* AsProto2() const = 0;

    virtual INetworkMessageInternal* GetNetMessage() const
    {
        // This shouldn't ever be called and the game provided vtable should be called instead!
        return nullptr;
    }

    virtual void* AllocAndCopy() const = 0;

    // Helper function to cast up the abstract message to a concrete CNetMessagePB<T> type.
    // Doesn't do any validity checks itself!
    template <typename T>
    const CNetMessagePB<T>* ToPB() const
    {
        return static_cast<const CNetMessagePB<T>*>(this);
    }

    template <typename T>
    CNetMessagePB<T>* ToPB()
    {
        return static_cast<CNetMessagePB<T>*>(this);
    }

private:
    char pad[0x28];
};

// AMNOTE: This is a stub class over real CNetMessagePB!
// This is mainly to access the game constructed objects, and not for direct initialization of them
// since this misses the CNetMessage implementation which requires supplying other proto related info like
// proto binding object, proto msg id/group, etc.
// So to allocate the message yourself use INetworkMessageInternal::AllocateMessage() or INetworkMessages::AllocateNetMessageAbstract()
// functions instead of direct initialization (they both are equivalent)!
// Example usage:
// CNetMessagePB<ProtoClass> *msg = INetworkMessageInternal::AllocateMessage()->ToPB<ProtoClass>();
// msg->field1( 2 );
// msg->field2( 3 );
// IGameEventSystem::PostEventAbstract( ..., msg, ... );
template <typename PROTO_TYPE>
class CNetMessagePB : public CNetMessage, public PROTO_TYPE
{
private:
    // Prevent manual construction of this object as per the comment above
    CNetMessagePB() {}
    CNetMessagePB(const CNetMessagePB&) {}
};

#define NETWORKMESSAGES_INTERFACE_VERSION "NetworkMessagesVersion001"

class INetworkMessages
{
    // IAppSystem
    virtual void Method_00() = 0;
    virtual void Method_01() = 0;
    virtual void Method_02() = 0;
    virtual void Method_03() = 0;
    virtual void Method_04() = 0;
    virtual void Method_05() = 0;
    virtual void Method_06() = 0;
    virtual void Method_07() = 0;
    virtual void Method_08() = 0;
    virtual void Method_09() = 0;
    virtual void Method_10() = 0;
    virtual void Method_11() = 0;

public:
    virtual NetMessageInfo_t*        GetNetMessageInfo(INetworkMessageInternal* pNetMessage) = 0;
    virtual INetworkMessageInternal* FindNetworkMessage(const char* szName)                  = 0;
    virtual INetworkMessageInternal* FindNetworkMessagePartial(const char* szPartialName)    = 0;
};

class INetworkSystem
{
    // IAppSystem
    virtual void Method_00() = 0;
    virtual void Method_01() = 0;
    virtual void Method_02() = 0;
    virtual void Method_03() = 0;
    virtual void Method_04() = 0;
    virtual void Method_05() = 0;
    virtual void Method_06() = 0;
    virtual void Method_07() = 0;
    virtual void Method_08() = 0;
    virtual void Method_09() = 0;
    virtual void Method_10() = 0;

public:
private:
    virtual void Method_11() = 0;
    virtual void Method_12() = 0;
    virtual void Method_13() = 0;
    virtual void Method_14() = 0;
    virtual void Method_15() = 0;
    virtual void Method_16() = 0;
    virtual void Method_17() = 0;
    virtual void Method_18() = 0;
    virtual void Method_19() = 0;
    virtual void Method_20() = 0;

    virtual void Method_21() = 0;
    virtual void Method_22() = 0;
    virtual void Method_23() = 0;
    virtual void Method_24() = 0;
    virtual void Method_25() = 0;
    virtual void Method_26() = 0;
    virtual void Method_27() = 0;
    virtual void Method_28() = 0;
    virtual void Method_29() = 0;
    virtual void Method_30() = 0;

    virtual void Method_31() = 0;
    virtual void Method_32() = 0;
    virtual void Method_33() = 0;
    virtual void Method_34() = 0;
    virtual void Method_35() = 0;
    virtual void Method_36() = 0;
    virtual void Method_37() = 0;
    virtual void Method_38() = 0;
    virtual void Method_39() = 0;
    virtual void Method_40() = 0;

    virtual void Method_41() = 0;
    virtual void Method_42() = 0;
    virtual void Method_43() = 0;
    virtual void Method_44() = 0;
    virtual void Method_45() = 0;
    virtual void Method_46() = 0;
    virtual void Method_47() = 0;

public:
    virtual void RejectNetChannel(uint32_t hNetInfo, uint32_t reason, void* pUnknown = nullptr) = 0;
};

class IGameEventSystem : public IAppSystem
{
    virtual void RegisterGameEvent(INetworkMessageInternal* pEvent)                                                     = 0;
    virtual void RegisterGameEventHandlerAbstract(void* nSlot, const void* delegate, INetworkMessageInternal* pEvent)   = 0;
    virtual void UnregisterGameEventHandlerAbstract(void* nSlot, const void* delegate, INetworkMessageInternal* pEvent) = 0;

public:
    virtual void PostEventAbstract_Local(uint32_t nSlot, INetworkMessageInternal* pEvent, const void* pData, unsigned long nSize) = 0;

    // BUG 这两兄弟在dll/so里面的位置跟这里相反, 我也不知道为什么
    // 大概率是编译器优化Overload
    virtual void PostEventAbstract(uint32_t nSlot, bool bLocalOnly, uint32_t nClientCount, const uint64_t* clients, INetworkMessageInternal* pEvent, const void* pData, uint64_t nSize, NetChannelBufType_t nReliable) = 0;
    virtual void PostEventAbstract(uint32_t nSlot, bool bLocalOnly, IRecipientFilter* pFilter, INetworkMessageInternal* pEvent, const void* pData, uint64_t nSize)                                                     = 0;

private:
    virtual void PostEntityEventAbstract(void* handle, INetworkMessageInternal* pEvent, const void* pData, unsigned long nSize, bool bReliable) = 0;

public:
    virtual void ProcessQueuedEvents()  = 0;
    virtual int  GetEventSource() const = 0;
    virtual void PurgeQueuedEvents()    = 0;
};

#endif
