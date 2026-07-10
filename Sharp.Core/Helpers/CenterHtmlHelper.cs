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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.Objects;
using Sharp.Shared.Objects;

namespace Sharp.Core.Helpers;

internal static class CenterHtmlHelper
{
    private static GameEvent? _showSurvivalRespawnStatusEvent;
    private static ConVar?    _gameType;
    private static ConVar?    _gameMode;

    private static int? _isWarmupOffset;
    private static int? _restartRoundTimeOffset;

    private static int _gameTypeVal;
    private static int _gameModeVal;

    private static int _lastGameRestartTick = -1;

    public static void InitDelegates()
    {
        Bridges.Forwards.Game.OnServerActivate += OnServerActivate;
    }

    private static void OnServerActivate()
    {
        InitializeResources();

        _gameTypeVal = _gameType?.GetInt32() ?? 0;
        _gameModeVal = _gameMode?.GetInt32() ?? 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrintCenterHtmlToAll(string message, int duration = 1)
    {
        PrepareEvent(message, duration);
        _showSurvivalRespawnStatusEvent!.FireToClients();
        FixHtmlFlashing();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrintCenterHtmlToClient(IGameClient client, string message, int duration = 1)
    {
        if (client.IsFakeClient || !client.IsConnected)
        {
            return;
        }

        PrepareEvent(message, duration);

        _showSurvivalRespawnStatusEvent!.FireToClient(client);
        FixHtmlFlashing();
    }

    public static void PrintCenterHtmlToClients(IEnumerable<IGameClient> clients, string message, int duration = 1)
    {
        if (clients is IGameClient[] array)
        {
            ProcessClientSpan(array, message, duration);

            return;
        }

        if (clients is List<IGameClient> list)
        {
            ProcessClientSpan(CollectionsMarshal.AsSpan(list), message, duration);

            return;
        }

        var hasValidClient = false;

        foreach (var client in clients)
        {
            if (client.IsFakeClient || !client.IsConnected)
            {
                continue;
            }

            if (!hasValidClient)
            {
                PrepareEvent(message, duration);
                hasValidClient = true;
            }

            _showSurvivalRespawnStatusEvent!.FireToClient(client);
        }

        if (hasValidClient)
        {
            FixHtmlFlashing();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrepareEvent(string message, int duration)
    {
        if (_showSurvivalRespawnStatusEvent is null || _gameType is null || _gameMode is null)
        {
            InitializeResources();
        }

        _showSurvivalRespawnStatusEvent!.SetString("loc_token", message);
        _showSurvivalRespawnStatusEvent.SetInt("duration", duration);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessClientSpan(ReadOnlySpan<IGameClient> clients, string message, int duration)
    {
        var hasValidClient = false;

        foreach (var client in clients)
        {
            if (client.IsFakeClient || !client.IsConnected)
            {
                continue;
            }

            if (!hasValidClient)
            {
                PrepareEvent(message, duration);
                hasValidClient = true;
            }

            _showSurvivalRespawnStatusEvent!.FireToClient(client);
        }

        if (hasValidClient)
        {
            FixHtmlFlashing();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitializeResources()
    {
        if (_showSurvivalRespawnStatusEvent is null)
        {
            _showSurvivalRespawnStatusEvent = GameEvent.CreateEditable(Event.CreateEvent("show_survival_respawn_status", true))
                                              ?? throw new Exception("Failed to create event show_survival_respawn_status");

            _showSurvivalRespawnStatusEvent.SetInt("userid", -1);
        }

        _gameType ??= ConVar.Create(Cvar.FindConVar("game_type", true))
                      ?? throw new Exception("Failed to find cvar \"game_type\"");

        _gameMode ??= ConVar.Create(Cvar.FindConVar("game_mode", true))
                      ?? throw new Exception("Failed to find cvar \"game_mode\"");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void FixHtmlFlashing()
    {
        // wouldnt work for deathmatch
        if (_gameTypeVal == 1 && _gameModeVal == 2)
        {
            return;
        }

        var globalPtr = Bridges.Natives.Core.GetGlobals();

        if (globalPtr == nint.Zero)
        {
            return;
        }

        var tick = *(int*) (globalPtr + GlobalVars.TickCountOffset);

        // avoid redundant reads + SetNetVarBool within the same server tick
        if (tick == _lastGameRestartTick)
        {
            return;
        }

        _lastGameRestartTick = tick;

        var gamerules = Bridges.Natives.Core.GetGameRules();

        if (gamerules == nint.Zero)
        {
            return;
        }

        _isWarmupOffset         ??= SchemaSystem.GetNetVarOffset("CCSGameRules", "m_bWarmupPeriod");
        _restartRoundTimeOffset ??= SchemaSystem.GetNetVarOffset("CCSGameRules", "m_flRestartRoundTime");

        // stop if during warmup
        if (*(bool*) (gamerules + _isWarmupOffset.Value))
        {
            return;
        }

        var time             = *(float*) (globalPtr + GlobalVars.CurTimeOffset);
        var restartRoundTime = *(float*) (gamerules + _restartRoundTimeOffset.Value);

        SchemaSystem.SetNetVarBool(gamerules,
                                   "CCSGameRules",
                                   "m_bGameRestart",
                                   restartRoundTime < time);
    }
}
