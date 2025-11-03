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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Refit;
using Sharp.Modules.ClientPreferences.Core.Models;
using Sharp.Shared.Units;

namespace Sharp.Modules.ClientPreferences.Core.Storages;

internal class RestStorage : IStorage
{
    private readonly ILogger<RestStorage>    _logger;
    private readonly CancellationTokenSource _ctSource;
    private readonly IRestApi                _restApi;

    public RestStorage(ILoggerFactory loggerFactory, CancellationTokenSource source, string connectionString)
    {
        _logger   = loggerFactory.CreateLogger<RestStorage>();
        _ctSource = CancellationTokenSource.CreateLinkedTokenSource(source.Token);

        var kv = ParseConnectionString(connectionString);

        if (!kv.TryGetValue("host", out var host))
        {
            throw new KeyNotFoundException("Missing 'host' in connection string");
        }

        var authorization = kv.GetValueOrDefault("authorization", "modsharp");

        _restApi = RestService.For<IRestApi>(host,
                                             new RefitSettings
                                             {
                                                 AuthorizationHeaderValueGetter = (message, token)
                                                     => Task.FromResult(authorization),
                                             });
    }

    public void Init()
    {
    }

    public void Shutdown()
    {
        try
        {
            _ctSource.Cancel();
        }
        finally
        {
            _ctSource.Dispose();
        }
    }

    public Task<CookieModel[]> LoadUserCookie(SteamID identity)
        => _restApi.Load(identity);

    public Task SaveUserCookie(SteamID identity, CookieModel[] cookies)
        => _restApi.Save(identity, cookies);

    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var chunks = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in chunks)
        {
            var kv = chunk.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (kv.Length != 2)
            {
                continue;
            }

            map.Add(kv[0], kv[1]);
        }

        return map;
    }

    private interface IRestApi
    {
        [Get("/cookie/{identity}")]
        Task<CookieModel[]> Load(ulong identity);

        [Put("/cookie/{identity}")]
        Task Save(ulong identity, [Body] CookieModel[] cookies);
    }
}

