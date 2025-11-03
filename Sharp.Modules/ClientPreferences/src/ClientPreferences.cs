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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.ClientPreferences.Core.Storages;
using Sharp.Modules.ClientPreferences.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Helpers;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.ClientPreferences.Core;

public sealed class ClientPreferences : IModSharpModule, IClientListener, IClientPreference
{
    public string DisplayName   => "ClientPrefs";
    public string DisplayAuthor => "Kxnrl";

    private readonly ILogger<ClientPreferences> _logger;
    private readonly IModSharp                  _modSharp;
    private readonly ISharpModuleManager        _modules;
    private readonly IStorage                   _driver;
    private readonly IConfiguration             _configuration;

    private readonly CancellationTokenSource _source;
    private readonly ConfigurationWatcher    _configurationWatcher;
    private readonly string                  _connectionString;

    private readonly List<Action<IGameClient>>         _loadCallbacks;
    private readonly Dictionary<SteamID, CookieBucket> _cookieStorage;

    public ClientPreferences(ISharedSystem sharedSystem,
        string                             dllPath,
        string                             sharpPath,
        Version                            version,
        IConfiguration                     configuration,
        bool                               hotReload)
    {
        var loggerFactory = sharedSystem.GetLoggerFactory();

        _logger        = loggerFactory.CreateLogger<ClientPreferences>();
        _modSharp      = sharedSystem.GetModSharp();
        _modules       = sharedSystem.GetSharpModuleManager();
        _configuration = configuration;

        var fillConnectionString = configuration.GetConnectionString("ClientPreferences")
                                   ?? throw new KeyNotFoundException("Missing 'ClientPreferences' in connection string.");

        var chunks
            = fillConnectionString.Split("://", 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (chunks.Length != 2)
        {
            throw new System.IO.InvalidDataException("Missing type of driver in connection string");
        }

        if (!Enum.TryParse(chunks[0], true, out StorageType driver))
        {
            throw new System.IO.InvalidDataException("Invalid driver type in connection string.");
        }

        var connectionString = chunks[1];
        var source           = new CancellationTokenSource();

        _driver = driver switch
        {
            StorageType.LiteDb => new LiteDbStorage(loggerFactory, source, connectionString, sharpPath),
            StorageType.Resp   => new RespStorage(loggerFactory, source, connectionString),
            StorageType.MySql  => new MySqlStorage(loggerFactory, source, connectionString),
            StorageType.Http   => new RestStorage(loggerFactory, source, connectionString),
            _                  => throw new NotSupportedException($"Storage type {driver} is not supported"),
        };

        _loadCallbacks = [];
        _cookieStorage = [];

        _source               = source;
        _connectionString     = fillConnectionString;
        _configurationWatcher = new ConfigurationWatcher(configuration, OnConfigReload, source.Token);
    }

    private void OnConfigReload()
    {
        var connectionString = _configuration.GetConnectionString("ClientPreferences") ?? string.Empty;

        if (_connectionString.Equals(connectionString))
        {
            return;
        }

        _logger.LogWarning(
            "Configuration changes detected. but we can't host reload the storage, if you really want to change the storage, please restart server.");
    }

#region IModSharpModule

    public bool Init()
    {
        _modules.RegisterSharpModuleInterface(this, IClientPreference.Identity, this);

        return true;
    }

    public void PostInit()
    {
        _driver.Init();
    }

    public void Shutdown()
    {
        try
        {
            _source.Cancel();
        }
        finally
        {
            _driver.Shutdown();
            _source.Dispose();
        }
    }

#endregion

#region IClientListener

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

    public void OnClientPostAdminCheck(IGameClient client)
    {
        if (client.IsFakeClient || client.IsHltv)
        {
            return;
        }

        var identity = client.SteamId;

        if (!identity.IsValidUserId())
        {
            return;
        }

        _modSharp.PushTimer(() =>
                            {
                                if (!client.IsValid)
                                {
                                    return;
                                }

                                Task.Run(() => LoadClient(client, identity), _source.Token);
                            },
                            1,
                            GameTimerFlags.StopOnMapEnd);
    }

    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (client.IsFakeClient || client.IsHltv)
        {
            return;
        }

        var identity = client.SteamId;

        if (!identity.IsValidUserId())
        {
            return;
        }

        if (!_cookieStorage.Remove(identity, out var storage))
        {
            return;
        }

        if (!storage.Dirty)
        {
            return;
        }

        Task.Run(() => _driver.SaveUserCookie(identity, storage.GetModels()), _source.Token);
    }

