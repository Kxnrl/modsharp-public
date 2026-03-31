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

using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Units;
using SqlSugar;

namespace Sharp.Modules.AdminCommands.SQLStorage;

internal sealed class StorageServiceImpl : IAdminOperationStorageService
{
    private readonly SqlSugarScope _db;

    public StorageServiceImpl(DbType dbType, string connectionString)
        => _db = CreateClient(dbType, connectionString);

    public void Init()
    {
        _db.CodeFirst.InitTables<AdminOperationEntity>();
    }

    public void Shutdown()
    {
        _db.Dispose();
    }

    public async Task<AdminOperationRecord?> GetAsync(SteamID steamId, AdminOperationType type)
    {
        var now  = DateTime.UtcNow;
        var kind = NormalizeType(type.Value);

        var entity = await _db.Queryable<AdminOperationEntity>()
                              .Where(x => x.TargetSteamId == steamId && x.Type == kind)
                              .Where(x => x.RemovedAt == null)
                              .Where(x => x.ExpiresAt == null || x.ExpiresAt > now)
                              .OrderByDescending(x => x.Id)
                              .FirstAsync();

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID steamId)
    {
        var now = DateTime.UtcNow;

        var entities = await _db.Queryable<AdminOperationEntity>()
                                .Where(x => x.TargetSteamId == steamId)
                                .Where(x => x.RemovedAt     == null)
                                .Where(x => x.ExpiresAt == null || x.ExpiresAt > now)
                                .ToListAsync();

        if (entities.Count == 0)
        {
            return [];
        }

        var list = new List<AdminOperationRecord>(entities.Count);

        foreach (var entity in entities)
        {
            list.Add(ToRecord(entity));
        }

        return list;
    }

    public async Task AddAsync(AdminOperationRecord record)
    {
        var now    = DateTime.UtcNow;
        var entity = ToEntity(record);

        await _db.Updateable<AdminOperationEntity>()
                 .SetColumns(x => x.RemovedAt == now)
                 .Where(x => x.TargetSteamId == entity.TargetSteamId && x.Type == entity.Type)
                 .Where(x => x.RemovedAt == null)
                 .Where(x => x.ExpiresAt == null || x.ExpiresAt > now)
                 .ExecuteCommandAsync();

        await _db.Insertable(entity).ExecuteCommandAsync();
    }

    public Task RemoveAsync(SteamID targetId, AdminOperationType type, SteamID? removedBy, string? reason)
    {
        var now  = DateTime.UtcNow;
        var kind = NormalizeType(type.Value);

        return _db.Updateable<AdminOperationEntity>()
                  .SetColumns(x => x.RemovedAt    == now)
                  .SetColumns(x => x.RemovedBy    == removedBy)
                  .SetColumns(x => x.RemoveReason == reason)
                  .Where(x => x.TargetSteamId == targetId && x.Type == kind)
                  .Where(x => x.RemovedAt == null) // Only close active ones
                  .Where(x => x.ExpiresAt == null || x.ExpiresAt > now)
                  .ExecuteCommandAsync();
    }

    public Task<bool> HasActiveAsync(SteamID steamId, AdminOperationType type)
    {
        var now  = DateTime.UtcNow;
        var kind = NormalizeType(type.Value);

        return _db.Queryable<AdminOperationEntity>()
                  .Where(x => x.TargetSteamId == steamId && x.Type == kind)
                  .Where(x => x.RemovedAt == null)
                  .Where(x => x.ExpiresAt == null || x.ExpiresAt > now)
                  .AnyAsync();
    }

    private static SqlSugarScope CreateClient(DbType dbType, string connectionString)
        => new (new ConnectionConfig
        {
            DbType                = dbType,
            ConnectionString      = connectionString,
            IsAutoCloseConnection = true,
            InitKeyType           = InitKeyType.Attribute,
        });

    private static AdminOperationEntity ToEntity(AdminOperationRecord record)
        => new ()
        {
            TargetSteamId = record.SteamId,
            Type          = NormalizeType(record.Type.Value),
            AdminSteamId  = record.AdminSteamId,
            CreatedAt     = new DateTimeOffset(record.CreatedAt),
            ExpiresAt     = record.ExpiresAt is null ? null : new DateTimeOffset(record.ExpiresAt.Value),
            Reason        = record.Reason,
            Metadata      = record.Metadata,
            RemovedBy     = record.RemovedBy,
            RemovedAt     = record.RemovedAt is null ? null : new DateTimeOffset(record.RemovedAt.Value),
            RemoveReason  = record.RemoveReason,
        };

    private static AdminOperationRecord ToRecord(AdminOperationEntity entity)
        => new (entity.TargetSteamId,
                new AdminOperationType(entity.Type),
                entity.AdminSteamId,
                entity.CreatedAt.UtcDateTime,
                entity.ExpiresAt?.UtcDateTime,
                entity.Reason,
                entity.Metadata,
                entity.RemovedBy,
                entity.RemovedAt?.UtcDateTime,
                entity.RemoveReason);

    private static string NormalizeType(string value)
        => value.Trim().ToLowerInvariant();
}
