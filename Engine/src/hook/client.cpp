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

#include "bridge/forwards/forward.h"
#include "bridge/natives/ClientNatives.h"
#include "global.h"
#include "hook/installer.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "module.h"

#include "CoreCLR/RuntimeProtobufMessage.h"

#include "cstrike/entity/PlayerController.h"
#include "cstrike/interface/ICvar.h"
#include "cstrike/interface/IMemAlloc.h"
#include "cstrike/interface/INetwork.h"
#include "cstrike/interface/IProtobufBinding.h"
#include "cstrike/interface/IServerGameClient.h"
#include "cstrike/type/CBufferString.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/VProf.h"

#include <proto/netmessages.pb.h>
#include <safetyhook.hpp>

#include <algorithm>
#include <random>
#include <string>

// #define CLIENT_HOOK_ASSERT

#ifdef CLIENT_HOOK_ASSERT
#    include "cstrike/interface/IEngineServer.h"
#    include "cstrike/type/CGlobalVars.h"
#endif

constexpr int32_t NET_MESSAGE_ID_VOICE = 47;

static CServerSideClient* s_pCommandContextClient  = nullptr;
static CConVarBaseData*   ms_log_chat              = nullptr;
static CConVarBaseData*   ms_chat_block_whitespace = nullptr;
static CConVarBaseData*   ms_fix_voice_chat        = nullptr;
static uint64_t           s_RandomSeed;
static uint64_t           s_PlayerSeed[CS_MAX_PLAYERS];

void SetFakeClientCommandContext(CServerSideClient* pClient)
{
    s_pCommandContextClient = pClient;
}

