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
using Sharp.Core.Bridges.Natives;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Types.Runtime;
using Sharp.Shared.Units;

namespace Sharp.Core.Managers;

internal interface ICoreAddonManager : IAddonManager;

internal class AddonManager : ICoreAddonManager
{
    private readonly ILogger<AddonManager> _logger;
    private readonly ICoreAssemblyManager  _assemblyManager;
    private readonly List<IAddonListener>  _listeners;
    private readonly List<ulong>           _queryBuffer;

    public AddonManager(ILogger<AddonManager> logger, ICoreAssemblyManager assemblyManager)
    {
        _logger          = logger;
        _assemblyManager = assemblyManager;
        _listeners       = [];
        _queryBuffer     = [];

        _assemblyManager.RegisterUnloadCleanup(ClearLeakedRegistrations);

        Bridges.Forwards.Client.OnClientQueryAddons += OnClientQueryAddons;
    }

    public IReadOnlyList<ulong> GetAddons()
        => Game.AddonGetAddons().AsSpan().ToArray();

    public unsafe bool SetAddons(IReadOnlyList<ulong> addons)
    {
        var array = addons.ToArray();

        fixed (ulong* ptr = array)
        {
            return Game.AddonSetAddons(ptr, array.Length);
        }
    }

    public void ResetClientCache(SteamID steamId = default)
        => Game.AddonResetClientCache(steamId);

    public bool RefreshClient(SteamID steamId)
        => Game.AddonRefreshClient(steamId);

    public void InstallAddonListener(IAddonListener listener)
    {
        if (_assemblyManager.IsAssemblyUnloaded(listener.GetType().Assembly))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (listener.ListenerVersion != IAddonListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_listeners.Contains(listener))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _listeners.Add(listener);
        _listeners.Sort((x, y) => y.ListenerPriority.CompareTo(x.ListenerPriority));
        Game.AddonSetClientQueryEnabled(true);
    }

    public void RemoveAddonListener(IAddonListener listener)
    {
        if (!_listeners.Remove(listener))
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        Game.AddonSetClientQueryEnabled(_listeners.Count > 0);
    }

    private void ClearLeakedRegistrations()
    {
        _assemblyManager.ClearLeakedListeners(_listeners, "AddonListener");
        Game.AddonSetClientQueryEnabled(_listeners.Count > 0);
    }

    private void OnClientQueryAddons(SteamID steamId, ref NativeFixedSpan<ulong> addons)
    {
        _queryBuffer.Clear();

        for (var i = 0; i < _listeners.Count; i++)
        {
            try
            {
                _listeners[i].OnClientQueryAddons(steamId, _queryBuffer);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnClientQueryAddons),
                                 _listeners[i].GetType().Name);
            }
        }

        if (_queryBuffer.Count > addons.Length)
        {
            _logger.LogWarning("Too many client addons for {steamId}, only the first {max} are delivered", steamId, addons.Length);
        }

        var count = Math.Min(_queryBuffer.Count, addons.Length);

        for (var i = 0; i < count; i++)
        {
            addons[i] = _queryBuffer[i];
        }

        addons.Count = count;
    }
}
