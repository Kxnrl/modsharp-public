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
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Managers;

namespace Sharp.Core.Managers;

internal interface ISharedManager : ISharedSystem;

internal class SharedManager : ISharedManager
{
    private readonly IServiceProvider       _serviceProvider;
    private readonly ILogger<SharedManager> _logger;

    public SharedManager(ILogger<SharedManager> logger, IServiceProvider services)
    {
        _logger          = logger;
        _serviceProvider = services;
    }

    public IClientManager GetClientManager()
        => _serviceProvider.GetRequiredService<ICoreClientManager>();

    public IEntityManager GetEntityManager()
        => _serviceProvider.GetRequiredService<ICoreEntityManager>();

    public IEventManager GetEventManager()
        => _serviceProvider.GetRequiredService<ICoreEventManager>();

    public IConVarManager GetConVarManager()
        => _serviceProvider.GetRequiredService<ICoreConVarManager>();

    public IHookManager GetHookManager()
        => _serviceProvider.GetRequiredService<ICoreHookManager>();

    public IModSharp GetModSharp()
        => _serviceProvider.GetRequiredService<ISharpCore>();

    public ILoggerFactory GetLoggerFactory()
        => _serviceProvider.GetRequiredService<ILoggerFactory>();

    public ITransmitManager GetTransmitManager()
        => _serviceProvider.GetRequiredService<ICoreTransmitManager>();

    public IFileManager GetFileManager()
        => _serviceProvider.GetRequiredService<ICoreFileManager>();

    public ISchemaManager GetSchemaManager()
        => _serviceProvider.GetRequiredService<ICoreSchemaManager>();

    public IEconItemManager GetEconItemManager()
        => _serviceProvider.GetRequiredService<ICoreEconItemManager>();

    public ILibraryModuleManager GetLibraryModuleManager()
        => _serviceProvider.GetRequiredService<ICoreLibraryModuleManager>();

    public ISoundManager GetSoundManager()
        => _serviceProvider.GetRequiredService<ICoreSoundManager>();

    public IPhysicsQueryManager GetPhysicsQueryManager()
        => _serviceProvider.GetRequiredService<ICorePhysicsQueryManager>();

    public ISharpModuleManager GetSharpModuleManager()
        => _serviceProvider.GetRequiredService<ICoreSharpModuleManager>();

    public IParticleManager GetParticleManager()
        => _serviceProvider.GetRequiredService<ICoreParticleManager>();
}
