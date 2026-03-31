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
using Sharp.Modules.CommandCenter.Shared;
using Sharp.Shared;

namespace Sharp.Modules.CommandCenter;

internal sealed class CommandCenter : IModSharpModule, ICommandCenter
{
    private readonly ISharedSystem          _shared;
    private readonly ILogger<CommandCenter> _logger;

    private readonly Dictionary<string, CommandRegistry> _registries;
    private readonly Dictionary<string, HashSet<string>> _registerCommands;

    public CommandCenter(ISharedSystem sharedSystem,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 coreConfiguration,
        bool                           hotReload)
    {
        _shared = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<CommandCenter>();

        _registries       = new Dictionary<string, CommandRegistry>(StringComparer.OrdinalIgnoreCase);
        _registerCommands = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    }

#region IModSharpModule

    public bool Init()
        => true;

    public void PostInit()
    {
        _shared.GetSharpModuleManager()
               .RegisterSharpModuleInterface<ICommandCenter>(this, ICommandCenter.Identity, this);
    }

    public void OnLibraryDisconnect(string name)
    {
        RemoveRegisteredCommands(name);
        RemoveRegistry(name);
    }

    public void Shutdown()
    {
        foreach (var registry in _registries.Values)
        {
            registry.Clear();
        }

        _registries.Clear();
        _registerCommands.Clear();
    }

    string IModSharpModule.DisplayName => "Sharp.Modules.CommandCenter";

    string IModSharpModule.DisplayAuthor => "laper32";

#endregion

#region ICommandManager

    public ICommandRegistry GetRegistry(string moduleIdentity)
    {
        if (_registries.TryGetValue(moduleIdentity, out var registry))
        {
            return registry;
        }

        _registerCommands[moduleIdentity] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        registry                          = new CommandRegistry(moduleIdentity, this, _shared);
        _registries[moduleIdentity]       = registry;

        return registry;
    }

#endregion

    public bool IsCommandExists(string command)
    {
        foreach (var (_, value) in _registerCommands)
        {
            if (value.Contains(command))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     获取经过ms_装饰后的指令，这个一般只有服务端控制台指令需要
    /// </summary>
    /// <param name="originalCommand"></param>
    /// <param name="addPrefix"></param>
    public static string GetAddPrefixCommand(string originalCommand, bool addPrefix = true)
    {
        string actualRegisterCommand;

        if (addPrefix)
        {
            actualRegisterCommand = !originalCommand.StartsWith("ms_") ? $"ms_{originalCommand}" : originalCommand;
        }
        else
        {
            actualRegisterCommand = originalCommand;
        }

        return actualRegisterCommand;
    }

    /// <summary>
    ///     判断是否有ms_前缀
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private static bool HasPrefix(string command)
        => command.StartsWith("ms_", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     获取移除ms_装饰后的指令，这个一般只有游戏内指令需要
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public static string GetStripPrefixCommand(string command)
        => HasPrefix(command)
            ? command[3..]
            : // ms_ => 3 char
            command;

    public void AddRegisteredCommand(string identity, string command)
    {
        if (_registerCommands.TryGetValue(identity, out var set))
        {
            set.Add(command);
        }

        set                         = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { command };
        _registerCommands[identity] = set;
    }

    private void RemoveRegistry(string identity)
    {
        if (!_registries.TryGetValue(identity, out var value))
        {
            return;
        }

        value.Clear();
        _registries.Remove(identity);
    }

    private void RemoveRegisteredCommands(string identity)
    {
        if (!_registerCommands.TryGetValue(identity, out var set))
        {
            return;
        }

        set.Clear();

        _registerCommands.Remove(identity);
    }
}
