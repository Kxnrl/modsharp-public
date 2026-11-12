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
#include "cstrike/type/CUtlString.h"

#include <cstddef>
#include <cstdint>
#include <string>
#include <string_view>

class CBaseHandle;
class CUtlAbstractDelegate;
class CUtlSlot;
class IRecipientFilter;
class CNetMessage;

enum NetworkSerializationMode_t
{
    NET_SERIALIZATION_MODE_0       = 0x0,
    NET_SERIALIZATION_MODE_1       = 0x1,
    NET_SERIALIZATION_MODE_COUNT   = 0x2,
    NET_SERIALIZATION_MODE_DEFAULT = 0x0,
    NET_SERIALIZATION_MODE_SERVER  = 0x0,
    NET_SERIALIZATION_MODE_CLIENT  = 0x1,
};

using NetworkMessageId     = int32_t;
using NetworkGroupId       = uint8_t;
using NetworkCategoryId    = uint32_t;
using NetMessageAllocateFn = CNetMessage* (*)();

struct NetMessageInfo_t
{
    CUtlString           m_szName; // "<proto type name> [<message id>]"; look up by type name with INetworkMessages::FindNetworkMessageByTypeName
    NetMessageAllocateFn m_pfnAllocateMessage;
    CUtlString           m_szGroup;
    NetworkMessageId     m_MessageId;
    uint32_t             m_nCategories;
    int32_t              m_Field_032;
    NetworkGroupId       m_GroupId;
    uint8_t              m_nFlags; // bits 0-4: default buf type, bit 5: not networked (Serialize rejects it, message id auto-assigned)

    CNetMessage* AllocateMessage() const { return m_pfnAllocateMessage(); }
};
static_assert(offsetof(NetMessageInfo_t, m_pfnAllocateMessage) == 0x08);
static_assert(offsetof(NetMessageInfo_t, m_MessageId) == 0x18);
static_assert(offsetof(NetMessageInfo_t, m_nCategories) == 0x1C);
static_assert(offsetof(NetMessageInfo_t, m_GroupId) == 0x24);
static_assert(offsetof(NetMessageInfo_t, m_nFlags) == 0x25);
static_assert(sizeof(NetMessageInfo_t) == 0x28);

template <typename PROTO_TYPE>
class CNetMessagePB;

class CNetMessage
{
public:
    virtual ~CNetMessage() {}

    // Returns the underlying proto object
    virtual void* AsProto() const  = 0;
    virtual void* AsProto2() const = 0;

    virtual NetMessageInfo_t* GetNetMessage() const
    {
        // This shouldn't ever be called and the game provided vtable should be called instead!
        return nullptr;
    }

    virtual void* AllocAndCopy() const = 0;

    virtual int GetType() const = 0;

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
    void*    m_pUnk08{};
    uint32_t m_nUnk10{};
    uint8_t  m_nUnk14{0xFF};
    uint8_t  m_nUnk15[3]{};
    int64_t  m_nUnk18{-1};
    float    m_flUnk20{-1.0f};
    uint32_t m_nUnk24{};
    int64_t  m_nUnk28{-1};
};
static_assert(sizeof(CNetMessage) == 0x30);

// taken from https://github.com/alliedmodders/hl2sdk/blob/cs2/public/networksystem/netmessage.h
// AMNOTE: This is a stub class over real CNetMessagePB!
// This is mainly to access the game constructed objects, and not for direct initialization of them
// since this misses the CNetMessage implementation which requires supplying other proto related info like
// proto msg id/group, etc.
// So to allocate the message yourself use NetMessageInfo_t::AllocateMessage() instead of direct initialization!
// Example usage:
// CNetMessagePB<ProtoClass> *msg = pNetMessageInfo->AllocateMessage()->ToPB<ProtoClass>();
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
    virtual void Method_00() = 0;
    virtual void Method_01() = 0;
    virtual void Method_02() = 0;
    virtual void Method_03() = 0;
    virtual void Method_04() = 0;
    virtual void Method_05() = 0;
    virtual void Method_06() = 0;
    virtual void Method_07() = 0;

public:
    virtual NetMessageInfo_t* GetNetMessageInfo(NetMessageInfo_t* pNetMessage)     = 0;
    virtual NetMessageInfo_t* FindNetworkMessage(const char* szName)               = 0;
    virtual NetMessageInfo_t* FindNetworkMessagePartial(const char* szPartialName) = 0;

    NetMessageInfo_t* FindNetworkMessageByTypeName(const char* szTypeName)
    {
        const auto prefix = std::string(szTypeName) + " [";
        const auto info   = FindNetworkMessagePartial(prefix.c_str());

        if (info == nullptr || !std::string_view(info->m_szName.Get()).starts_with(prefix))
            return nullptr;

        return info;
    }
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
    virtual void RegisterGameEvent(NetMessageInfo_t* pEvent)                                                                                      = 0;
    virtual void RegisterGameEventHandlerAbstract(CUtlSlot* pSlot, const CUtlAbstractDelegate& delegate, NetMessageInfo_t* pEvent, int nPriority) = 0;
    virtual void UnregisterGameEventHandlerAbstract(CUtlSlot* pSlot, const CUtlAbstractDelegate& delegate, NetMessageInfo_t* pEvent)              = 0;

public:
    virtual void PostEventAbstract_Local(uint32_t nSlot, NetMessageInfo_t* pEvent, const void* pData, uint64_t nSize) = 0;

    virtual void PostEventAbstract(uint32_t nSlot, bool bLocalOnly, uint32_t nClientCount, const uint64_t* clients, NetMessageInfo_t* pEvent, const void* pData, uint64_t nSize, NetChannelBufType_t bufType) = 0;
    virtual void PostEventAbstract(uint32_t nSlot, bool bLocalOnly, IRecipientFilter* pFilter, NetMessageInfo_t* pEvent, const void* pData, uint64_t nSize)                                                   = 0;

private:
    virtual void PostEntityEventAbstract(const CBaseHandle& hHandle, NetMessageInfo_t* pEvent, const void* pData, uint64_t nSize, NetChannelBufType_t bufType, bool bAllClients) = 0;

public:
    virtual void ProcessQueuedEvents()  = 0;
    virtual int  GetEventSource() const = 0;
    virtual void PurgeQueuedEvents()    = 0;
};

#endif
