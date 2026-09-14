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
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.Objects;

/// <summary>
///     A persistent callback (a JS function) handed to C# by a script; it lives across calls and can be
///     invoked from C# on demand via <see cref="Invoke" />. It MUST be disposed via
///     <see cref="IDisposable.Dispose" /> (otherwise the underlying v8::Global leaks until the map ends);
///     <b>
///         Dispose must also run on the
///         main thread
///     </b>
///     — it resets a v8::Global (which touches the isolate), so calling it off the main thread throws
///     <see cref="System.InvalidOperationException" /> (the handle stays unreleased and you can retry on
///     the main thread). Once the point_script that owns it is gone, <see cref="IsAvailable" /> becomes
///     false and any later <see cref="Invoke" /> fails with an exception (see its docs).
/// </summary>
public interface IScriptCallback : IContextObject, IDisposable
{
    /// <summary>
    ///     The point_script that owns this callback (the current-script at creation time). Resolved fresh
    ///     on each access (with a serial check); returns <see langword="null" /> if that entity has been
    ///     destroyed.
    /// </summary>
    IBaseEntity? Owner { get; }

    /// <summary>The callback is still usable (not disposed, and the owning point_script is still alive).</summary>
    bool IsAvailable { get; }

    /// <summary>
    ///     Invoke this callback on the main (script) thread, passing <paramref name="args" /> in order.
    /// </summary>
    /// <returns>
    ///     The JS return value (scalar / string / vector / entity) <br />
    ///     <see langword="null" /> -> null/undefined/void
    /// </returns>
    /// <exception cref="System.InvalidOperationException"> !IsAvailable / Not game thread</exception>
    /// <exception cref="System.ObjectDisposedException">The callback has been <see cref="IDisposable.Dispose" />d</exception>
    ScriptReturnValue? Invoke(params ReadOnlySpan<ScriptArgument> args);
}
