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
using System.Linq;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameEntities;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;
using Native = Sharp.Core.Bridges.Natives.Entity;
using Forward = Sharp.Core.Bridges.Forwards.Entity;

namespace Sharp.Core.Managers;

internal interface ICoreEntityManager : IEntityManager;

// ReSharper disable ForCanBeConvertedToForeach
internal class EntityManager : ICoreEntityManager
{
    private readonly List<IEntityListener>  _listeners;
    private readonly ILogger<EntityManager> _logger;

    public EntityManager(ILogger<EntityManager> logger)
    {
        _logger    = logger;
        _listeners = [];

        Forward.OnEntityCreated     += OnEntityCreated;
        Forward.OnEntityDeleted     += OnEntityDeleted;
        Forward.OnEntitySpawned     += OnEntitySpawned;
        Forward.OnEntityFollowed    += OnEntityFollowed;
        Forward.OnEntityFireOutput  += OnEntityFireOutput;
        Forward.OnEntityAcceptInput += OnEntityAcceptInput;
    }

    private void OnEntityCreated(nint ptr)
    {
        var entity = BaseEntity.Create(ptr);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntityCreated");

            return;
        }

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnEntityCreated(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntityCreated),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private void OnEntityDeleted(nint ptr)
    {
        var entity = BaseEntity.Create(ptr);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntityDeleted");

            return;
        }

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnEntityDeleted(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntityDeleted),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private void OnEntitySpawned(nint ptr)
    {
        var entity = BaseEntity.Create(ptr);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntitySpawned");

            return;
        }

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnEntitySpawned(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntitySpawned),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private void OnEntityFollowed(nint ptr, nint ownerPtr)
    {
        var entity = BaseEntity.Create(ptr);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntitySpawned");

            return;
        }

