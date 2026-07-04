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
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Sharp.Core.Utilities;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using Sharp.Shared.Units;
using Native = Sharp.Core.Bridges.Natives.SendProxy;
using Forward = Sharp.Core.Bridges.Forwards.Extern.SendProxy;

namespace Sharp.Core.Managers;

internal interface ICoreSendProxyManager : ISendProxyManager;

internal class SendProxyManager : ICoreSendProxyManager
{
    private readonly ILogger<SendProxyManager> _logger;
    private readonly ICoreEntityManager        _entityManager;

    // (entity, murmur(field)) -> (field, callback); native routes by the hash, the field is kept for native Unhook.
    private readonly Dictionary<(int Entity, uint Hash), (string Field, ISendProxyManager.DelegateSendProxyBatch Callback)> _hooks;

    private readonly HashSet<AssemblyLoadContext> _tracked;

    public SendProxyManager(ILogger<SendProxyManager> logger, ICoreEntityManager entityManager)
    {
        _logger         = logger;
        _entityManager  = entityManager;
        _hooks          = new();
        _tracked        = [];

        Forward.OnSendProxyBatch += OnSendProxyBatch;
    }

    public void Hook(IBaseEntity entity, string field, ISendProxyManager.DelegateSendProxyBatch callback)
    {
        var index = entity.Index.AsPrimitive();
        var key   = (index, MurmurHash2.Compute(field));

        if (_hooks.TryGetValue(key, out var existing) && existing.Callback != callback)
        {
            _logger.LogWarning("SendProxy hook on entity {Entity} field {Field} replaced by another registration", index, field);
        }

        _hooks[key] = (field, callback);
        TrackOwner(callback);
        Native.HookField((EntityIndex) index, field);
    }

    public void Unhook(IBaseEntity entity, string field)
    {
        var index = entity.Index.AsPrimitive();
        if (_hooks.Remove((index, MurmurHash2.Compute(field))))
        {
            Native.UnhookField((EntityIndex) index, field);
        }
    }

    public void UnhookEntity(IBaseEntity entity)
    {
        var index = entity.Index.AsPrimitive();
        RemoveWhere(k => k.Entity == index);
        Native.ClearEntity((EntityIndex) index);
    }

    // Drop a module's hooks if it unloads without calling Unhook. The strong delegate refs mean the ALC only
    // unloads on the explicit main-thread unload, so this never fires on a GC/finalizer thread.
    private void TrackOwner(ISendProxyManager.DelegateSendProxyBatch callback)
    {
        var alc = AssemblyLoadContext.GetLoadContext(callback.Method.Module.Assembly);
        if (alc is { IsCollectible: true } && _tracked.Add(alc))
        {
            alc.Unloading += OnOwnerUnloading;
        }
    }

    private void OnOwnerUnloading(AssemblyLoadContext alc)
    {
        _tracked.Remove(alc);
        RemoveWhere(k => AssemblyLoadContext.GetLoadContext(_hooks[k].Callback.Method.Module.Assembly) == alc);
    }

    private void RemoveWhere(Func<(int Entity, uint Hash), bool> predicate)
    {
        List<(int Entity, uint Hash)>? toRemove = null;
        foreach (var key in _hooks.Keys)
        {
            if (predicate(key))
            {
                (toRemove ??= new List<(int, uint)>()).Add(key);
            }
        }

        if (toRemove is null)
        {
            return;
        }

        foreach (var key in toRemove)
        {
            var field = _hooks[key].Field;
            _hooks.Remove(key);
            Native.UnhookField((EntityIndex) key.Entity, field);
        }
    }

    private void OnSendProxyBatch(int entityIndex, uint fieldHash, int fieldType, nint ptrBatch)
    {
        // Main thread, inside the per-client encode; must not throw across the unmanaged boundary (fail-fasts).
        try
        {
            if (!_hooks.TryGetValue((entityIndex, fieldHash), out var hook))
            {
                return;
            }

            var entity = _entityManager.FindEntityByIndex((EntityIndex) entityIndex);
            if (entity is null)
            {
                return;
            }

            var batch = new SendProxyBatch(ptrBatch, (SendProxyValueKind) fieldType);
            hook.Callback(entity, batch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendProxy callback threw for entity {Entity}", entityIndex);
        }
    }
}
