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

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Sharp.Modules.ClientPreferences.Core.Models;
using Sharp.Shared.Units;

namespace Sharp.Modules.ClientPreferences.Core.Storages;

internal class MySqlStorage : IStorage
{
    private readonly ILogger<MySqlStorage>   _logger;
    private readonly CancellationTokenSource _ctSource;
    private readonly string                  _configuration;

    private volatile bool _ready;

    public MySqlStorage(ILoggerFactory loggerFactory, CancellationTokenSource source, string connectionString)
    {
        _logger        = loggerFactory.CreateLogger<MySqlStorage>();
        _ctSource      = CancellationTokenSource.CreateLinkedTokenSource(source.Token);
        _configuration = connectionString;
    }

    public void Init()
        => Task.Run(async () =>
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            try
                            {
                                await using var connection = new MySqlConnection(_configuration);
                                await connection.OpenAsync(_ctSource.Token);

                                await using var command = new MySqlCommand(CreateTableSql, connection);

                                await command.ExecuteNonQueryAsync(_ctSource.Token);

                                _ready = true;

                                return;
                            }
                            catch (Exception e)
                            {
                                if (e is OperationCanceledException)
                                {
                                    return;
                                }

                                _logger.LogError(e, "Failed to check table");
                                await Task.Delay(TimeSpan.FromSeconds(1), _ctSource.Token);
                            }

                            if (!_ready)
                            {
                                _logger.LogCritical("Failed to initialize MySqlStorage after 5 attempts");
                            }
                        }
                    },
                    _ctSource.Token);

    public void Shutdown()
    {
        _ready = false;

        try
        {
            _ctSource.Cancel();
        }
        finally
        {
            _ctSource.Dispose();
        }
    }

    public async Task<CookieModel[]> LoadUserCookie(SteamID identity)
    {
        const string sql = "SELECT `data` FROM `ClientPreferences` WHERE `identity` = @id LIMIT 1";

        await using var connection = await GetDatabase();
        await using var command    = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", identity.ToString());

        await using var reader = await command.ExecuteReaderAsync(_ctSource.Token);

        if (await reader.ReadAsync(_ctSource.Token))
        {
            var data = reader.GetString("data");

            try
            {
                var cookies = JsonSerializer.Deserialize<CookieModel[]>(data)
                              ?? throw new JsonException("Invalid CookieModel[] json");

                return cookies ?? [];
            }
            catch (JsonException)
            {
            }
        }

        return [];
    }

    public async Task SaveUserCookie(SteamID identity, CookieModel[] cookies)
    {
        const string sql = """
                           INSERT INTO `ClientPreferences` (`identity`, `data`)
                           VALUES (@id, @data)
                           ON DUPLICATE KEY UPDATE data = @data;
                           """;

        await using var connection = await GetDatabase();
        await using var command    = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id",   identity.ToString());
        command.Parameters.AddWithValue("@data", JsonSerializer.Serialize(cookies));

        await command.ExecuteNonQueryAsync(_ctSource.Token);
    }

    private async Task<MySqlConnection> GetDatabase()
    {
        if (!_ready)
        {
            throw new DatabaseUnavailableException("MySql is unavailable");
        }

        var connection = new MySqlConnection(_configuration);
        await connection.OpenAsync(_ctSource.Token);

        return connection;
    }

    private const string CreateTableSql
        =
        """
        CREATE TABLE IF NOT EXISTS ClientPreferences (
            identity BIGINT UNSIGNED PRIMARY KEY DEFAULT 0,
            data VARCHAR(4096) NOT NULL DEFAULT '[]',
            updatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
        );
        """;
}