BeginMemberHookScope(CSource2GameClients)
{
    DeclareVirtualHook(CheckConnect, bool, (IServerGameClient * pServerGameClients, PlayerSlot_t slot, const char* pszName, SteamId_t steamId, const char* pszAddress, bool bFakeClient, CBufferString* pRejectReason))
    {
        const auto pClient = sv->GetClientSafety(slot);
        if (!pClient)
        {
            LogError("CheckConnect -> %s<%llu> with nullptr", pszName, steamId);
            pRejectReason->Append("Service is unavailable!");
            return false;
        }

#ifdef CLIENT_HOOK_ASSERT
        LOG("%10s: 0x%p\n"
            "%10s: %d\n"
            "%10s: %s\n"
            "%10s: %llu\n"
            "%10s: %s\n"
            "%10s: %s\n"
            "%10s: %f\n"
            "%10s: %d\n",
            "this", pServerGameClients, "Slot", slot, "name", pszName, "SteamId", steamId, "Address", pszAddress, "Bot", BOOLVAL(bFakeClient), "flCurTime", gpGlobals->flCurTime, "Auth", BOOLVAL(pClient->IsAuthenticated()));
#endif

        g_pHookManager->Call_ClientConnect(HookType_Pre, slot, pszName, steamId, bFakeClient);

        char       rejectReason[256];
        bool       allow  = true;
        const auto action = forwards::OnClientConnect->Invoke(steamId, pszName, rejectReason, sizeof(rejectReason), &allow);

        if (action == EHookAction::SkipCallReturnOverride)
        {
            if (rejectReason[0])
            {
                LogInfo("CheckConnect -> %s<%llu> with reason: %s", pszName, steamId, rejectReason);
                pRejectReason->Append(rejectReason);
            }
            goto endCall;
        }

        if (action == EHookAction::Ignored)
        {
            // do ...
        }
        else
        {
            FatalError("Not impl yet");
            return false;
        }

        allow = CheckConnect(pServerGameClients, slot, pszName, steamId, pszAddress, bFakeClient, pRejectReason);

    endCall:

        if (allow)
        {
            forwards::OnClientConnected->Invoke(pClient);
            g_pHookManager->Call_ClientConnect(HookType_Post, slot, pszName, steamId, bFakeClient);
        }
        else
        {
            LogInfo("CheckConnect -> %s<%llu> with default false", pszName, steamId);
        }

        return allow;
    }

    DeclareMemberDetourHook(Connected, void, (IServerGameClient * pServerGameClient, PlayerSlot_t slot, const char* pszName, SteamId_t steamId, const char* pszNetworkId, const char* pszAddress, bool bFakeClient))
    {
        const auto pClient = sv->GetClient(slot);
        AssertPtr(pClient);

#ifdef CLIENT_HOOK_ASSERT
        LOG("%10s: 0x%p\n"
            "%10s: %d\n"
            "%10s: %s\n"
            "%10s: %llu\n"
            "%10s: %s\n"
            "%10s: %s\n"
            "%10s: %s\n"
            "%10s: %f\n"
            "%10s: %d\n",
            "this", pServerGameClient, "Slot", slot, "name", pszName, "SteamId", steamId, "NetworkId", pszNetworkId, "Address", pszAddress, "Bot", BOOLVAL(bFakeClient), "flCurTime", gpGlobals->flCurTime, "Auth", BOOLVAL(pClient->IsAuthenticated()));
#endif

        if (bFakeClient)
            g_pHookManager->Call_ClientConnect(HookType_Pre, slot, pszName, steamId, bFakeClient);

        Connected(pServerGameClient, slot, pszName, steamId, pszNetworkId, pszAddress, bFakeClient);

        if (!bFakeClient)
            AssertBool(!pClient->IsAuthenticated());

        if (bFakeClient)
            g_pHookManager->Call_ClientConnect(HookType_Post, slot, pszName, steamId, bFakeClient);

        if (bFakeClient)
            forwards::OnClientConnected->Invoke(pClient);
    }

    DeclareMemberDetourHook(Disconnect, void, (IServerGameClient * pServerGameClient, PlayerSlot_t slot, IServerGameClient::ENetworkDisconnectionReason eReason, const char* pszName, SteamId_t steamId, const char* pszNetworkId))
    {
#ifdef CLIENT_HOOK_ASSERT
        LOG("%10s: %d\n"
            "%10s: %d\n"
            "%10s: %s\n"
            "%10s: %llu\n"
            "%10s: %s\n"
            "%10s: %f",
            "Slot", slot, "Reason", eReason, "Name", pszName, "SteamId", steamId, "NetworkId", pszNetworkId, "flCurTime", gpGlobals->flCurTime);
#endif

        const auto pClient = sv->GetClient(slot);
        AssertPtr(pClient);

        g_pHookManager->Call_ClientDisconnect(HookType_Pre, slot, eReason, pszName, steamId);

        forwards::OnClientDisconnecting->Invoke(pClient, eReason);

        Disconnect(pServerGameClient, slot, eReason, pszName, steamId, pszNetworkId);

        g_pHookManager->Call_ClientDisconnect(HookType_Post, slot, eReason, pszName, steamId);

        forwards::OnClientDisconnected->Invoke(pClient, eReason);
    }

    DeclareMemberDetourHook(FullyConnected, void, (IServerGameClient * pServerGameClient, PlayerSlot_t slot))
    {
#ifdef CLIENT_HOOK_ASSERT
        const auto pController = CCSPlayerController::FindBySlot(slot);

        LOG("%10s: 0x%p\n"
            "%10s: %d\n"
            "%10s: %s\n"
            "%10s: %llu\n"
            "%10s: %f",
            "this", pServerGameClient, "Slot", slot, "Name", pController->GetName(), "SteamId", pController->GetSteamID(), "flCurTime", gpGlobals->flCurTime);
#endif

        g_pHookManager->Call_ClientFullyConnect(HookType_Pre, slot);

        FullyConnected(pServerGameClient, slot);

        g_pHookManager->Call_ClientFullyConnect(HookType_Post, slot);
    }

    DeclareMemberDetourHook(PutInServer, void, (IServerGameClient * pServerGameClient, PlayerSlot_t slot, const char* pszName, SteamId_t steamId))
    {
        g_pHookManager->Call_ClientPutInServer(HookType_Pre, slot, pszName, steamId);

        PutInServer(pServerGameClient, slot, pszName, steamId);

        g_pHookManager->Call_ClientPutInServer(HookType_Post, slot, pszName, steamId);

#ifdef CLIENT_HOOK_ASSERT
        const auto pController = CCSPlayerController::FindBySlot(slot);

        LOG("%10s: 0x%p\n"
            "%10s: %d\n"
            "%10s: %s\n"
            "%10s: %llu\n"
            "%10s: %f",
            "this", pServerGameClient, "Slot", slot, "Name", pController->GetName(), "SteamId", pController->GetSteamID(), "flCurTime", gpGlobals->flCurTime);
#endif
    }

    DeclareMemberDetourHook(Active, void, (IServerGameClient * pServerGameClient, PlayerSlot_t slot, bool bLoadGame, const char* pszName, SteamId_t steamId))
    {
#ifdef CLIENT_HOOK_ASSERT
        const auto pController = CCSPlayerController::FindBySlot(slot);

        LOG("%10s: 0x%p\n"
            "%10s: %d\n"
            "%10s: %s\n"
            "%10s: %llu\n"
            "%10s: %f",
            "this", pServerGameClient, "Slot", slot, "Name", pController->GetName(), "SteamId", pController->GetSteamID(), "flCurTime", gpGlobals->flCurTime);
#endif

        g_pHookManager->Call_ClientActivate(HookType_Pre, slot, pszName, steamId);

        Active(pServerGameClient, slot, bLoadGame, pszName, steamId);

        forwards::OnClientActive->Invoke(sv->GetClient(slot));

        g_pHookManager->Call_ClientActivate(HookType_Post, slot, pszName, steamId);
    }

    DeclareVirtualHook(SettingsChanged, void, (IServerGameClient * pServerGameClient, PlayerSlot_t slot))
    {
        SettingsChanged(pServerGameClient, slot);

        const auto client = sv->GetClientSafety(slot);
        if (client == nullptr)
            return;

        forwards::OnClientSettingChanged->Invoke(client);
    }

    // 为什么用这个Hook?
    // 因为里面判断了是否是已经注册ConCommand且符合FCVAR_CHEAT | FCVAR_CLIENT_CAN_EXECUTE等判断
    DeclareMemberDetourHook(Command, void, (IServerGameClient * pServerGameClient, PlayerSlot_t slot, const CCommand* pCommand))
    {
        AssertPtr(s_pCommandContextClient);
        AssertBool((s_pCommandContextClient->GetSlot() == slot));

        const auto pClient = s_pCommandContextClient;

        if (natives::client::PostCommand(pClient, pCommand->Arg(0), pCommand->ArgC() > 1 ? pCommand->ArgS() : nullptr) >= ECommandAction::Handled)
            return;

        Command(pServerGameClient, slot, pCommand);
    }
}

