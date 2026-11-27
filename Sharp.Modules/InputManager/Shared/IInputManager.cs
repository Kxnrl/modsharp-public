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

namespace Sharp.Modules.InputManager.Shared;

public interface IInputManager
{
    const string Identity = nameof(IInputManager);

    /// <summary>
    ///     Get or create an input listener registry for a module.
    /// </summary>
    /// <param name="moduleIdentity">The exact module assembly name (case-sensitive) so listeners are cleaned up automatically on <c>OnLibraryDisconnect</c>.</param>
    /// <returns>The input listener registry for the module</returns>
    public IInputListenerRegistry GetInputListenerRegistry(string moduleIdentity);
}

public interface IInputListenerRegistry
{
    /// <summary>
    ///     Add a single key input listener.
    /// </summary>
    /// <param name="key">The input key to listen for</param>
    /// <param name="action">Callback function to invoke</param>
    /// <param name="state">The key state to listen for</param>
    /// <param name="holdDuration">
    ///     Hold duration in seconds. Must be <c>0</c> for <see cref="InputState.KeyDown" /> and <see cref="InputState.KeyUp" />
    ///     (any positive value prevents those states from firing). For <see cref="InputState.KeyHold" />, a positive value fires once
    ///     per press after the key is held for that duration, whereas <c>0</c> fires every tick while held.
    /// </param>
    /// <exception cref="NotSupportedException"><paramref name="key" /> is not supported (e.g. obsolete placeholder keys).</exception>
    void AddInputListener(InputKey key,
        Action<IGameClient>        action,
        InputState                 state        = InputState.KeyDown,
        float                      holdDuration = 0f);

    /// <summary>
    ///     Add a combination key listener (all keys must be pressed simultaneously).
    /// </summary>
    /// <param name="keys">Non-empty array of supported keys for the combination</param>
    /// <param name="action">Callback function to invoke</param>
    /// <param name="state">The key state to listen for, defaults to KeyDown</param>
    /// <exception cref="ArgumentException"><paramref name="keys" /> is <see langword="null" /> or empty.</exception>
    /// <exception cref="NotSupportedException">One or more entries in <paramref name="keys" /> are not supported.</exception>
    void AddCombinationListener(InputKey[] keys, Action<IGameClient> action, InputState state = InputState.KeyDown);
}
