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
using Microsoft.Extensions.Logging;
using Sharp.Core.GameEntities;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using Forward = Sharp.Core.Bridges.Forwards.CustomHud;
using Native = Sharp.Core.Bridges.Natives.CustomHud;

namespace Sharp.Core.Managers;

internal interface ICoreCustomHudManager : ICustomHudManager;

internal class CustomHudManager : ICoreCustomHudManager
{
    private readonly ICoreEntityManager                                  _entityManager;
    private readonly ILogger<CustomHudManager>                            _logger;
    private readonly List<ICustomHudManager.CustomHudClickedHandler>      _clickListeners;

    public CustomHudManager(ICoreEntityManager entityManager, ILogger<CustomHudManager> logger)
    {
        _entityManager  = entityManager;
        _logger         = logger;
        _clickListeners = [];

        Forward.OnCustomHudClicked += OnCustomHudClicked;
    }

    public ICustomHudLayout? CreateLayout(string layoutResource, string? targetName = null)
    {
        if (string.IsNullOrWhiteSpace(layoutResource))
        {
            throw new ArgumentException("A compiled custom HUD layout resource is required.", nameof(layoutResource));
        }

        var keyValues = new Dictionary<string, KeyValuesVariantValueItem>
        {
            ["layout"] = layoutResource,
        };

        if (targetName is not null)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                throw new ArgumentException("Target name cannot be empty or whitespace.", nameof(targetName));
            }

            keyValues["targetname"] = targetName;
        }

        return _entityManager.SpawnEntitySync<ICustomHudLayout>("custom_hud_layout", keyValues);
    }

    public void InstallClickListener(ICustomHudManager.CustomHudClickedHandler listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        if (_clickListeners.Contains(listener))
        {
            _logger.LogError("Custom HUD click listener is already installed.\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _clickListeners.Add(listener);
    }

    public void RemoveClickListener(ICustomHudManager.CustomHudClickedHandler listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        if (!_clickListeners.Remove(listener))
        {
            _logger.LogError("Custom HUD click listener has not been installed.\n{stackTrace}", Environment.StackTrace);
        }
    }

    private void OnCustomHudClicked(nint playerPointer, nint layoutPointer, string buttonId)
    {
        var player = PlayerController.Create(playerPointer);
        var layout = CustomHudLayout.Create(layoutPointer);

        if (player is null || layout is null)
        {
            _logger.LogError("Invalid custom HUD click payload: player={player}, layout={layout}",
                             playerPointer,
                             layoutPointer);

            return;
        }

        for (var i = 0; i < _clickListeners.Count; i++)
        {
            try
            {
                _clickListeners[i].Invoke(player, layout, buttonId);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling custom HUD click listener {listener}",
                                 _clickListeners[i].Method.DeclaringType?.Name ?? _clickListeners[i].Method.Name);
            }
        }
    }
}
