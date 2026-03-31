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

using System.Text.Json;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

public record AdminOperationRecord(
    SteamID            SteamId,
    AdminOperationType Type,
    SteamID?           AdminSteamId,
    DateTime           CreatedAt,
    DateTime?          ExpiresAt, // null = permanent
    string             Reason,
    string?            Metadata     = null,
    SteamID?           RemovedBy    = null,
    DateTime?          RemovedAt    = null,
    string?            RemoveReason = null
)
{
    public bool IsExpired   => RemovedAt.HasValue || (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow);
    public bool IsPermanent => !ExpiresAt.HasValue;

    public T? GetMetadata<T>()
    {
        if (string.IsNullOrWhiteSpace(Metadata))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(Metadata);
        }
        catch
        {
            return default;
        }
    }
}
