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
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameEntities;
using Sharp.Core.Helpers;
using Sharp.Core.Objects;
using Sharp.Core.Types;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;
using Forward = Sharp.Core.Bridges.Forwards.Entity;
using Native = Sharp.Core.Bridges.Natives.Entity;

namespace Sharp.Core.Managers;

internal interface ICoreEntityManager : IEntityManager;

// ReSharper disable ForCanBeConvertedToForeach
internal class EntityManager : ICoreEntityManager
{
    private readonly ILogger<EntityManager> _logger;
    private readonly ICoreAssemblyManager   _assemblyManager;

    private readonly SubscriberList<IEntityListener> _listeners;

    private PlayerSlot _maxSlots;

    public EntityManager(ILogger<EntityManager> logger, ICoreAssemblyManager assemblyManager)
    {
        _logger          = logger;
        _assemblyManager = assemblyManager;
        _listeners       = new SubscriberList<IEntityListener>(x => x.ListenerPriority);

        Forward.OnEntityCreated     += OnEntityCreated;
        Forward.OnEntityDeleted     += OnEntityDeleted;
        Forward.OnEntitySpawned     += OnEntitySpawned;
        Forward.OnEntityFollowed    += OnEntityFollowed;
        Forward.OnEntityFireOutput  += OnEntityFireOutput;
        Forward.OnEntityAcceptInput += OnEntityAcceptInput;

        Bridges.Forwards.Game.OnServerInit += OnServerInit;

        _assemblyManager.RegisterUnloadCleanup(() => _listeners.Edit(x => _assemblyManager.ClearLeakedListeners(x, "EntityListener")));
    }

    private void OnServerInit()
    {
        var globalVars = GlobalVars.Create(Bridges.Natives.Core.GetGlobals());

        _maxSlots = new PlayerSlot((byte) (globalVars?.MaxClients ?? 1));
    }

    private void OnEntityCreated(nint ptr)
    {
        var entity = BaseEntity.Create(ptr);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntityCreated");

            return;
        }

        foreach (var listener in _listeners)
        {
            try
            {
                listener.OnEntityCreated(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntityCreated),
                                 listener.GetType().Name);
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

        foreach (var listener in _listeners)
        {
            try
            {
                listener.OnEntityDeleted(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntityDeleted),
                                 listener.GetType().Name);
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

        foreach (var listener in _listeners)
        {
            try
            {
                listener.OnEntitySpawned(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntitySpawned),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnEntityFollowed(nint ptr, nint ownerPtr)
    {
        var entity = BaseEntity.Create(ptr);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntityFollowed");

            return;
        }

        var owner = BaseEntity.Create(ownerPtr);

        foreach (var listener in _listeners)
        {
            try
            {
                listener.OnEntityFollowed(entity, owner);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnEntityFollowed),
                                 listener.GetType().Name);
            }
        }
    }

    private EHookAction OnEntityFireOutput(nint ptrEntity, string output, nint ptrActivator, float delay)
    {
        var entity = BaseEntity.Create(ptrEntity);

        if (entity is null)
        {
            _logger.LogError("Entity is nullptr in OnEntityFireOutput");

            return EHookAction.Ignored;
        }

        var activator = BaseEntity.Create(ptrActivator);
        var action    = EHookAction.Ignored;

        foreach (var listener in _listeners)
        {
            try
            {
                var low = listener.OnEntityFireOutput(entity, output, activator, delay);

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
                                 listener.GetType().Name,
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
            _logger.LogError("Entity is nullptr in OnEntityAcceptInput");

            return EHookAction.Ignored;
        }

        var activator = BaseEntity.Create(ptrActivator);
        var caller    = BaseEntity.Create(ptrCaller);
        var action    = EHookAction.Ignored;

        foreach (var listener in _listeners)
        {
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
                                 listener.GetType().Name,
                                 input);
            }
        }

        return action;
    }

    public void InstallEntityListener(IEntityListener listener)
    {
        if (_assemblyManager.IsAssemblyUnloaded(listener.GetType().Assembly))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (listener.ListenerVersion != IEntityListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_listeners.Exists(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _listeners.Add(listener);
    }

    public void RemoveEntityListener(IEntityListener listener)
    {
        if (!_listeners.Remove(listener))
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);
        }
    }

    private enum EntityClassNameKind
    {
        Classname,
        CppClass,
    }

    public IBaseEntity[] GetAllEntitiesByClassname(string classname)
        => GetAllEntitiesByClassnameCore<IBaseEntity>(classname, EntityClassNameKind.Classname);

    public IBaseEntity[] GetAllEntitiesByCppClass(string cppClass)
        => GetAllEntitiesByClassnameCore<IBaseEntity>(cppClass, EntityClassNameKind.CppClass);

    public T[] GetAllEntitiesByClassname<T>(string classname) where T : class, IBaseEntity
        => GetAllEntitiesByClassnameCore<T>(classname, EntityClassNameKind.Classname);

