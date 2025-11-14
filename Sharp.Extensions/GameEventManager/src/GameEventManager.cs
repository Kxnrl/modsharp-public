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
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Abstractions;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Extensions.GameEventManager;

internal class GameEventManager : IGameEventManager, ISharpExtension, IEventListener
{
    private readonly ILogger<GameEventManager> _logger;
    private readonly IEventManager             _manager;

    private readonly Dictionary<string, IGameEventManager.DelegateOnEventFired?>        _listeners;
    private readonly Dictionary<string, HashSet<IGameEventManager.DelegateOnHookEvent>> _hooks;
    private readonly HashSet<string>                                                    _events;

    public GameEventManager(ISharedSystem shared)
    {
        _logger  = shared.GetLoggerFactory().CreateLogger<GameEventManager>();
        _manager = shared.GetEventManager();

        _events    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _hooks     = new Dictionary<string, HashSet<IGameEventManager.DelegateOnHookEvent>>(StringComparer.OrdinalIgnoreCase);
        _listeners = new Dictionary<string, IGameEventManager.DelegateOnEventFired?>(StringComparer.OrdinalIgnoreCase);
    }

    public void Load()
    {
        _manager.InstallEventListener(this);
    }

    public void Shutdown()
    {
        _manager.RemoveEventListener(this);
    }

    // For this module should use this EventManager instead Shared.Managers.EventManager
    int IEventListener.ListenerPriority => int.MaxValue;
    int IEventListener.ListenerVersion  => IEventListener.ApiVersion;

    public bool HookFireEvent(IGameEvent e, ref bool serverOnly)
    {
        var eventName = e.Name;

        if (!_hooks.TryGetValue(eventName, out var callbacks))
        {
            return true;
        }

        var param  = false;
        var result = EHookAction.Ignored;

        foreach (var callback in callbacks)
        {
            try
            {
                var ac = callback(e, ref param);

                if (ac.Action > result)
                {
                    result = ac.Action;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling listener::HookFireEvent");
            }
        }

        // Block Event
        if (result == EHookAction.SkipCallReturnOverride)
        {
            return false;
        }

        // Allow Event
        if (result != EHookAction.Ignored)
        {
            serverOnly = param;
        }

        return true;
    }

    public void FireGameEvent(IGameEvent e)
    {
        if (_listeners.GetValueOrDefault(e.Name) is not { } action)
        {
            return;
        }

        foreach (var callback in action.GetInvocationList())
        {
            try
            {
                ((IGameEventManager.DelegateOnEventFired) callback).Invoke(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling listener::FireGameEvent");
            }
        }
    }

    public void HookEvent(string eventName, IGameEventManager.DelegateOnHookEvent callback)
    {
        if (_events.Add(eventName))
        {
            _manager.HookEvent(eventName);
        }

        if (!_hooks.ContainsKey(eventName))
        {
            _hooks[eventName] = [];
        }

        _hooks[eventName].Add(callback);
    }

    public void ListenEvent(string eventName, IGameEventManager.DelegateOnEventFired callback)
    {
        if (_events.Add(eventName))
        {
            _manager.HookEvent(eventName);
        }

        if (!_listeners.ContainsKey(eventName))
        {
            _listeners[eventName] = callback;
        }
        else
        {
            _listeners[eventName] += callback;
        }
    }

    public T? CreateEvent<T>(bool force) where T : class, IGameEvent
        => _manager.CreateEvent<T>(force);

    public IGameEvent? CreateEvent(string eventName, bool force)
        => _manager.CreateEvent(eventName, force);

    public T? CloneEvent<T>(T e) where T : class, IGameEvent
        => _manager.CloneEvent(e);
}