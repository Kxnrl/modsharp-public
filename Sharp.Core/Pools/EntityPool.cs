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
using System.Diagnostics;
using Sharp.Core.Bridges.Forwards;
using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Core.Pools;

internal sealed class EntityPool<T> where T : class, IBaseEntity
{
    private readonly Dictionary<uint, T> _pool;

    public EntityPool()
    {
        _pool = new Dictionary<uint, T>(4096);

        Game.OnRoundRestart        += () => _pool.Clear();
        Game.OnGameActivate        += CleanEntityState;
        Game.OnGameShutdown        += CleanEntityState;
        Entity.OnEntityDeletedPost += OnEntityDeleted;
    }

    private void OnEntityDeleted(nint pointer)
    {
        if (pointer == nint.Zero)
        {
            return;
        }

        if (_pool.Count == 0)
        {
            return;
        }

        var handle = BaseEntity.GetHandle(pointer).GetValue();

        if (!_pool.Remove(handle, out var entity))
        {
            return;
        }

        if (entity is ContextObject context)
        {
            context.MarkAsDisposed();
        }
    }

    private void CleanEntityState()
    {
        if (_pool.Count == 0)
        {
            return;
        }

        foreach (var (_, e) in _pool)
        {
            if (e is ContextObject context)
            {
                context.MarkAsDisposed();
            }
        }

        _pool.Clear();
    }

    internal T? Find(CEntityHandle<IBaseEntity> handle)
        => _pool.GetValueOrDefault(handle.GetValue());

    internal void Set(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

#if DEBUG
        Debug.Print(
            $"Cached Entity {entity.GetAbsPtr()}::{entity.Handle.GetValue()}::{entity.Handle.GetEntryIndex().AsPrimitive()}::{entity.Handle.GetSerialNum()} as {typeof(T).Name} :: {entity.Classname}");
#endif

        if (BaseEntity.IsMarkedForDelete(entity.GetAbsPtr()))
        {
            return;
        }

        _pool[entity.Handle.GetValue()] = entity;
    }
}
