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

#include "bridge/natives/ClientNatives.h"
#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"

#include "global.h"
#include "logging.h"
#include "netmessage.h"

#include "cstrike/consts.h"
#include "cstrike/interface/ICvar.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/IServerGameClient.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"

#include <array>
#include <string>
#include <vector>

extern void SetFakeClientCommandContext(CServerSideClient* pClient);
extern void ExecuteClientStringCommand(CServerSideClient* pClient, const char* pCommandString);

namespace natives::client
{
struct Command_t
{
    Command_t(const std::string& command) :
        m_FullCommand(command)
    {
    }

    [[nodiscard]] const char*        GetCommand() const noexcept { return m_FullCommand.c_str(); }
    [[nodiscard]] const std::string& GetString() const noexcept { return m_FullCommand; }

    [[nodiscard]] bool MatchFull(const char* pCommand) const noexcept
    {
        return strcasecmp(pCommand, m_FullCommand.c_str()) == 0;
    }

    bool operator==(const Command_t& other) const noexcept
    {
        return strcasecmp(m_FullCommand.c_str(), other.GetCommand()) == 0;
    }

    bool operator!=(const Command_t& other) const noexcept
    {
        return strcasecmp(m_FullCommand.c_str(), other.GetCommand()) != 0;
    }

private:
    std::string m_FullCommand;
};

// FIXME 某些命令不知道为什么完全没有String, 也不是ConCommand
static const std::array s_hackCommands = {std::string("drop")};

static std::vector<Command_t>   s_hookedCommand;
static std::vector<std::string> s_commandListener;
static std::vector<Command_t>   s_commandProxyListener;

static bool IsHackCommand(const std::string& command)
{
    for (const auto& c : s_hackCommands)
    {
        if (c == command)
        {
            return true;
        }
    }
    return false;
}

ECommandAction PostCommand(CServerSideClient* pClient, const char* pCommandString)
{
    CCommand tokens;
    if (!tokens.Tokenize(pCommandString) || tokens.ArgC() == 0)
        return ECommandAction::Stopped;

    const auto pName = tokens.Arg(0);
    const auto pArgs = tokens.ArgC() > 1 ? tokens.ArgS() : nullptr;

    if (strncasecmp(pName, "meta", 4) == 0)
    {
        UTIL_TextMsgSingle(pClient->GetSlot(), Hud_Console, "please use command 'ms' or 'modsharp' instead!");
        return ECommandAction::Stopped;
    }
    if (strncasecmp(pName, "ms_", 3) == 0)
    {
        if (strlen(pName) < 4)
        {
            return ECommandAction::Stopped;
        }

        const auto pActualCommand = &pName[3];

        for (const auto& command : s_hookedCommand)
        {
            if (command.MatchFull(pActualCommand))
            {
                return forwards::OnClientStringCommand->Invoke(pClient, command.GetCommand(), false, pArgs);
            }
        }

        return ECommandAction::Stopped;
    }

    // NOTE 某些实体命令走的是CCommand::Callback
    for (const auto& sCommand : s_commandProxyListener)
    {
        if (sCommand.MatchFull(pName))
        {
            return PostCommand(pClient, sCommand.GetCommand(), pArgs);
        }
    }

    return ECommandAction::Skipped;
}

ECommandAction PostCommand(CServerSideClient* pClient, const char* pCommand, const char* pArgString)
{
    for (auto& command : s_commandListener)
    {
        if (strcasecmp(pCommand, command.c_str()) == 0)
        {
            const auto action = forwards::OnClientConsoleCommand->Invoke(pClient, command.c_str(), pArgString);
            return action;
        }
    }
    return ECommandAction::Skipped;
}

static CServerSideClient* GetClientBySlot(PlayerSlot_t slot)
{
    return sv ? sv->GetClientSafety(slot) : nullptr;
}
static CServerSideClient* GetClientByUserId(UserId_t userId)
{
    return sv ? sv->GetClientByUserId(userId) : nullptr;
}
static CServerSideClient* GetClientBySteamId(SteamId_t steamId)
{
    return sv ? sv->GetClientBySteamId(steamId) : nullptr;
}
static void KickClient(CServerSideClient* pClient, const char* reason, IServerGameClient::ENetworkDisconnectionReason msgId = IServerGameClient::NETWORK_DISCONNECT_INVALID)
{
    engine->KickClient(pClient->GetSlot(), reason, msgId ? msgId : IServerGameClient::NETWORK_DISCONNECT_KICKED);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static const char* ClientGetClientAddress(CServerSideClient* pClient)
{
    return pClient->GetNetWorkAddress();
}
static float ClientGetClientTimeConnected(CServerSideClient* pClient)
{
    return pClient->GetTimeConnected();
}
static void ClientConsolePrint(CServerSideClient* pClient, const char* message)
{
    pClient->ConsolePrint(message);
}
static void ClientSetClientName(CServerSideClient* pClient, const char* name)
{
    pClient->SetName(name);
}
static bool ClientSayChatMessage(CServerSideClient* pClient, bool teamOnly, const char* message)
{
    return pClient->SayChatMessage(teamOnly, message);
}
static void AddCommandCallback(const char* pCommand)
{
    Command_t command(pCommand);

    for (const auto& c : s_hookedCommand)
    {
        if (c == command)
        {
            return;
        }
    }

    s_hookedCommand.emplace_back(command);
}
static void AddCommandListener(const char* command)
{
    std::string sCommand(command);
    const auto  pCommand = g_pCVar->FindConCommandIterator(command);
    const auto  gameDll  = pCommand != nullptr && pCommand->GetFlags() & FCVAR_GAMEDLL;
    if (gameDll || IsHackCommand(sCommand))
    {
        auto exists = false;

        for (const auto& c : s_commandProxyListener)
        {
            if (c.GetString() == sCommand)
            {
                exists = true;
            }
        }

        if (!exists)
            s_commandProxyListener.emplace_back(sCommand);
    }

    for (const auto& c : s_commandListener)
    {
        if (c == sCommand)
        {
            return;
        }
    }

    s_commandListener.emplace_back(sCommand);
}
static const char* ClientGetConVarValue(CServerSideClient* pClient, const char* name)
{
    AssertPtr(pClient);
    return engine->GetClientConVarValue(pClient->GetSlot(), name);
}
static void ClientCommand(CServerSideClient* pClient, const char* command)
{
    AssertPtr(pClient);
    engine->ClientCommand(pClient->GetSlot(), "%s\n", command);
}
static void FakeClientCommand(CServerSideClient* pClient, const char* pCommand)
{
    AssertPtr(pClient);

    {
        CCommand command;
        if (command.Tokenize(pCommand))
            gameClients->ClientCommand(pClient->GetSlot(), command);
    }
}
static void ExecuteStringCommand(CServerSideClient* pClient, const char* pCommand)
{
    AssertPtr(pClient);
    ExecuteClientStringCommand(pClient, pCommand);
}

void Init()
{
    // sv
    bridge::CreateNative("Client.GetClientBySlot", reinterpret_cast<void*>(GetClientBySlot));
    bridge::CreateNative("Client.GetClientByUserId", reinterpret_cast<void*>(GetClientByUserId));
    bridge::CreateNative("Client.GetClientBySteamId", reinterpret_cast<void*>(GetClientBySteamId));
    bridge::CreateNative("Client.KickClient", reinterpret_cast<void*>(KickClient));

    // cl
    bridge::CreateNative("Client.GetNetWorkAddress", reinterpret_cast<void*>(ClientGetClientAddress));
    bridge::CreateNative("Client.GetTimeConnected", reinterpret_cast<void*>(ClientGetClientTimeConnected));
    bridge::CreateNative("Client.ConsolePrint", reinterpret_cast<void*>(ClientConsolePrint));
    bridge::CreateNative("Client.SetName", reinterpret_cast<void*>(ClientSetClientName));
    bridge::CreateNative("Client.GetConVarValue", reinterpret_cast<void*>(ClientGetConVarValue));
    bridge::CreateNative("Client.SayChatMessage", reinterpret_cast<void*>(ClientSayChatMessage));
    bridge::CreateNative("Client.ClientCommand", reinterpret_cast<void*>(ClientCommand));
    bridge::CreateNative("Client.FakeClientCommand", reinterpret_cast<void*>(FakeClientCommand));
    bridge::CreateNative("Client.ExecuteStringCommand", reinterpret_cast<void*>(ExecuteStringCommand));

    bridge::CreateNative("Client.AddCommandCallback", reinterpret_cast<void*>(AddCommandCallback));
    bridge::CreateNative("Client.AddCommandListener", reinterpret_cast<void*>(AddCommandListener));
}

} // namespace natives::client