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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared;
using SqlSugar;

namespace Sharp.Modules.AdminCommands.SQLStorage;

public sealed class SqlStorage : IModSharpModule
{
    public string DisplayName   => "AdminCommands -- SQLStorage";
    public string DisplayAuthor => "Nukoooo";

    private const string ConnectionStringKey       = "AdminCommands";
    private const string ModuleConnectionStringKey = "AdminCommands.SQLStorage";

    private readonly ISharedSystem       _sharedSystem;
    private readonly ILogger<SqlStorage> _logger;

    private readonly IAdminOperationStorageService _impl;

    public SqlStorage(ISharedSystem sharedSystem,
        string                      dllPath,
        string                      sharpPath,
        Version                     version,
        IConfiguration              coreConfiguration,
        bool                        hotReload)
    {
        _sharedSystem = sharedSystem;
        _logger       = sharedSystem.GetLoggerFactory().CreateLogger<SqlStorage>();

        var rawConnection = ResolveConnectionString(coreConfiguration);
        var (dbType, connectionString) = ParseConnectionString(rawConnection, sharpPath);

        _impl = new StorageServiceImpl(dbType, connectionString);
    }

    public bool Init()
    {
        try
        {
            ((StorageServiceImpl) _impl).Init();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize admin operation SQL storage.");

            return false;
        }
    }

    public void PostInit()
    {
        _sharedSystem.GetSharpModuleManager().RegisterSharpModuleInterface(this, IAdminOperationStorageService.Identity, _impl);
    }

    public void Shutdown()
    {
        ((StorageServiceImpl) _impl).Shutdown();
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ModuleConnectionStringKey)
                               ?? configuration.GetConnectionString(ConnectionStringKey);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new KeyNotFoundException(
                $"Missing '{ModuleConnectionStringKey}' or '{ConnectionStringKey}' in connection strings.");
        }

        return connectionString;
    }

    private static (DbType DbType, string ConnectionString) ParseConnectionString(string rawConnection, string sharpPath)
    {
        var parts = rawConnection.Split("://",
                                        2,
                                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            throw new InvalidDataException("Missing database type in connection string (expected '{schema}://{connection}').");
        }

        var schema = parts[0].ToLowerInvariant();
        var conn   = parts[1];

        var dbType = schema switch
        {
            "mysql" => DbType.MySql,
            "pgsql" or "postgres" or "postgresql" => DbType.PostgreSQL,
            _ => throw new NotSupportedException($"Unsupported database type '{schema}'. Use mysql, or pgsql."),
        };

        return (dbType, conn);
    }
}
