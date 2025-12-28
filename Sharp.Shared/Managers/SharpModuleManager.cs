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

namespace Sharp.Shared.Managers;

public interface ISharpModuleManager
{
    /// <summary>
    ///     Register your current module as a library to allow other modules to use the interface
    /// </summary>
    public void RegisterSharpModuleInterface<T>(IModSharpModule owner, string identity, T impl) where T : class;

    /// <summary>
    ///     Get the required sharp module interface. If the interface is not found, an exception is thrown.
    /// </summary>
    /// <returns></returns>
    public IModSharpModuleInterface<T> GetRequiredSharpModuleInterface<T>(string identity) where T : class;

    /// <summary>
    ///     Get the optional sharp module interface.
    /// </summary>
    public IModSharpModuleInterface<T>? GetOptionalSharpModuleInterface<T>(string identity) where T : class;

    /// <summary>
    ///     Register dynamic native functions for convenient debugging, performance is actually decent too<br />
    ///     <remarks>It is the same as SourceMod, you can use it however you want</remarks>
    /// </summary>
    public void RegisterDynamicNative(IModSharpModule owner, string name, Delegate function);

    /// <summary>
    ///     Get dynamic native function
    ///     <remarks>After retrieving, you need to use 'is' operator for type conversion to ensure type safety</remarks>
    /// </summary>
    /// <code>
    /// var func = moduleManager.GetDynamicNative("MyFunction");
    /// if (func is Func&lt;int, int, int&gt; myFunction)
    ///    result = myFunction(1, 2);
    /// </code>
    public Delegate? GetDynamicNative(string name);
}
