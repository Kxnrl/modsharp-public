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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.Helpers;
using Sharp.Core.Objects;
using Sharp.Shared.Enums;
using Sharp.Shared.Helpers;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;
using Forward = Sharp.Core.Bridges.Forwards.Client;
using Native = Sharp.Core.Bridges.Natives.Client;

namespace Sharp.Core.Managers;

internal interface ICoreClientManager : IClientManager;

// ReSharper disable ForCanBeConvertedToForeach
internal class ClientManager : ICoreClientManager
{
    private record QueryConVarInfo(
        string                                                      Name,
        SteamID                                                     SteamId,
        Action<IGameClient, QueryConVarValueStatus, string, string> Callback);

    private readonly ILogger<ClientManager> _logger;
    private readonly IConfiguration         _configuration;
    private readonly ConfigurationWatcher   _configurationWatcher;

    private readonly List<IClientListener>                                     _listeners;
    private readonly Queue<GameClient>                                         _adminRunCheckQueue;
    private readonly Dictionary<string, IClientManager.DelegateClientCommand?> _commandHooks;
    private readonly Dictionary<string, IClientManager.DelegateClientCommand?> _commandListeners;
    private readonly Queue<Action>                                             _commandQueue;
    private readonly Dictionary<int, QueryConVarInfo>                          _queryConVarInfos;
    private readonly Dictionary<SteamID, Admin>                                _admins;
    private readonly HashSet<char>                                             _publicTriggers;
    private readonly HashSet<char>                                             _silentTriggers;

    private static volatile bool _sReloadAdmin;
    private static volatile int  _sQueryCookie;

    public ClientManager(ILogger<ClientManager> logger, IConfiguration configuration, IShutdownMonitor shutdown)
    {
        _logger               = logger;
        _configuration        = configuration;
        _configurationWatcher = new ConfigurationWatcher(configuration, LoadCommandPrefix, shutdown.Token);

        _listeners          = [];
        _adminRunCheckQueue = [];
        _commandHooks       = new Dictionary<string, IClientManager.DelegateClientCommand?>(StringComparer.OrdinalIgnoreCase);
        _commandListeners   = new Dictionary<string, IClientManager.DelegateClientCommand?>(StringComparer.OrdinalIgnoreCase);
        _commandQueue       = [];
        _queryConVarInfos   = [];
        _admins             = [];
        _publicTriggers     = [];
        _silentTriggers     = [];

        Forward.OnClientConnected                += OnClientConnected;
        Forward.OnClientActive                   += OnClientPutInServer;
        Forward.OnClientDisconnecting            += OnClientDisconnecting;
        Forward.OnClientDisconnected             += OnClientDisconnected;
        Forward.OnClientSettingChanged           += OnClientSettingChanged;
        Forward.OnClientStringCommand            += OnClientStringCommand;
        Forward.OnClientSayCommand               += OnClientSayCommand;
        Forward.OnClientConsoleCommand           += OnClientConsoleCommand;
        Forward.OnClientQueryConVar              += OnClientQueryConVar;
        Bridges.Forwards.Game.OnGameFramePre     += OnGameFramePre;
        Bridges.Forwards.Game.OnGameFramePost    += OnGameFramePost;
        Bridges.Forwards.Game.OnGamePreShutdown  += OnGameShutdown;
        Bridges.Forwards.Game.OnGameShutdown     += OnGameShutdown;
        Bridges.Forwards.Game.OnStartupServerPre += OnServerReset;

        configuration.GetReloadToken().RegisterChangeCallback(_ => LoadCommandPrefix(), null);
        LoadCommandPrefix();
    }

