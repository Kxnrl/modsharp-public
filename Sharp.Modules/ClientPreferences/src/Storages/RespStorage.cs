/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sharp.Modules.ClientPreferences.Core.Models;
using Sharp.Shared.Units;
using StackExchange.Redis;

namespace Sharp.Modules.ClientPreferences.Core.Storages;

internal class RespStorage : IStorage
{
    private readonly ILogger<RespStorage>    _logger;
    private readonly CancellationTokenSource _ctSource;
    private readonly ConfigurationOptions    _configuration;

    private volatile ConnectionMultiplexer? _database;

    public RespStorage(ILoggerFactory loggerFactory, CancellationTokenSource source, string connectionString)
    {
        var config = ConfigurationOptions.Parse(connectionString);
        config.LoggerFactory = loggerFactory;

        _logger        = loggerFactory.CreateLogger<RespStorage>();
        _ctSource      = CancellationTokenSource.CreateLinkedTokenSource(source.Token);
        _configuration = config;
    }

    public void Init()
        => Task.Run(async () => _database = await ConnectionMultiplexer.ConnectAsync(_configuration), _ctSource.Token);

    public void Shutdown()
    {
        try
        {
            _ctSource.Cancel();
        }
        finally
        {
            _database?.Dispose();
            _ctSource.Dispose();
        }
    }

    public async Task<CookieModel[]> LoadUserCookie(SteamID identity)
    {
        var db = GetDatabase();

        if (await db.StringGetLeaseAsync(identity.ToString()) is not { } bytes)
        {
            return [];
        }

        try
        {
            var cookies = JsonSerializer.Deserialize<CookieModel[]>(bytes.Span)
                          ?? throw new JsonException("Invalid CookieModel[] json");

            return cookies;
        }
        catch (JsonException)
        {
        }

        return [];
    }

    public async Task SaveUserCookie(SteamID identity, CookieModel[] cookies)
    {
        var db = GetDatabase();

        var bytes = JsonSerializer.Serialize(cookies);

        await db.StringSetAsync(identity.ToString(), bytes);
    }

    private IDatabase GetDatabase()
        => _database?.GetDatabase() ?? throw new DatabaseUnavailableException("RESP is unavailable");
}
