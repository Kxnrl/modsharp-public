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

#ifndef CSTRIKE_INTERFACE_SERVERGAMECLIENTS_H
#define CSTRIKE_INTERFACE_SERVERGAMECLIENTS_H

#include "cstrike/interface/IAppSystem.h"

#include "definitions.h"

/* IServerGameClients */
class IServerGameClient : IAppSystem
{
public:
    enum EClientType
    {
        ClientType_Human,
        ClientType_Bot,
        ClientType_Unknown,
    };
    enum ENetworkDisconnectionReason : int32_t
    {
        NETWORK_DISCONNECT_INVALID                               = 0,
        NETWORK_DISCONNECT_SHUTDOWN                              = 1,
        NETWORK_DISCONNECT_DISCONNECT_BY_USER                    = 2,  // "#GameUI_Disconnect_User"
        NETWORK_DISCONNECT_DISCONNECT_BY_SERVER                  = 3,  // "#GameUI_Disconnect_Server"
        NETWORK_DISCONNECT_LOST                                  = 4,  // "#GameUI_Disconnect_ConnectionLost"
        NETWORK_DISCONNECT_OVERFLOW                              = 5,  // "#GameUI_Disconnect_ConnectionOverflow"
        NETWORK_DISCONNECT_STEAM_BANNED                          = 6,  // "#GameUI_Disconnect_SteamIDBanned"
        NETWORK_DISCONNECT_STEAM_INUSE                           = 7,  // "#GameUI_Disconnect_SteamIDInUse"
        NETWORK_DISCONNECT_STEAM_TICKET                          = 8,  // "#GameUI_Disconnect_SteamTicket"
        NETWORK_DISCONNECT_STEAM_LOGON                           = 9,  // "#GameUI_Disconnect_SteamLogon"
        NETWORK_DISCONNECT_STEAM_AUTHCANCELLED                   = 10, // "#GameUI_Disconnect_SteamLogon"
        NETWORK_DISCONNECT_STEAM_AUTHALREADYUSED                 = 11, // "#GameUI_Disconnect_SteamLogon"
        NETWORK_DISCONNECT_STEAM_AUTHINVALID                     = 12, // "#GameUI_Disconnect_SteamLogon"
        NETWORK_DISCONNECT_STEAM_VACBANSTATE                     = 13, // "#GameUI_Disconnect_SteamVAC"
        NETWORK_DISCONNECT_STEAM_LOGGED_IN_ELSEWHERE             = 14, // "#GameUI_Disconnect_SteamInUse"
        NETWORK_DISCONNECT_STEAM_VAC_CHECK_TIMEDOUT              = 15, // "#GameUI_Disconnect_SteamTimeOut"
        NETWORK_DISCONNECT_STEAM_DROPPED                         = 16, // "#GameUI_Disconnect_SteamDropped"
        NETWORK_DISCONNECT_STEAM_OWNERSHIP                       = 17, // "#GameUI_Disconnect_SteamOwnership"
        NETWORK_DISCONNECT_SERVERINFO_OVERFLOW                   = 18, // "#GameUI_Disconnect_ServerInfoOverflow"
        NETWORK_DISCONNECT_TICKMSG_OVERFLOW                      = 19, // "#GameUI_Disconnect_TickMessage"
        NETWORK_DISCONNECT_STRINGTABLEMSG_OVERFLOW               = 20, // "#GameUI_Disconnect_StringTableMessage"
        NETWORK_DISCONNECT_DELTAENTMSG_OVERFLOW                  = 21, // "#GameUI_Disconnect_DeltaEntMessage"
        NETWORK_DISCONNECT_TEMPENTMSG_OVERFLOW                   = 22, // "#GameUI_Disconnect_TempEntMessage"
        NETWORK_DISCONNECT_SOUNDSMSG_OVERFLOW                    = 23, // "#GameUI_Disconnect_SoundsMessage"
        NETWORK_DISCONNECT_SNAPSHOTOVERFLOW                      = 24, // "#GameUI_Disconnect_SnapshotOverflow"
        NETWORK_DISCONNECT_SNAPSHOTERROR                         = 25, // "#GameUI_Disconnect_SnapshotError"
        NETWORK_DISCONNECT_RELIABLEOVERFLOW                      = 26, // "#GameUI_Disconnect_ReliableOverflow"
        NETWORK_DISCONNECT_BADDELTATICK                          = 27, // "#GameUI_Disconnect_BadClientDeltaTick"
        NETWORK_DISCONNECT_NOMORESPLITS                          = 28, // "#GameUI_Disconnect_NoMoreSplits"
        NETWORK_DISCONNECT_TIMEDOUT                              = 29, // "#GameUI_Disconnect_TimedOut"
        NETWORK_DISCONNECT_DISCONNECTED                          = 30, // "#GameUI_Disconnect_Disconnected"
        NETWORK_DISCONNECT_LEAVINGSPLIT                          = 31, // "#GameUI_Disconnect_LeavingSplit"
        NETWORK_DISCONNECT_DIFFERENTCLASSTABLES                  = 32, // "#GameUI_Disconnect_DifferentClassTables"
        NETWORK_DISCONNECT_BADRELAYPASSWORD                      = 33, // "#GameUI_Disconnect_BadRelayPassword"
        NETWORK_DISCONNECT_BADSPECTATORPASSWORD                  = 34, // "#GameUI_Disconnect_BadSpectatorPassword"
        NETWORK_DISCONNECT_HLTVRESTRICTED                        = 35, // "#GameUI_Disconnect_HLTVRestricted"
        NETWORK_DISCONNECT_NOSPECTATORS                          = 36, // "#GameUI_Disconnect_NoSpectators"
        NETWORK_DISCONNECT_HLTVUNAVAILABLE                       = 37, // "#GameUI_Disconnect_HLTVUnavailable"
        NETWORK_DISCONNECT_HLTVSTOP                              = 38, // "#GameUI_Disconnect_HLTVStop"
        NETWORK_DISCONNECT_KICKED                                = 39, // "#GameUI_Disconnect_Kicked"
        NETWORK_DISCONNECT_BANADDED                              = 40, // "#GameUI_Disconnect_BanAdded"
        NETWORK_DISCONNECT_KICKBANADDED                          = 41, // "#GameUI_Disconnect_KickBanAdded"
        NETWORK_DISCONNECT_HLTVDIRECT                            = 42, // "#GameUI_Disconnect_HLTVDirect"
        NETWORK_DISCONNECT_PURESERVER_CLIENTEXTRA                = 43, // "#GameUI_Disconnect_PureServer_ClientExtra"
        NETWORK_DISCONNECT_PURESERVER_MISMATCH                   = 44, // "#GameUI_Disconnect_PureServer_Mismatch"
        NETWORK_DISCONNECT_USERCMD                               = 45, // "#GameUI_Disconnect_UserCmd"
        NETWORK_DISCONNECT_REJECTED_BY_GAME                      = 46, // "#GameUI_Disconnect_RejectedByGame"
        NETWORK_DISCONNECT_MESSAGE_PARSE_ERROR                   = 47, // "#GameUI_Disconnect_MessageParseError"
        NETWORK_DISCONNECT_INVALID_MESSAGE_ERROR                 = 48, // "#GameUI_Disconnect_InvalidMessageError"
        NETWORK_DISCONNECT_BAD_SERVER_PASSWORD                   = 49, // "#GameUI_Disconnect_BadServerPassword"
        NETWORK_DISCONNECT_DIRECT_CONNECT_RESERVATION            = 50,
        NETWORK_DISCONNECT_CONNECTION_FAILURE                    = 51, // "#GameUI_Disconnect_ConnectionFailure"
        NETWORK_DISCONNECT_NO_PEER_GROUP_HANDLERS                = 52, // "#GameUI_Disconnect_NoPeerGroupHandlers"
        NETWORK_DISCONNECT_RECONNECTION                          = 53,
        NETWORK_DISCONNECT_LOOPSHUTDOWN                          = 54,  // "#GameUI_Disconnect_LoopShutdown"
        NETWORK_DISCONNECT_LOOPDEACTIVATE                        = 55,  // "#GameUI_Disconnect_LoopDeactivate"
        NETWORK_DISCONNECT_HOST_ENDGAME                          = 56,  // "#GameUI_Disconnect_Host_EndGame"
        NETWORK_DISCONNECT_LOOP_LEVELLOAD_ACTIVATE               = 57,  // "#GameUI_Disconnect_LoopLevelLoadActivate"
        NETWORK_DISCONNECT_CREATE_SERVER_FAILED                  = 58,  // "#GameUI_Disconnect_CreateServerFailed"
        NETWORK_DISCONNECT_EXITING                               = 59,  // "#GameUI_Disconnect_ExitingEngine"
        NETWORK_DISCONNECT_REQUEST_HOSTSTATE_IDLE                = 60,  // "#GameUI_Disconnect_Request_HSIdle"
        NETWORK_DISCONNECT_REQUEST_HOSTSTATE_HLTVRELAY           = 61,  // "#GameUI_Disconnect_Request_HLTVRelay"
        NETWORK_DISCONNECT_CLIENT_CONSISTENCY_FAIL               = 62,  // "#GameUI_ClientConsistencyFail"
        NETWORK_DISCONNECT_CLIENT_UNABLE_TO_CRC_MAP              = 63,  // "#GameUI_ClientUnableToCRCMap"
        NETWORK_DISCONNECT_CLIENT_NO_MAP                         = 64,  // "#GameUI_ClientNoMap"
        NETWORK_DISCONNECT_CLIENT_DIFFERENT_MAP                  = 65,  // "#GameUI_ClientDifferentMap"
        NETWORK_DISCONNECT_SERVER_REQUIRES_STEAM                 = 66,  // "#GameUI_ServerRequireSteams"
        NETWORK_DISCONNECT_STEAM_DENY_MISC                       = 67,  // "#GameUI_Disconnect_SteamDeny_Misc"
        NETWORK_DISCONNECT_STEAM_DENY_BAD_ANTI_CHEAT             = 68,  // "#GameUI_Disconnect_SteamDeny_BadAntiCheat"
        NETWORK_DISCONNECT_SERVER_SHUTDOWN                       = 69,  // "#GameUI_Disconnect_ServerShutdown"
        NETWORK_DISCONNECT_REPLAY_INCOMPATIBLE                   = 71,  // "#GameUI_Disconnect_ReplayIncompatible"
        NETWORK_DISCONNECT_CONNECT_REQUEST_TIMEDOUT              = 72,  // "#GameUI_Disconnect_ConnectionTimedout"
        NETWORK_DISCONNECT_SERVER_INCOMPATIBLE                   = 73,  // "#GameUI_Disconnect_ServerIncompatible"
        NETWORK_DISCONNECT_LOCALPROBLEM_MANYRELAYS               = 74,  // "#GameUI_Disconnect_LocalProblem_ManyRelays"
        NETWORK_DISCONNECT_LOCALPROBLEM_HOSTEDSERVERPRIMARYRELAY = 75,  // "#GameUI_Disconnect_LocalProblem_HostedServerPrimaryRelay"
        NETWORK_DISCONNECT_LOCALPROBLEM_NETWORKCONFIG            = 76,  // "#GameUI_Disconnect_LocalProblem_NetworkConfig"
        NETWORK_DISCONNECT_LOCALPROBLEM_OTHER                    = 77,  // "#GameUI_Disconnect_LocalProblem_Other"
        NETWORK_DISCONNECT_REMOTE_TIMEOUT                        = 79,  // "#GameUI_Disconnect_RemoteProblem_Timeout"
        NETWORK_DISCONNECT_REMOTE_TIMEOUT_CONNECTING             = 80,  // "#GameUI_Disconnect_RemoteProblem_TimeoutConnecting"
        NETWORK_DISCONNECT_REMOTE_OTHER                          = 81,  // "#GameUI_Disconnect_RemoteProblem_Other"
        NETWORK_DISCONNECT_REMOTE_BADCRYPT                       = 82,  // "#GameUI_Disconnect_RemoteProblem_BadCrypt"
        NETWORK_DISCONNECT_REMOTE_CERTNOTTRUSTED                 = 83,  // "#GameUI_Disconnect_RemoteProblem_BadCert"
        NETWORK_DISCONNECT_UNUSUAL                               = 84,  // "#GameUI_Disconnect_Unusual"
        NETWORK_DISCONNECT_INTERNAL_ERROR                        = 85,  // "#GameUI_Disconnect_InternalError"
        NETWORK_DISCONNECT_REJECT_BADCHALLENGE                   = 128, // "#GameUI_ServerRejectBadChallenge"
        NETWORK_DISCONNECT_REJECT_NOLOBBY                        = 129, // "#GameUI_ServerNoLobby"
        NETWORK_DISCONNECT_REJECT_BACKGROUND_MAP                 = 130, // "#Valve_Reject_Background_Map"
        NETWORK_DISCONNECT_REJECT_SINGLE_PLAYER                  = 131, // "#Valve_Reject_Single_Player"
        NETWORK_DISCONNECT_REJECT_HIDDEN_GAME                    = 132, // "#Valve_Reject_Hidden_Game"
        NETWORK_DISCONNECT_REJECT_LANRESTRICT                    = 133, // "#GameUI_ServerRejectLANRestrict"
        NETWORK_DISCONNECT_REJECT_BADPASSWORD                    = 134, // "#GameUI_ServerRejectBadPassword"
        NETWORK_DISCONNECT_REJECT_SERVERFULL                     = 135, // "#GameUI_ServerRejectServerFull"
        NETWORK_DISCONNECT_REJECT_INVALIDRESERVATION             = 136, // "#GameUI_ServerRejectInvalidReservation"
        NETWORK_DISCONNECT_REJECT_FAILEDCHANNEL                  = 137, // "#GameUI_ServerRejectFailedChannel"
        NETWORK_DISCONNECT_REJECT_CONNECT_FROM_LOBBY             = 138, // "#Valve_Reject_Connect_From_Lobby"
        NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY             = 139, // "#Valve_Reject_Reserved_For_Lobby"
        NETWORK_DISCONNECT_REJECT_INVALIDKEYLENGTH               = 140, // "#GameUI_ServerReject_InvalidKeyLength"
        NETWORK_DISCONNECT_REJECT_OLDPROTOCOL                    = 141, // "#GameUI_ServerRejectOldProtocol"
        NETWORK_DISCONNECT_REJECT_NEWPROTOCOL                    = 142, // "#GameUI_ServerRejectNewProtocol"
        NETWORK_DISCONNECT_REJECT_INVALIDCONNECTION              = 143, // "#GameUI_ServerRejectInvalidConnection"
        NETWORK_DISCONNECT_REJECT_INVALIDCERTLEN                 = 144, // "#GameUI_ServerRejectInvalidCertLen"
        NETWORK_DISCONNECT_REJECT_INVALIDSTEAMCERTLEN            = 145, // "#GameUI_ServerRejectInvalidSteamCertLen"
        NETWORK_DISCONNECT_REJECT_STEAM                          = 146, // "#GameUI_ServerRejectSteam"
        NETWORK_DISCONNECT_REJECT_SERVERAUTHDISABLED             = 147, // "#GameUI_ServerAuthDisabled"
        NETWORK_DISCONNECT_REJECT_SERVERCDKEYAUTHINVALID         = 148, // "#GameUI_ServerCDKeyAuthInvalid"
        NETWORK_DISCONNECT_REJECT_BANNED                         = 149, // "#GameUI_ServerRejectBanned"
        NETWORK_DISCONNECT_KICKED_TEAMKILLING                    = 150, // "#Player_DisconnectReason_TeamKilling"
        NETWORK_DISCONNECT_KICKED_TK_START                       = 151, // "#Player_DisconnectReason_TK_Start"
        NETWORK_DISCONNECT_KICKED_UNTRUSTEDACCOUNT               = 152, // "#Player_DisconnectReason_UntrustedAccount"
        NETWORK_DISCONNECT_KICKED_CONVICTEDACCOUNT               = 153, // "#Player_DisconnectReason_ConvictedAccount"
        NETWORK_DISCONNECT_KICKED_COMPETITIVECOOLDOWN            = 154, // "#Player_DisconnectReason_CompetitiveCooldown"
        NETWORK_DISCONNECT_KICKED_TEAMHURTING                    = 155, // "#Player_DisconnectReason_TeamHurting"
        NETWORK_DISCONNECT_KICKED_HOSTAGEKILLING                 = 156, // "#Player_DisconnectReason_HostageKilling"
        NETWORK_DISCONNECT_KICKED_VOTEDOFF                       = 157, // "#Player_DisconnectReason_VotedOff"
        NETWORK_DISCONNECT_KICKED_IDLE                           = 158, // "#Player_DisconnectReason_Idle"
        NETWORK_DISCONNECT_KICKED_SUICIDE                        = 159, // "#Player_DisconnectReason_Suicide"
        NETWORK_DISCONNECT_KICKED_NOSTEAMLOGIN                   = 160, // "#Player_DisconnectReason_NoSteamLogin"
        NETWORK_DISCONNECT_KICKED_NOSTEAMTICKET                  = 161, // "#Player_DisconnectReason_NoSteamTicket"
    };

public:
    virtual void ClientConnect(PlayerSlot_t nSlot, const char* pszName, SteamId_t steamId, const char* pszNetworkId, const char* pszAddress, bool bFakePlayer) = 0; // 11
    virtual bool CheckConnect(PlayerSlot_t nSlot, const char* pszName, SteamId_t xuid, const char* pszNetworkId, bool unk1, void* pRejectReason)               = 0; // 12
    virtual void ClientPutInServer(PlayerSlot_t nSlot, const char* pszName, EClientType type, SteamId_t steamId)                                               = 0; // 13
    virtual void ClientActive(PlayerSlot_t nSlot, bool bLoadGame, const char* pszName, SteamId_t steamId)                                                      = 0; // 14
    virtual void ClientFullyConnect(PlayerSlot_t nSlot)                                                                                                        = 0; // 15
    virtual void ClientDisconnect(PlayerSlot_t nSlot, ENetworkDisconnectionReason reason, const char* pszName, SteamId_t steamId, const char* pszNetworkId)    = 0; // 16
    virtual void ClientCommand(PlayerSlot_t nSlot, const class CCommand& pCommand)                                                                             = 0; // 17
    virtual void ClientStringTableChanged(PlayerSlot_t slot, void* pData)                                                                                      = 0; // 18
    virtual void ClientSettingsChanged(PlayerSlot_t nSlot)                                                                                                     = 0; // 19

private:
    virtual void Unknown20() = 0;

public:
    virtual float ProcessUserCommands(PlayerSlot_t nSlot, void* buf, bool paused)    = 0; // 21
    virtual bool  IsPlayerControllerExistsBySlot(PlayerSlot_t nSlot)                 = 0; // 22
    virtual bool  IsPlayerAlive(PlayerSlot_t nSlot)                                  = 0; // 23
    virtual int   GetPlayerScore(PlayerSlot_t nSlot)                                 = 0; // 24
    virtual void  GetClientEyePosition(PlayerSlot_t nSlot, class Vector* pEyeOrigin) = 0; // 25
    virtual void  GetBugReportInfo(void* buf)                                        = 0; // 26
    virtual void  ClientVoice(PlayerSlot_t nSlot)                                    = 0; // 27

    /* Null Sub */
    virtual void NetworkIDValidated(const char* pszName, const char* pszNetworkId) = 0; // 28

    virtual void ClientCommandKeyValues(PlayerSlot_t nSlot, /* KeyValues*/ void* pKeyValues) = 0; // 29

public:
    enum offset_t
    {
        eClientConnect = 11, // pre connect
        eCheckConnect,
        eClientPutInServer,  // connected
        eClientActivate,     // after put in game ( 所有Client都触发 )
        eClientFullyConnect, // put in game ( Hltv / Replay 之类的不触发 )
        eClientDisconnect,
        eClientCommand,
        eClientSettingsChanged,
        eUnknown19,
        eProcessUserCommands,
        eIsPlayerControllerExistsBySlot,
        eIsPlayerAlive,
        eGetPlayerScore,
        eGetClientEyePosition,
        eGetBugReportInfo,
        eClientVoice,
        eNetworkIDValidated,
        eClientCommandKeyValues
    };
};

#endif