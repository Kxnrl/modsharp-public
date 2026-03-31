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

using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services;

/// <summary>
///     Thin wrapper over IAdminOperationStorageService with basic guardrails (skip duplicate active records).
/// </summary>
internal sealed class AdminOperationService
{
    private readonly ILogger<AdminOperationService> _logger;
    private readonly IAdminOperationStorageService  _storage;

    public AdminOperationService(ILogger<AdminOperationService> logger, IAdminOperationStorageService storage)
    {
        _logger  = logger;
        _storage = storage;
    }

    public async Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID steamId,
        CancellationToken                                                      cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _storage.GetAllAsync(steamId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load punishment records for {SteamId}", steamId);

            return [];
        }
    }

    public async Task<bool> HasActiveAsync(SteamID steamId,
        AdminOperationType                         type,
        CancellationToken                          cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _storage.HasActiveAsync(steamId, type).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check active {Type} for {SteamId}", type, steamId);

            return false;
        }
    }

    public async Task<bool> AddAsync(AdminOperationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (await _storage.HasActiveAsync(record.SteamId, record.Type).ConfigureAwait(false))
            {
                _logger.LogInformation("Skip {Type}: {SteamId} already has active record.", record.Type, record.SteamId);

                return false;
            }

            await _storage.AddAsync(record).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Type} punishment for {SteamId}", record.Type, record.SteamId);

            return false;
        }
    }

    public async Task RemoveAsync(SteamID steamId,
        AdminOperationType                type,
        SteamID?                          adminId,
        string?                           reason,
        CancellationToken                 cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _storage.RemoveAsync(steamId, type, adminId, reason).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove {Type} punishment for {SteamId}", type, steamId);
        }
    }
}
