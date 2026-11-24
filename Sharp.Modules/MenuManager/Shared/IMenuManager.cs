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

using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

public interface IMenuManager
{
    const string Identity = nameof(IMenuManager);

    /// <summary>
    ///     Displays a menu to the specified client, replacing any existing active menu session.
    /// </summary>
    void DisplayMenu(IGameClient client, Menu menu);

    /// <summary>
    ///     Displays a menu to the specified client and outputs the new session identifier.
    /// </summary>
    void DisplayMenu(IGameClient client, Menu menu, out ulong sessionId);

    /// <summary>
    ///     Closes the client's active menu session.
    /// </summary>
    /// <remarks>
    ///     Disposal and <see cref="Menu.OnExit" /> callbacks are scheduled asynchronously on a short timer (~0.01s),
    ///     so <see cref="IsInMenu(IGameClient)" /> remains <see langword="true" /> briefly after this method returns.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">The client does not currently have an active menu session.</exception>
    void QuitMenu(IGameClient client);

    /// <summary>
    ///     Returns whether the client currently has an active menu session.
    /// </summary>
    bool IsInMenu(IGameClient client);

    /// <summary>
    ///     Returns whether the client is currently in the specified menu instance,
    ///     including parent menus in the current menu chain.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="menuInstance" /> is <see langword="null" />.</exception>
    bool IsInMenu(IGameClient client, Menu menuInstance);

    /// <summary>
    ///     Returns whether the client is currently in the specified menu session.
    /// </summary>
    bool IsInMenu(IGameClient client, ulong sessionId);

    /// <summary>
    ///     Returns whether the current top menu is the specified menu instance.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="menuInstance" /> is <see langword="null" />.</exception>
    bool IsInCurrentMenu(IGameClient client, Menu menuInstance);

    /// <summary>
    ///     Returns the current menu session identifier when the client is in a menu.
    /// </summary>
    bool TryGetCurrentMenuSessionId(IGameClient client, out ulong sessionId);
}
