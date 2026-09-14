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
using System.Linq;
using Microsoft.Extensions.Logging;
using Sharp.Core.Objects;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using Native = Sharp.Core.Bridges.Natives.Event;
using Forward = Sharp.Core.Bridges.Forwards.Event;

namespace Sharp.Core.Managers;

internal interface ICoreEventManager : IEventManager;

// ReSharper disable ForCanBeConvertedToForeach
internal partial class EventManager : ICoreEventManager
{
    private readonly ILogger<EventManager> _logger;
    private readonly ICoreAssemblyManager  _assemblyManager;

    private readonly List<IEventListener> _listeners;

    public EventManager(ILogger<EventManager> logger, ICoreAssemblyManager assemblyManager)
    {
        _logger          = logger;
        _assemblyManager = assemblyManager;
        _listeners       = [];

        Forward.HookFireEvent += OnHookFireEvent;
        Forward.FireGameEvent += OnFireGameEvent;

        _assemblyManager.RegisterUnloadCleanup(() => _assemblyManager.ClearLeakedListeners(_listeners, "EventListener"));
    }

    private void OnFireGameEvent(nint ptr)
    {
        if (MakeEvent(ptr, false) is not { } @event)
        {
            return;
        }

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].FireGameEvent(@event);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name} :: {v}",
                                 nameof(OnFireGameEvent),
                                 _listeners[i].GetType().Name,
                                 @event.Name);
            }
        }

        if (@event is GameEvent ev)
        {
            ev.MarkAsDisposed();
        }
    }

    private bool OnHookFireEvent(nint ptr, ref bool serverOnly)
    {
        if (_listeners.Count == 0)
        {
            return false;
        }

        if (MakeEvent(ptr, true) is not { } @event)
        {
            return false;
        }

        var block  = false;
        var svOnly = serverOnly;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                if (_listeners[i].HookFireEvent(@event, ref svOnly))
                {
                    block = true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name} :: {v}",
                                 nameof(OnHookFireEvent),
                                 _listeners[i].GetType().Name,
                                 @event.Name);
            }
        }

        serverOnly = svOnly;

        if (@event is GameEvent ev)
        {
            ev.MarkAsDisposed();
        }

        return block;
    }

    public void InstallEventListener(IEventListener listener)
    {
        if (_assemblyManager.IsAssemblyUnloaded(listener.GetType().Assembly))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (listener.ListenerVersion != IEventListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_listeners.Any(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!");

            return;
        }

        _listeners.Add(listener);
        _listeners.Sort((x, y) => y.ListenerPriority.CompareTo(x.ListenerPriority));
    }

    public void RemoveEventListener(IEventListener listener)
    {
        if (!_listeners.Remove(listener))
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);
        }
    }

    private partial IGameEvent? MakeEvent(string name, nint ptr, bool editable);

    private IGameEvent? MakeEvent(nint ptr, bool editable)
        => ptr == nint.Zero ? null : MakeEvent(GameEvent.GetEventName(ptr), ptr, editable);

    public IGameEvent? CreateEvent(string name, bool force)
        => MakeEvent(name, Native.CreateEvent(name, force), true);

    public partial T? CreateEvent<T>(bool force) where T : class, IGameEvent;

    public partial T? CloneEvent<T>(T @event) where T : class, IGameEvent;

    public void HookEvent(string name)
        => Native.HookEvent(name);

    public bool FindListener(PlayerSlot slot, string name)
        => Native.FindListener(slot, name);
}
