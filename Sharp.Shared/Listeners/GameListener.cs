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

using Sharp.Shared.Enums;

namespace Sharp.Shared.Listeners;

public interface IGameListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     Listener version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority (higher values run first)
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     Called during map startup after <see cref="IModSharp.GetIServer" /> and <see cref="IModSharp.GetGlobals" /> become available.
    /// </summary>
    void OnServerInit()
    {
    }

    /// <summary>
    ///     Called after <c>CNetworkGameServer::SpawnServer</c> completes. Safe to execute <c>.cfg</c> files from here.
    /// </summary>
    void OnServerSpawn()
    {
    }

    /// <summary>
    ///     Called after <c>CNetworkGameServer::ActiveServer</c> activates the server for the current map.
    /// </summary>
    void OnServerActivate()
    {
    }

    /// <summary>
    ///     Called before game resources are precached (<c>CGameRulesGameSystem::OnPrecacheResource</c> pre-hook).
    /// </summary>
    /// <remarks>
    ///     <see cref="IModSharp.PrecacheResource" /> is only valid while this callback is executing.
    /// </remarks>
    void OnResourcePrecache()
    {
    }

    /// <summary>
    ///     Called after <c>CGameRulesGameSystem::GameInit</c>. <see cref="IModSharp.GetGameRules" /> becomes available here.
    /// </summary>
    void OnGameInit()
    {
    }

    /// <summary>
    ///     Called after <c>CGameRulesGameSystem::GamePostInit</c>.
    /// </summary>
    void OnGamePostInit()
    {
    }

    /// <summary>
    ///     Called after <c>CGameRulesGameSystem::GameActivate</c>.
    /// </summary>
    void OnGameActivate()
    {
    }

    /// <summary>
    ///     Called after <c>CGameRulesGameSystem::GameDeactivate</c> when the map/game is deactivating.
    /// </summary>
    void OnGameDeactivate()
    {
    }

    /// <summary>
    ///     Called after <c>CGameRulesGameSystem::GamePreShutdown</c>.
    /// </summary>
    /// <remarks>
    ///     Map-scoped timers (<see cref="GameTimerFlags.StopOnMapEnd" />) are cleared after all listeners finish this callback.
    /// </remarks>
    void OnGamePreShutdown()
    {
    }

    /// <summary>
    ///     Called after <c>CGameRulesGameSystem::GameShutdown</c>.
    /// </summary>
    /// <remarks>
    ///     <see cref="IModSharp.GetGameRules" /> is already disposed and not usable here.
    ///     <see cref="IModSharp.GetIServer" /> and <see cref="IModSharp.GetGlobals" /> are disposed immediately after all listeners return.
    /// </remarks>
    void OnGameShutdown()
    {
    }

    /// <summary>
    ///     Called before a round restarts (<c>CCSGameRules::RestartRound</c> pre-hook).
    /// </summary>
    /// <remarks>
    ///     Round-scoped timers (<see cref="GameTimerFlags.StopOnRoundEnd" />) are stopped before this callback runs.
    /// </remarks>
    void OnRoundRestart()
    {
    }

    /// <summary>
    ///     Called after a round has restarted (<c>CCSGameRules::RestartRound</c> post-hook).
    /// </summary>
    void OnRoundRestarted()
    {
    }

    /// <summary>
    ///     Called before a server console <c>say</c> message (<c>HostSay</c> with null controller) or script chat broadcast
    ///     (<c>ScriptPrintMessageChatAll</c>) is sent to players.
    /// </summary>
    /// <param name="message">The non-empty message text being broadcast.</param>
    /// <returns>
    ///     Only <see cref="ECommandAction.Stopped" /> blocks the message; <see cref="ECommandAction.Handled" /> lets it through like <see cref="ECommandAction.Skipped" />.
    ///     All installed listeners run without short-circuiting and the highest <see cref="ECommandAction" /> value is used.
    /// </returns>
    ECommandAction ConsoleSay(string message)
        => ECommandAction.Skipped;
}

//    Server lifecycle order ⬇
//
//    OnServerInit          -> safe to get sv/globals
//    OnGameInit            -> safe to get GameRules
//    OnGamePostInit
//    OnResourcePrecache    -> safe to call PrecacheResource
//    OnServerSpawn         -> safe to execute .cfg
//    OnGameActivate
//    OnServerActivate
//    ...
//    OnGameDeactivate
//    OnGamePreShutdown
//    OnGameShutdown        -> GameRules is already disposed; sv/globals disposed after callback
