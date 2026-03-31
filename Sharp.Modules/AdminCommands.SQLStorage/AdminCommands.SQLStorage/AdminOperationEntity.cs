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
using SqlSugar;

namespace Sharp.Modules.AdminCommands.SQLStorage;

[SugarTable("admin_operations")]
[SugarIndex("idx_operations_lookup",
            nameof(TargetSteamId),
            OrderByType.Asc,
            nameof(Type),
            OrderByType.Asc,
            nameof(RemovedAt),
            OrderByType.Asc,
            nameof(ExpiresAt),
            OrderByType.Asc)]
internal sealed class AdminOperationEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(SqlParameterDbType = typeof(SteamIdDataConvert), ColumnDataType = "BIGINT UNSIGNED")]
    public SteamID TargetSteamId { get; set; }

    [SugarColumn(Length = 64)]
    public string Type { get; set; } = string.Empty;

    [SugarColumn(SqlParameterDbType = typeof(SteamIdDataConvert), ColumnDataType = "BIGINT UNSIGNED", IsNullable = true)]
    public SteamID? AdminSteamId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? ExpiresAt { get; set; }

    [SugarColumn(Length = 2048)]
    public string Reason { get; set; } = string.Empty;

    [SugarColumn(Length = 4096, IsNullable = true)]
    public string? Metadata { get; set; }

    [SugarColumn(SqlParameterDbType = typeof(SteamIdDataConvert), ColumnDataType = "BIGINT UNSIGNED", IsNullable = true)]
    public SteamID? RemovedBy { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? RemovedAt { get; set; }

    [SugarColumn(Length = 2048, IsNullable = true)]
    public string? RemoveReason { get; set; }
}
