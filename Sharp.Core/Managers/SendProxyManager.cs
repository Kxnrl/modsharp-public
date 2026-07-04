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

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Units;
using CoreSendProxy = Sharp.Core.Bridges.Natives.SendProxy;

namespace Sharp.Core.Managers;

internal interface ICoreSendProxyManager : ISendProxyManager;

internal sealed class SendProxyManager : ICoreSendProxyManager
{
    private readonly ILogger<SendProxyManager> _logger;
    private readonly ICoreEntityManager        _entityManager;

    // (entityIndex, field) -> callback. All access on the main thread (registration + per-client dispatch +
    // module-unload purge, which runs on the main thread while this holds a strong ref to each delegate).
    private readonly Dictionary<(int Entity, string Field), SendProxyCallback> _hooks = new();

    // ALCs we've subscribed to so a module's hooks are purged if it unloads without cleaning up itself.
    private readonly HashSet<AssemblyLoadContext> _tracked = new();

    public SendProxyManager(ILogger<SendProxyManager> logger, ICoreEntityManager entityManager)
    {
        _logger                                              = logger;
        _entityManager                                       = entityManager;
        Bridges.Forwards.Extern.SendProxy.OnSendProxyBatch += Dispatch;
    }

    public void Hook(IBaseEntity entity, string field, SendProxyCallback callback)
    {
        var index = entity.Index.AsPrimitive();
        var key   = (index, field);

        if (_hooks.TryGetValue(key, out var existing) && existing != callback)
        {
            _logger.LogWarning("SendProxy hook on entity {Entity} field {Field} replaced by another registration", index, field);
        }

        _hooks[key] = callback;
        TrackOwner(callback);
        CoreSendProxy.HookField(index, field);
    }

    public void Unhook(IBaseEntity entity, string field)
    {
        var index = entity.Index.AsPrimitive();
        if (_hooks.Remove((index, field)))
        {
            CoreSendProxy.UnhookField(index, field);
        }
    }

    public void UnhookEntity(IBaseEntity entity)
    {
        var index = entity.Index.AsPrimitive();
        RemoveWhere(k => k.Entity == index);
        CoreSendProxy.ClearEntity(index);
    }

    // Subscribe once per module ALC so its hooks are dropped if the module unloads without calling Unhook.
    // The manager holds a strong ref to every delegate, so a collectible ALC can only unload via the explicit
    // main-thread unload — this fires there, never on a GC/finalizer thread.
    private void TrackOwner(SendProxyCallback callback)
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
        RemoveWhere(k => AssemblyLoadContext.GetLoadContext(_hooks[k].Method.Module.Assembly) == alc);
    }

    private void RemoveWhere(System.Func<(int Entity, string Field), bool> predicate)
    {
        List<(int Entity, string Field)>? toRemove = null;
        foreach (var key in _hooks.Keys)
        {
            if (predicate(key))
            {
                (toRemove ??= new List<(int, string)>()).Add(key);
            }
        }

        if (toRemove is null)
        {
            return;
        }

        foreach (var key in toRemove)
        {
            _hooks.Remove(key);
            CoreSendProxy.UnhookField(key.Entity, key.Field);
        }
    }

    // Fires once per tick for a proxied (entity, field). Resolves the entity + callback once and lets the
    // callback fill the native per-slot batch table; native then applies it to every receiver this tick.
    private void Dispatch(int entityIndex, nint ptrField, int fieldType, nint ptrBatch)
    {
        var field = Marshal.PtrToStringUTF8(ptrField);
        if (field is null || !_hooks.TryGetValue((entityIndex, field), out var callback))
        {
            return;
        }

        var entity = _entityManager.FindEntityByIndex((EntityIndex) entityIndex);
        if (entity is null)
        {
            return;
        }

        // Runs on the main thread deep inside the per-client encode; a module exception must never escape the
        // unmanaged boundary (that fail-fasts the server) — log it and leave the real value.
        try
        {
            var batch = new SendProxyBatch(ptrBatch, (SendProxyValueKind) fieldType);
            callback(entity, batch);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "SendProxy callback threw for entity {Entity} field {Field}", entityIndex, field);
        }
    }
}
