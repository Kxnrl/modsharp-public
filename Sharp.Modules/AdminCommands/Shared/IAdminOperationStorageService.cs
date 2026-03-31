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

using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Storage contract for admin operations (ban/mute/gag). This is the primary external extension point; implement this
///     to plug in your own persistence.
///     <para>
///         Metadata on <see cref="AdminOperationRecord" /> is an optional JSON string for handler-specific payload
///         (e.g., weapon lists for custom operations).
///     </para>
///     <para>Register your implementation with identity <see cref="Identity" />.</para>
///     <para>All methods are async and should not touch anything from the game.</para>
/// </summary>
public interface IAdminOperationStorageService
{
    public const string Identity = nameof(IAdminOperationStorageService);

    /// <summary>
    ///     Returns a single record for the given SteamID and operation type, or null if none/expired.
    /// </summary>
    Task<AdminOperationRecord?> GetAsync(SteamID steamId, AdminOperationType type);

    /// <summary>
    ///     Returns all records for a SteamID (may include expired/removed ones depending on implementation policy).
    /// </summary>
    Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID steamId);

    /// <summary>
    ///     Adds a new record. Implementations should be idempotent if desired (skip or replace duplicates as needed).
    /// </summary>
    Task AddAsync(AdminOperationRecord record);

    /// <summary>
    ///     Removes a record of the given type for the SteamID (no-op if missing).
    /// </summary>
    Task RemoveAsync(SteamID targetId, AdminOperationType type, SteamID? removedBy, string? reason);

    /// <summary>
    ///     Returns true if there is an active (non-expired/non-removed) record of the given type.
    /// </summary>
    Task<bool> HasActiveAsync(SteamID steamId, AdminOperationType type);
}
