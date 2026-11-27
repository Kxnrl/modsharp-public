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

using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

public interface IEventManager
{
    /// <summary>
    ///     Add <see cref="IEventListener" /> to listen for events
    /// </summary>
    /// <exception cref="System.InvalidOperationException"><see cref="IEventListener.ListenerVersion" /> does not match <see cref="IEventListener.ApiVersion" />.</exception>
    void InstallEventListener(IEventListener listener);

    /// <summary>
    ///     Remove <see cref="IEventListener" />
    /// </summary>
    void RemoveEventListener(IEventListener listener);

    /// <summary>
    ///     Create Game Event
    /// </summary>
    IGameEvent? CreateEvent(string name, bool force);

    /// <summary>
    ///     Create Game Event
    /// </summary>
    T? CreateEvent<T>(bool force) where T : class, IGameEvent;

    /// <summary>
    ///     Clone event (editable)
    /// </summary>
    /// <remarks>Clones a new custom-owned event; callers must either fire or dispose it.</remarks>
    /// <exception cref="System.ObjectDisposedException"><paramref name="event" /> has been disposed</exception>
    T? CloneEvent<T>(T @event) where T : class, IGameEvent;

    /// <summary>
    ///     Register a game event name with the engine so <see cref="IEventListener.HookFireEvent" /> and
    ///     <see cref="IEventListener.FireGameEvent" /> are dispatched when it fires.
    /// </summary>
    /// <remarks>
    ///     No events are hooked by default. Registrations are idempotent, global across modules, and permanent (there is no unhook API).
    /// </remarks>
    void HookEvent(string name);

    /// <summary>
    ///     Check client subscribed events
    /// </summary>
    bool FindListener(PlayerSlot slot, string name);
}
