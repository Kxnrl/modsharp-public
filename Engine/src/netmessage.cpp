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

#include "netmessage.h"
#include "global.h"
#include "hook/network.h"
#include "logging.h"

#include "cstrike/interface/IMemAlloc.h"
#include "cstrike/interface/INetwork.h"

#include <proto/netmessages.pb.h>
#include <proto/usermessages.pb.h>

#define DeclareNetMessage(name)                                            \
    const auto net = g_pNetworkMessages->FindNetworkMessagePartial(#name); \
    AssertPtr(net);                                                        \
    auto msg = net->AllocateMessage()->ToPB<name>()

#define SendNetMessageF()                                                 \
    SetNetworkMessageBypassHook(true);                                    \
    g_pGameEventSystem->PostEventAbstract(0, false, filter, net, msg, 0); \
    SetNetworkMessageBypassHook(false);                                   \
    g_pMemAlloc->Free(msg)

#define SendNetMessageB()                                                                           \
    SetNetworkMessageBypassHook(true);                                                              \
    g_pGameEventSystem->PostEventAbstract(0, false, bitWide, &pClients, net, msg, 0, BUF_RELIABLE); \
    SetNetworkMessageBypassHook(false);                                                             \
    g_pMemAlloc->Free(msg)

void UTIL_SayText2Filter(IRecipientFilter* filter, bool chat, EntityIndex_t entity, const char* message, const char* param1, const char* param2, const char* param3, const char* param4)
{
    DeclareNetMessage(CUserMessageSayText2);

    msg->set_chat(chat);
    msg->set_entityindex(entity);
    msg->set_messagename(message);

    msg->set_param1(param1 ? param1 : "");
    msg->set_param2(param2 ? param2 : "");
    msg->set_param3(param3 ? param3 : "");
    msg->set_param4(param4 ? param4 : "");

    SendNetMessageF();
}

void UTIL_SayText2Filter(int bitWide, NetworkReceiver_t pClients, bool chat, EntityIndex_t entity, const char* message, const char* param1, const char* param2, const char* param3, const char* param4)
{
    DeclareNetMessage(CUserMessageSayText2);

    msg->set_chat(chat);
    msg->set_entityindex(entity);
    msg->set_messagename(message);

    msg->set_param1(param1 ? param1 : "");
    msg->set_param2(param2 ? param2 : "");
    msg->set_param3(param3 ? param3 : "");
    msg->set_param4(param4 ? param4 : "");

    SendNetMessageB();
}

void UTIL_TextMsgFilter(IRecipientFilter* filter, HudPrint_t hudPrint, const char* message, const char* param1, const char* param2, const char* param3, const char* param4)
{
    DeclareNetMessage(CUserMessageTextMsg);

    msg->set_dest(hudPrint);
    msg->add_param(message);

    if (param1)
        msg->add_param(param1);

    if (param2)
        msg->add_param(param2);

    if (param3)
        msg->add_param(param3);

    if (param4)
        msg->add_param(param4);

    SendNetMessageF();
}

void UTIL_TextMsgFilter(int bitWide, NetworkReceiver_t pClients, HudPrint_t hudPrint, const char* message, const char* param1, const char* param2, const char* param3, const char* param4)
{
    DeclareNetMessage(CUserMessageTextMsg);

    msg->set_dest(hudPrint);
    msg->add_param(message);

    if (param1)
        msg->add_param(param1);

    if (param2)
        msg->add_param(param2);

    if (param3)
        msg->add_param(param3);

    if (param4)
        msg->add_param(param4);

    SendNetMessageB();
}

void UTIL_PrintFilter(IRecipientFilter* filter, const char* message)
{
    DeclareNetMessage(CSVCMsg_Print);

    msg->set_text(message);

    SendNetMessageF();
}

void UTIL_PrintFilter(int bitWide, NetworkReceiver_t pClients, const char* message)
{
    DeclareNetMessage(CSVCMsg_Print);

    msg->set_text(message);

    SendNetMessageB();
}

void UTIL_ReplicateConVarFilter(IRecipientFilter* filter, const char* pName, const char* pValue)
{
    DeclareNetMessage(CNETMsg_SetConVar);

    CMsg_CVars_CVar* cvar = msg->mutable_convars()->add_cvars();
    cvar->set_name(pName);
    cvar->set_value(pValue);

    SendNetMessageF();
}
