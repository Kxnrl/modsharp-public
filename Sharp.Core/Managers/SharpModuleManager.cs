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
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Sharp.Core.Helpers;
using Sharp.Core.Objects;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers;

internal interface ICoreSharpModuleManager : ISharpModuleManager
{
    bool InitModules();
}

internal class SharpModuleManager : ICoreSharpModuleManager
{
    private readonly record struct DynamicNative(string Owner, Delegate Function);

    private readonly record struct SharpLibrary(string Owner, object Interface);

    private readonly ILogger<SharpModuleManager> _logger;
    private readonly ISharpCore                  _sharpCore;
    private readonly ISharedManager              _sharedManager;
    private readonly IConfiguration              _configuration;

    private readonly List<ModSharpModule>              _modules;
    private readonly Dictionary<string, SharpLibrary>  _libraries;
    private readonly Dictionary<string, DynamicNative> _natives;

    public SharpModuleManager(ILogger<SharpModuleManager> logger,
        IShutdownMonitor                                  shutdownMonitor,
        ISharpCore                                        sharpCore,
        ISharedManager                                    sharedManager,
        IConfiguration                                    configuration)
    {
        _logger        = logger;
        _sharpCore     = sharpCore;
        _sharedManager = sharedManager;
        _configuration = configuration;

        _modules   = [];
        _libraries = new Dictionary<string, SharpLibrary>(StringComparer.OrdinalIgnoreCase);
        _natives   = new Dictionary<string, DynamicNative>(StringComparer.OrdinalIgnoreCase);

        Bridges.Forwards.Game.OnStartupServerPre       += OnStartupServer;
        Bridges.Forwards.Core.OnModSharpModulesCommand += OnModSharpModulesCommand;

        shutdownMonitor.RegisterShutdownEvent(OnShutdown, int.MaxValue);
    }

    private void OnShutdown()
    {
        foreach (var module in _modules)
        {
            module.ShutdownMutex();
        }

        foreach (var module in _modules)
        {
            module.Unload(OnModuleUnload);
        }
    }

    private void OnStartupServer()
    {
        var updates = new List<ModSharpModule>();

        foreach (var module in _modules)
        {
            try
            {
                if (!module.IsNeedUpdate())
                {
                    continue;
                }

                Log.Logger.Information("Reloading module [{Name}]...", module.Name);
                module.Unload(OnModuleUnload);
                module.Update();
                module.Load(true, OnModuleLoad, _sharedManager, _configuration);
                updates.Add(module);
                Log.Logger.Information("Module [{Name}] reloaded successfully", module.Name);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Failed to reload module [{Name}]", module.Name);
            }
        }

        if (updates.Count > 0)
        {
            updates.ForEach(x => x.CallOnAllLoad());
        }

        if (_modules.Count(x => x.State == ModuleLoadState.Running) == 0)
        {
            Bridges.Natives.Core.FatalError("No more modules running");
        }
    }

    private void OnModSharpModulesCommand(GameClient? client)
    {
        var builder = new StringBuilder();

        builder.Append("  ModSharp Modules:\n");

        var result = 0;

        for (var i = 0; i < _modules.Count; i++)
        {
            var index   = i + 1;
            var module  = _modules[i];
            var version = module.DisplayVersion;

            if (client is null)
            {
                builder.Append($"    #{index,-2} {module.Name}\n");

                builder.Append($"      module : {module.DisplayName}\n");
                builder.Append($"      version: {version.Major}.{version.Minor}.{version.Build}\n");

                if (module.DisplayAuthor is { } author)
                {
                    builder.Append($"      author : {author}\n");
                }

                builder.Append($"      state  : {module.State.ToString()}\n");
                result++;
            }
            else if (module.State is ModuleLoadState.Running)
            {
                builder.Append($"    #{index,-2} {module.DisplayName}");
                builder.Append($" ({version.Major}.{version.Minor}.{version.Build})");

                if (module.DisplayAuthor is { } author)
                {
                    builder.Append($" by {author}");
                }

                builder.Append('\n');
                result++;
            }
        }

        if (result == 0)
        {
            builder.Append("    No modules found.\n");
        }

        if (client is null)
        {
            Console.WriteLine(builder.ToString());
        }
        else
        {
            var receiver = new RecipientFilter(client.Slot);

            unsafe
            {
                // TODO use protobuf message instead
                Bridges.Natives.Game.PrintChannelFilter(HudPrintChannel.Console, builder.ToString(), &receiver);
            }
        }
    }

    public bool InitModules()
    {
        var gamePath = _sharpCore.GetGamePath();

        if (string.IsNullOrWhiteSpace(gamePath))
        {
            _logger.LogError("Cannot get game directory!");

            return false;
        }

        var sharpPath = Path.Combine(gamePath, "../sharp");

        if (string.IsNullOrWhiteSpace(sharpPath))
        {
            _logger.LogError("{path} doesn't exist!", sharpPath);

            return false;
        }

        var root = Path.GetFullPath(sharpPath);

        LoadSharedLibrary(root);

        return LoadModules(root);
    }

    public void RegisterSharpModuleInterface<T>(IModSharpModule owner, string identity, T impl) where T : class
    {
        if (_modules.Find(x => owner.Equals(x.Instance)) is not { } module)
        {
            throw new InvalidOperationException("Module is not actual IModSharpModule of assembly");
        }

        if (module.State != ModuleLoadState.Running)
        {
            throw new InvalidOperationException("What the fuck you are here?");
        }

        if (!_libraries.TryAdd(identity, new SharpLibrary(module.Name, new ModSharpModuleInterface<T>(identity, impl))))
        {
            throw new InvalidOperationException($"Identity {identity} already registered");
        }
    }

