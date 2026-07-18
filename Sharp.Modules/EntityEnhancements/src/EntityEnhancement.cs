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
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.EntityEnhancements.Modules;
using Sharp.Shared;

namespace Sharp.Modules.EntityEnhancements;

public sealed class EntityEnhancement : IModSharpModule
{
    public string DisplayName   => "Entity Enhancements";
    public string DisplayAuthor => "Kxnrl";

    private readonly ILogger<EntityEnhancement> _logger;
    private readonly IModSharp                  _modSharp;

    private readonly List<IEnhancement> _modules;

    public EntityEnhancement(ISharedSystem sharedSystem,
        string                             dllPath,
        string                             sharpPath,
        Version                            version,
        IConfiguration                     coreConfiguration,
        bool                               hotReload)
    {
        _logger   = sharedSystem.GetLoggerFactory().CreateLogger<EntityEnhancement>();
        _modSharp = sharedSystem.GetModSharp();

        _modules =
        [
            new FilterCrashFixes(sharedSystem),
            new GameUI(sharedSystem),
            new PointViewControl(sharedSystem),
            new SteamIdApi(sharedSystem),
        ];
    }

    public bool Init()
        => true;

    public void PostInit()
    {
        foreach (var module in _modules)
        {
            try
            {
                module.Init();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to initialize enhancement module {Module}", module.GetType().Name);
            }
        }
    }

    public void Shutdown()
    {
        foreach (var module in _modules)
        {
            try
            {
                module.Shutdown();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to shutdown enhancement module {Module}", module.GetType().Name);
            }
        }
    }
}
