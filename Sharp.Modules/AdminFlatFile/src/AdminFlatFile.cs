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
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminFlatFile;

public sealed class AdminFlatFile : IModSharpModule, IClientListener
{
    public string DisplayName   => "Admin FlatFile";
    public string DisplayAuthor => "Kxnrl";

    private readonly ILogger<AdminFlatFile> _logger;
    private readonly IClientManager         _clientManager;
    private readonly string                 _configPath;

    public AdminFlatFile(ISharedSystem sharedSystem,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 coreConfiguration,
        bool                           hotReload)
    {
        _logger        = sharedSystem.GetLoggerFactory().CreateLogger<AdminFlatFile>();
        _clientManager = sharedSystem.GetClientManager();
        _configPath    = Path.Combine(sharpPath, "configs", "admins.json");
    }

    public bool Init()
        => true;

    public void PostInit()
    {
        _clientManager.InstallClientListener(this);

        ReadAdminFile();
    }

    public void Shutdown()
    {
        _clientManager.RemoveClientListener(this);
    }

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

    public void OnAdminCacheReload()
        => ReadAdminFile();

    private void ReadAdminFile()
    {
        if (!File.Exists(_configPath))
        {
            return;
        }

        try
        {
            ReadFromFile(_configPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to read the admin configs");
        }
    }

    private void ReadFromFile(string path)
    {
        var text = File.ReadAllText(path, Encoding.UTF8);

        var json = JsonSerializer.Deserialize<Dictionary<SteamID, AdminInfo>>(text)
                   ?? throw new InvalidDataException($"Invalid admin configs: {text}");

        foreach (var (identity, info) in json)
        {
            if (!identity.IsValidUserId())
            {
                _logger.LogWarning("Invalid admin section<{identity}>: {@info}", identity, info);

                continue;
            }

            var immunity = (byte) int.Clamp(info.Immunity.GetValueOrDefault(), 0, byte.MaxValue);

            var admin = _clientManager.FindAdmin(identity) ?? _clientManager.CreateAdmin(identity, info.Name, immunity);

            if (info.Permissions is { } permissions)
            {
                foreach (var permission in permissions)
                {
                    if (!admin.HasPermission(permission))
                    {
                        admin.AddPermission(permission);
                    }
                }
            }
        }
    }
}

// ReSharper disable once ClassNeverInstantiated.Local
file record AdminInfo(
    [property: JsonRequired]
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("immunity")]
    int? Immunity,
    [property: JsonPropertyName("permissions")]
    HashSet<string>? Permissions);