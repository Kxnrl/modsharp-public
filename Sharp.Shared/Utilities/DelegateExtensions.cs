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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Shared.Utilities;

public static class DelegateExtensions
{
    /// <summary>
    ///     The assembly that owns the delegate <br />
    ///     <remarks>
    ///         Closures / instance delegates are owned by the Target type; static method groups by the declaring module. <br />
    ///         A multicast delegate only reflects the ownership of its tail subscriber — any ownership-sensitive scenario
    ///         must process each subscriber (<see cref="Delegate.GetInvocationList" />) or reject multicast input outright.
    ///     </remarks>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Assembly GetOwnerAssembly(this Delegate callback)
        => callback.Target?.GetType().Assembly ?? callback.Method.Module.Assembly;

    /// <summary>
    ///     A readable signature of the delegate (DeclaringType.Method)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetMethodSignature(this Delegate callback)
        => $"{callback.Method.DeclaringType?.FullName ?? "<unknown>"}.{callback.Method.Name}";
}
