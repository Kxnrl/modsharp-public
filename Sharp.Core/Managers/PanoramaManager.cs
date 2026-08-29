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
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges.Forwards;
using Sharp.Core.GameEntities;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using CustomHudClickedCallback = Sharp.Shared.Managers.IPanoramaManager.CustomHudClickedHandler;

namespace Sharp.Core.Managers;

internal interface ICorePanoramaManager : IPanoramaManager;

internal class PanoramaManager : ICorePanoramaManager
{
    private readonly ILogger<PanoramaManager> _logger;
    private readonly ICoreEntityManager       _entityManager;
    private readonly ICoreAssemblyManager     _assemblyManager;

    private readonly List<CustomHudClickedCallback>                               _clickListeners;
    private readonly Dictionary<ICustomHudLayout, List<CustomHudClickedCallback>> _clickCallbacks;
    private readonly Dictionary<uint, ICustomHudLayout>                           _managedLayouts;

    public PanoramaManager(ILogger<PanoramaManager> logger,
        ICoreEntityManager                          entityManager,
        ICoreAssemblyManager                        assemblyManager)
    {
        _logger          = logger;
        _entityManager   = entityManager;
        _assemblyManager = assemblyManager;

        _clickListeners = [];
        _clickCallbacks = [];

        _managedLayouts = [];

        Game.OnGameShutdown               += OnGameShutdown;
        Entity.OnEntityDeleted            += OnEntityDeleted;
        Panorama.OnCustomHudLayoutClicked += OnCustomHudLayoutClicked;

        assemblyManager.RegisterUnloadCleanup(ClearLeakedRegistrations);
    }

    public IEnumerable<ICustomHudLayout> GetManagedLayouts()
    {
        foreach (var entity in _managedLayouts.Values.ToArray())
        {
            if (!entity.IsValid() || entity.IsMarkedForDeletion())
            {
                continue;
            }

            yield return entity;
        }
    }

    public ICustomHudLayout? CreateLayout(string layoutResource, string? targetName = null)
    {
        if (string.IsNullOrWhiteSpace(layoutResource))
        {
            throw new ArgumentException("A compiled custom HUD layout resource is required.", nameof(layoutResource));
        }

        var keyValues = new Dictionary<string, KeyValuesVariantValueItem>
        {
            { "layout", layoutResource },
            { "targetname", string.IsNullOrWhiteSpace(targetName) ? "ms_custom_hud_layout" : targetName },
        };

        if (_entityManager.SpawnEntitySync<ICustomHudLayout>("custom_hud_layout", keyValues) is not { } layout)
        {
            return null;
        }

        _managedLayouts[layout.Handle.GetValue()] = layout;

        return layout;
    }

    public void InstallClickListener(CustomHudClickedCallback listener)
    {
        if (_assemblyManager.IsDelegateUnloaded(listener))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (_clickListeners.Contains(listener))
        {
            _logger.LogError("Custom HUD click listener is already installed.\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _clickListeners.Add(listener);
    }

    public void RemoveClickListener(CustomHudClickedCallback listener)
    {
        if (!_clickListeners.Remove(listener))
        {
            _logger.LogError("Custom HUD click listener has not been installed.\n{stackTrace}", Environment.StackTrace);
        }
    }

    public void InstallClickCallback(ICustomHudLayout layout, CustomHudClickedCallback callback)
    {
        if (_assemblyManager.IsDelegateUnloaded(callback))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (!_managedLayouts.ContainsKey(layout.Handle.GetValue()))
        {
            throw new InvalidOperationException("Entity is not a managed layout");
        }

        if (!_clickCallbacks.TryGetValue(layout, out var callbacks))
        {
            callbacks               = [];
            _clickCallbacks[layout] = callbacks;
        }

        if (callbacks.Contains(callback))
        {
            _logger.LogError("Custom HUD click callbacks is already installed.\n{stackTrace}", Environment.StackTrace);

            return;
        }

        callbacks.Add(callback);
    }

    public void RemoveClickCallback(ICustomHudLayout layout, CustomHudClickedCallback callback)
    {
        if (!_clickCallbacks.TryGetValue(layout, out var callbacks))
        {
            return;
        }

        if (!callbacks.Remove(callback))
        {
            _logger.LogError("Custom HUD click callback has not been installed.\n{stackTrace}", Environment.StackTrace);
        }
        else if (callbacks.Count <= 0)
        {
            _clickCallbacks.Remove(layout);
        }
    }

    private void ClearLeakedRegistrations()
    {
        _assemblyManager.ClearLeakedCallbacks(_clickListeners, "CustomHudClickListener", x => x);

        foreach (var layout in _clickCallbacks.Keys.ToArray())
        {
            var callbacks = _clickCallbacks[layout];

            _assemblyManager.ClearLeakedCallbacks(callbacks, "CustomHudClickCallback", x => x);

            if (callbacks.Count <= 0)
            {
                _clickCallbacks.Remove(layout);
            }
        }
    }

    private void OnGameShutdown()
    {
        _managedLayouts.Clear();

        // entity based callback will purge on map end, no leak here
        _clickCallbacks.Clear();
    }

    private void OnEntityDeleted(nint pointer)
    {
        var handle = BaseEntity.GetHandleValue(pointer);

        // not managed layout
        if (!_managedLayouts.Remove(handle, out var layout))
        {
            return;
        }

        if (_clickCallbacks.Remove(layout, out var callbacks))
        {
            // clear ALC refs
            callbacks.Clear();
        }
    }

    private void OnCustomHudLayoutClicked(nint playerPointer, nint layoutPointer, string buttonId)
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

        if (!_managedLayouts.ContainsKey(layout.Handle.GetValue()))
        {
            return;
        }

        BroadcastAll(player, layout, buttonId);
        BroadcastSingle(player, layout, buttonId);
    }

    private void BroadcastAll(PlayerController controller, CustomHudLayout layout, string buttonId)
        => Dispatch(_clickListeners, controller, layout, buttonId);

    private void BroadcastSingle(PlayerController controller, CustomHudLayout layout, string buttonId)
    {
        if (_clickCallbacks.TryGetValue(layout, out var callbacks))
        {
            // 这里因为有可能在Callback中Kill了, 会触发清理, 所以必须使用Clone Array
            Dispatch(callbacks, controller, layout, buttonId);
        }
    }

    private void Dispatch(List<CustomHudClickedCallback> handlers,
        PlayerController                                 controller,
        CustomHudLayout                                  layout,
        string                                           buttonId)
    {
        var count = handlers.Count;

        if (count == 0)
        {
            return;
        }

        var buffer = ArrayPool<CustomHudClickedCallback>.Shared.Rent(count);

        try
        {
            handlers.CopyTo(buffer);

            for (var i = 0; i < count; i++)
            {
                try
                {
                    buffer[i].Invoke(controller, layout, buttonId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                                     "An error occurred while calling custom HUD click handler {handler}",
                                     buffer[i].Method.DeclaringType?.Name ?? buffer[i].Method.Name);
                }
            }
        }
        finally
        {
            // for preventing ALC leak, clear array is required
            ArrayPool<CustomHudClickedCallback>.Shared.Return(buffer, true);
        }
    }
}
