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
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Abstractions;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using CLCommandCallback = Sharp.Extensions.CommandManager.ICommandManager.DelegateOnClientCommand;
using SVCommandCallback = Sharp.Extensions.CommandManager.ICommandManager.DelegateOnServerCommand;

namespace Sharp.Extensions.CommandManager;

internal class CommandManager : ICommandManager, ISharpExtension
{
    private const string EmptyPermission = "<EmptyPermission>";

    private readonly ILogger<CommandManager> _logger;
    private readonly IModSharp               _modSharp;
    private readonly IClientManager          _clientManager;
    private readonly IConVarManager          _conVarManager;

    private readonly Dictionary<string, Dictionary<string, CLCommandCallback?>> _clientCommandHooks;
    private readonly Dictionary<string, SVCommandCallback?>                     _serverCommandHooks;

    public CommandManager(ISharedSystem sharedSystem)
    {
        _logger        = sharedSystem.GetLoggerFactory().CreateLogger<CommandManager>();
        _modSharp      = sharedSystem.GetModSharp();
        _clientManager = sharedSystem.GetClientManager();
        _conVarManager = sharedSystem.GetConVarManager();

        var comparer = StringComparer.OrdinalIgnoreCase;

        _clientCommandHooks = new Dictionary<string, Dictionary<string, CLCommandCallback?>>(comparer);
        _serverCommandHooks = new Dictionary<string, SVCommandCallback?>(comparer);
    }

    public void Load()
    {
    }

    public void Shutdown()
    {
        foreach (var c in _clientCommandHooks.Keys)
        {
            _clientManager.RemoveCommandCallback(c, OnClientCommand);
        }

        foreach (var c in _serverCommandHooks.Keys)
        {
            _conVarManager.ReleaseCommand(c);
        }
    }

    public void RegisterClientCommand(string command, CLCommandCallback callback)
        => RegisterAdminCommand(command, callback, EmptyPermission);

    public void RegisterAdminCommand(string rawCommand, CLCommandCallback callback, string permission)
    {
        var command = rawCommand.ToLower();

        if (!_clientCommandHooks.ContainsKey(command))
        {
            _clientCommandHooks[command] = new Dictionary<string, CLCommandCallback?>(StringComparer.OrdinalIgnoreCase);
            _clientManager.InstallCommandCallback(command, OnClientCommand);
        }

        if (!_clientCommandHooks[command].ContainsKey(permission))
        {
            _clientCommandHooks[command][permission] = callback;
        }
        else
        {
            _clientCommandHooks[command][permission] += callback;
        }
    }

    public void RegisterServerCommand(string rawCommand, string description, SVCommandCallback callback)
    {
        var command = rawCommand.ToLower();

        if (!_serverCommandHooks.ContainsKey(command))
        {
            _serverCommandHooks[command] = callback;
            _conVarManager.CreateServerCommand(command, OnServerCommand, description, ConVarFlags.Release);
        }
        else
        {
            _serverCommandHooks[command] += callback;
        }
    }

    private ECommandAction OnClientCommand(IGameClient client, StringCommand command)
    {
        if (!_clientCommandHooks.TryGetValue(command.CommandName, out var commands))
        {
            return ECommandAction.Skipped;
        }

        var admin = client.IsAuthenticated ? _clientManager.FindAdmin(client.SteamId) : null;

        foreach (var (requiredPermission, callback) in commands)
        {
            if (callback is null)
            {
                continue;
            }

            if (requiredPermission.Equals(EmptyPermission))
            {
                InvokeClientCallback(client, command, callback);

                continue;
            }

            if (admin is not null && admin.HasPermission(requiredPermission))
            {
                InvokeClientCallback(client, command, callback);
            }
            else
            {
                if (command.ChatTrigger)
                {
                    _modSharp.PrintChannelFilter(HudPrintChannel.Chat,
                                                 $" {ChatColor.Green}[MS] {ChatColor.White}You have no permission to use this command!",
                                                 new RecipientFilter(client));
                }
                else
                {
                    _modSharp.PrintChannelFilter(HudPrintChannel.Console,
                                                 "[MS]  You have no permission to use this command!",
                                                 new RecipientFilter(client));
                }
            }
        }

        return ECommandAction.Stopped;
    }

    private ECommandAction OnServerCommand(StringCommand command)
    {
        if (!_serverCommandHooks.TryGetValue(command.CommandName, out var callbacks) || callbacks is null)
        {
            return ECommandAction.Skipped;
        }

        callbacks.Invoke(command);

        return ECommandAction.Stopped;
    }

    private void InvokeClientCallback(IGameClient client, StringCommand command, CLCommandCallback callbacks)
    {
        foreach (var callback in callbacks.GetInvocationList())
        {
            try
            {
                ((CLCommandCallback) callback).Invoke(client, command);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling CommandCallback::{s}", command.CommandName);
            }
        }
    }
}