BeginMemberHookScope(CServerSideClient)
{
    DeclareMemberDetourHook(ExecuteStringCommand, bool, (CServerSideClient * pClient, CNetMessagePB<CNETMsg_StringCmd> * pConCommand))
    {
        VPROF_MS_HOOK();

        const auto pCommandString = pConCommand->command().c_str();

#ifdef CLIENT_HOOK_ASSERT
        LOG("%10s: 0%p -> %s(%d) %llu\n"
            "%10s: %s\n"
            "%10s: %f",
            "this", pClient, pClient->GetName(), pClient->GetSlot(), pClient->GetSteamId(), "Command", pCommandString, "flCurTime", gpGlobals->flCurTime);
#endif
        if (natives::client::PostCommand(pClient, pCommandString) == ECommandAction::Stopped)
            return true;

        AssertBool((s_pCommandContextClient == nullptr));

        s_pCommandContextClient = pClient;
        const auto result       = ExecuteStringCommand(pClient, pConCommand);
        s_pCommandContextClient = nullptr;

        return result;
    }

    DeclareMemberDetourHook(IsHearingClient, bool, (CServerSideClient * pClient, int32_t nSlot))
    {
        if (pClient->GetSlot() == nSlot)
            return IsHearingClient(pClient, nSlot);

        if (nSlot < 0 || nSlot > 63)
            return false;

        // HACK 可能这里需要判nullptr?
        const auto pSource = sv->GetClient(static_cast<PlayerSlot_t>(nSlot));
        AssertPtr(pSource);

        auto       allow  = true;
        const auto action = forwards::OnClientAllowHear->Invoke(pClient, pSource, &allow);
        if (action == EHookAction::Ignored)
            return IsHearingClient(pClient, nSlot);

        if (action == EHookAction::SkipCallReturnOverride)
            return allow;

        FatalError("Not impl yet!");
        return false;
    }

    DeclareVirtualHook(CLCMsg_VoiceData, bool, (CServerSideClient * pClient, CNetMessage * pVoiceData))
    {
        const auto  msg           = static_cast<const CCLCMsg_VoiceData*>(pVoiceData->AsProto());
        const auto& audio         = msg->audio();
        const auto  sectionNumber = audio.section_number();
        const auto& voiceData     = audio.voice_data();
        const auto  voiceDataPtr  = voiceData.data();
        const auto  voiceDataSize = voiceData.size();
        const auto  xuid          = msg->xuid();

        const auto action = forwards::OnClientSpeakPre->Invoke(pClient, xuid, sectionNumber, voiceDataPtr, voiceDataSize);
        if (action == EHookAction::SkipCallReturnOverride)
        {
            forwards::OnClientSpeakPost->Invoke(pClient, xuid, sectionNumber, voiceDataPtr, voiceDataSize, action);
            return true; // always true
        }

        if (action == EHookAction::Ignored)
        {
            CLCMsg_VoiceData(pClient, pVoiceData);
            forwards::OnClientSpeakPost->Invoke(pClient, xuid, sectionNumber, voiceDataPtr, voiceDataSize, action);
            return true;
        }

        FatalError("Not impl yet!");
        return true;
    }

    DeclareVirtualHook(CLCMsg_RespondCvarValue, bool, (CServerSideClient * pClient, CNetMessage * pData))
    {
        const auto msg    = static_cast<const CCLCMsg_RespondCvarValue*>(pData->AsProto());
        const auto cookie = msg->cookie();

        if (cookie == 0)
            return true;

        const auto status = msg->status_code();
        const auto name   = msg->name().c_str();
        const auto value  = msg->value().c_str();

        forwards::OnClientQueryConVar->Invoke(pClient, cookie, status, name, value);

        return true;
    }

    DeclareMemberDetourHook(SendNetMessage, bool, (CServerSideClient * pClient, CNetMessage * pData, int8_t bufType))
    {
        if (const auto info = pData->GetNetMessage()->GetNetMessageInfo())
        {
            if (info->m_MessageId == NET_MESSAGE_ID_VOICE && ms_fix_voice_chat->GetValue<bool>()
                // ignore csharp proto
                && nullptr == dynamic_cast<CSharpNetworkDataWrapper*>(pData))
            {
                const auto msg   = pData->ToPB<CSVCMsg_VoiceData>();
                const auto slot  = pClient->GetSlot();
                const auto index = msg->client();
                const auto xuid  = s_PlayerSeed[slot] + index;
                msg->set_xuid(xuid);
            }
        }

        return SendNetMessage(pClient, pData, bufType);
    }
}