    public T[] GetAllEntitiesByCppClass<T>(string cppClass) where T : class, IBaseEntity
        => GetAllEntitiesByClassnameCore<T>(cppClass, EntityClassNameKind.CppClass);

    private static unsafe int EnumerateEntityPointers(string name, nint* buffer, int capacity, EntityClassNameKind kind)
        => kind == EntityClassNameKind.CppClass
            ? Native.EnumerateByCppClass(name, buffer, capacity)
            : Native.FindAllByClassname(name, buffer, capacity);

    private static unsafe T[] GetAllEntitiesByClassnameCore<T>(string name, EntityClassNameKind kind)
        where T : class, IBaseEntity
    {
        const int maxEntityCount = 32768;

        var rented = ArrayPool<nint>.Shared.Rent(maxEntityCount);

        try
        {
            int total;

            fixed (nint* p = rented)
            {
                total = EnumerateEntityPointers(name, p, rented.Length, kind);
            }

            if (total > rented.Length)
            {
                throw new InvalidOperationException($"Entity count {total} exceeds the engine limit {maxEntityCount}");
            }

            return total == 0 ? [] : WrapEntities<T>(rented.AsSpan(0, total));
        }
        finally
        {
            ArrayPool<nint>.Shared.Return(rented);
        }
    }

    private static T[] WrapEntities<T>(ReadOnlySpan<nint> pointers) where T : class, IBaseEntity
    {
        var result  = new T[pointers.Length];
        var written = 0;

        foreach (var pointer in pointers)
        {
            if (BaseEntity.Create(pointer)?.As<T>() is { } entity)
            {
                result[written++] = entity;
            }
        }

        if (written != result.Length)
        {
            Array.Resize(ref result, written);
        }

        return result;
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

    public IBaseEntity? FindEntityByCppClass(IBaseEntity? start, string cppClass)
        => BaseEntity.Create(Native.FindByCppClass(start?.GetAbsPtr() ?? IntPtr.Zero, cppClass));

    public IBaseEntity? FindEntityByName(IBaseEntity? start, string name)
        => BaseEntity.Create(Native.FindByName(start?.GetAbsPtr() ?? IntPtr.Zero, name));

    public unsafe IBaseEntity? FindEntityInSphere(IBaseEntity? start, Vector center, float radius)
        => BaseEntity.Create(Native.FindInSphere(start?.GetAbsPtr() ?? IntPtr.Zero, &center, radius));

    [SkipLocalsInit]
    public unsafe IBaseEntity? SpawnEntitySync(string          classname,
        IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues)
    {
        var count = keyValues.Count;

        if (count == 0)
        {
            return BaseEntity.Create(Native.SpawnEntitySync(classname, null, 0));
        }

        if (count > 128)
        {
            throw new ArgumentException("Too many key values");
        }

        var items = stackalloc EntityKeyValuesVariant[count];

        var index = 0;

        foreach (var (key, value) in keyValues)
        {
            ref var item = ref items[index];
            item.Update(key, value);
            index++;
        }

        return BaseEntity.Create(Native.SpawnEntitySync(classname, items, count));
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

    public unsafe IBaseEntity? CreateTrigger(Vector origin, Vector mins, Vector maxs)
        => BaseEntity.Create(Native.CreateTrigger(&origin, &mins, &maxs));

    public CUtlSymbolLarge AllocPooledString(string content)
        => new (Native.AllocPooledString(content));

    public void HookEntityOutput(string classname, string output)
        => Native.HookOutput(classname, output);

    public void HookEntityInput(string classname, string input)
        => Native.HookInput(classname, input);

    public IBasePlayerPawn? FindPlayerPawnBySlot(PlayerSlot slot)
        => BasePlayerPawn.Create(Player.PawnFindBySlot(slot));

    public IPlayerController? FindPlayerControllerBySlot(PlayerSlot slot)
        => PlayerController.Create(Player.ControllerFindBySlot(slot));

    public IEnumerable<IPlayerController> GetPlayerControllers(bool inGame = true)
    {
        for (PlayerSlot slot = 0; slot < _maxSlots; slot++)
        {
            if (FindPlayerControllerBySlot(slot) is not { } controller)
            {
                continue;
            }

            if (!inGame || controller.IsConnected())
            {
                yield return controller;
            }
        }
    }

    public List<IPlayerController> FindPlayerControllers(bool inGame = true)
    {
        var list = new List<IPlayerController>(_maxSlots.AsPrimitive());

        for (PlayerSlot slot = 0; slot < _maxSlots; slot++)
        {
            if (FindPlayerControllerBySlot(slot) is not { } controller)
            {
                continue;
            }

            if (!inGame || controller.IsConnected())
            {
                list.Add(controller);
            }
        }

        return list;
    }

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
        => Econ.UpdateItemAttributes(entity.GetAbsPtr(),
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

    public IBaseTeam? GetGlobalCStrikeTeam(CStrikeTeam team)
        => BaseTeam.Create(Native.GetGlobalCStrikeTeam(team));
}
