/* 
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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

#ifndef MS_FORWARDS_H
#define MS_FORWARDS_H

#include "bridge/adapter.h"
#include "definitions.h"

#include <cstddef>

class CCSPlayerPawn;
class CServerSideClient;
class CBaseEntity;
class IGameEvent;
class CConVarBaseData;
class CBasePlayerPawn;
class CCSPlayerController;
class CPlayer_MovementServices;
class CCSPlayer_MovementServices;
class CCSPlayer_WeaponServices;
class CBaseWeapon;
class CTakeDamageInfo;
class CMoveData;
class Vector;
template <typename T>
class NativeFixedSpan;
class IGameRules;
class Variant_t;
class INetChannel;
struct FireBulletParams;
struct CTakeDamageResult;
struct TeamRewardInfo;

namespace google::protobuf
{
class Message;
}

#define FORWARD_ARG(...) , __VA_ARGS__
#define DECLARE_FORWARD(type, name, ret, ...) inline ForwardItem<ret __VA_ARGS__>* name = bridge::CreateForward<ret __VA_ARGS__>(#type "." #name)

namespace forwards
{
// Core
DECLARE_FORWARD(Core, OnModSharpModulesCommand, void, FORWARD_ARG(CServerSideClient*));

// Game
DECLARE_FORWARD(Game, OnStartupServerPre, void);
DECLARE_FORWARD(Game, OnGameFramePre, void, FORWARD_ARG(bool, bool, bool));
DECLARE_FORWARD(Game, OnGameFramePost, void, FORWARD_ARG(bool, bool, bool));
DECLARE_FORWARD(Game, OnServerInit, void);
DECLARE_FORWARD(Game, OnServerSpawn, void);
DECLARE_FORWARD(Game, OnServerActivate, void);
DECLARE_FORWARD(Game, OnGameInit, void);
DECLARE_FORWARD(Game, OnGameShutdown, void);
DECLARE_FORWARD(Game, OnGamePostInit, void);
DECLARE_FORWARD(Game, OnGamePreShutdown, void);
DECLARE_FORWARD(Game, OnGameActivate, void);
DECLARE_FORWARD(Game, OnGameDeactivate, void);
DECLARE_FORWARD(Game, OnResourcePrecache, void, FORWARD_ARG(void*));
DECLARE_FORWARD(Game, OnFrameUpdatePreEntityThink, void);
DECLARE_FORWARD(Game, OnFrameUpdatePostEntityThink, void);
DECLARE_FORWARD(Game, OnServerGamePostSimulate, void);
DECLARE_FORWARD(Game, OnPrintStatus, EHookAction, FORWARD_ARG(CServerSideClient*));
DECLARE_FORWARD(Game, OnRoundRestart, void);
DECLARE_FORWARD(Game, OnRoundRestarted, void);
DECLARE_FORWARD(Game, OnConsoleSay, ECommandAction, FORWARD_ARG(const char*));
DECLARE_FORWARD(Game, OnEmitSound, EHookAction, FORWARD_ARG(EntityIndex_t, char*, float*, uint64_t*, bool*));
DECLARE_FORWARD(Game, OnEmitSoundPost, void, FORWARD_ARG(EntityIndex_t, const char*, float, uint64_t, bool, EHookAction, int64_t));
DECLARE_FORWARD(Game, OnEmitMusicPost, void, FORWARD_ARG(EntityIndex_t, const char*, float, uint64_t, int64_t));
DECLARE_FORWARD(Game, OnSoundEvent, EHookAction, FORWARD_ARG(EntityIndex_t, const char*, float, uint64_t*, bool*));
DECLARE_FORWARD(Game, OnSoundEventPost, void, FORWARD_ARG(EntityIndex_t, const char*, float, uint64_t, bool, EHookAction, int64_t));
DECLARE_FORWARD(Game, OnMapVoteCreated, void, FORWARD_ARG(IGameRules*));
DECLARE_FORWARD(Game, OnRuntimeTraceFilterCallback, bool, FORWARD_ARG(CBaseEntity*));
DECLARE_FORWARD(Game, OnTerminateRoundPre, EHookAction, FORWARD_ARG(IGameRules*, float*, uint32_t*, NativeFixedSpan<TeamRewardInfo>*));
DECLARE_FORWARD(Game, OnTerminateRoundPost, void, FORWARD_ARG(IGameRules*, float, uint32_t, NativeFixedSpan<TeamRewardInfo>*, EHookAction));

// Network
DECLARE_FORWARD(Network, OnTextMsg, EHookAction, FORWARD_ARG(uint32_t, const char*, NetworkReceiver_t, bool*, NetworkReceiver_t*));
DECLARE_FORWARD(Network, OnPostEventAbstract, EHookAction, FORWARD_ARG(uint16_t, const void*, NetworkReceiver_t, bool*, NetworkReceiver_t*));

// Client
DECLARE_FORWARD(Client, OnConnectClient, EHookAction, FORWARD_ARG(SteamId_t, const char*, void*));
DECLARE_FORWARD(Client, OnClientConnect, EHookAction, FORWARD_ARG(SteamId_t, const char*, char*, int, bool*));
DECLARE_FORWARD(Client, OnClientConnected, void, FORWARD_ARG(CServerSideClient*));
DECLARE_FORWARD(Client, OnClientActive, void, FORWARD_ARG(CServerSideClient*));
DECLARE_FORWARD(Client, OnClientDisconnecting, void, FORWARD_ARG(CServerSideClient*, int32_t));
DECLARE_FORWARD(Client, OnClientDisconnected, void, FORWARD_ARG(CServerSideClient*, int32_t));
DECLARE_FORWARD(Client, OnClientSettingChanged, void, FORWARD_ARG(CServerSideClient*));
DECLARE_FORWARD(Client, OnClientStringCommand, ECommandAction, FORWARD_ARG(CServerSideClient*, const char*, bool, const char*));
DECLARE_FORWARD(Client, OnClientSayCommand, ECommandAction, FORWARD_ARG(CServerSideClient*, bool, const char*, const char*));
DECLARE_FORWARD(Client, OnClientAllowHear, EHookAction, FORWARD_ARG(CServerSideClient*, CServerSideClient*, bool*));
DECLARE_FORWARD(Client, OnClientConsoleCommand, ECommandAction, FORWARD_ARG(CServerSideClient*, const char*, const char*));
DECLARE_FORWARD(Client, OnClientSpeakPre, EHookAction, FORWARD_ARG(CServerSideClient*, uint64_t, uint32_t, const char*, size_t));
DECLARE_FORWARD(Client, OnClientSpeakPost, void, FORWARD_ARG(CServerSideClient*, uint64_t, uint32_t, const char*, size_t, EHookAction));
DECLARE_FORWARD(Client, OnClientQueryConVar, void, FORWARD_ARG(CServerSideClient*, int32_t, int32_t, const char*, const char*));

// Player
DECLARE_FORWARD(Player, OnPlayerSpawnPre, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*));
DECLARE_FORWARD(Player, OnPlayerSpawnPost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*));
DECLARE_FORWARD(Player, OnPlayerPreThink, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*));
DECLARE_FORWARD(Player, OnPlayerPostThink, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*));
DECLARE_FORWARD(Player, OnPlayerKilledPre, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CTakeDamageResult*));
DECLARE_FORWARD(Player, OnPlayerKilledPost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CTakeDamageResult*));
DECLARE_FORWARD(Player, OnPlayerRunCommandPre, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CBasePlayerPawn*, CPlayer_MovementServices*, void* /*CUserCmd*/));
DECLARE_FORWARD(Player, OnPlayerRunCommandPost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CBasePlayerPawn*, CPlayer_MovementServices*, void* /*CUserCmd*/, EHookAction));
DECLARE_FORWARD(Player, OnPlayerProcessMovePre, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CCSPlayer_MovementServices*, CMoveData*));
DECLARE_FORWARD(Player, OnPlayerProcessMovePost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CCSPlayer_MovementServices*, CMoveData*));
DECLARE_FORWARD(Player, OnPlayerWalkMove, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CCSPlayer_MovementServices*, CMoveData*, int*));
DECLARE_FORWARD(Player, OnPlayerSwitchWeapon, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CCSPlayer_WeaponServices*, CBaseWeapon*));
DECLARE_FORWARD(Player, OnPlayerEquipWeapon, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CCSPlayer_WeaponServices*, CBaseWeapon*, int64_t));
DECLARE_FORWARD(Player, OnPlayerDropWeapon, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CCSPlayer_WeaponServices*, CBaseWeapon*));
DECLARE_FORWARD(Player, OnPlayerWeaponCanUse, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CBaseWeapon*, bool*));
DECLARE_FORWARD(Player, OnPlayerWeaponCanSwitch, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CBaseWeapon*, bool*));
DECLARE_FORWARD(Player, OnPlayerWeaponCanEquip, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, CBaseWeapon*, bool*));
DECLARE_FORWARD(Player, OnPlayerGetMaxSpeed, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, float, float*));