    private void OnClientConnected(IntPtr ptr)
    {
        var client = GameClient.Create(ptr)!;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnClientConnected(client);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnClientConnected),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private void OnClientPutInServer(IntPtr ptr)
    {
        var client = GameClient.Create(ptr)!;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnClientPutInServer(client);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnClientPutInServer),
                                 _listeners[i].GetType().Name);
            }
        }

        _adminRunCheckQueue.Enqueue(client);
    }

    private void OnClientDisconnecting(IntPtr ptr, NetworkDisconnectionReason reason)
    {
        var client = GameClient.Create(ptr)!;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnClientDisconnecting(client, reason);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnClientDisconnecting),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private void OnClientDisconnected(IntPtr ptr, NetworkDisconnectionReason reason)
    {
        var client = GameClient.Create(ptr)!;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnClientDisconnected(client, reason);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnClientDisconnected),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private void OnClientSettingChanged(IntPtr ptr)
    {
        var client = GameClient.Create(ptr)!;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnClientSettingChanged(client);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnClientSettingChanged),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private ECommandAction OnClientStringCommand(IntPtr ptr, string rawCommand, bool chatTrigger, string? arguments)
    {
        var client = GameClient.Create(ptr)!;

        if (!_commandHooks.TryGetValue(rawCommand, out var callbacks) || callbacks is null)
        {
            return chatTrigger ? ECommandAction.Skipped : ECommandAction.Stopped;
        }

        return InvokeCommandCallback(client, callbacks, rawCommand, chatTrigger, arguments);
    }

    private ECommandAction OnClientSayCommand(IntPtr ptr, bool teamOnly, string chatCommand, string message)
    {
        var client = GameClient.Create(ptr);

        if (client == null || string.IsNullOrWhiteSpace(message))
        {
            return ECommandAction.Stopped;
        }

        var isCommand = false;

        // is chat trigger
        if (TryParseChatCommand(message, out var silent, out var command, out var arguments)
            && _commandHooks.TryGetValue(command, out var callbacks)
            && callbacks is not null)
        {
            isCommand = true;

            _commandQueue.Enqueue(() =>
            {
                if (client.IsValid)
                {
                    InvokeCommandCallback(client, callbacks, command, true, arguments);
                }
            });
        }

        var action = ECommandAction.Skipped;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                var low = _listeners[i].OnClientSayCommand(client, teamOnly, isCommand, chatCommand, message);

                if (low > action)
                {
                    action = low;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnClientSayCommand),
                                 _listeners[i].GetType().Name);
            }
        }

        return silent ? ECommandAction.Stopped : action;
    }

    private ECommandAction OnClientConsoleCommand(nint ptr, string rawCommand, string? arguments)
    {
        var client = GameClient.Create(ptr)!;

        if (!_commandListeners.TryGetValue(rawCommand, out var callbacks) || callbacks is null)
        {
            return ECommandAction.Skipped;
        }

        try
        {
            var context = new StringCommand(rawCommand, false, arguments);

            return callbacks.Invoke(client, context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while calling command callback: {command}", rawCommand);

            return ECommandAction.Skipped;
        }
    }

    private void OnClientQueryConVar(nint ptr, int cookie, QueryConVarValueStatus status, string name, string value)
    {
        if (!_queryConVarInfos.Remove(cookie, out var info) || GameClient.Create(ptr) is not { } client)
        {
            return;
        }

        if (client.SteamId != info.SteamId || !name.Equals(info.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            info.Callback(client, status, name, value);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while calling QueryConVar callback");
        }
    }

    private void OnGameFramePre(bool simulating, bool first, bool last)
    {
        if (!first)
        {
            return;
        }

        if (_commandQueue.Count == 0)
        {
            return;
        }

        try
        {
            while (_commandQueue.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while calling command queue!");
        }
    }

    private void OnGameFramePost(bool simulating, bool first, bool last)
    {
        // must be end of the frame!
        if (!last)
        {
            return;
        }

        if (_sReloadAdmin)
        {
            for (var i = 0; i < _listeners.Count; i++)
            {
                try
                {
                    _listeners[i].OnAdminCacheReload();
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                                     "An error occurred while calling listener<{s}> {name}",
                                     "OnAdminCacheReload",
                                     _listeners[i].GetType().Name);
                }
            }

            _sReloadAdmin = false;
        }

        if (_adminRunCheckQueue.Count == 0)
        {
            return;
        }

        var back = new List<GameClient>();

        while (_adminRunCheckQueue.TryDequeue(out var client))
        {
            if (!client.IsValid)
            {
                continue;
            }

            if (client.IsAuthenticated)
            {
                var c = client;

                var block = false;

                for (var i = 0; i < _listeners.Count; i++)
                {
                    try
                    {
                        if (_listeners[i].OnClientPreAdminCheck(c))
                        {
                            block = true;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,
                                         "An error occurred while calling listener<{s}> {name}",
                                         "OnClientPreAdminCheck",
                                         _listeners[i].GetType().Name);
                    }
                }

                if (block)
                {
                    _logger.LogWarning("Blocked {client}<{steamId}> as AdminCheck", c.Name, c.SteamId);

                    continue;
                }

                for (var i = 0; i < _listeners.Count; i++)
                {
                    try
                    {
                        _listeners[i].OnClientPostAdminCheck(c);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,
                                         "An error occurred while calling listener<{s}> {name}",
                                         "OnClientPostAdminCheck",
                                         _listeners[i].GetType().Name);
                    }
                }
            }
            else
            {
                back.Add(client);
            }
        }

        if (back.Count > 0)
        {
            back.ForEach(_adminRunCheckQueue.Enqueue);
        }
    }

    private void OnGameShutdown()
    {
        _commandQueue.Clear();
        _adminRunCheckQueue.Clear();
    }

    private void OnServerReset()
    {
        _queryConVarInfos.Clear();
    }

    public void InstallClientListener(IClientListener listener)
    {
        if (listener.ListenerVersion != IClientListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_listeners.Any(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _listeners.Add(listener);
        _listeners.Sort((x, y) => y.ListenerPriority.CompareTo(x.ListenerPriority));
    }

    public void RemoveClientListener(IClientListener listener)
    {
        if (!_listeners.Contains(listener))
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _listeners.Remove(listener);
    }

    public void InstallCommandCallback(string rawCommand, IClientManager.DelegateClientCommand callback)
    {
        var command = rawCommand.ToLower();

        if (_commandHooks.ContainsKey(command))
        {
            _commandHooks[command] += callback;
        }
        else
        {
            _commandHooks[command] = callback;
            Native.AddCommandCallback(command);
        }
    }

    public void RemoveCommandCallback(string rawCommand, IClientManager.DelegateClientCommand callback)
    {
        var command = rawCommand.ToLower();

        if (!_commandHooks.TryGetValue(command, out var callbacks))
        {
            throw new InvalidOperationException($"Hook <{command}> is not exists!");
        }

        callbacks -= callback;

        if (callbacks is null)
        {
            _commandHooks.Remove(command);
        }
    }

    public void InstallCommandListener(string rawCommand, IClientManager.DelegateClientCommand callback)
    {
        var command = rawCommand.ToLower();

        if (_commandListeners.ContainsKey(command))
        {
            _commandListeners[command] += callback;
        }
        else
        {
            _commandListeners[command] = callback;
            Native.AddCommandListener(command);
        }
    }

    public void RemoveCommandListener(string rawCommand, IClientManager.DelegateClientCommand callback)
    {
        var command = rawCommand.ToLower();

        if (!_commandListeners.TryGetValue(command, out var callbacks))
        {
            throw new InvalidOperationException($"Listener <{command}> is not exists!");
        }

        callbacks -= callback;

        if (callbacks is null)
        {
            _commandListeners.Remove(command);
        }
    }

    public IGameClient? GetGameClient(PlayerSlot slot)
        => GameClient.Create(Native.GetClientBySlot(slot));

    public IGameClient? GetGameClient(UserID userId)
        => GameClient.Create(Native.GetClientByUserId(userId));

    public IGameClient? GetGameClient(SteamID steamId)
        => GameClient.Create(Native.GetClientBySteamId(steamId));

    public void KickClient(IGameClient client, string internalReason, NetworkDisconnectionReason networkDisconnectionReason)
        => Native.KickClient(client, internalReason, networkDisconnectionReason);

    public unsafe int QueryConVar(IGameClient                       client,
        string                                                      name,
        Action<IGameClient, QueryConVarValueStatus, string, string> callback)
    {
        if (client.IsFakeClient)
        {
            throw new InvalidOperationException("You can not handle this with BOT");
        }

        _sQueryCookie++;

        var msg = new CSVCMsg_GetCvarValue { Cookie = _sQueryCookie, CvarName = name };

        var bytes = msg.ToByteArray();
        var size  = bytes.Length;

        var filter = new RecipientFilter(client.Slot);

        fixed (byte* pBytes = bytes)
        {
            Net.SendNetMessage(&filter, nameof(CSVCMsg_GetCvarValue), pBytes, size);
        }

        _queryConVarInfos.Add(_sQueryCookie, new QueryConVarInfo(name, client.SteamId, callback));

        return _sQueryCookie;
    }

    public void ReloadAdmins()
    {
        if (_sReloadAdmin)
        {
            return;
        }

        _sReloadAdmin = true;

        _admins.Clear();
    }

    public IAdmin? FindAdmin(SteamID identity)
        => _admins.GetValueOrDefault(identity);

    public IAdmin? FindAdmin(string name)
    {
        foreach (var (_, admin) in _admins)
        {
            if (admin.Name.Equals(name))
            {
                return admin;
            }
        }

        return null;
    }

    public IAdmin CreateAdmin(SteamID identity, string name, byte immunity = 0)
    {
        if (FindAdmin(identity) is not null)
        {
            throw new InvalidOperationException($"Admin identity {identity} is already created");
        }

        if (FindAdmin(name) is not null)
        {
            throw new InvalidOperationException($"Admin name {identity} is already in use");
        }

        var admin = new Admin(name, identity, immunity);

        return !_admins.TryAdd(identity, admin)
            ? throw new InvalidOperationException($"Admin {identity}::{name} is already created")
            : admin;
    }

    public void DeleteAdmin(IAdmin admin)
    {
        if (!_admins.Remove(admin.Identity, out _))
        {
            throw new KeyNotFoundException($"Admin [{admin.Identity}] is not an admin");
        }
    }

    public IReadOnlyCollection<IAdmin> GetAdmins()
        => _admins.Values;

    private ECommandAction InvokeCommandCallback(IGameClient client,
        IClientManager.DelegateClientCommand                 callbacks,
        string                                               command,
        bool                                                 chatTrigger,
        string?                                              arguments)
    {
        try
        {
            var context = new StringCommand(command, chatTrigger, arguments);

            return callbacks.Invoke(client, context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while calling command callback: {command}", command);

            return ECommandAction.Stopped;
        }
    }

    private unsafe bool TryParseChatCommand(string input,
        out                     bool               silent,
        [NotNullWhen(true)] out string?            command,
        out                     string?            arguments)
    {
        silent    = false;
        command   = null;
        arguments = null;

        var memory = input.AsSpan();

        if (memory.Length is < 2 or >= 256)
        {
            return false;
        }

        silent = _silentTriggers.Contains(memory[0]);

        if (!silent && !_publicTriggers.Contains(memory[0]))
        {
            return false;
        }

        memory = memory[1..];

        var spacer = memory.IndexOf(' ');

        var commandSpan = spacer == -1 ? memory : memory[..spacer];
        var buffer      = stackalloc char[commandSpan.Length];

        for (var i = 0; i < commandSpan.Length; i++)
        {
            buffer[i] = char.ToLower(commandSpan[i]);
        }

        command = new string(buffer, 0, commandSpan.Length);

        if (spacer != -1 && memory.Length > spacer + 1)
        {
            arguments = memory[(spacer + 1)..].ToString();
        }

        return true;
    }

    private void LoadCommandPrefix()
    {
        _publicTriggers.Clear();
        _silentTriggers.Clear();

        var section = _configuration.GetSection("ChatCommand.TriggerPrefixes");

        if (section.GetSection("Public").Get<List<string>>() is { } publicList)
        {
            foreach (var prefix in publicList)
            {
                if (string.IsNullOrWhiteSpace(prefix) || prefix.Length != 1 || !char.IsAscii(prefix[0]))
                {
                    _logger.LogWarning(
                        "Chat command trigger prefix <Public> '{prefix}' must be a single ascii character, skipped it",
                        prefix);

                    continue;
                }

                _publicTriggers.Add(prefix[0]);
            }
        }
        else
        {
            _logger.LogWarning(
                "'ChatCommand.TriggerPrefixes/Public' is invalid or empty, chat command public trigger will not work");
        }

        if (section.GetSection("Silent").Get<List<string>>() is { } silentList)
        {
            foreach (var prefix in silentList)
            {
                if (string.IsNullOrWhiteSpace(prefix) || prefix.Length != 1 || !char.IsAscii(prefix[0]))
                {
                    _logger.LogWarning(
                        "Chat command trigger prefix <Silent> '{prefix}' must be a single ascii character, skipped it",
                        prefix);

                    continue;
                }

                _silentTriggers.Add(prefix[0]);
            }
        }
        else
        {
            _logger.LogWarning(
                "'ChatCommand.TriggerPrefixes/Silent' is invalid or empty, chat command silent trigger will not work");
        }
    }
}