    public IModSharpModuleInterface<T> GetRequiredSharpModuleInterface<T>(string identity) where T : class
    {
        if (!_libraries.TryGetValue(identity, out var library))
        {
            throw new EntryPointNotFoundException($"Couldn't find interface {identity}");
        }

        if (library.Interface is ModSharpModuleInterface<T> i)
        {
            return i;
        }

        throw new EntryPointNotFoundException($"Interface {identity} is not {typeof(T).Name}");
    }

    public IModSharpModuleInterface<T>? GetOptionalSharpModuleInterface<T>(string identity) where T : class
        => _libraries.TryGetValue(identity, out var library) && library.Interface is ModSharpModuleInterface<T> i ? i : null;

    public void RegisterDynamicNative(IModSharpModule owner, string name, Delegate function)
    {
        if (_modules.Find(x => owner.Equals(x.Instance)) is not { } module)
        {
            throw new InvalidOperationException("Module is not actual IModSharpModule of assembly");
        }

        if (module.State != ModuleLoadState.Running || module.Instance is not { } instance)
        {
            throw new InvalidOperationException("What the fuck you are here?");
        }

        var assembly = function.Method.Module.Assembly;

        if (!instance.GetType().Assembly.Equals(assembly))
        {
            throw new InvalidOperationException("Assembly Error!");
        }

        if (!_natives.TryAdd(name, new DynamicNative(module.Name, function)))
        {
            throw new DuplicateNameException($"{name} was registered");
        }
    }

    public Delegate? GetDynamicNative(string name)
        => _natives.TryGetValue(name, out var value) ? value.Function : null;

    private sealed class ModSharpModuleInterface<T> : IDisposable, IModSharpModuleInterface<T> where T : class
    {
        public ModSharpModuleInterface(string identity, T instance)
        {
            Instance = instance;
            Identity = identity;
        }

        public string Identity { get; }
        public T?     Instance { get; private set; }

        public void Dispose()
        {
            Instance = null;
        }
    }

    private void LoadSharedLibrary(string root)
    {
        var sharedFolder = Path.Combine(root, "shared");

        if (!Directory.Exists(sharedFolder))
        {
            return;
        }

        var map = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        foreach (var dllPath in Directory.GetDirectories(sharedFolder, "*", SearchOption.TopDirectoryOnly))
        {
            if (File.Exists(Path.Combine(dllPath, ".disabled")) || dllPath.EndsWith(".disabled"))
            {
                continue;
            }

            var name = Path.GetFileName(dllPath);
            var ext  = name.EndsWith(".dll") ? string.Empty : ".dll";
            var file = Path.Combine(dllPath, $"{name}{ext}");

            try
            {
                if (!File.Exists(file))
                {
                    throw new FileNotFoundException("Missing Assembly", file);
                }

                var loader = PluginLoader.CreateFromAssemblyFile(file,
                                                                 config =>
                                                                 {
                                                                     config.PreferSharedTypes = true;
                                                                     config.IsLazyLoaded      = false;
                                                                     config.IsUnloadable      = false;
                                                                     config.LoadInMemory      = true;
                                                                     config.EnableHotReload   = false;
                                                                 });

                var assembly = loader.LoadDefaultAssembly();

                map.Add(assembly.GetName().Name!, assembly);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to load shared lib {name}", name);
            }
        }

        if (map.Count > 0)
        {
            AssemblyLoadContext.Default.Resolving += (_, name) => map.GetValueOrDefault(name.Name!);
        }
    }

    private bool LoadModules(string root)
    {
        var pluginsFolder = Path.Combine(root, "modules");

        foreach (var dllPath in Directory.GetDirectories(pluginsFolder, "*", SearchOption.TopDirectoryOnly))
        {
            if (File.Exists(Path.Combine(dllPath, ".disabled")) || dllPath.EndsWith(".disabled"))
            {
                continue;
            }

            var runtimeConfigFile = Directory
                                    .GetFiles(dllPath, "*.deps.json")
                                    .FirstOrDefault();

            var entryDll = runtimeConfigFile?.Replace(".deps.json", ".dll");

            if (string.IsNullOrEmpty(entryDll) || !File.Exists(entryDll))
            {
                continue;
            }

            var module = new ModSharpModule(_sharpCore, entryDll, dllPath, root);

            _modules.Add(module);

            try
            {
                module.Load(false, OnModuleLoad, _sharedManager, _configuration);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to load module [{name}]", module.Name);
            }
        }

        if (_modules.Count(x => x.State == ModuleLoadState.Running) == 0)
        {
            _logger.LogError("Count not load any module!");

            return false;
        }

        foreach (var m in _modules)
        {
            try
            {
                m.CallOnAllLoad();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling OnAllModulesLoaded");
            }
        }

        return true;
    }

    private void OnModuleLoad(string name)
    {
        foreach (var m in _modules)
        {
            try
            {
                m.CallOnLibraryConnected(name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling OnLibraryConnected");
            }
        }
    }

    private void OnModuleUnload(string name)
    {
        foreach (var m in _modules)
        {
            try
            {
                m.CallOnLibraryDisconnect(name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling CallOnLibraryDisconnect");
            }
        }

        var natives = _natives.Where(x => x.Value.Owner.Equals(name))
                              .Select(x => x.Key)
                              .ToArray();

        foreach (var t in natives)
        {
            _natives.Remove(t);
        }

        var libraries = _libraries.Where(x => x.Value.Owner.Equals(name))
                                  .Select(x => x.Key)
                                  .ToArray();

        foreach (var identity in libraries)
        {
            if (!_libraries.Remove(identity, out var library))
            {
                continue;
            }

            if (library.Interface is IDisposable item)
            {
                item.Dispose();
            }
        }
    }
}
