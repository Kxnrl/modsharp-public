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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Sharp.Core.Objects;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;
using Native = Sharp.Core.Bridges.Natives.Cvar;
using Forward = Sharp.Core.Bridges.Forwards.Cvar;

namespace Sharp.Core.Managers;

internal interface ICoreConVarManager : IConVarManager;

internal class ConVarManager : ICoreConVarManager
{
    private readonly ILogger<ConVarManager> _logger;
    private readonly ICoreAssemblyManager   _assemblyManager;

    private readonly Dictionary<nint /* ConVar Ptr */, IConVarManager.DelegateConVarChange?>                    _conVarHooks;
    private readonly Dictionary<long /* ConCommandHandle */, Func<IGameClient?, StringCommand, ECommandAction>> _commands;
    private readonly Dictionary<long /* ConCommandHandle */, Func<StringCommand, ECommandAction>>               _serverCommands;

    public ConVarManager(ILogger<ConVarManager> logger, ICoreAssemblyManager assemblyManager)
    {
        _logger          = logger;
        _assemblyManager = assemblyManager;
        _conVarHooks     = new Dictionary<nint, IConVarManager.DelegateConVarChange?>();
        _commands        = new Dictionary<long, Func<IGameClient?, StringCommand, ECommandAction>>();
        _serverCommands  = new Dictionary<long, Func<StringCommand, ECommandAction>>();

        Forward.OnConVarChanged     += OnConVarChanged;
        Forward.OnConCommandTrigger += OnConCommandTrigger;

        _assemblyManager.RegisterUnloadCleanup(ClearLeakedRegistrations);

        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        _ = CreateConVar("modsharp_runtime_version",
                         $"{version.Major}.{version.Minor}+dev{version.Build} | .NET " + Environment.Version,
                         "Version of ModSharp Runtime.",
                         ConVarFlags.Replicated | ConVarFlags.Release)
            ?? throw new InvalidOperationException("Could not create core version ConVar");
    }

    private void OnConVarChanged(nint ptr)
    {
        if (!_conVarHooks.TryGetValue(ptr, out var callbacks) || callbacks is null)
        {
            return;
        }

        var conVar = ConVar.Create(ptr);

        if (conVar is null)
        {
            return;
        }

        callbacks.Invoke(conVar);
    }

    public IConVar? FindConVar(string name, bool useIterator = false)
        => ConVar.Create(Native.FindConVar(name, useIterator));

    private void ClearLeakedRegistrations()
    {
        foreach (var key in _conVarHooks.Keys.ToArray())
        {
            var cleaned = _assemblyManager.ClearLeakedMulticast(_conVarHooks[key], "ConVarChangeHook", $"0x{key:X}");

            if (cleaned is not null)
            {
                _conVarHooks[key] = cleaned;

                continue;
            }

            _conVarHooks.Remove(key);
            Native.RemoveChangeHook(key);
        }

        ClearLeakedCommands(_commands, "ConsoleCommand");
        ClearLeakedCommands(_serverCommands, "ServerCommand");
    }

    private void ClearLeakedCommands<T>(Dictionary<long, T> commands, string kind) where T : Delegate
    {
        foreach (var handle in commands.Keys.ToArray())
        {
            var cleaned = _assemblyManager.ClearLeakedMulticast(commands[handle], kind, handle.ToString());

            if (cleaned is null)
            {
                commands.Remove(handle);
            }
            else
            {
                commands[handle] = cleaned;
            }
        }
    }

    public void InstallChangeHook(IConVar conVar, IConVarManager.DelegateConVarChange callback)
    {
        if (_assemblyManager.IsDelegateUnloaded(callback))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        var key = conVar.GetAbsPtr();

        if (_conVarHooks.ContainsKey(key))
        {
            _conVarHooks[key] += callback;
        }
        else
        {
            _conVarHooks[key] = callback;
            Native.InstallChangeHook(key);
        }
    }

    public void RemoveChangeHook(IConVar conVar, IConVarManager.DelegateConVarChange callback)
    {
        var key = conVar.GetAbsPtr();

        if (!_conVarHooks.TryGetValue(key, out var callbacks))
        {
            throw new InvalidOperationException($"Hook <{conVar.Name}> is not exists!");
        }

        callbacks -= callback;

        if (callbacks is not null)
        {
            return;
        }

        _conVarHooks.Remove(key);
        Native.RemoveChangeHook(key);
    }

#region ConVarCreation

    public IConVar? CreateConVar(string name,
        bool                            defaultValue,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Bool,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name,
        short                           defaultValue,
        short                           min,
        short                           max,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int16,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString,
                                new ConVarVariantValue(min),
                                new ConVarVariantValue(max));

    public IConVar? CreateConVar(string name,
        short                           defaultValue,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int16,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name,
        ushort                          defaultValue,
        ushort                          min,
        ushort                          max,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int16,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString,
                                new ConVarVariantValue(min),
                                new ConVarVariantValue(max));

    public IConVar? CreateConVar(string name,
        ushort                          defaultValue,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int16,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name,
        int                             defaultValue,
        int                             min,
        int                             max,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString,
                                new ConVarVariantValue(min),
                                new ConVarVariantValue(max));

