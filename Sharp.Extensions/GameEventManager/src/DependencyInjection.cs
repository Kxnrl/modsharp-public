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
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace Sharp.Extensions.GameEventManager;

public static class DependencyInjection
{
    /// <summary>
    ///     Add GameEventManager to the service collection.
    /// </summary>
    public static IServiceCollection AddGameEventManager(this IServiceCollection services, ISharedSystem shared)
        => services.AddSingleton(shared)
                   .AddSingleton<GameEventManager>()
                   .AddSingleton<IGameEventManager, GameEventManager>(x => x.GetRequiredService<GameEventManager>())
                   .AddSingleton<ILoadable, GameEventManager>(x => x.GetRequiredService<GameEventManager>());

    /// <summary>
    ///     Add GameEventManager to the service collection. <br />
    ///     <remark>requires register <see cref="ISharedSystem" /> before add this</remark>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     If <see cref="ISharedSystem" /> is not registered in the service
    ///     collection.
    /// </exception>
    public static IServiceCollection AddGameEventManager(this IServiceCollection services)
    {
        if (services.All(s => s.ServiceType != typeof(ISharedSystem)))
        {
            throw new InvalidOperationException(
                $"{typeof(ISharedSystem).FullName} is not registered in the service collection. Please register it before adding GameEventManager.");
        }

        return services.AddSingleton<GameEventManager>()
                       .AddSingleton<IGameEventManager, GameEventManager>(x => x.GetRequiredService<GameEventManager>())
                       .AddSingleton<ILoadable, GameEventManager>(x => x.GetRequiredService<GameEventManager>());
    }
}
