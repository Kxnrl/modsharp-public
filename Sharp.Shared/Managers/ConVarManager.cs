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
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

public interface IConVarManager
{
    /// <summary>
    ///     Callback called when ConVar value changes.
    /// </summary>
    /// <remarks>Modifying values within the callback will cause a <see cref="StackOverflowException" />.</remarks>
    public delegate void DelegateConVarChange(IConVar conVar);

    /// <summary>
    ///     Search for an existing ConVar
    /// </summary>
    /// <param name="name">Parameter name (case sensitive)</param>
    /// <param name="useIterator">Whether to use iterator mode, slower than direct lookup but can find hidden ConVars</param>
    IConVar? FindConVar(string name, bool useIterator = false);

    /// <summary>
    ///     Listen for changes to a ConVar value.
    /// </summary>
    /// <remarks>Modifying values within the callback will immediately cause a <see cref="StackOverflowException" />.</remarks>
    void InstallChangeHook(IConVar conVar, DelegateConVarChange callback);

    /// <summary>
    ///     Remove ConVar listener.
    /// </summary>
    /// <exception cref="InvalidOperationException">No change hook is currently registered for <paramref name="conVar" />.</exception>
    void RemoveChangeHook(IConVar conVar, DelegateConVarChange callback);

    /// <summary>
    ///     Create a boolean ConVar, or return the existing ConVar unchanged if one with <paramref name="name" /> already exists.
    /// </summary>
    /// <remarks>
    ///     When <paramref name="flags" /> is <see langword="null" />, <see cref="ConVarFlags.Release" /> is used.
    ///     If a ConVar with the same name already exists, its existing value, bounds, help string, and flags are preserved.
    /// </remarks>
    IConVar? CreateConVar(string name,
        bool                     defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        short                    defaultValue,
        short                    min,
        short                    max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        short                    defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ushort                   defaultValue,
        ushort                   min,
        ushort                   max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ushort                   defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        int                      defaultValue,
        int                      min,
        int                      max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        int                      defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        uint                     defaultValue,
        uint                     min,
        uint                     max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        uint                     defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        long                     defaultValue,
        long                     min,
        long                     max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        long                     defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ulong                    defaultValue,
        ulong                    min,
        ulong                    max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ulong                    defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        float                    defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        float                    defaultValue,
        float                    min,
        float                    max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    /// <summary>
    ///     Create a string ConVar, or return the existing ConVar unchanged if one with <paramref name="name" /> already exists.
    /// </summary>
    /// <remarks>
    ///     <paramref name="defaultValue" /> is truncated to 255 UTF-8 bytes when creating the ConVar.
    ///     When <paramref name="flags" /> is <see langword="null" />, <see cref="ConVarFlags.Release" /> is used.
    /// </remarks>
    IConVar? CreateConVar(string name,
        string                   defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    /// <summary>
    ///     Create a console command that can be used by both client and server.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For high-performance client-only commands that support both console and chat triggers,
    ///         use <see cref="IClientManager.InstallCommandCallback" /> instead.
    ///     </para>
    ///     <para>
    ///         The callback's return value does not affect command execution.
    ///         When <paramref name="flags" /> is <see langword="null" />, defaults to
    ///         <see cref="ConVarFlags.ClientCanExecute" /> | <see cref="ConVarFlags.LinkedConCommand" /> | <see cref="ConVarFlags.GameDll" />.
    ///         Commands with the same name share one engine command: its flags are the union of all registrations,
    ///         and its help text comes from the first one.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Native command creation returned an invalid handle.</exception>
    void CreateConsoleCommand(string                      name,
        Func<IGameClient?, StringCommand, ECommandAction> fn,
        string?                                           description = null,
        ConVarFlags?                                      flags       = null);

    /// <summary>
    ///     Create a server-only command, which only responds to non-client callers such as the server console.
    /// </summary>
    /// <remarks>
    ///     The callback's return value does not affect command execution.
    ///     When <paramref name="flags" /> is <see langword="null" />, defaults to <see cref="ConVarFlags.GameDll" />.
    ///     Commands with the same name share one engine command: its flags are the union of all registrations,
    ///     and its help text comes from the first one.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Native command creation returned an invalid handle.</exception>
    void CreateServerCommand(string         name,
        Func<StringCommand, ECommandAction> fn,
        string?                             description = null,
        ConVarFlags?                        flags       = null);

    /// <summary>
    ///     Deprecated no-op. The engine command is released automatically once its callbacks drain.
    /// </summary>
    [Obsolete("Use ReleaseConsoleCommandCallback/ReleaseServerCommandCallback; the engine command is released once its callbacks drain.")]
    bool ReleaseCommand(string name);

    /// <summary>
    ///     Release a previously registered console command callback; the engine command is released once all its callbacks drain
    /// </summary>
    void ReleaseConsoleCommandCallback(string name, Func<IGameClient?, StringCommand, ECommandAction> fn);

    /// <summary>
    ///     Release a previously registered server command callback; the engine command is released once all its callbacks drain
    /// </summary>
    void ReleaseServerCommandCallback(string name, Func<StringCommand, ECommandAction> fn);
}
