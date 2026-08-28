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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Managers;

internal interface ICoreAssemblyManager
{
    void MarkAssemblyUnloaded(Assembly assembly);

    bool IsAssemblyUnloaded(Assembly assembly);

    bool IsDelegateUnloaded(Delegate callback);

    bool IsDelegateOwnedBy(Delegate callback, Assembly anchorAssembly);

    void RegisterUnloadCleanup(Action callback);

    void InvokeUnloadCleanup();

    void ClearLeaked<T>(List<T> list, string kind, Func<T, Assembly> owner, Func<T, string> describe);

    void ClearLeakedListeners<T>(List<T> listeners, string kind) where T : class;

    void ClearLeakedCallbacks<T>(List<T> list, string kind, Func<T, Delegate> callback);

    T? ClearLeakedMulticast<T>(T? multicast, string kind, string context) where T : Delegate;
}

internal class AssemblyManager : ICoreAssemblyManager
{
    private readonly ILogger<AssemblyManager> _logger;

    private readonly ConditionalWeakTable<AssemblyLoadContext, object> _unloadedContexts;
    private readonly List<Action>                                      _unloadCleanups;

    public AssemblyManager(ILogger<AssemblyManager> logger)
    {
        _logger = logger;

        _unloadedContexts = new ConditionalWeakTable<AssemblyLoadContext, object>();
        _unloadCleanups   = [];
    }

    public void MarkAssemblyUnloaded(Assembly assembly)
    {
        var alc = AssemblyLoadContext.GetLoadContext(assembly);

        if (alc is null || alc == AssemblyLoadContext.Default)
        {
            _logger.LogError("Refused to mark {assembly} as unloaded: not in a collectible ALC", assembly.FullName);

            return;
        }

        _unloadedContexts.AddOrUpdate(alc, this);
    }

    public bool IsAssemblyUnloaded(Assembly assembly)
        => AssemblyLoadContext.GetLoadContext(assembly) is { } alc && _unloadedContexts.TryGetValue(alc, out _);

    public bool IsDelegateUnloaded(Delegate callback)
    {
        if (callback.HasSingleTarget)
        {
            return IsSingleDelegateUnloaded(callback);
        }

        foreach (var single in callback.GetInvocationList())
        {
            if (IsSingleDelegateUnloaded(single))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSingleDelegateUnloaded(Delegate callback)
        => IsAssemblyUnloaded(callback.Method.Module.Assembly)
           || (callback.Target is { } target && IsAssemblyUnloaded(target.GetType().Assembly));

    public bool IsDelegateOwnedBy(Delegate callback, Assembly anchorAssembly)
    {
        if (AssemblyLoadContext.GetLoadContext(anchorAssembly) is not { } anchor)
        {
            return false;
        }

        return AssemblyLoadContext.GetLoadContext(callback.Method.Module.Assembly) == anchor
               || (callback.Target is { } target
                   && AssemblyLoadContext.GetLoadContext(target.GetType().Assembly) == anchor);
    }

    public void RegisterUnloadCleanup(Action callback)
        => _unloadCleanups.Add(callback);

    public void InvokeUnloadCleanup()
    {
        for (var i = 0; i < _unloadCleanups.Count; i++)
        {
            try
            {
                _unloadCleanups[i].Invoke();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while invoking unload cleanup");
            }
        }
    }

    public void ClearLeaked<T>(List<T> list, string kind, Func<T, Assembly> owner, Func<T, string> describe)
    {
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var item = list[i];

            if (!IsAssemblyUnloaded(owner(item)))
            {
                continue;
            }

            list.RemoveAt(i);

            _logger.LogWarning("{kind} leaked by unloaded module, removed: {detail}", kind, describe(item));
        }
    }

    public void ClearLeakedListeners<T>(List<T> listeners, string kind) where T : class
        => ClearLeaked(listeners, kind, x => x.GetType().Assembly, x => x.GetType().FullName ?? x.GetType().Name);

    public void ClearLeakedCallbacks<T>(List<T> list, string kind, Func<T, Delegate> callback)
    {
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var cb = callback(list[i]);

            if (!IsDelegateUnloaded(cb))
            {
                continue;
            }

            list.RemoveAt(i);

            _logger.LogWarning("{kind} leaked by unloaded module, removed: {detail}", kind, cb.GetMethodSignature());
        }
    }

    public T? ClearLeakedMulticast<T>(T? multicast, string kind, string context) where T : Delegate
    {
        if (multicast is null)
        {
            return null;
        }

        var current = (Delegate?) multicast;

        foreach (var callback in multicast.GetInvocationList())
        {
            if (!IsDelegateUnloaded(callback))
            {
                continue;
            }

            current = Delegate.Remove(current, callback);

            _logger.LogWarning("{kind} leaked by unloaded module, removed: {detail} ({context})",
                               kind,
                               callback.GetMethodSignature(),
                               context);
        }

        return (T?) current;
    }
}
