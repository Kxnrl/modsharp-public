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
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

public interface IConVarManager
{
    /// <summary>
    ///     Callback called when ConVar value changes <br />
    ///     <remarks>Modifying values within the Callback will cause StackOverflow</remarks>
    /// </summary>
    public delegate void DelegateConVarChange(IConVar conVar);

    /// <summary>
    ///     Search for an existing ConVar
    /// </summary>
    /// <param name="name">Parameter name (case sensitive)</param>
    /// <param name="useIterator">Whether to use iterator mode, slower than direct lookup but can find hidden ConVars</param>
    IConVar? FindConVar(string name, bool useIterator = false);

    /// <summary>
    ///     Listen for changes to a ConVar value <br />
    ///     <remarks>Modifying values within the Callback will immediately cause StackOverflow</remarks>
    /// </summary>
    void InstallChangeHook(IConVar conVar, DelegateConVarChange callback);

    /// <summary>
    ///     Remove ConVar listener
    /// </summary>
    void RemoveChangeHook(IConVar conVar, DelegateConVarChange callback);

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

    IConVar? CreateConVar(string name,
        string                   defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    /// <summary>
    ///     Create a console command that can be used by both client and server. If you want to create <br />
    ///     <remarks>
    ///         High performance, <br />
    ///         Client-only, <br />
    ///         Support both console and chat<br />
    ///         please use <see cref="IClientManager.InstallCommandCallback" />
    ///     </remarks>
    /// </summary>
    void CreateConsoleCommand(string                      name,
        Func<IGameClient?, StringCommand, ECommandAction> fn,
        string?                                           description = null,
        ConVarFlags?                                      flags       = null);

    /// <summary>
    ///     Create a server only command
    /// </summary>
    void CreateServerCommand(string         name,
        Func<StringCommand, ECommandAction> fn,
        string?                             description = null,
        ConVarFlags?                        flags       = null);

    /// <summary>
    ///     Must be called during unload, otherwise leak occurs
    /// </summary>
    bool ReleaseCommand(string name);
}
