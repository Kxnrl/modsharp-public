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

namespace Sharp.Modules.AdminCommands.Storage;

/// <summary>
///     Delegates IAdminOperationStorageService calls to a switchable underlying implementation so we can
///     swap in user-provided storage at runtime without rebuilding the container.
/// </summary>
internal sealed class AdminOperationStorage : IAdminOperationStorageService
{
    private readonly ILogger<AdminOperationStorage> _logger;
    private readonly IAdminOperationStorageService  _fallback;

    private IAdminOperationStorageService _current;

    public AdminOperationStorage(ILogger<AdminOperationStorage> logger, JsonAdminOperationStorage fallback)
    {
        _logger   = logger;
        _fallback = fallback;
        _current  = fallback;
    }

    public IAdminOperationStorageService Current => Volatile.Read(ref _current);

    public void Use(IAdminOperationStorageService storage, string? providerName = null)
    {
        if (ReferenceEquals(Current, storage))
        {
            return;
        }

        Volatile.Write(ref _current, storage);

        if (!ReferenceEquals(storage, _fallback))
        {
            if (!string.IsNullOrWhiteSpace(providerName))
            {
                _logger.LogInformation("Using external admin operation storage from {provider}.", providerName);
            }
            else
            {
                _logger.LogInformation("Using custom admin operation storage instance.");
            }
        }
        else
        {
            _logger.LogInformation("Using built-in JSON admin operation storage.");
        }
    }

    public void UseFallback()
        => Use(_fallback);

    public Task<AdminOperationRecord?> GetAsync(SteamID steamId, AdminOperationType type)
        => Current.GetAsync(steamId, type);

    public Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID steamId)
        => Current.GetAllAsync(steamId);

    public Task AddAsync(AdminOperationRecord record)
        => Current.AddAsync(record);

    public Task RemoveAsync(SteamID steamId, AdminOperationType type, SteamID? removedBy, string? reason)
        => Current.RemoveAsync(steamId, type, removedBy, reason);

    public Task<bool> HasActiveAsync(SteamID steamId, AdminOperationType type)
        => Current.HasActiveAsync(steamId, type);
}
