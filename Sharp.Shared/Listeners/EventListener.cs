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

namespace Sharp.Shared.Listeners;

public interface IEventListener
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
    ///     Pre-hook called before a hooked game event is fired (<c>CGameEventManager::FireEvent</c> pre-hook).
    ///     If you only need to observe the event, implement <see cref="FireGameEvent" /> instead.
    /// </summary>
    /// <param name="event">The editable, engine-owned game event. Valid only for the duration of this callback; do not call <see cref="IGameEvent.Fire" /> or <see cref="IGameEvent.Dispose" />.</param>
    /// <param name="serverOnly">Controls whether the event is broadcast only on the server; modifications propagate to subsequent listeners.</param>
    /// <returns><see langword="false" /> to block the event from firing (and prevent <see cref="FireGameEvent" />); <see langword="true" /> to allow it.</returns>
    /// <remarks>
    ///     Only fires for event names registered via <see cref="Managers.IEventManager.HookEvent" />.
    ///     All installed listeners execute in descending priority order without short-circuiting; the event is blocked if any listener returns <see langword="false" />.
    ///     The default implementation returns <see langword="true" /> and does not override another listener's block.
    /// </remarks>
    bool HookFireEvent(IGameEvent @event, ref bool serverOnly)
        => true;

    /// <summary>
    ///     Called when a hooked game event fires (<c>IGameEventListener2::FireGameEvent</c>).
    /// </summary>
    /// <param name="event">The read-only game event. Valid only for the duration of this callback; mutating, firing, or disposing it throws <see cref="System.InvalidOperationException" />.</param>
    /// <remarks>
    ///     Only fires for event names registered via <see cref="Managers.IEventManager.HookEvent" /> and not blocked by <see cref="HookFireEvent" />.
    ///     The event wrapper is disposed immediately after all listeners return.
    /// </remarks>
    void FireGameEvent(IGameEvent @event);
}
