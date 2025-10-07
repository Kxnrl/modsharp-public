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
using Microsoft.Extensions.DependencyInjection;

namespace Sharp.Shared.Abstractions;

/// <summary>
///     This interface use only for Extensions package. <br />
///     <remark>if you want to inherit this, you need to know what you are doing.</remark>
/// </summary>
public interface ISharpExtension
{
    void Load();

    void Shutdown();
}

public static class SharpExtensionExtensions
{
    public static void LoadAllSharpExtension(this IServiceProvider services)
    {
        foreach (var loadable in services.GetServices<ISharpExtension>())
        {
            loadable.Load();
        }
    }

    public static void ShutdownAllSharpExtension(this IServiceProvider services)
    {
        foreach (var loadable in services.GetServices<ISharpExtension>())
        {
            loadable.Shutdown();
        }
    }
}   