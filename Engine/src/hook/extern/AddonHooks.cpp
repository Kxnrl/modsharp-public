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

#include "hook/extern/AddonHooks.h"

#include "global.h"
#include "hook/installer.h"
#include "hook/network.h"

#include "cstrike/interface/INetChannel.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/IProtobufBinding.h"
#include "cstrike/type/CHostState.h"

#include <proto/networkbasetypes.pb.h>

#include <safetyhook.hpp>

namespace
{
constexpr int32_t                NET_MESSAGE_ID_SIGNON = 7;
AddonHooks::IAddonStrategy*      s_pStrategy           = nullptr;
} // namespace

BeginStaticHookScope(HostStateRequest)
{
    DeclareStaticDetourHook(HostStateRequest, void, (void* a1, CHostStateRequest* pRequest))
    {
        if (s_pStrategy)
            s_pStrategy->OnHostStateRequestPre(a1, pRequest);

        HostStateRequest(a1, pRequest);
    }
}

BeginMemberHookScope(INetChannel)
{
    DeclareMemberDetourHook(SendNetMessage, bool, (INetChannel * pNetChannel, CNetMessagePB<CNETMsg_SignonState> * pData, int a4))
    {
        if (!s_bBypassNetMessageHook && s_pStrategy)
        {
            const auto pInfo = pData->GetNetMessage()->GetNetMessageInfo();
            if (pInfo->m_MessageId == NET_MESSAGE_ID_SIGNON)
                s_pStrategy->OnSignonStateNetMessagePre(pNetChannel, pData);
        }

        return SendNetMessage(pNetChannel, pData, a4);
    }
}

namespace AddonHooks
{
void Install(IAddonStrategy* strategy)
{
    s_pStrategy = strategy;
    SHOOK(HostStateRequest);
    HOOK(INetChannel, SendNetMessage);
}
} // namespace AddonHooks
