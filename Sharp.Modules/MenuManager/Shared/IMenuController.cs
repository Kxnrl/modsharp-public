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
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

/// <summary>
///     Controls an active menu session for a client.
/// </summary>
public interface IMenuController : IDisposable
{
    /// <summary>
    ///     The currently active top menu.
    /// </summary>
    Menu Menu { get; }

    /// <summary>
    ///     Re-evaluates and redraws the current menu for <see cref="Client" />.
    /// </summary>
    void Refresh();

    /// <summary>
    ///     The client viewing this menu session.
    /// </summary>
    IGameClient Client { get; }

    /// <summary>
    ///     Pushes a child submenu onto the menu stack and invokes its <see cref="Menu.OnEnter" /> (does not invoke the parent menu's <see cref="Menu.OnExit" />).
    /// </summary>
    void Next(Menu menu);

    /// <summary>
    ///     Creates and pushes a child submenu onto the menu stack via a per-client factory and invokes its <see cref="Menu.OnEnter" />.
    /// </summary>
    void Next(Func<IGameClient, Menu> menuFactory);

    /// <summary>
    ///     Schedules closure of the entire menu session (invoking <see cref="Menu.OnExit" /> on the current menu and all parent menus after a short ~0.01s timer).
    /// </summary>
    void Exit();

    /// <summary>
    ///     Pops the current submenu (invoking its <see cref="Menu.OnExit" />) and returns to the parent menu (without re-invoking the parent's <see cref="Menu.OnEnter" />).
    /// </summary>
    void GoBack();
}
