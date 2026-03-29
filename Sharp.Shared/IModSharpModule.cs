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

namespace Sharp.Shared;

/// <summary>
///     Module Interface <br />
///     <remarks>the constructor must be</remarks>
///     <code>public YourModule(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)</code>
/// </summary>
public interface IModSharpModule
{
    string DisplayName   { get; }
    string DisplayAuthor { get; }

    /// <summary>
    ///     Load
    /// </summary>
    bool Init();

    /// <summary>
    ///     Post Load <br />
    ///     <remarks>Safe to register dynamic native or shared interface here</remarks>
    /// </summary>
    void PostInit()
    {
    }

    /// <summary>
    ///     Shutdown
    /// </summary>
    void Shutdown();

    /// <summary>
    ///     Called after modules loaded, this does not call for the current module itself.
    /// </summary>
    /// <remarks>
    ///     This only fires for modules loaded AFTER the current module. 
    ///     If ModuleA loads first and then ModuleB loads, ModuleA will receive <see cref="OnLibraryConnected"/> for ModuleB, 
    ///     but ModuleB will NOT receive <see cref="OnLibraryConnected"/> for ModuleA (since ModuleA was already loaded).
    ///     Use <see cref="OnAllModulesLoaded"/> to safely access all modules regardless of load order.
    /// </remarks>
    /// <param name="name">The assembly file name, without extension(.dll), of the loaded module (e.g., "Sharp.Modules.LocalizerManager")</param>
    void OnLibraryConnected(string name)
    {
    }

    /// <summary>
    ///     Called before library unloaded, this does not call for the current module itself
    /// </summary>
    /// <param name="name">The assembly file name, without extension(.dll), of the unloading module (e.g., "Sharp.Modules.LocalizerManager")</param>
    void OnLibraryDisconnect(string name)
    {
    }

    /// <summary>
    ///     Called after all modules loaded
    /// </summary>
    void OnAllModulesLoaded()
    {
    }
}

public interface IModSharpModuleInterface<out T> where T : class
{
    /// <summary>
    ///     Is the interface available, safe to use
    /// </summary>
    public bool IsAvailable => Instance is not null;

    /// <summary>
    ///     Identity of this interface
    /// </summary>
    public string Identity { get; }

    /// <summary>
    ///     Instance of this interface <br />
    ///     <remarks>null if interface no more available</remarks>
    /// </summary>
    public T? Instance { get; }
}
