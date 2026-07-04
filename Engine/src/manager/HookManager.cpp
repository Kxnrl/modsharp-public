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

#include "manager/HookManager.h"

#include <array>
#include <safetyhook.hpp>

#include <unordered_map>
#include <vector>

class CHookManager : public HookManager
{
    std::vector<safetyhook::InlineHook*> m_InlineHooks;
    std::vector<safetyhook::MidHook*>    m_MidHooks;

public:
    void Shutdown() const
    {
        for (auto hook : m_InlineHooks)
        {
            hook->reset();
        }
        for (auto hook : m_MidHooks)
        {
            hook->reset();
        }
    }

    void Register(safetyhook::InlineHook* hook) override
    {
        m_InlineHooks.emplace_back(hook);
    }

    void Register(safetyhook::MidHook* hook) override
    {
        m_MidHooks.emplace_back(hook);
    }

    void Hook_ServerInit(HookType_t hook, HookCallback_t callback) override
    {
        m_ServerInit[hook].push_back(callback);
    }
    void Hook_ServerSpawn(HookType_t hook, HookCallback_t callback) override
    {
        m_ServerSpawn[hook].push_back(callback);
    }
    void Hook_ServerActivate(HookType_t hook, HookCallback_t callback) override
    {
        m_ServerActivate[hook].push_back(callback);
    }

    void Hook_GameFrame(HookType_t hook, GameFrameCallback_t callback) override
    {
        m_GameFrame[hook].push_back(callback);
    }

    void Hook_GameInit(HookType_t hook, HookCallback_t callback) override
    {
        m_GameInit[hook].push_back(callback);
    }
    void Hook_GameShutdown(HookType_t hook, HookCallback_t callback) override
    {
        m_GameShutdown[hook].push_back(callback);
    }
    void Hook_GameActivate(HookType_t hook, HookCallback_t callback) override
    {
        m_GameActivate[hook].push_back(callback);
    }
    void Hook_GameDeactivate(HookType_t hook, HookCallback_t callback) override
    {
        m_GameDeactivate[hook].push_back(callback);
    }

    void Hook_RoundRestart(HookType_t hook, HookCallback_t callback) override
    {
        m_RoundStart[hook].push_back(callback);
    }

    void Hook_ClientConnect(HookType_t hook, ClientConnectCallback_t callback) override
    {
        m_ClientConnect[hook].push_back(callback);
    }
    void Hook_ClientPutInServer(HookType_t hook, ClientPutInServerCallback_t callback) override
    {
        m_ClientPutInServer[hook].push_back(callback);
    }
    void Hook_ClientActivate(HookType_t hook, ClientActivateCallback_t callback) override
    {
        m_ClientActivate[hook].push_back(callback);
    }
    void Hook_ClientFullyConnect(HookType_t hook, ClientFullyConnectCallback_t callback) override
    {
        m_ClientFullyConnect[hook].push_back(callback);
    }
    void Hook_ClientDisconnect(HookType_t hook, ClientDisconnectCallback_t callback) override
    {
        m_ClientDisconnect[hook].push_back(callback);
    }
    void Hook_PlayerSpawned(HookType_t hook, PlayerSpawnedCallback_t callback) override
    {
        m_PlayerSpawned[hook].push_back(callback);
    }
    void Hook_PlayerKilled(HookType_t hook, PlayerKilledCallback_t callback) override
    {
        m_PlayerKilled[hook].push_back(callback);
    }

    void Call_ServerInit(HookType_t hook) override
    {
        for (const auto& callback : m_ServerInit[hook])
            callback();
    }
    void Call_ServerSpawn(HookType_t hook) override
    {
        for (const auto& callback : m_ServerSpawn[hook])
            callback();
    }
    void Call_ServerActivate(HookType_t hook) override
    {
        for (const auto& callback : m_ServerActivate[hook])
            callback();
    }

    void Call_GameFrame(HookType_t hook, bool simulating, bool firstTick, bool lastTick) override
    {
        for (const auto& callback : m_GameFrame[hook])
            callback(simulating, firstTick, lastTick);
    }

    void Call_GameInit(HookType_t hook) override
    {
        for (const auto& callback : m_GameInit[hook])
            callback();
    }
    void Call_GameShutdown(HookType_t hook) override
    {
        for (const auto& callback : m_GameShutdown[hook])
            callback();
    }
    void Call_GameActivate(HookType_t hook) override
    {
        for (const auto& callback : m_GameActivate[hook])
            callback();
    }
    void Call_GameDeactivate(HookType_t hook) override
    {
        for (const auto& callback : m_GameDeactivate[hook])
            callback();
    }

