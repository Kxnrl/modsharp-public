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
using Sharp.Modules.CommandCenter.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using static Sharp.Shared.Managers.IClientManager;

namespace Sharp.Modules.CommandCenter;

internal sealed class CommandRegistry : ICommandRegistry
{
    private readonly string                   _identity;
    private readonly CommandCenter            _self;
    private readonly IClientManager           _clientManager;
    private readonly ILogger<CommandRegistry> _logger;

    private readonly List<CommandListenerInfo> _hookCommands    = [];
    private readonly List<ClientCommandInfo>   _clientCommands  = [];
    private readonly List<ConsoleCommandInfo>  _consoleCommands = [];
    private readonly List<GenericCommandInfo>  _genericCommands = [];
    private readonly IConVarManager            _conVarManager;

    public CommandRegistry(string identity, CommandCenter self, ISharedSystem sharedSystem)
    {
        _identity      = identity;
        _self          = self;
        _logger        = sharedSystem.GetLoggerFactory().CreateLogger<CommandRegistry>();
        _clientManager = sharedSystem.GetClientManager();
        _conVarManager = sharedSystem.GetConVarManager();
    }

    public void RegisterClientCommand(string command, Action<IGameClient, StringCommand> call)
        => RegisterClientCommand(command,
                                 (client, stringCommand) =>
                                 {
                                     call(client, stringCommand);

                                     return ECommandAction.Handled;
                                 });

    private void RegisterClientCommand(string command, DelegateClientCommand call)
    {
        if (_self.IsCommandExists(command))
        {
            _logger.LogWarning("Command `{Name}` has already registered.", command);

            return;
        }

        var info = new ClientCommandInfo(command, CommandCenter.GetStripPrefixCommand(command), call);
        _clientManager.InstallCommandCallback(info.StripPrefixCommand, info.Function);
        _clientCommands.Add(info);
        _self.AddRegisteredCommand(_identity, info.Command);
    }

    public void RegisterServerCommand(string command,
        Action<StringCommand>                call,
        string                               description = "",
        bool                                 addPrefix   = true)
        => RegisterServerCommand(command,
                                 stringCommand =>
                                 {
                                     call(stringCommand);

                                     return ECommandAction.Handled;
                                 },
                                 description,
                                 addPrefix);

    public void RegisterServerCommand(string command, Action call, string description = "", bool addPrefix = true)
        => RegisterServerCommand(command, _ => { call(); }, description, addPrefix);

    private void RegisterServerCommand(string command,
        Func<StringCommand, ECommandAction>   call,
        string                                description = "",
        bool                                  addPrefix   = true)
    {
        if (_self.IsCommandExists(command))
        {
            _logger.LogWarning("Command `{Name}` has already registered.", command);

            return;
        }

        var info = new ConsoleCommandInfo(command,
                                          CommandCenter.GetAddPrefixCommand(command),
                                          addPrefix,
                                          (_, stringCommand) => call(stringCommand));

        _conVarManager.CreateServerCommand(info.AddPrefix ? info.AddPrefixCommand : info.Command, info.OnServerCommand);
        _consoleCommands.Add(info);
        _self.AddRegisteredCommand(_identity, info.Command);
    }

    public void RegisterGenericCommand(string command, Action<IGameClient?, StringCommand> call, string description = "")
        => RegisterGenericCommand(command,
                                  (client, stringCommand) =>
                                  {
                                      call(client, stringCommand);

                                      return ECommandAction.Handled;
                                  },
                                  description);

    private void RegisterGenericCommand(string            command,
        Func<IGameClient?, StringCommand, ECommandAction> call,
        string                                            description = "")
    {
        if (_self.IsCommandExists(command))
        {
            _logger.LogWarning("Command `{Name}` has already registered.", command);

            return;
        }

        var info = new GenericCommandInfo(command,
                                          CommandCenter.GetAddPrefixCommand(command),
                                          CommandCenter.GetStripPrefixCommand(command),
                                          call);

        _clientManager.InstallCommandCallback(info.StripPrefixCommand, info.OnClientCommand);
        _conVarManager.CreateServerCommand(info.AddPrefixCommand, info.OnServerCommand, description);
        _genericCommands.Add(info);
        _self.AddRegisteredCommand(_identity, info.Command);
    }

    public void RegisterConsoleCommand(string command,
        Action<IGameClient?, StringCommand>   callback,
        bool                                  addPrefix = true)
        => RegisterConsoleCommand(command,
                                  (client, stringCommand) =>
                                  {
                                      callback(client, stringCommand);

                                      return ECommandAction.Handled;
                                  },
                                  addPrefix);

    private void RegisterConsoleCommand(string            command,
        Func<IGameClient?, StringCommand, ECommandAction> callback,
        bool                                              addPrefix = true)
    {
        if (_self.IsCommandExists(command))
        {
            _logger.LogWarning("Command `{Name}` has already registered.", command);

            return;
        }

        var info = new ConsoleCommandInfo(command, CommandCenter.GetAddPrefixCommand(command), addPrefix, callback);

        _conVarManager.CreateConsoleCommand(info.AddPrefix ? info.AddPrefixCommand : info.Command,
                                            info.OnConsoleCommand);

        _consoleCommands.Add(info);
        _self.AddRegisteredCommand(_identity, info.Command);
    }

    public void AddCommandListener(string commandName, DelegateClientCommand callback)
    {
        var info = new CommandListenerInfo(commandName, callback);

        _clientManager.InstallCommandListener(info.Command, info.Function);
        _hookCommands.Add(info);
    }

    public void Clear()
    {
        foreach (var info in _clientCommands)
        {
            _clientManager.RemoveCommandCallback(info.StripPrefixCommand, info.Function);
        }

        foreach (var info in _genericCommands)
        {
            _conVarManager.ReleaseCommand(info.AddPrefixCommand);
            _clientManager.RemoveCommandCallback(info.StripPrefixCommand, info.OnClientCommand);
        }

        foreach (var info in _consoleCommands)
        {
            _conVarManager.ReleaseCommand(info.AddPrefix ? info.AddPrefixCommand : info.Command);
        }

        foreach (var info in _hookCommands)
        {
            _clientManager.RemoveCommandListener(info.Command, info.Function);
        }
    }

    private record CommandListenerInfo(string Command, DelegateClientCommand Function);

    private record ClientCommandInfo(string Command, string StripPrefixCommand, DelegateClientCommand Function);

    private record GenericCommandInfo(
        string                                            Command,
        string                                            AddPrefixCommand,
        string                                            StripPrefixCommand,
        Func<IGameClient?, StringCommand, ECommandAction> Function)
    {
        public ECommandAction OnClientCommand(IGameClient client, StringCommand command)
            => Function(client, command);

        public ECommandAction OnServerCommand(StringCommand command)
            => Function(null, command);
    }

    private record ConsoleCommandInfo(
        string                                            Command,
        string                                            AddPrefixCommand,
        bool                                              AddPrefix,
        Func<IGameClient?, StringCommand, ECommandAction> Function)
    {
        public ECommandAction OnConsoleCommand(IGameClient? client, StringCommand command)
            => Function(client, command);

        public ECommandAction OnServerCommand(StringCommand command)
            => Function(null, command);
    }
}
