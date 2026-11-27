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
using Sharp.Shared.Types;

namespace Sharp.Extensions.GameEventManager;

public interface IGameEventManager
{
    delegate void DelegateOnEventFired(IGameEvent e);

    delegate HookReturnValue<bool> DelegateOnHookEvent(IGameEvent e, ref bool serverOnly);

    /// <summary>
    ///     Pre-hook a game event before it fires (typically to modify fields, change broadcast scope, or block it).
    /// </summary>
    /// <remarks>
    ///     Only <see cref="HookReturnValue{T}.Action" /> is evaluated (<see cref="HookReturnValue{T}.ReturnValue" /> is unused):
    ///     return <see cref="Sharp.Shared.Enums.EHookAction.SkipCallReturnOverride" /> to block the event,
    ///     <see cref="Sharp.Shared.Enums.EHookAction.ChangeParamReturnDefault" /> (or higher) to propagate modifications to <c>serverOnly</c>,
    ///     or <see cref="Sharp.Shared.Enums.EHookAction.Ignored" /> to leave <c>serverOnly</c> unchanged.
    /// </remarks>
    /// <param name="eventName">Game event name to hook.</param>
    /// <param name="callback">Pre-hook callback.</param>
    void HookEvent(string eventName, DelegateOnHookEvent callback);

    /// <summary>
    ///     Listen for a game event when it fires (read-only).
    /// </summary>
    /// <param name="eventName">Game event name to listen for.</param>
    /// <param name="callback">Read-only event listener callback.</param>
    void ListenEvent(string eventName, DelegateOnEventFired callback);

    /// <summary>
    ///     Create a custom game event.
    /// </summary>
    /// <remarks>Call <see cref="IGameEvent.Fire" /> (which transfers ownership to the engine) or <see cref="IGameEvent.Dispose" /> when finished.</remarks>
    /// <param name="eventName">Game event name.</param>
    /// <param name="force">Force creation even if there are no listeners.</param>
    /// <returns>The created event, or <see langword="null" /> if creation failed.</returns>
    IGameEvent? CreateEvent(string eventName, bool force);

    /// <summary>
    ///     Create a typed custom game event.
    /// </summary>
    /// <remarks>Call <see cref="IGameEvent.Fire" /> (which transfers ownership to the engine) or <see cref="IGameEvent.Dispose" /> when finished.</remarks>
    /// <typeparam name="T">Typed game event interface.</typeparam>
    /// <param name="force">Force creation even if there are no listeners.</param>
    /// <returns>The created event, or <see langword="null" /> if creation failed.</returns>
    T? CreateEvent<T>(bool force) where T : class, IGameEvent;

    /// <summary>
    ///     Clone an existing game event into a new custom-owned editable event.
    /// </summary>
    /// <remarks>Call <see cref="IGameEvent.Fire" /> (which transfers ownership to the engine) or <see cref="IGameEvent.Dispose" /> when finished.</remarks>
    /// <typeparam name="T">Game event interface type.</typeparam>
    /// <param name="e">Source event to clone.</param>
    /// <returns>The cloned event, or <see langword="null" /> if cloning failed.</returns>
    T? CloneEvent<T>(T e) where T : class, IGameEvent;
}
