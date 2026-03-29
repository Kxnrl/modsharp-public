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

#ifndef MS_HOOK_MANAGER_H
#define MS_HOOK_MANAGER_H

#include "definitions.h"

#include <cstdint>

namespace safetyhook
{
class MidHook;
class InlineHook;
} // namespace safetyhook

enum HookType_t
{
    HookType_Pre,
    HookType_Post,
    HookType_Max,
};

class CServerSideClient;
class CCSPlayerPawn;

using HookCallback_t               = void (*)();
using GameFrameCallback_t          = void (*)(bool, bool, bool);
using ClientConnectCallback_t      = void (*)(PlayerSlot_t, const char*, SteamId_t, bool);
using ClientActivateCallback_t     = void (*)(PlayerSlot_t, const char*, SteamId_t);
using ClientPutInServerCallback_t  = void (*)(PlayerSlot_t, const char*, SteamId_t);
using ClientFullyConnectCallback_t = void (*)(PlayerSlot_t);
using ClientDisconnectCallback_t   = void (*)(PlayerSlot_t, int32_t, const char*, SteamId_t);
using PlayerSpawnedCallback_t      = void (*)(CCSPlayerPawn*, CServerSideClient*);
using PlayerKilledCallback_t       = PlayerSpawnedCallback_t;

class HookManager
{
public:
    virtual ~HookManager() = default;
    static void Install();
    static void Uninstall();

    virtual void Register(safetyhook::InlineHook* hook) = 0;
    virtual void Register(safetyhook::MidHook* hook)    = 0;

    virtual void Hook_ServerInit(HookType_t hook, HookCallback_t callback)     = 0;
    virtual void Hook_ServerSpawn(HookType_t hook, HookCallback_t callback)    = 0;
    virtual void Hook_ServerActivate(HookType_t hook, HookCallback_t callback) = 0;

    virtual void Hook_GameFrame(HookType_t hook, GameFrameCallback_t) = 0;

    virtual void Hook_GameInit(HookType_t hook, HookCallback_t callback)       = 0;
    virtual void Hook_GameShutdown(HookType_t hook, HookCallback_t callback)   = 0;
    virtual void Hook_GameActivate(HookType_t hook, HookCallback_t callback)   = 0;
    virtual void Hook_GameDeactivate(HookType_t hook, HookCallback_t callback) = 0;

    virtual void Hook_RoundRestart(HookType_t hook, HookCallback_t callback) = 0;

    virtual void Hook_ClientConnect(HookType_t hook, ClientConnectCallback_t callback)           = 0;
    virtual void Hook_ClientPutInServer(HookType_t hook, ClientPutInServerCallback_t callback)   = 0;
    virtual void Hook_ClientActivate(HookType_t hook, ClientActivateCallback_t callback)         = 0;
    virtual void Hook_ClientFullyConnect(HookType_t hook, ClientFullyConnectCallback_t callback) = 0;
    virtual void Hook_ClientDisconnect(HookType_t hook, ClientDisconnectCallback_t callback)     = 0;

    virtual void Hook_PlayerSpawned(HookType_t hook, PlayerSpawnedCallback_t callback) = 0;
    virtual void Hook_PlayerKilled(HookType_t hook, PlayerKilledCallback_t callback)   = 0;

    virtual void Call_ServerInit(HookType_t hook)     = 0;
    virtual void Call_ServerSpawn(HookType_t hook)    = 0;
    virtual void Call_ServerActivate(HookType_t hook) = 0;

    virtual void Call_GameFrame(HookType_t hook, bool simulating, bool firstTick, bool lastTick) = 0;

    virtual void Call_GameInit(HookType_t hook)       = 0;
    virtual void Call_GameShutdown(HookType_t hook)   = 0;
    virtual void Call_GameActivate(HookType_t hook)   = 0;
    virtual void Call_GameDeactivate(HookType_t hook) = 0;

    virtual void Call_RoundRestart(HookType_t hook) = 0;

    virtual void Call_ClientConnect(HookType_t hook, PlayerSlot_t slot, const char* name, SteamId_t steamId, bool isFakeClient) = 0;
    virtual void Call_ClientPutInServer(HookType_t hook, PlayerSlot_t slot, const char* name, SteamId_t steamId)                = 0;
    virtual void Call_ClientActivate(HookType_t hook, PlayerSlot_t slot, const char* name, SteamId_t steamId)                   = 0;
    virtual void Call_ClientFullyConnect(HookType_t hook, PlayerSlot_t slot)                                                    = 0;
    virtual void Call_ClientDisconnect(HookType_t hook, PlayerSlot_t slot, int32_t reason, const char* name, SteamId_t steamId) = 0;

    virtual void Call_PlayerSpawned(HookType_t hook, CCSPlayerPawn* player, CServerSideClient* client) = 0;
    virtual void Call_PlayerKilled(HookType_t hook, CCSPlayerPawn* player, CServerSideClient* client)  = 0;
};

//    Server ⬇
//
//    CNetworkServerService::StartupServer
//    CNetworkGameServer::SpawnServer_Unknown   -> safe to get sv/globals
//    GameSystem::GameInit                      -> safe to get GameRules
//    GameSystem::GamePostInit
//    GameSystem::PrecacheResource
//    CNetworkGameServer::SpawnServer           -> safe to execute .cfg
//    GameSystem::GameActivate
//    CNetworkGameServer::ActivateServer
//    ...
//    GameSystem::GameDeactivate
//    GameSystem::GamePreShutdown
//    GameSystem::GameShutdown

#endif