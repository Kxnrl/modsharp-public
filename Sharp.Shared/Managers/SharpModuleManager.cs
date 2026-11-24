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

namespace Sharp.Shared.Managers;

public interface ISharpModuleManager
{
    /// <summary>
    ///     Register a shared interface for your module so other modules can consume it.
    /// </summary>
    /// <remarks>
    ///     Must be called once the module is in the <c>Running</c> state (<see cref="IModSharpModule.PostInit" /> or later; calling in <see cref="IModSharpModule.Init" /> throws).
    ///     <paramref name="identity" /> lookup is case-insensitive.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     <paramref name="owner" /> is not the module's entry instance, the module is not yet in the <c>Running</c> state, or <paramref name="identity" /> is already registered.
    /// </exception>
    public void RegisterSharpModuleInterface<T>(IModSharpModule owner, string identity, T impl) where T : class;

    /// <summary>
    ///     Get the required sharp module interface (case-insensitive lookup).
    /// </summary>
    /// <returns>The module interface wrapper.</returns>
    /// <exception cref="EntryPointNotFoundException">No interface is registered for <paramref name="identity" />, or it is registered under a different type.</exception>
    public IModSharpModuleInterface<T> GetRequiredSharpModuleInterface<T>(string identity) where T : class;

    /// <summary>
    ///     Get the optional sharp module interface (case-insensitive lookup).
    /// </summary>
    public IModSharpModuleInterface<T>? GetOptionalSharpModuleInterface<T>(string identity) where T : class;

    /// <summary>
    ///     Register a dynamic native function (case-insensitive name).
    /// </summary>
    /// <remarks>
    ///     Must be called once the module is in the <c>Running</c> state (<see cref="IModSharpModule.PostInit" /> or later).
    ///     The <paramref name="function" /> method must belong to <paramref name="owner" />'s assembly.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     <paramref name="owner" /> is not the module's entry instance, the module is not yet in the <c>Running</c> state, or <paramref name="function" /> does not belong to <paramref name="owner" />'s assembly.
    /// </exception>
    /// <exception cref="System.Data.DuplicateNameException"><paramref name="name" /> is already registered.</exception>
    public void RegisterDynamicNative(IModSharpModule owner, string name, Delegate function);

    /// <summary>
    ///     Get a dynamic native function by name (case-insensitive).
    /// </summary>
    /// <remarks>
    ///     After retrieving, use the <c>is</c> operator for type conversion to ensure type safety. <br />
    ///     The returned delegate belongs to the registering module's ALC: do not cache it long-term;
    ///     you must drop any cached delegate when you receive <see cref="IModSharpModule.OnLibraryDisconnect" />,
    ///     otherwise you will pin the unloaded module's ALC and may invoke unloaded code.
    /// </remarks>
    /// <example>
    /// <code>
    /// var func = moduleManager.GetDynamicNative("MyFunction");
    /// if (func is Func&lt;int, int, int&gt; myFunction)
    ///    result = myFunction(1, 2);
    /// </code>
    /// </example>
    public Delegate? GetDynamicNative(string name);
}
