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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using Sharp.Modules.ClientPreferences.Core.Models;
using Sharp.Shared.Units;

namespace Sharp.Modules.ClientPreferences.Core.Storages;

internal class LiteDbStorage : IStorage
{
    private readonly ILogger<LiteDbStorage>  _logger;
    private readonly LiteDatabase            _database;
    private readonly CancellationTokenSource _ctSource;

    public LiteDbStorage(ILoggerFactory loggerFactory,
        CancellationTokenSource         source,
        string                          connectionString,
        string                          sharpPath)
    {
        var data = Path.Combine(sharpPath, "data");
        Directory.CreateDirectory(data);

        _logger   = loggerFactory.CreateLogger<LiteDbStorage>();
        _database = new LiteDatabase(connectionString.Replace("{sharp::data}", data));
        _ctSource = CancellationTokenSource.CreateLinkedTokenSource(source.Token);
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
            _database.Dispose();
            _ctSource.Dispose();
        }
    }

    public Task<CookieModel[]> LoadUserCookie(SteamID identity)
        => Task.Run(() =>
                    {
                        var id = (long) identity.AsPrimitive();

                        var collection = _database.GetCollection<LiteCookieEntity>("cookies");

                        var query = collection.FindById(id);

                        return query is null ? [] : query.Body;
                    },
                    _ctSource.Token);

    public Task SaveUserCookie(SteamID identity, CookieModel[] cookies)
        => Task.Run(() =>
                    {
                        var id = (long) identity.AsPrimitive();

                        var collection = _database.GetCollection<LiteCookieEntity>("cookies");

                        collection.Delete(id);
                        collection.Insert(new LiteCookieEntity { Identity = identity.AsPrimitive(), Body = cookies });
                    },
                    _ctSource.Token);
}

file class LiteCookieEntity
{
    [BsonId]
    public required ulong Identity { get; init; }

    public required CookieModel[] Body { get; init; }
}
