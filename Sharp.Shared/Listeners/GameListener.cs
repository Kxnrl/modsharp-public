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

using Sharp.Shared.Enums;

namespace Sharp.Shared.Listeners;

public interface IGameListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     Listenver version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority
    /// </summary>
    int ListenerPriority { get; }

    void OnServerInit()
    {
    }

    void OnServerSpawn()
    {
    }

    void OnServerActivate()
    {
    }

    void OnResourcePrecache()
    {
    }

    void OnGameInit()
    {
    }

    void OnGamePostInit()
    {
    }

    void OnGameActivate()
    {
    }

    void OnGameDeactivate()
    {
    }

    void OnGamePreShutdown()
    {
    }

    /// <summary>
    ///     <see cref="IModSharp.GetGameRules"/> 在此处不可用!
    /// </summary>
    void OnGameShutdown()
    {
    }

    void OnRoundRestart()
    {
    }

    void OnRoundRestarted()
    {
    }

    ECommandAction ConsoleSay(string message)
        => ECommandAction.Skipped;
}

//    Server ⬇
//
//    OnServerInit          -> safe to get sv/globals
//    OnGameInit            -> safe to get GameRules
//    OnGamePostInit
//    OnResourcePrecache
//    OnServerSpawn         -> safe to execute .cfg
//    OnGameActivate
//    OnActivateServer
//    ...
//    OnGameDeactivate
//    OnGamePreShutdown
//    OnGameShutdown

//    Frame ⬇
//    OnGameFramePre
//    OnFrameUpdatePreEntityThink
//    OnFrameUpdatePostEntityThink
//    OnGameFramePost
//    OnServerGamePostSimulate
