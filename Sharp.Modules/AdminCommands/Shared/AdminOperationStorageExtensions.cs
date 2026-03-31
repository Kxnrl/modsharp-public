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
///     Convenience helpers for constructing admin operation records against the storage contract.
/// </summary>
public static class AdminOperationStorageExtensions
{
    public static Task AddBanAsync(this IAdminOperationStorageService storage,
        SteamID                                                       targetId,
        SteamID?                                                      adminId,
        TimeSpan?                                                     duration,
        string                                                        reason,
        string?                                                       metadata = null)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Ban, duration, reason, metadata));

    public static Task RemoveBanAsync(this IAdminOperationStorageService storage,
        SteamID                                                          targetId,
        SteamID?                                                         adminId,
        string?                                                          removeReason)
        => storage.RemoveAsync(targetId, AdminOperationType.Ban, adminId, removeReason);

    public static Task AddMuteAsync(this IAdminOperationStorageService storage,
        SteamID                                                        targetId,
        SteamID?                                                       adminId,
        TimeSpan?                                                      duration,
        string                                                         reason,
        string?                                                        metadata = null)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Mute, duration, reason, metadata));

    public static Task RemoveMuteAsync(this IAdminOperationStorageService storage,
        SteamID                                                           targetId,
        SteamID?                                                          adminId,
        string?                                                           removeReason)
        => storage.RemoveAsync(targetId, AdminOperationType.Mute, adminId, removeReason);

    public static Task AddGagAsync(this IAdminOperationStorageService storage,
        SteamID                                                       targetId,
        SteamID?                                                      adminId,
        TimeSpan?                                                     duration,
        string                                                        reason,
        string?                                                       metadata = null)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Gag, duration, reason, metadata));

    public static Task RemoveGagAsync(this IAdminOperationStorageService storage,
        SteamID                                                          targetId,
        SteamID?                                                         adminId,
        string?                                                          removeReason)
        => storage.RemoveAsync(targetId, AdminOperationType.Gag, adminId, removeReason);

    private static AdminOperationRecord CreateRecord(SteamID targetId,
        SteamID?                                             adminId,
        AdminOperationType                                   type,
        TimeSpan?                                            duration,
        string                                               reason,
        string?                                              metadata = null)
    {
        var expiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : (DateTime?) null;

        return new AdminOperationRecord(targetId, type, adminId, DateTime.UtcNow, expiresAt, reason, metadata);
    }
}
