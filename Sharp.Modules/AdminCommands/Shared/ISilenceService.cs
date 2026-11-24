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

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Silence/unsilence operations (mute+gag). Provided for callers; not intended for external implementations.
///     Side effects: updates cache, may notify players. Storage skips writing duplicate active records,
///     but callers should still perform duplicate checks before invoking to avoid redundant notifications
///     (built-in command handlers already do this).
/// </summary>
public interface ISilenceService
{
    /// <summary>
    ///     Apply silence (mute+gag) to an online target (updates cache/storage, may notify).
    /// </summary>
    void Silence(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason);

    /// <summary>
    ///     Remove silence (mute+gag) from an online target (updates cache/storage).
    /// </summary>
    void Unsilence(IGameClient? admin, IGameClient target, string reason);
}