// Entity
DECLARE_FORWARD(Entity, OnEntityCreated, void, FORWARD_ARG(CBaseEntity*));
DECLARE_FORWARD(Entity, OnEntitySpawned, void, FORWARD_ARG(CBaseEntity*));
DECLARE_FORWARD(Entity, OnEntityDeleted, void, FORWARD_ARG(CBaseEntity*));
DECLARE_FORWARD(Entity, OnEntityFollowed, void, FORWARD_ARG(CBaseEntity*, CBaseEntity*));
DECLARE_FORWARD(Entity, OnEntityFireOutput, EHookAction, FORWARD_ARG(CBaseEntity*, const char*, CBaseEntity*, float));
DECLARE_FORWARD(Entity, OnEntityAcceptInput, EHookAction, FORWARD_ARG(CBaseEntity*, const char*, Variant_t*, CBaseEntity*, CBaseEntity*));

// Event
DECLARE_FORWARD(Event, HookFireEvent, bool, FORWARD_ARG(IGameEvent*, bool*));
DECLARE_FORWARD(Event, FireGameEvent, void, FORWARD_ARG(IGameEvent*));

// ConVar
DECLARE_FORWARD(Cvar, OnConVarChanged, void, FORWARD_ARG(CConVarBaseData*));
DECLARE_FORWARD(Cvar, OnConCommandTrigger, EHookAction, FORWARD_ARG(int64_t, CServerSideClient*, const char*));