        var owner = BaseEntity.Create(ownerPtr);

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnEntityFollowed(entity, owner);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntityFollowed),
                                 _listeners[i].GetType().Name);
            }
        }
    }

    private EHookAction OnEntityFireOutput(nint ptrEntity, string output, nint ptrActivator, float delay)
    {
        var entity = BaseEntity.Create(ptrEntity);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntitySpawned");

            return EHookAction.Ignored;
        }

        var activator = BaseEntity.Create(ptrActivator);
        var action    = EHookAction.Ignored;

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                var low = _listeners[i].OnEntityFireOutput(entity, output, activator, delay);

                if (low > action)
                {
                    action = low;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name} :: {output}",
                                 nameof(OnEntityFireOutput),
                                 _listeners[i].GetType().Name,
                                 output);
            }
        }

        return action;
    }

    private EHookAction OnEntityAcceptInput(nint ptrEntity,
        string                                   input,
        in EntityVariant                         variant,
        nint                                     ptrActivator,
        nint                                     ptrCaller)
    {
        var entity = BaseEntity.Create(ptrEntity);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntitySpawned");

            return EHookAction.Ignored;
        }

        var activator = BaseEntity.Create(ptrActivator);
        var caller    = BaseEntity.Create(ptrCaller);
        var action    = EHookAction.Ignored;

        for (var i = 0; i < _listeners.Count; i++)
        {
            var listener = _listeners[i];

            try
            {
                var low = listener.OnEntityAcceptInput(entity, input, in variant, activator, caller);

                if (low > action)
                {
                    action = low;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name} :: {output}",
                                 nameof(OnEntityAcceptInput),
                                 _listeners[i].GetType().Name,
                                 input);
            }
        }

        return action;
    }

    public void InstallEntityListener(IEntityListener listener)
    {
        if (listener.ListenerVersion != IEntityListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_listeners.Any(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _listeners.Add(listener);
        _listeners.Sort((x, y) => y.ListenerPriority.CompareTo(x.ListenerPriority));
    }

    public void RemoveEntityListener(IEntityListener listener)
    {
        if (!_listeners.Remove(listener))
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);
        }
    }

    public T? FindEntityByHandle<T>(CEntityHandle<T> eHandle) where T : class, IBaseEntity
        => BaseEntity.Create(Native.FindByEHandle(eHandle.GetValue())) is not { } entity ? null : entity.As<T>();

    public IBaseEntity? FindEntityByIndex(EntityIndex index)
        => BaseEntity.Create(Native.FindByIndex(index));

    public T MakeEntityFromPointer<T>(IntPtr entity) where T : class, IBaseEntity
        => BaseEntity.Create(entity)?.As<T>() ?? throw new InvalidOperationException("nullptr");

    public T? FindEntityByIndex<T>(EntityIndex index) where T : class, IBaseEntity
        => FindEntityByIndex(index) is not { } entity ? null : entity.As<T>();

    public IBaseEntity? FindEntityByClassname(IBaseEntity? start, string classname)
        => BaseEntity.Create(Native.FindByClassname(start?.GetAbsPtr() ?? IntPtr.Zero, classname));

    public IBaseEntity? FindEntityByName(IBaseEntity? start, string name)
        => BaseEntity.Create(Native.FindByName(start?.GetAbsPtr() ?? IntPtr.Zero, name));

    public unsafe IBaseEntity? FindEntityInSphere(IBaseEntity? start, Vector center, float radius)
        => BaseEntity.Create(Native.FindInSphere(start?.GetAbsPtr() ?? IntPtr.Zero, &center, radius));

    public unsafe IBaseEntity? SpawnEntitySync(string          classname,
        IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues)
    {
        var items = keyValues.Select((x) => new KeyValuesVariantItem(x.Key, x.Value)).ToArray();
        var count = items.Length;

        fixed (KeyValuesVariantItem* ptr = items)
        {
            return BaseEntity.Create(Native.SpawnEntitySync(classname, ptr, count));
        }
    }

    public T? SpawnEntitySync<T>(string                        classname,
        IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues) where T : class, IBaseEntity
    {
        var e = SpawnEntitySync(classname, keyValues);

        return e?.As<T>();
    }

    public IBaseEntity? CreateEntityByName(string classname)
        => BaseEntity.Create(Native.CreateByName(classname));

    public T? CreateEntityByName<T>(string classname) where T : class, IBaseEntity
    {
        var e = CreateEntityByName(classname);

        return e?.As<T>();
    }

    public CUtlSymbolLarge AllocPooledString(string content)
        => new (Native.AllocPooledString(content));

    public void HookEntityOutput(string classname, string output)
        => Native.HookOutput(classname, output);

    public void HookEntityInput(string classname, string input)
        => Native.HookInput(classname, input);

    public IPlayerPawn? FindPlayerPawnBySlot(PlayerSlot slot)
        => PlayerPawn.Create(Player.PawnFindBySlot(slot));

    public IPlayerController? FindPlayerControllerBySlot(PlayerSlot slot)
        => PlayerController.Create(Player.ControllerFindBySlot(slot));

    public bool UpdateEconItemAttributes(IBaseEntity entity,
        uint                                         accountId,
        string                                       nameTag,
        int                                          paint,
        int                                          pattern,
        float                                        wear,
        int                                          nSticker1,
        float                                        flSticker1,
        int                                          nSticker2,
        float                                        flSticker2,
        int                                          nSticker3,
        float                                        flSticker3,
        int                                          nSticker4,
        float                                        flSticker4)
    {
        if (!entity.IsValid())
        {
            throw new InvalidOperationException("Entity is invalid");
        }

        return Econ.UpdateItemAttributes(entity.GetAbsPtr(),
                                         accountId,
                                         nameTag,
                                         paint,
                                         pattern,
                                         wear,
                                         nSticker1,
                                         flSticker1,
                                         nSticker2,
                                         flSticker2,
                                         nSticker3,
                                         flSticker3,
                                         nSticker4,
                                         flSticker4);
    }

    public IBaseTeam? GetGlobalCStrikeTeam(CStrikeTeam team)
        => BaseTeam.Create(Native.GetGlobalCStrikeTeam(team));
}