BeginStaticHookScope(HostSay)
{
    DeclareStaticDetourHook(HostSay, void, (CCSPlayerController * pFrom, const CCommand& args, bool bTeamOnly, uint32_t a4, const char* a5))
    {
        VPROF_MS_HOOK();

        if (!pFrom)
        {
            if (ms_log_chat->GetValue<bool>())
                LOG("Console %s -> %s", args.Arg(0), args.ArgS());

            const auto action = forwards::OnConsoleSay->Invoke(args.ArgS());
            if (action == ECommandAction::Stopped)
                return;

            if (action == ECommandAction::Skipped)
            {
                HostSay(pFrom, args, bTeamOnly, a4, a5);
                return;
            }

            FatalError("Not impl yet!");
            return;
        }

        const auto pClient = sv->GetClient(pFrom->GetPlayerSlot());
        if (pClient == nullptr)
        {
            WARN("pClient == nullptr");
            return;
        }

        const auto pCommand = args.Arg(0);
        const auto pArgs    = args.ArgS();

        AssertPtr(pCommand);
        AssertPtr(pArgs);

        std::string text(pArgs);

        // Y/U打字是会被引号包裹的
        // 但是空格的是没有的, 这里需要手动strip

        // Strip Quotes
        if (text.size() >= 2 && pArgs[0] == '"' && pArgs[text.size() - 1] == '"')
        {
            text.pop_back();
            text.erase(0, 1);
        }

        if (text.empty())
        {
            if (ms_log_chat->GetValue<bool>())
                LOG("Player %s -> %s -> EmptyString => [%s]", pClient->GetName(), pCommand, args.ArgS());

            return;
        }

        static auto starts_with_unicode = [](const std::string_view s) {
            const auto c = static_cast<uint8_t>(s[0]);
            // ASCII?
            if ((c & 0x80) == 0)
                return false;
            // Continuation byte?
            if ((c & 0xC0) == 0x80)
                return false;
            // Leading-byte patterns for 2–4 byte sequences
            return (c & 0xE0) == 0xC0     // 2-byte
                   || (c & 0xF0) == 0xE0  // 3-byte
                   || (c & 0xF8) == 0xF0; // 4-byte
        };

        if (!starts_with_unicode(text) && std::iscntrl(text[0]))
        {
            if (ms_log_chat->GetValue<bool>())
                LOG("Player %s -> %s -> control character => [%s]", pClient->GetName(), pCommand, args.ArgS());

            return;
        }

        if (ms_chat_block_whitespace->GetValue<bool>() && std::ranges::all_of(text, [](const char c) { return c == ' '; }))
        {
            // All Space
            if (ms_log_chat->GetValue<bool>())
                LOG("Player %s -> %s -> AllSpace", pClient->GetName(), pCommand);

            return;
        }

        const auto pArgument = text.c_str();

        if (ms_log_chat->GetValue<bool>())
            LOG("Player %s -> %s -> [%s] -> size=%d args=%d", pClient->GetName(), pCommand, pArgument, static_cast<int32_t>(text.size()), args.ArgC());

        const auto response = forwards::OnClientSayCommand->Invoke(pClient, bTeamOnly, pCommand, pArgument);
        if (response >= ECommandAction::Handled)
            return;

        HostSay(pFrom, args, bTeamOnly, a4, a5);
    }
}

