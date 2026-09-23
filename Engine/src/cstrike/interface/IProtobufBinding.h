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

#ifndef CSTRIKE_INTERFACE_PROTOBUFBINDING_H
#define CSTRIKE_INTERFACE_PROTOBUFBINDING_H

#include "cstrike/type/CUtlString.h"

#include <cstddef>
#include <cstdint>

class Color;

using NetworkMessageId  = int32_t;
using NetworkGroupId    = uint8_t;
using NetworkCategoryId = uint32_t;

class IProtobufBinding
{
public:
    virtual const char* GetName() = 0;
    virtual int         GetSize() = 0;

    virtual const char* ToString(const void* pData, CUtlString& sResult) = 0;

    virtual const char* GetGroup()      = 0;
    virtual Color       GetGroupColor() = 0;
    virtual int32_t     GetBufType()    = 0;

    // virtual bool ReadFromBuffer(void* pData, bf_read& pBuf)       = 0;
    // virtual bool WriteToBuffer(const void* pData, bf_write& pBuf) = 0;

    virtual void* AllocateMessage() = 0;
    // virtual void  DeallocateMessage(void* pMsg) = 0;

    virtual bool OkToRedispatch() = 0;
    virtual bool unk001()         = 0;
};

struct NetMessageInfo_t
{
    int               m_nCategories;
    IProtobufBinding* m_pBinding;
    CUtlString        m_szGroup;
    NetworkMessageId  m_MessageId;
    NetworkGroupId    m_GroupId;
    uint8_t           m_nFlags;
    int               m_unk001;
    bool              m_bOkayToRedispatch;
};
static_assert(offsetof(NetMessageInfo_t, m_MessageId) == 0x18);
static_assert(offsetof(NetMessageInfo_t, m_GroupId) == 0x1C);
static_assert(offsetof(NetMessageInfo_t, m_nFlags) == 0x1D);
static_assert(offsetof(NetMessageInfo_t, m_bOkayToRedispatch) == 0x24);
static_assert(sizeof(NetMessageInfo_t) == 0x28);

#endif