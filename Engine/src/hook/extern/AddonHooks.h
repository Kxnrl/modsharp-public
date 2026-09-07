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

#ifndef MS_HOOK_EXTERN_ADDONHOOKS_H
#define MS_HOOK_EXTERN_ADDONHOOKS_H

struct CHostStateRequest;
class INetChannel;
class CNETMsg_SignonState;
template <typename T>
class CNetMessagePB;

namespace AddonHooks
{
class IAddonStrategy
{
public:
    virtual ~IAddonStrategy() = default;

    // Pre-call: opportunity to mutate pRequest before HostStateRequest runs.
    virtual void OnHostStateRequestPre(void* a1, CHostStateRequest* pRequest) = 0;

    // Pre-send: opportunity to mutate the SignonState message before the engine sends it.
    // Only called when m_MessageId == NET_MESSAGE_ID_SIGNON and the bypass flag is off.
    virtual void OnSignonStateNetMessagePre(INetChannel* pNetChannel, CNetMessagePB<CNETMsg_SignonState>* pData) = 0;
};

void Install(IAddonStrategy* strategy);
} // namespace AddonHooks

#endif
