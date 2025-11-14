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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using static Sharp.Extensions.EntityHookManager.IEntityHookManager;

namespace Sharp.Extensions.EntityHookManager;

internal class EntityHookManager : IEntityHookManager, ISharpExtension, IEntityListener
{
    private readonly ILogger<EntityHookManager> _logger;
    private readonly IEntityManager             _manager;

    private readonly Dictionary<string, EventDelegate?> _createdEvent;
    private readonly Dictionary<string, EventDelegate?> _spawnedEvent;
    private readonly Dictionary<string, EventDelegate?> _deletedEvent;

    // custom wildcard
    private WeaponEventDelegate? _weaponSpawn;

    // [output][[classname][callback]] = callback
    private readonly Dictionary<string, Dictionary<string, OutputDelegate?>> _outputHook;
    private readonly Dictionary<string, Dictionary<string, InputDelegate?>>  _inputHook;

    public EntityHookManager(ISharedSystem shared)
    {
        _logger  = shared.GetLoggerFactory().CreateLogger<EntityHookManager>();
        _manager = shared.GetEntityManager();

        _createdEvent = new Dictionary<string, EventDelegate?>(StringComparer.OrdinalIgnoreCase);
        _spawnedEvent = new Dictionary<string, EventDelegate?>(StringComparer.OrdinalIgnoreCase);
        _deletedEvent = new Dictionary<string, EventDelegate?>(StringComparer.OrdinalIgnoreCase);

        _outputHook = new Dictionary<string, Dictionary<string, OutputDelegate?>>(StringComparer.OrdinalIgnoreCase);
        _inputHook  = new Dictionary<string, Dictionary<string, InputDelegate?>>(StringComparer.OrdinalIgnoreCase);
    }

    public void Load()
    {
        _manager.InstallEntityListener(this);
    }

    public void Shutdown()
    {
        _manager.RemoveEntityListener(this);
    }

    private void CallEventDelegate(EventDelegate action, string caller, string classname, IBaseEntity entity)
    {
        foreach (var callback in action.GetInvocationList())
        {
            try
            {
                ((EventDelegate) callback).Invoke(classname, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling listener::{caller}", caller);
            }
        }
    }

    int IEntityListener.ListenerPriority => int.MaxValue;
    int IEntityListener.ListenerVersion  => IEntityListener.ApiVersion;

    public void OnEntityCreated(IBaseEntity entity)
    {
        if (_createdEvent.Count == 0)
        {
            return;
        }

        var classname = entity.Classname;

        if (_createdEvent.GetValueOrDefault(classname) is not { } callback)
        {
            return;
        }

        CallEventDelegate(callback, nameof(OnEntityCreated), classname, entity);
    }

    public void OnEntityDeleted(IBaseEntity entity)
    {
        if (_deletedEvent.Count == 0)
        {
            return;
        }

        var classname = entity.Classname;

        if (_deletedEvent.GetValueOrDefault(classname) is not { } callback)
        {
            return;
        }

        CallEventDelegate(callback, nameof(OnEntityDeleted), classname, entity);
    }

    public void OnEntitySpawned(IBaseEntity entity)
    {
        if (_spawnedEvent.Count == 0 && _weaponSpawn is null)
        {
            return;
        }

        var classname = entity.Classname;

        if (_weaponSpawn is not null && entity.AsBaseWeapon() is { } weapon)
        {
            foreach (var e in _weaponSpawn.GetInvocationList())
            {
                try
                {
                    ((WeaponEventDelegate) e).Invoke(classname, weapon);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while calling listener::{caller}", nameof(OnEntitySpawned));
                }
            }
        }

        if (_spawnedEvent.GetValueOrDefault(classname) is not { } callback)
        {
            return;
        }

        CallEventDelegate(callback, nameof(OnEntitySpawned), classname, entity);
    }

    public EHookAction OnEntityFireOutput(IBaseEntity entity, string output, IBaseEntity? activator, float delay)
    {
        if (_outputHook.Count == 0 || !_outputHook.TryGetValue(output, out var classCallbacks))
        {
            return EHookAction.Ignored;
        }

        var classname = entity.Classname;

        if (classCallbacks.GetValueOrDefault(classname) is not { } callbacks)
        {
            return EHookAction.Ignored;
        }

        var result = EHookAction.Ignored;

        foreach (var callback in callbacks.GetInvocationList())
        {
            try
            {
                ((OutputDelegate) callback).Invoke(classname, output, entity, activator, delay, ref result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling listener::{caller}", nameof(OnEntityFireOutput));
            }
        }

        return result;
    }

    public EHookAction OnEntityAcceptInput(IBaseEntity entity,
        string                                         input,
        in EntityVariant                               value,
        IBaseEntity?                                   activator,
        IBaseEntity?                                   caller)
    {
        if (_inputHook.Count == 0 || !_inputHook.TryGetValue(input, out var classCallbacks))
        {
            return EHookAction.Ignored;
        }

        var classname = entity.Classname;

        if (classCallbacks.GetValueOrDefault(classname) is not { } callbacks)
        {
            return EHookAction.Ignored;
        }

        var result = EHookAction.Ignored;

        foreach (var callback in callbacks.GetInvocationList())
        {
            try
            {
                ((InputDelegate) callback).Invoke(classname, input, value, entity, activator, caller, ref result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling listener::{caller}", nameof(OnEntityFireOutput));
            }
        }

        return result;
    }

    public void ListenEntityCreate(string classname, EventDelegate callback)
    {
        if (!_createdEvent.ContainsKey(classname))
        {
            _createdEvent[classname] = callback;
        }
        else
        {
            _createdEvent[classname] += callback;
        }
    }

    public void ListenEntityDelete(string classname, EventDelegate callback)
    {
        if (!_deletedEvent.ContainsKey(classname))
        {
            _deletedEvent[classname] = callback;
        }
        else
        {
            _deletedEvent[classname] += callback;
        }
    }

    public void ListenEntitySpawn(string classname, EventDelegate callback)
    {
        if (!_spawnedEvent.ContainsKey(classname))
        {
            _spawnedEvent[classname] = callback;
        }
        else
        {
            _spawnedEvent[classname] += callback;
        }
    }

    public void ListenWeaponSpawn(WeaponEventDelegate callback)
    {
        _weaponSpawn += callback;
    }

    public void HookEntityInput(string classname, string input, InputDelegate callback)
    {
        if (_inputHook.TryGetValue(input, out var classCallbacks))
        {
            if (classCallbacks.ContainsKey(classname))
            {
                classCallbacks[classname] += callback;
            }
            else
            {
                classCallbacks[classname] = callback;
                _manager.HookEntityInput(classname, input);
            }
        }
        else
        {
            _inputHook[input]
                = new Dictionary<string, InputDelegate?>(StringComparer.OrdinalIgnoreCase) { [classname] = callback };

            _manager.HookEntityInput(classname, input);
        }
    }

    public void HookEntityOutput(string classname, string output, OutputDelegate callback)
    {
        if (_outputHook.TryGetValue(output, out var classCallbacks))
        {
            if (classCallbacks.ContainsKey(classname))
            {
                classCallbacks[classname] += callback;
            }
            else
            {
                classCallbacks[classname] = callback;
                _manager.HookEntityOutput(classname, output);
            }
        }
        else
        {
            _outputHook[output]
                = new Dictionary<string, OutputDelegate?>(StringComparer.OrdinalIgnoreCase) { [classname] = callback };

            _manager.HookEntityOutput(classname, output);
        }
    }
}
