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
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Ban/unban operations. Provided for callers; not intended for external implementations.
///     Side effects: updates cache, may kick/notify. Storage skips writing duplicate active records,
///     but callers should still perform duplicate checks before invoking to avoid redundant kicks or notifications
///     (built-in command handlers already do this).
/// </summary>
public interface IBanService
{
    /// <summary>
    ///     Ban an online target (may kick/notify, updates cache).
    /// </summary>
    void Ban(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason);

    /// <summary>
    ///     Ban by SteamID (offline path; updates cache/storage, may kick if target joins).
    /// </summary>
    void Ban(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason);

    /// <summary>
    ///     Remove a ban for the given SteamID (updates cache/storage).
    /// </summary>
    void Unban(IGameClient? admin, SteamID steamId, string reason);
}