BeginStaticHookScope(ScriptPrintMessageChatAll)
{
    DeclareStaticDetourHook(ScriptPrintMessageChatAll, void, (const char* pszMessage))
    {
        const auto action = forwards::OnConsoleSay->Invoke(pszMessage);
        if (action == ECommandAction::Stopped)
            return;

        if (action == ECommandAction::Skipped)
        {
            ScriptPrintMessageChatAll(pszMessage);
            return;
        }

        FatalError("Not impl yet!");
    }
}

void InstallClientHooks()
{
    // NOTE 修改初始Seed以避免不同服务器的Seed相同, 再更换服务器后可能出现问题
    std::random_device rd;
    s_RandomSeed = (static_cast<uint64_t>(rd()) << 32) | rd();

    VHOOK(CSource2GameClients, CheckConnect, server);
    HOOK(CSource2GameClients, Connected);
    HOOK(CSource2GameClients, Disconnect);
    HOOK(CSource2GameClients, PutInServer);
    HOOK(CSource2GameClients, Active);
    VHOOK(CSource2GameClients, SettingsChanged, server);
    HOOK(CSource2GameClients, Command);
    HOOK(CSource2GameClients, FullyConnected);

    HOOK(CServerSideClient, ExecuteStringCommand);
    HOOK(CServerSideClient, IsHearingClient);
    VHOOK(CServerSideClient, CLCMsg_VoiceData, engine);
    VHOOK(CServerSideClient, CLCMsg_RespondCvarValue, engine);
    HOOK(CServerSideClient, SendNetMessage);

    SHOOK(HostSay);
    SHOOK(ScriptPrintMessageChatAll);

    g_pHookManager->Hook_ClientFullyConnect(HookType_Post, [](PlayerSlot_t slot) {
        const auto pClient = sv->GetClient(slot);
        AssertPtr(pClient);
        pClient->ConsolePrint(MODSHARP_MESSAGE);
    });

    g_pHookManager->Hook_ClientConnect(HookType_Post, [](PlayerSlot_t slot, const char*, SteamId_t, bool) {
        s_PlayerSeed[slot] = s_RandomSeed;
        s_RandomSeed += 66;
    });

    ms_log_chat              = g_ConVarManager.CreateConVar("ms_log_chat", false, "Log chat messages.", FCVAR_RELEASE);
    ms_chat_block_whitespace = g_ConVarManager.CreateConVar("ms_chat_block_whitespace", true, "Block whitespace messages.", FCVAR_RELEASE);
    ms_fix_voice_chat        = g_ConVarManager.CreateConVar("ms_fix_voice_chat", true, "Fix voice chat.", FCVAR_RELEASE);
}

void ExecuteClientStringCommand(CServerSideClient* pClient, const char* pCommandString)
{
    AssertPtr(pClient);
    AssertPtr(pCommandString);

    const auto net = g_pNetworkMessages->FindNetworkMessagePartial("CNETMsg_StringCmd");
    AssertPtr(net);
    auto msg = net->AllocateMessage()->ToPB<CNETMsg_StringCmd>();

    msg->set_command(pCommandString);

    CServerSideClient_Hooks::ExecuteStringCommand(pClient, msg);

    g_pMemAlloc->Free(msg);
}