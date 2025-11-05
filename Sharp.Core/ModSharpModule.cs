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
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Configuration;
using Sharp.Core.Utilities;
using Sharp.Shared;

namespace Sharp.Core;

internal enum ModuleLoadState
{
    Initializing,
    Loading,
    Unloading,
    Failure,
    Running,
    Unloaded,
}

internal sealed class ModSharpModule
{
    private const string ReloadCheckFolder = "reload";

    private readonly ISharpCore _sharpCore;
    private readonly string     _dllPath;
    private readonly string     _dllFile;
    private readonly string     _rootPath;
    private readonly int        _threadId;

    public string           Name           => Path.GetFileNameWithoutExtension(_dllFile);
    public string           DisplayName    => _instance?.DisplayName ?? Name;
    public string?          DisplayAuthor  => _instance?.DisplayAuthor;
    public Version          DisplayVersion => _instance?.GetType().Assembly.GetName().Version ?? new Version(0, 0, 0, 0);
    public IModSharpModule? Instance       => _instance;

    public ModuleLoadState State { get; private set; }

    private PluginLoader?    _loader;
    private Mutex?           _mutex;
    private IModSharpModule? _instance;

    internal ModSharpModule(
        ISharpCore sharpCore,
        string     dllFile,
        string     dllPath,
        string     rootPath)
    {
        _sharpCore = sharpCore;
        _dllPath   = dllPath;
        _rootPath  = rootPath;
        _dllFile   = dllFile;
        _threadId  = Environment.CurrentManagedThreadId;

        State = ModuleLoadState.Initializing;
    }

    private string[] GetUpdateFiles()
    {
        var tempFolder  = Path.Combine(_dllPath, ReloadCheckFolder);
        var sameNameDir = Path.GetFileName(_dllPath);

        if (sameNameDir.Length > 0 && Directory.Exists(Path.Combine(_dllPath, sameNameDir)))
        {
            return Directory.GetFiles(Path.Combine(_dllPath, sameNameDir));
        }

        return Directory.Exists(tempFolder) ? Directory.GetFiles(tempFolder) : [];
    }

    public bool IsNeedUpdate()
        => GetUpdateFiles().Length > 0;

    public bool Update()
    {
        try
        {
            var files = GetUpdateFiles();

            if (files.Length == 0)
            {
                return true;
            }

            foreach (var file in files)
            {
                File.Copy(file, Path.Combine(_dllPath, Path.GetFileName(file)), true);
                File.Delete(file);
            }
        }
        catch (Exception e)
        {
            Printer.Error($"Failed to update module: {_dllPath}", e);

            return false;
        }

        try
        {
            var reloadFolder = Path.Combine(_dllPath, ReloadCheckFolder);

            if (Directory.Exists(reloadFolder))
            {
                Directory.Delete(reloadFolder);
            }
        }
        catch (Exception e)
        {
            Printer.Error($"Failed to cleanup module: {_dllPath}", e);

            return false;
        }

        return true;
    }

    public void ShutdownMutex()
    {
        if (_threadId != Environment.CurrentManagedThreadId)
        {
            throw new InvalidOperationException("Shutdown must be called from the same thread as the constructor.");
        }

        _mutex?.ReleaseMutex();
        _mutex?.Close();
        _mutex = null;
    }

    public void Unload(Action<string> onUnload)
    {
        try
        {
            _mutex?.ReleaseMutex();
            _mutex?.Close();

            State = ModuleLoadState.Unloading;

            if (_instance is not null)
            {
                _sharpCore.OnModuleUnload(_instance);
                onUnload(Name);
                _instance.Shutdown();
            }

            State = ModuleLoadState.Unloaded;

            _loader?.Dispose();
        }
        catch
        {
            State = ModuleLoadState.Failure;

            throw;
        }
        finally
        {
            _mutex    = null;
            _instance = null;
            _loader   = null;
        }
    }

    public void Load(bool hotReload, Action<string> onLoad, ISharedSystem shared, IConfiguration configuration)
    {
        State = ModuleLoadState.Loading;

        PluginLoader? loader = null;

        try
        {
            if (!hotReload && !Update())
            {
                throw new ApplicationException("Failed to update module while starting");
            }

            loader = PluginLoader.CreateFromAssemblyFile(_dllFile,
                                                         config =>
                                                         {
                                                             config.PreferSharedTypes = true;
                                                             config.IsLazyLoaded      = false;
                                                             config.IsUnloadable      = true;
                                                             config.LoadInMemory      = true;
                                                             config.EnableHotReload   = false;
                                                         });

            var assembly = loader.LoadDefaultAssembly();

            var module = assembly.GetTypes()
                                 .FirstOrDefault(t => typeof(IModSharpModule).IsAssignableFrom(t) && !t.IsAbstract)
                         ?? throw new BadImageFormatException("IModSharpModule is not implemented.");

            if (assembly.GetName().Version is not { } version)
            {
                throw new VersionNotFoundException("Could not versioning");
            }

            var matches = Regex.Matches(_dllFile, "[a-zA-Z0-9]+", RegexOptions.Compiled | RegexOptions.Singleline);
            var builder = new StringBuilder();

            foreach (Match m in matches)
            {
                builder.Append(m.Value);
            }

            var key = builder.ToString();
            _mutex = new Mutex(true, key, out var success);

            if (!success)
            {
                throw new AbandonedMutexException("Double load!");
            }

            if (Activator.CreateInstance(module, shared, _dllPath, _rootPath, version, configuration, hotReload)
                is not IModSharpModule instance)
            {
                throw new TypeLoadException("Failed to create instance.");
            }

            if (!instance.Init())
            {
                throw new ApplicationException("Failed to init.");
            }

            _loader   = loader;
            _instance = instance;

            State = ModuleLoadState.Running;

            try
            {
                _instance.PostInit();
            }
            catch (Exception e)
            {
                Printer.Error($"An error occurred in PostInit of {Name}", e);
            }

            onLoad(Name);
        }
        catch
        {
            State = ModuleLoadState.Failure;

            try
            {
                loader?.Dispose();
            }
            catch
            {
                // empty
            }
            finally
            {
                _loader = null;
            }

            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Close();
            }
            catch
            {
                // empty
            }
            finally
            {
                _mutex = null;
            }

            _instance = null;

            throw;
        }
    }

    internal void CallOnAllLoad()
    {
        if (State != ModuleLoadState.Running || _instance is null)
        {
            return;
        }

        _instance.OnAllModulesLoaded();
    }

    internal void CallOnLibraryConnected(string name)
    {
        if (State != ModuleLoadState.Running || _instance is null)
        {
            return;
        }

        if (name.Equals(Name))
        {
            return;
        }

        _instance.OnLibraryConnected(name);
    }

    internal void CallOnLibraryDisconnect(string name)
    {
        if (State != ModuleLoadState.Running || _instance is null)
        {
            return;
        }

        if (name.Equals(Name))
        {
            return;
        }

        _instance.OnLibraryDisconnect(name);
    }
}