// Steam
DECLARE_FORWARD(Steam, OnGroupStatusResult, void, FORWARD_ARG(SteamId_t, uint64_t, bool, bool));
DECLARE_FORWARD(Steam, OnSteamServersConnected, void);
DECLARE_FORWARD(Steam, OnSteamServersDisconnected, void, FORWARD_ARG(int32_t));
DECLARE_FORWARD(Steam, OnSteamServersConnectFailure, void, FORWARD_ARG(int32_t, bool));
DECLARE_FORWARD(Steam, OnDownloadItemResult, void, FORWARD_ARG(uint64_t, int));
DECLARE_FORWARD(Steam, OnItemInstalled, void, FORWARD_ARG(uint64_t));

////////////////////////////////////////////////////////////////////////////////////////////////////////

// GiveNamedItem
DECLARE_FORWARD(Extern.GiveNamedItem, OnGiveNamedItemPre, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, char*, bool*));
DECLARE_FORWARD(Extern.GiveNamedItem, OnGiveNamedItemPost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, const char*, bool, EHookAction, CBaseEntity*));
DECLARE_FORWARD(Extern.GiveNamedItem, OnGiveGloveItemPost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*));
DECLARE_FORWARD(Extern.GiveNamedItem, OnCanAcquirePre, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, uint16_t, int, int*));

// HandleCommandJoinTeam
DECLARE_FORWARD(Extern.HandleCommandJoinTeam, OnHandleCommandJoinTeamPre, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, int32_t*, bool*, int32_t*, bool*));
DECLARE_FORWARD(Extern.HandleCommandJoinTeam, OnHandleCommandJoinTeamPost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, int32_t, bool, int32_t, EHookAction, bool));

// PointServerCommand
DECLARE_FORWARD(Extern.PointServerCommand, OnPointServerCommand, EHookAction, FORWARD_ARG(CBaseEntity*, const char*));

// DamageManager
DECLARE_FORWARD(Extern.DamageProcessor, OnPlayerDispatchTraceAttackPre, EHookAction, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, const CTakeDamageInfo*, const CTakeDamageResult*));
DECLARE_FORWARD(Extern.DamageProcessor, OnEntityDispatchTraceAttackPre, EHookAction, FORWARD_ARG(CBaseEntity*, const CTakeDamageInfo*, const CTakeDamageResult*));
DECLARE_FORWARD(Extern.DamageProcessor, OnPlayerDispatchTraceAttackPost, void, FORWARD_ARG(CServerSideClient*, CCSPlayerController*, CCSPlayerPawn*, const CTakeDamageInfo*, const CTakeDamageResult*, EHookAction));
DECLARE_FORWARD(Extern.DamageProcessor, OnEntityDispatchTraceAttackPost, void, FORWARD_ARG(CBaseEntity*, const CTakeDamageInfo*, const CTakeDamageResult*, EHookAction));
} // namespace forwards

#endif