    public IConVar? CreateConVar(string name,
        int                             defaultValue,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name,
        uint                            defaultValue,
        uint                            min,
        uint                            max,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString,
                                new ConVarVariantValue(min),
                                new ConVarVariantValue(max));

    public IConVar? CreateConVar(string name,
        uint                            defaultValue,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name,
        long                            defaultValue,
        long                            min,
        long                            max,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString,
                                new ConVarVariantValue(min),
                                new ConVarVariantValue(max));

    public IConVar? CreateConVar(string name,
        long                            defaultValue,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name,
        ulong                           defaultValue,
        ulong                           min,
        ulong                           max,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString,
                                new ConVarVariantValue(min),
                                new ConVarVariantValue(max));

    public IConVar? CreateConVar(string name,
        ulong                           defaultValue,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Int32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name, float defaultValue, string? helpString = null, ConVarFlags? flags = null)
        => CreateConVarInternal(ConVarType.Float32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString);

    public IConVar? CreateConVar(string name,
        float                           defaultValue,
        float                           min,
        float                           max,
        string?                         helpString = null,
        ConVarFlags?                    flags      = null)
        => CreateConVarInternal(ConVarType.Float32,
                                name,
                                new ConVarVariantValue(defaultValue),
                                flags,
                                helpString,
                                new ConVarVariantValue(min),
                                new ConVarVariantValue(max));

    public unsafe IConVar? CreateConVar(string name,
        string                                 defaultValue,
        string?                                helpString = null,
        ConVarFlags?                           flags      = null)
    {
        Span<byte> bytes = stackalloc byte[256];

        bytes.WriteStringUtf8(defaultValue);

        fixed (byte* p = bytes)
        {
            var val = new ConVarVariantValue(new nint(p));

            return CreateConVarInternal(ConVarType.String,
                                        name,
                                        val,
                                        flags,
                                        helpString);
        }
    }

    private static unsafe IConVar? CreateConVarInternal(ConVarType type,
        string                                                     name,
        ConVarVariantValue                                         defaultValue,
        ConVarFlags?                                               flags      = null,
        string?                                                    helpString = null,
        ConVarVariantValue?                                        min        = null,
        ConVarVariantValue?                                        max        = null)
        => ConVar.Create(Native.CreateConVar(name,
                                             &defaultValue,
                                             helpString ?? string.Empty,
                                             type,
                                             flags ?? ConVarFlags.Release,
                                             &min,
                                             &max));

#endregion

    private ECommandAction OnConCommandTrigger(long handle, GameClient? client, string wholeCommand)
    {
        var buffer = wholeCommand.AsSpan();

        // You cannot guarantee whether user input will contain space, what will happen if the command does not contain the space?
        // Then you need to specify the length of the buffer...
        var commandEndIndex = buffer.IndexOf(' ');

        if (commandEndIndex == -1)
        {
            commandEndIndex = buffer.Length;
        }

        var command   = buffer[..commandEndIndex].ToString();
        var arguments = buffer[commandEndIndex..].Trim().ToString();

        {
            if (_commands.TryGetValue(handle, out var cb))
            {
                var context = new StringCommand(command, false, arguments);

                cb.Invoke(client, context);
            }
        }

        {
            if (_serverCommands.TryGetValue(handle, out var cb))
            {
                var context = new StringCommand(command, false, arguments);

                cb.Invoke(context);
            }
        }

        return ECommandAction.Stopped;
    }

    public void CreateConsoleCommand(string               name,
        Func<IGameClient?, StringCommand, ECommandAction> fn,
        string?                                           description = null,
        ConVarFlags?                                      flags       = null)
    {
        if (_assemblyManager.IsDelegateUnloaded(fn))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        var handle = Native.CreateCommand(name,
                                          description ?? string.Empty,
                                          flags
                                          ?? ConVarFlags.ClientCanExecute | ConVarFlags.LinkedConCommand | ConVarFlags.GameDll);

        if (handle == 0)
        {
            throw new InvalidOperationException($"Failed to create console command <{name}>");
        }

        if (_commands.ContainsKey(handle))
        {
            _commands[handle] += fn;
        }
        else
        {
            _commands[handle] = fn;
        }
    }

    public void CreateServerCommand(string  name,
        Func<StringCommand, ECommandAction> fn,
        string?                             description = null,
        ConVarFlags?                        flags       = null)
    {
        if (_assemblyManager.IsDelegateUnloaded(fn))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        var handle = Native.CreateCommand(name,
                                          description ?? string.Empty,
                                          flags
                                          ?? ConVarFlags.GameDll);

        if (handle == 0)
        {
            throw new InvalidOperationException($"Failed to create server command <{name}>");
        }

        if (_serverCommands.ContainsKey(handle))
        {
            _serverCommands[handle] += fn;
        }
        else
        {
            _serverCommands[handle] = fn;
        }
    }

    public bool ReleaseCommand(string name)
        => Native.ReleaseCommand(name);
}
