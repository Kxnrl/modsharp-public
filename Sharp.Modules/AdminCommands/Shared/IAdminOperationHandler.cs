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
using System.Collections.Frozen;
using System.Collections.Generic;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Handler for a specific admin operation type (e.g. Ban, Mute, Gag).
///     Implementations run on the game thread and must remain fast/non-blocking (no I/O, no await).
///     OnApplied/OnRemoved may be called with a null target when the client is offline.
///     Use the provided record to implement your operation punishment to a player
/// </summary>
public interface IAdminOperationHandler
{
    /// <summary>
    ///     The operation type this handler is responsible for.
    /// </summary>
    AdminOperationType Type { get; }

    /// <summary>
    ///     Called when an operation is applied. <br />
    ///     Caller must guarantee that when <paramref name="targetClient" /> is not null, it matches
    ///     <see cref="AdminOperationRecord.SteamId" />.
    /// </summary>
    void OnApplied(AdminOperationRecord record, IGameClient? targetClient);

    /// <summary>
    ///     Called when an operation is removed. <br />
    ///     Caller must guarantee that when <paramref name="targetClient" /> is not null, it matches
    ///     <paramref name="steamId" />.
    /// </summary>
    void OnRemoved(SteamID steamId, IGameClient? targetClient);

    /// <summary>
    ///     Returns the localization key and fallback message when this operation is applied.
    /// </summary>
    (string Key, string Fallback) GetAppliedNotification(IGameClient target, string durationText);

    /// <summary>
    ///     Returns the localization key and fallback message when this operation is removed.
    /// </summary>
    (string Key, string Fallback) GetRemovedNotification(IGameClient target);

    /// <summary>
    ///     Returns the SteamIDs this handler currently has cached, so the periodic cache refresh
    ///     can re-validate them against storage and evict entries removed externally
    ///     (e.g. an unban written directly to a shared database). Called on the game thread. <br />
    ///     Entries added within <paramref name="grace" /> must be excluded so a freshly-applied
    ///     punishment is not evicted before its storage write has propagated. <br />
    ///     Handlers that do not cache state can rely on the default empty implementation.
    /// </summary>
    IReadOnlySet<SteamID> GetCachedIdentities(TimeSpan grace)
        => FrozenSet<SteamID>.Empty;
}