    private async Task LoadClient(IGameClient client, SteamID identity)
    {
        const int maxRetries = 3;

        for (var i = 1; i <= maxRetries; i++)
        {
            try
            {
                var cookies = await _driver.LoadUserCookie(identity);

                var cookieMap = new Dictionary<string, CookieItem>();

                foreach (var cookie in cookies)
                {
                    cookieMap[cookie.Key] = cookie.Type switch
                    {
                        CookieValueType.String => new CookieItem(cookie.String),
                        CookieValueType.Number =>
                            new CookieItem(cookie.Number.GetValueOrDefault()),
                        CookieValueType.Double =>
                            new CookieItem(cookie.Double.GetValueOrDefault()),
                        _ => throw new NotSupportedException($"{cookie.Type} is not support"),
                    };
                }

                _modSharp.InvokeAction(() => OnClientLoaded(client, identity, cookieMap));

                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while loading cookies for {s}, attempt {i}/{m}",
                                 identity,
                                 i,
                                 maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(1), _source.Token);
            }
        }
    }

    private void OnClientLoaded(IGameClient client, SteamID identity, Dictionary<string, CookieItem> cookies)
    {
        if (!client.IsValid)
        {
            return;
        }

        _cookieStorage[identity] = new CookieBucket(cookies);

        foreach (var callback in _loadCallbacks)
        {
            try
            {
                callback.Invoke(client);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling OnCookieLoad for {s}",
                                 identity);
            }
        }
    }

#endregion

#region IClientPreference

    public void ListenOnLoad(Action<IGameClient> callback)
        => _loadCallbacks.Add(callback);

    public bool IsLoaded(SteamID identity)
        => identity.IsValidUserId()
            ? _cookieStorage.ContainsKey(identity)
            : throw new ArgumentException("Invalid SteamId", nameof(identity));

    public ICookieItem? GetCookie(SteamID identity, string key)
    {
        if (!identity.IsValidUserId())
        {
            throw new ArgumentException("Invalid SteamId", nameof(identity));
        }

        return _cookieStorage.GetValueOrDefault(identity)?.Get(key);
    }

    public bool DeleteCookie(SteamID identity, string key)
    {
        if (!identity.IsValidUserId())
        {
            throw new ArgumentException("Invalid SteamId", nameof(identity));
        }

        return _cookieStorage.TryGetValue(identity, out var bucket) && bucket.Delete(key);
    }

    public ICookieItem SetCookie(SteamID identity, string key, bool value)
        => SetCookie(identity, key, value ? 1L : 0L);

    public ICookieItem SetCookie(SteamID identity, string key, long value)
    {
        if (!identity.IsValidUserId())
        {
            throw new ArgumentException("Invalid SteamId", nameof(identity));
        }

        if (!_cookieStorage.TryGetValue(identity, out var bucket))
        {
            throw new InvalidOperationException($"Client {identity} has not been loaded yet");
        }

        var item = new CookieItem(value);

        bucket.Set(key, item);

        return item;
    }

    public ICookieItem SetCookie(SteamID identity, string key, double value)
    {
        if (!identity.IsValidUserId())
        {
            throw new ArgumentException("Invalid SteamId", nameof(identity));
        }

        if (!_cookieStorage.TryGetValue(identity, out var bucket))
        {
            throw new InvalidOperationException($"Client {identity} has not been loaded yet");
        }

        var item = new CookieItem(value);

        bucket.Set(key, item);

        return item;
    }

    public ICookieItem SetCookie(SteamID identity, string key, string value)
    {
        if (!identity.IsValidUserId())
        {
            throw new ArgumentException("Invalid SteamId", nameof(identity));
        }

        if (!_cookieStorage.TryGetValue(identity, out var bucket))
        {
            throw new InvalidOperationException($"Client {identity} has not been loaded yet");
        }

        var item = new CookieItem(value);

        bucket.Set(key, item);

        return item;
    }

    public ICookieItem SetCookie<T>(SteamID identity, string key, T value) where T : ISerializableCookieItem<T>
        => SetCookie(identity, key, value.Serialize());

#endregion
}