    void Call_RoundRestart(HookType_t hook) override
    {
        for (const auto& callback : m_RoundStart[hook])
            callback();
    }

    void Call_ClientConnect(HookType_t hook, PlayerSlot_t slot, const char* name, SteamId_t steamId, bool isFakeClient) override
    {
        for (const auto& callback : m_ClientConnect[hook])
            callback(slot, name, steamId, isFakeClient);
    }
    void Call_ClientPutInServer(HookType_t hook, PlayerSlot_t slot, const char* name, SteamId_t steamId) override
    {
        for (const auto& callback : m_ClientPutInServer[hook])
            callback(slot, name, steamId);
    }
    void Call_ClientActivate(HookType_t hook, PlayerSlot_t slot, const char* name, SteamId_t steamId) override
    {
        for (const auto& callback : m_ClientActivate[hook])
            callback(slot, name, steamId);
    }
    void Call_ClientFullyConnect(HookType_t hook, PlayerSlot_t slot) override
    {
        for (const auto& callback : m_ClientFullyConnect[hook])
            callback(slot);
    }
    void Call_ClientDisconnect(HookType_t hook, PlayerSlot_t slot, int32_t reason, const char* name, SteamId_t steamId) override
    {
        for (const auto& callback : m_ClientDisconnect[hook])
            callback(slot, reason, name, steamId);
    }
    void Call_PlayerSpawned(HookType_t hook, CCSPlayerPawn* player, CServerSideClient* client) override
    {
        for (const auto& callback : m_PlayerSpawned[hook])
            callback(player, client);
    }
    void Call_PlayerKilled(HookType_t hook, CCSPlayerPawn* player, CServerSideClient* client) override
    {
        for (const auto& callback : m_PlayerKilled[hook])
            callback(player, client);
    }

private:
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_ServerInit;
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_ServerSpawn;
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_ServerActivate;
    std::array<std::vector<GameFrameCallback_t>, HookType_Max>          m_GameFrame;
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_GameInit;
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_GameShutdown;
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_GameActivate;
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_GameDeactivate;
    std::array<std::vector<HookCallback_t>, HookType_Max>               m_RoundStart;
    std::array<std::vector<ClientConnectCallback_t>, HookType_Max>      m_ClientConnect;
    std::array<std::vector<ClientPutInServerCallback_t>, HookType_Max>  m_ClientPutInServer;
    std::array<std::vector<ClientActivateCallback_t>, HookType_Max>     m_ClientActivate;
    std::array<std::vector<ClientFullyConnectCallback_t>, HookType_Max> m_ClientFullyConnect;
    std::array<std::vector<ClientDisconnectCallback_t>, HookType_Max>   m_ClientDisconnect;
    std::array<std::vector<PlayerSpawnedCallback_t>, HookType_Max>      m_PlayerSpawned;
    std::array<std::vector<PlayerKilledCallback_t>, HookType_Max>       m_PlayerKilled;
};

static CHookManager manager;
HookManager*        g_pHookManager = &manager;

void InitHookManager()
{
    HookManager::Install();
}

void HookManager::Install()
{
    extern void InstallEngineHooks();
    extern void InstallServerHooks();
    extern void InstallClientHooks();
    extern void InstallEventHooks();
    extern void InstallSoundHooks();
    extern void InstallEntityHooks();
    extern void InstallGameRulesHooks();
    extern void InstallPlayerHooks();
    extern void InstallNetworkHooks();
    extern void InstallGiveNamedItemHooks();
    extern void InstallHandleCommandJoinTeamHooks();
    extern void InstallMovementHook();
    extern void InstallTransmitHook();
    extern void InstallDamageManagerHooks();
    extern void InstallValveConsoleLog();
    extern void InstallDualMountAddonHooks();

    InstallValveConsoleLog();
    InstallEngineHooks();
    InstallServerHooks();
    InstallClientHooks();
    InstallEventHooks();
    InstallEntityHooks();
    InstallGameRulesHooks();
    InstallPlayerHooks();
    InstallNetworkHooks();
    InstallGiveNamedItemHooks();
    InstallHandleCommandJoinTeamHooks();
    InstallMovementHook();
    InstallTransmitHook();
    InstallDamageManagerHooks();
    InstallDualMountAddonHooks();
    InstallSoundHooks();
}

void HookManager::Uninstall()
{
    manager.Shutdown();
}
