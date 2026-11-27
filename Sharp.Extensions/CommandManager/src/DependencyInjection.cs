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
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace Sharp.Extensions.CommandManager;

public static class DependencyInjection
{
    /// <summary>
    ///     Add CommandManager to the service collection.
    /// </summary>
    public static IServiceCollection AddCommandManager(this IServiceCollection services, ISharedSystem shared)
        => services.AddSingleton(shared)
                   .AddSingleton<CommandManager>()
                   .AddSingleton<ICommandManager, CommandManager>(x => x.GetRequiredService<CommandManager>())
                   .AddSingleton<ISharpExtension, CommandManager>(x => x.GetRequiredService<CommandManager>());

    /// <summary>
    ///     Add CommandManager to the service collection.
    /// </summary>
    /// <remarks>Requires registering <see cref="ISharedSystem" /> before calling this method.</remarks>
    /// <exception cref="InvalidOperationException">
    ///     If <see cref="ISharedSystem" /> is not registered in the service
    ///     collection.
    /// </exception>
    public static IServiceCollection AddCommandManager(this IServiceCollection services)
    {
        if (services.All(s => s.ServiceType != typeof(ISharedSystem)))
        {
            throw new InvalidOperationException(
                $"{typeof(ISharedSystem).FullName} is not registered in the service collection. Please register it before adding CommandManager.");
        }

        return services.AddSingleton<CommandManager>()
                       .AddSingleton<ICommandManager, CommandManager>(x => x.GetRequiredService<CommandManager>())
                       .AddSingleton<ISharpExtension, CommandManager>(x => x.GetRequiredService<CommandManager>());
    }
}
