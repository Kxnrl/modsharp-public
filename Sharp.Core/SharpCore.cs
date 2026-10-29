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
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Sharp.Core.Bridges;
using Sharp.Core.Bridges.Interfaces;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.Helpers;
using Sharp.Core.Managers;
using Sharp.Core.Objects;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;
using Sharp.Shared.Utilities;

namespace Sharp.Core;

internal interface ISharpCore : IModSharp
{
    void InitMainThread();

    bool IsOnMainThread();

    void OnModuleUnload(IModSharpModule module);

    void OnModuleUnloaded(IModSharpModule module);

    void OnModuleLoadFailure(Assembly assembly);
}

// ReSharper disable ForCanBeConvertedToForeach
internal partial class SharpCore : ISharpCore
{
    private record GameFrameHookInfo(Action<bool, bool, bool> Callback, int Priority);

    private record EntityThinkHookInfo(Action Callback, int Priority);

    private record GameSimulateHookInfo(Action Callback, int Priority);

    private readonly ILogger<SharpCore>   _logger;
    private readonly ICoreAssemblyManager _assemblyManager;

    private readonly SubscriberList<IGameListener>             _gameListeners;
    private readonly SubscriberList<ISteamListener>            _steamListeners;
    private readonly SubscriberList<GameFrameHookInfo>         _framePreHooks;
    private readonly SubscriberList<GameFrameHookInfo>         _framePostHooks;
    private readonly SubscriberList<EntityThinkHookInfo>       _entityPreHooks;
    private readonly SubscriberList<EntityThinkHookInfo>       _entityPostHooks;
    private readonly SubscriberList<GameSimulateHookInfo>      _gameSimulateHooks;
    private readonly ConcurrentQueue<AssemblyAction>           _actionQueue;
    private readonly ConcurrentQueue<AssemblyTimer>            _stoppedTimers;
    private readonly ConcurrentDictionary<Guid, AssemblyTimer> _timers;
    private readonly ICoreScriptManager                        _scriptManager;
    private readonly FrozenDictionary<string, string?>         _commandLines;

    private readonly string _gamePath;
    private readonly int    _bootThreadId;

    private int          _mainThreadId;
    private IDisposable? _logContext;
    private nint         _precacheContext;
    private double       _lastFrameCalled;
    private double       _totalFrameTime;

    private NetworkServer? _sv;
    private GlobalVars?    _globalVars;
    private GameRules?     _gameRules;

    public SharpCore(ILogger<SharpCore> logger, IShutdownMonitor shutdownMonitor, ICoreAssemblyManager assemblyManager, ICoreScriptManager scriptManager)
    {
        _logger          = logger;
        _assemblyManager = assemblyManager;
        _scriptManager   = scriptManager;

        shutdownMonitor.RegisterShutdownEvent(Shutdown);

        _gameListeners     = new SubscriberList<IGameListener>(x => x.ListenerPriority);
        _steamListeners    = new SubscriberList<ISteamListener>(x => x.ListenerPriority);
        _framePreHooks     = new SubscriberList<GameFrameHookInfo>(x => x.Priority);
        _framePostHooks    = new SubscriberList<GameFrameHookInfo>(x => x.Priority);
        _entityPreHooks    = new SubscriberList<EntityThinkHookInfo>(x => x.Priority);
        _entityPostHooks   = new SubscriberList<EntityThinkHookInfo>(x => x.Priority);
        _gameSimulateHooks = new SubscriberList<GameSimulateHookInfo>(null);
        _actionQueue       = [];
        _stoppedTimers     = [];
        _timers            = [];

        _precacheContext = nint.Zero;
        _commandLines    = ParseCommandLine();
        _bootThreadId    = Environment.CurrentManagedThreadId;

        _lastFrameCalled = 0;
        _totalFrameTime  = 0;

        var path = Marshal.PtrToStringUTF8(Bridges.Natives.Core.GetGameDirectory())
                   ?? throw new NullReferenceException("Failed to get GamePath");

        _gamePath = Path.GetFullPath(Path.Combine(path, "csgo"));

        Bridges.Forwards.Game.OnServerInit                 += OnServerInit;
        Bridges.Forwards.Game.OnServerSpawn                += OnServerSpawn;
        Bridges.Forwards.Game.OnServerActivate             += OnServerActivate;
        Bridges.Forwards.Game.OnGameInit                   += OnGameInit;
        Bridges.Forwards.Game.OnGameShutdown               += OnGameShutdown;
        Bridges.Forwards.Game.OnGamePostInit               += OnGamePostInit;
        Bridges.Forwards.Game.OnGamePreShutdown            += OnGamePreShutdown;
        Bridges.Forwards.Game.OnGameActivate               += OnGameActivate;
        Bridges.Forwards.Game.OnGameDeactivate             += OnGameDeactivate;
        Bridges.Forwards.Game.OnResourcePrecache           += OnResourcePrecache;
        Bridges.Forwards.Game.OnGameFramePre               += OnGameFramePre;
        Bridges.Forwards.Game.OnGameFramePost              += OnGameFramePost;
        Bridges.Forwards.Game.OnRoundRestart               += OnRoundRestart;
        Bridges.Forwards.Game.OnRoundRestarted             += OnRoundRestarted;
        Bridges.Forwards.Game.OnConsoleSay                 += OnConsoleSay;
        Bridges.Forwards.Game.OnFrameUpdatePreEntityThink  += OnFrameUpdatePreEntityThink;
        Bridges.Forwards.Game.OnFrameUpdatePostEntityThink += OnFrameUpdatePostEntityThink;
        Bridges.Forwards.Game.OnServerGamePostSimulate     += OnServerGamePostSimulate;

        Bridges.Forwards.Steam.OnGroupStatusResult          += OnGroupStatusResult;
        Bridges.Forwards.Steam.OnSteamServersConnected      += OnSteamServersConnected;
        Bridges.Forwards.Steam.OnSteamServersDisconnected   += OnSteamServersDisconnected;
        Bridges.Forwards.Steam.OnSteamServersConnectFailure += OnSteamServersConnectFailure;
        Bridges.Forwards.Steam.OnDownloadItemResult         += OnDownloadItemResult;
        Bridges.Forwards.Steam.OnItemInstalled              += OnItemInstalled;

        _assemblyManager.RegisterUnloadCleanup(ClearLeakedRegistrations);

        CenterHtmlHelper.InitDelegates();
    }

#region Game Hook and Forward

    private void OnServerInit()
    {
        _sv         = NetworkServer.Create(Bridges.Natives.Core.GetIServer());
        _globalVars = GlobalVars.Create(Bridges.Natives.Core.GetGlobals());

        _logContext?.Dispose();
        _logContext = LogContext.PushProperty("MapName", $"| {GetMapName() ?? "<empty>"}");

        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnServerInit();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnServerInit),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnServerSpawn()
    {
        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnServerSpawn();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnServerSpawn),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnServerActivate()
    {
        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnServerActivate();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnServerActivate),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnGameInit()
    {
        _gameRules = GameRules.Create(Bridges.Natives.Core.GetGameRules());

        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnGameInit();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameInit),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnGameShutdown()
    {
        _gameRules?.MarkAsDisposed();
        _gameRules = null;

        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnGameShutdown();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameShutdown),
                                 listener.GetType().Name);
            }
        }

        _sv?.MarkAsDisposed();
        _globalVars?.MarkAsDisposed();

        ClearLeakedMapTimers();

        _logContext?.Dispose();
        _logContext = null;

        _sv         = null;
        _globalVars = null;
    }

    private void OnGamePostInit()
    {
        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnGamePostInit();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGamePostInit),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnGamePreShutdown()
    {
        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnGamePreShutdown();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGamePreShutdown),
                                 listener.GetType().Name);
            }
        }

        ClearMapTimer();
    }

    private void OnGameActivate()
    {
        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnGameActivate();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameActivate),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnGameDeactivate()
    {
        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnGameDeactivate();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameDeactivate),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnResourcePrecache(nint pContext)
    {
        _precacheContext = pContext;

        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnResourcePrecache();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnResourcePrecache),
                                 listener.GetType().Name);
            }
        }

        _precacheContext = nint.Zero;
    }

    private void OnRoundRestart()
    {
        RunStoppedTimers();

        // Stop timer only current round
        {
            var timers = _timers.Where(x => x.Value.Flags.HasFlag(GameTimerFlags.StopOnRoundEnd))
                                .Select(x => x.Key)
                                .ToArray();

            for (var i = 0; i < timers.Length; i++)
            {
                var x = timers[i];

                if (_timers.TryRemove(x, out var timer) && timer.Flags.HasFlag(GameTimerFlags.ForceCallOnStop))
                {
                    ForceRunOnStop(timer);
                }
            }

            RunStoppedTimers();
        }

        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnRoundRestart();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnRoundRestart),
                                 listener.GetType().Name);
            }
        }

        ClearLeakedRoundTimers();
    }

    private void OnRoundRestarted()
    {
        foreach (var listener in _gameListeners)
        {
            try
            {
                listener.OnRoundRestarted();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnRoundRestarted),
                                 listener.GetType().Name);
            }
        }
    }

    private ECommandAction OnConsoleSay(string message)
    {
        var action = ECommandAction.Skipped;

        foreach (var listener in _gameListeners)
        {
            try
            {
                var low = listener.ConsoleSay(message);

                if (low > action)
                {
                    action = low;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling listener {name}", nameof(OnConsoleSay));
            }
        }

        return action;
    }

    private void OnFrameUpdatePreEntityThink()
    {
        if (_entityPreHooks.Count == 0)
        {
            return;
        }

        foreach (var hook in _entityPreHooks)
        {
            try
            {
                hook.Callback();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling hook callback {name}",
                                 nameof(OnFrameUpdatePreEntityThink));
            }
        }
    }

    private void OnFrameUpdatePostEntityThink()
    {
        if (_entityPostHooks.Count == 0)
        {
            return;
        }

        foreach (var hook in _entityPostHooks)
        {
            try
            {
                hook.Callback();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling hook callback {name}",
                                 nameof(OnFrameUpdatePostEntityThink));
            }
        }
    }

    private void OnServerGamePostSimulate()
    {
        if (_gameSimulateHooks.Count == 0)
        {
            return;
        }

        foreach (var hook in _gameSimulateHooks)
        {
            try
            {
                hook.Callback();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling hook callback {name}", nameof(OnServerGamePostSimulate));
            }
        }
    }

    private void OnGameFramePre(bool simulating, bool firstTick, bool lastTick)
    {
        if (simulating)
        {
            var globalVars = GetGlobals();
            var frameTime  = globalVars.CurTime;

            if (_lastFrameCalled == 0)
            {
                _totalFrameTime += frameTime - _lastFrameCalled;
            }
            else
            {
                _totalFrameTime += globalVars.FrameTime;
            }

            _lastFrameCalled = frameTime;
        }

        if (_framePreHooks.Count > 0)
        {
            foreach (var hook in _framePreHooks)
            {
                try
                {
                    hook.Callback(simulating, firstTick, lastTick);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while calling hook callback {name}", nameof(OnGameFramePre));
                }
            }
        }

        // if game does not run frame
        if (!simulating || _timers.IsEmpty)
        {
            return;
        }

        var timers = _timers.Values.ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            var timer = timers[i];

            if (!_timers.ContainsKey(timer.UniqueId))
            {
                continue;
            }

            if (_assemblyManager.IsDelegateUnloaded(timer.Callback))
            {
                _timers.TryRemove(timer.UniqueId, out _);

                continue;
            }

            var remove = false;

            try
            {
                if (!timer.IsInitialized)
                {
                    timer.Initialize(_totalFrameTime);
                }

                if (!timer.RunThink(_totalFrameTime))
                {
                    continue;
                }

                if (!timer.Flags.HasFlag(GameTimerFlags.Repeatable))
                {
                    remove = true;
                }
            }
            catch (Exception e)
            {
                remove = true;
                _logger.LogError(e, "An error occurred while timer elapsed.");
            }
            finally
            {
                if (remove)
                {
                    _timers.TryRemove(timer.UniqueId, out _);
                }
            }
        }
    }

    private void OnGameFramePost(bool simulating, bool firstTick, bool lastTick)
    {
        if (_framePostHooks.Count > 0)
        {
            foreach (var hook in _framePostHooks)
            {
                try
                {
                    hook.Callback(simulating, firstTick, lastTick);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while calling hook callback {name}", nameof(OnGameFramePost));
                }
            }
        }

        // call only end of frame
        if (!lastTick)
        {
            return;
        }

        RunStoppedTimers();

        while (_actionQueue.TryDequeue(out var action))
        {
            if (_assemblyManager.IsDelegateUnloaded(action.Owner))
            {
                DiscardAction(action.OnDiscard);

                continue;
            }

            try
            {
                action.Action.Invoke();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling frame action");
            }
        }
    }

#endregion

#region Steam Forward

    private void OnGroupStatusResult(SteamID steamId, SteamID groupId, bool member, bool officer)
    {
        foreach (var listener in _steamListeners)
        {
            try
            {
                listener.OnGroupStatusResult(steamId, groupId, member, officer);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGroupStatusResult),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnSteamServersConnected()
    {
        foreach (var listener in _steamListeners)
        {
            try
            {
                listener.OnSteamServersConnected();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnSteamServersConnected),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnSteamServersDisconnected(SteamApiResult reason)
    {
        foreach (var listener in _steamListeners)
        {
            try
            {
                listener.OnSteamServersDisconnected(reason);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnSteamServersDisconnected),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnSteamServersConnectFailure(SteamApiResult reason, bool stillRetrying)
    {
        foreach (var listener in _steamListeners)
        {
            try
            {
                listener.OnSteamServersConnectFailure(reason, stillRetrying);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnSteamServersConnectFailure),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnDownloadItemResult(ulong sharedFileId, SteamApiResult result)
    {
        foreach (var listener in _steamListeners)
        {
            try
            {
                listener.OnDownloadItemResult(sharedFileId, result);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnDownloadItemResult),
                                 listener.GetType().Name);
            }
        }
    }

    private void OnItemInstalled(ulong sharedFileId)
    {
        foreach (var listener in _steamListeners)
        {
            try
            {
                listener.OnItemInstalled(sharedFileId);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnItemInstalled),
                                 listener.GetType().Name);
            }
        }
    }

#endregion

#region Timer and FrameAction

    public void InvokeAction(Action action)
    {
        if (Environment.CurrentManagedThreadId == _mainThreadId)
        {
            action.Invoke();
        }
        else
        {
            EnqueueAction(action);
        }
    }

    public void InvokeFrameAction(Action action)
        => EnqueueAction(action);

    public Task<T> InvokeFrameActionAsync<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!action.HasSingleTarget)
        {
            throw new ArgumentException("Multicast delegates are not supported, register each callback separately",
                                        nameof(action));
        }

        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        EnqueueAction(() =>
                      {
                          if (cancellationToken.IsCancellationRequested)
                          {
                              source.SetCanceled(cancellationToken);

                              return;
                          }

                          try
                          {
                              var result = action();
                              source.SetResult(result);
                          }
                          catch (Exception e)
                          {
                              source.SetException(e);
                          }
                      },
                      action,
                      () => source.TrySetCanceled());

        return source.Task;
    }

    public Task InvokeFrameActionAsync(Action action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!action.HasSingleTarget)
        {
            throw new ArgumentException("Multicast delegates are not supported, register each callback separately",
                                        nameof(action));
        }

        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        EnqueueAction(() =>
                      {
                          if (cancellationToken.IsCancellationRequested)
                          {
                              source.SetCanceled(cancellationToken);

                              return;
                          }

                          try
                          {
                              action();
                              source.SetResult();
                          }
                          catch (Exception e)
                          {
                              source.SetException(e);
                          }
                      },
                      action,
                      () => source.TrySetCanceled());

        return source.Task;
    }

    public Guid PushTimer(Action action, double interval, GameTimerFlags flags = GameTimerFlags.None)
    {
        if (!action.HasSingleTarget)
        {
            throw new ArgumentException("Multicast delegates are not supported, register each callback separately",
                                        nameof(action));
        }

        if (_assemblyManager.IsDelegateUnloaded(action))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return Guid.Empty;
        }

        var timer = new AssemblyActionTimer(interval, flags, action);

        if (Environment.CurrentManagedThreadId == _mainThreadId)
        {
            timer.Initialize(_totalFrameTime);
        }

        if (!_timers.TryAdd(timer.UniqueId, timer))
        {
            throw new InvalidOperationException($"Could not push timer {timer.UniqueId}");
        }

        if (_assemblyManager.IsDelegateUnloaded(action))
        {
            _timers.TryRemove(timer.UniqueId, out _);

            return Guid.Empty;
        }

        return timer.UniqueId;
    }

    public Guid PushTimer(Func<TimerAction> action, double interval, GameTimerFlags flags = GameTimerFlags.None)
    {
        if (!action.HasSingleTarget)
        {
            throw new ArgumentException("Multicast delegates are not supported, register each callback separately",
                                        nameof(action));
        }

        if (_assemblyManager.IsDelegateUnloaded(action))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return Guid.Empty;
        }

        var timer = new AssemblyFunctionTimer(interval, flags, action);

        if (Environment.CurrentManagedThreadId == _mainThreadId)
        {
            timer.Initialize(_totalFrameTime);
        }

        if (!_timers.TryAdd(timer.UniqueId, timer))
        {
            throw new InvalidOperationException($"Could not push timer {timer.UniqueId}");
        }

        if (_assemblyManager.IsDelegateUnloaded(action))
        {
            _timers.TryRemove(timer.UniqueId, out _);

            return Guid.Empty;
        }

        return timer.UniqueId;
    }

    public void StopTimer(Guid uniqueId)
    {
        if (!IsOnMainThread())
        {
            if (!_timers.TryGetValue(uniqueId, out var stopping))
            {
                return;
            }

            if (stopping.Flags.HasFlag(GameTimerFlags.ForceCallOnStop))
            {
                _stoppedTimers.Enqueue(stopping);
            }

            _timers.TryRemove(uniqueId, out _);

            return;
        }

        if (_timers.TryRemove(uniqueId, out var timer) && timer.Flags.HasFlag(GameTimerFlags.ForceCallOnStop))
        {
            ForceRunOnStop(timer);
        }
    }

    public bool IsValidTimer(Guid uniqueId)
        => _timers.ContainsKey(uniqueId);

#endregion

#region CommandLine

    public bool HasCommandLine(string key)
        => _commandLines.ContainsKey(key);

    public T? GetCommandLine<T>(string key) where T : IParsable<T>
    {
        if (!_commandLines.TryGetValue(key, out var value) || value is null)
        {
            return default;
        }

        return T.TryParse(value, null, out var result) ? result : default;
    }

    public string? GetCommandLine(string key)
        => _commandLines.TryGetValue(key, out var value) ? value : null;

#endregion

#region Logging

    public void LogMessage(string message)
        => Bridges.Natives.Core.LogMessage(message);

    public void LogWarning(string message)
        => Bridges.Natives.Core.LogWarning(message);

    public void LogColorText(Color color, string text)
        => Bridges.Natives.Core.LogColorText(color.R, color.G, color.B, text);

    public void FatalError(string message)
        => Bridges.Natives.Core.FatalError(message);

#endregion

#region Native Object

    public IGlobalVars GetGlobals()
        => _globalVars ?? throw new InvalidOperationException("You can not get this now!");

    public INetworkServer GetIServer()
        => _sv ?? throw new InvalidOperationException("You can not get this now!");

    public nint GetIEngine()
        => CoreBridge.Engine;

    public IGameRules GetGameRules()
        => _gameRules ?? throw new InvalidOperationException("You can not get this now!");

    public IGameData GetGameData()
        => CoreGameData.Core;

    public ISteamApi GetSteamGameServer()
        => CoreBridge.SteamApiInstance;

    public unsafe IGameSystem? FindGameSystemByName(string name)
    {
        var pName = stackalloc byte[256];
        Utf8.FromUtf16(name, new Span<byte>(pName, 256), out _, out var bytesWritten);
        pName[bytesWritten] = 0;

        var factory = (CGameSystemFactory*) Game.GetGameSystemFactory();

        while (factory is not null)
        {
            if (NativeString.CStringCmp(factory->m_pszName.Value, pName) == 0)
            {
                return GameSystem.Create(GetGameSystemInstance(factory));
            }

            factory = factory->m_pNext;
        }

        return null;
    }

    private static unsafe nint GetGameSystemInstance(CGameSystemFactory* factory)
    {
        var index          = CoreGameData.Core.GetVFuncIndex("IGameSystemFactory::IsReallocating");
        var isReallocating = ((delegate* unmanaged<CGameSystemFactory*, byte>) (*(nint**) factory)[index])(factory) != 0;

        if (!isReallocating)
        {
            return factory->m_pInstance;
        }

        return factory->m_pInstance == nint.Zero ? nint.Zero : *(nint*) factory->m_pInstance;
    }

    public IntPtr FindValveInterface(string module, string name)
        => Bridges.Natives.Core.FindValveInterface(module, name);

    public nint GetNativeFunctionPointer(string name)
        => Adapter.NativeFunctions.GetValueOrDefault(name, nint.Zero);

    public IntPtr FindPattern(string module, string pattern)
        => Bridges.Natives.Core.FindPattern(module, pattern);

    public nint GetEntityClassThinkFunc(string classname, string thinkName)
        => Bridges.Natives.Entity.GetThinkFunc(classname, thinkName);

    public IntPtr GetVTableByClass(string module, string className)
        => Bridges.Natives.Core.GetVTableByClass(module, className);

    public IntPtr GetVirtualTableByClass(string module, string className)
        => Bridges.Natives.Core.GetVTableByClass(module, className);

    public INetworkingStringTable? FindStringTable(string name)
        => NetworkingStringTable.Create(Bridges.Natives.Core.FindStringTable(name));

    public IKeyValues CreateKeyValues(string name)
    {
        var kv = KeyValues.Create(CoreBridge.KeyValuesHelperInstance.CreateKeyValues(name))!;
        kv.MakeDeletable();

        return kv;
    }

    public IKeyValues3 CreateKeyValues3(KeyValues3Type type, KeyValues3SubType subType)
    {
        var kv = KeyValues3.Create(CoreBridge.KeyValues3HelperInstance.CreateKeyValues3(type, subType))!;
        kv.MakeDeletable();

        return kv;
    }

    public ILibraryModule GetLibraryModule(string module)
        => LibraryModule.Create(Bridges.Natives.Core.GetLibraryModule(module))!;

    public unsafe bool SetMemoryAccess(nint pMemory, long size, MemoryAccess access)
        => Bridges.Natives.Core.SetMemoryAccess(pMemory, size, access, null);

    public unsafe bool SetMemoryAccess(nint pMemory, long size, MemoryAccess access, out MemoryAccess originalAccess)
    {
        originalAccess = 0;

        fixed (MemoryAccess* ptr = &originalAccess)
        {
            return Bridges.Natives.Core.SetMemoryAccess(pMemory, size, access, ptr);
        }
    }

    public IMemAlloc GetMemAlloc()
        => CoreBridge.MemAlloc;

    public uint MakeStringToken(string str)
        => Bridges.Natives.Core.MakeStringToken(str);

#endregion

#region Map

    public void ChangeLevel(string map)
        => Engine.ChangeLevel(map);

    public bool IsMapValid(string map)
        => Engine.IsMapValid(map);

    public unsafe List<string>? GetMapGroupMapList(string mapGroup)
    {
        ref var vector = ref Unsafe.AsRef<CUtlVector<CUtlString>>(Game.GetMapGroupMapList(mapGroup).ToPointer());

        if (Unsafe.IsNullRef(ref vector))
        {
            return null;
        }

        var items = new List<string>(vector.Count);

        foreach (var pString in vector)
        {
            items.Add(pString.Get());
        }

        return items;
    }

    public unsafe byte[]? FindResourceDataBlockInfo(string filePath, string pathId)
    {
        if (!filePath.EndsWith("_c"))
        {
            filePath += "_c";
        }

        var size = Game.FindResourceDataBlockInfo(filePath, pathId);

        if (size <= 0)
        {
            return null;
        }

        var data = new byte[size];

        fixed (byte* pData = data)
        {
            Game.CopyResourceDataBlock(pData, size);
        }

        return data;
    }

    public ResourceStatus GetResourceStatus(string filePath)
        => Game.GetResourceStatus(filePath);

#endregion

#region Command

    public void ServerCommand(string command)
        => Engine.ServerCommand(command);

#endregion

#region Effects

    public unsafe int DispatchParticleEffect(string particle, Vector origin, Vector angles, RecipientFilter filter = default)
        => Game.DispatchParticleEffectPosition(particle, &origin, &angles, &filter);

    public unsafe int DispatchParticleEffect(string particle,
        IBaseEntity                                 entity,
        Vector                                      origin,
        Vector                                      angles,
        bool                                        resetEntity = false,
        RecipientFilter                             filter      = default)
        => Game.DispatchParticleEffectEntityPosition(particle, entity.GetAbsPtr(), &origin, &angles, resetEntity, &filter);

    public unsafe int DispatchParticleEffect(string particle,
        ParticleAttachmentType                      attachType,
        IBaseEntity                                 entity,
        byte                                        attachmentIndex = 0,
        bool                                        resetEntity     = false,
        RecipientFilter                             filter          = default)
        => Game.DispatchParticleEffectAttachment(particle,
                                                 attachType,
                                                 entity.GetAbsPtr(),
                                                 attachmentIndex,
                                                 resetEntity,
                                                 &filter);

#endregion

#region NetMessages

    public void PrintToChatAll(string message)
        => NetMessageHelper.PrintChannelFilter(default, HudPrintChannel.Chat, message);

    public void PrintToChat(CStrikeTeam team, string message)
        => NetMessageHelper.PrintChannelFilter(new RecipientFilter(team), HudPrintChannel.Chat, message);

    public void PrintChannelAll(HudPrintChannel channel, string message)
        => NetMessageHelper.PrintChannelFilter(default, channel, message);

    public unsafe void PrintChannelFilter(HudPrintChannel channel, string message, RecipientFilter receiver)
        => NetMessageHelper.PrintChannelFilter(receiver, channel, message);

    public void PrintChannelTeam(HudPrintChannel channel, CStrikeTeam team, string message)
        => NetMessageHelper.PrintChannelFilter(new RecipientFilter(team), channel, message);

    public void RadioTextTeam(CStrikeTeam team,
        PlayerSlot                        slot,
        string                            name,
        string?                           params1,
        string?                           params2,
        string?                           params3,
        string?                           params4)
        => NetMessageHelper.PrintRadioMessage(new RecipientFilter(team), slot, name, params1, params2, params3, params4);

    public void RadioTextAll(PlayerSlot slot, string name, string? params1, string? params2, string? params3, string? params4)
        => NetMessageHelper.PrintRadioMessage(default, slot, name, params1, params2, params3, params4);

    public unsafe bool SendNetMessage<T>(RecipientFilter filter, T data) where T : class, IMessage
        => NetMessageHelper.SendNetMessage(filter, data);

    public void HookNetMessage(ProtobufNetMessageType msgId)
        => Net.HookNetMessage(msgId);

    public void UnhookNetMessage(ProtobufNetMessageType msgId)
        => _logger.LogWarning("Attempt unhook net message: {id}", msgId);

#endregion

#region Listener

    public void InstallGameListener(IGameListener listener)
    {
        if (_assemblyManager.IsAssemblyUnloaded(listener.GetType().Assembly))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (listener.ListenerVersion != IGameListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_gameListeners.Exists(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _gameListeners.Add(listener);
    }

    public void RemoveGameListener(IGameListener listener)
    {
        if (!_gameListeners.Remove(listener))
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);
        }
    }

    public void InstallSteamListener(ISteamListener listener)
    {
        if (_assemblyManager.IsAssemblyUnloaded(listener.GetType().Assembly))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (listener.ListenerVersion != ISteamListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_steamListeners.Exists(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _steamListeners.Add(listener);
    }

    public void RemoveSteamListener(ISteamListener listener)
    {
        if (!_steamListeners.Remove(listener))
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);
        }
    }

#endregion

#region Misc

    public void PrecacheResource(string resource)
    {
        if (_precacheContext == nint.Zero)
        {
            throw new InvalidOperationException("You must be call this in 'OnPrecacheResource'");
        }

        Engine.PrecacheResource(_precacheContext, resource);
    }

    public string GetGamePath()
        => _gamePath;

    public void InstallGameFrameHook(Action<bool, bool, bool>? pre,
        Action<bool, bool, bool>?                              post,
        int                                                    prePriority  = 0,
        int                                                    postPriority = 0)
    {
        if ((pre is not null && !pre.HasSingleTarget) || (post is not null && !post.HasSingleTarget))
        {
            _logger.LogError("Multicast delegates are not supported, install each callback separately!\n{stackTrace}",
                             Environment.StackTrace);

            return;
        }

        if ((pre is not null && _assemblyManager.IsDelegateUnloaded(pre))
            || (post is not null && _assemblyManager.IsDelegateUnloaded(post)))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        GameFrameHookInfo? preHook  = null;
        GameFrameHookInfo? postHook = null;

        if (pre is not null)
        {
            if (_framePreHooks.Exists(x => x.Callback.Equals(pre)))
            {
                _logger.LogError("You are already installed this hook<pre>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            preHook = new GameFrameHookInfo(pre, prePriority);
        }

        if (post is not null)
        {
            if (_framePostHooks.Exists(x => x.Callback.Equals(post)))
            {
                _logger.LogError("You are already installed this hook<post>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            postHook = new GameFrameHookInfo(post, postPriority);
        }

        if (preHook is not null)
        {
            _framePreHooks.Add(preHook);
        }

        if (postHook is not null)
        {
            _framePostHooks.Add(postHook);
        }
    }

    public void InstallEntityThinkHook(Action? pre, Action? post, int prePriority = 0, int postPriority = 0)
    {
        if ((pre is not null && !pre.HasSingleTarget) || (post is not null && !post.HasSingleTarget))
        {
            _logger.LogError("Multicast delegates are not supported, install each callback separately!\n{stackTrace}",
                             Environment.StackTrace);

            return;
        }

        if ((pre is not null && _assemblyManager.IsDelegateUnloaded(pre))
            || (post is not null && _assemblyManager.IsDelegateUnloaded(post)))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        EntityThinkHookInfo? preHook  = null;
        EntityThinkHookInfo? postHook = null;

        if (pre is not null)
        {
            if (_entityPreHooks.Exists(x => x.Callback.Equals(pre)))
            {
                _logger.LogError("You are already installed this hook<pre>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            preHook = new EntityThinkHookInfo(pre, prePriority);
        }

        if (post is not null)
        {
            if (_entityPostHooks.Exists(x => x.Callback.Equals(post)))
            {
                _logger.LogError("You are already installed this hook<post>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            postHook = new EntityThinkHookInfo(post, postPriority);
        }

        if (preHook is not null)
        {
            _entityPreHooks.Add(preHook);
        }

        if (postHook is not null)
        {
            _entityPostHooks.Add(postHook);
        }
    }

    public void InstallServerGameSimulateHook(Action callback, int priority = 0)
    {
        if (!callback.HasSingleTarget)
        {
            _logger.LogError("Multicast delegates are not supported, install each callback separately!\n{stackTrace}",
                             Environment.StackTrace);

            return;
        }

        if (_assemblyManager.IsDelegateUnloaded(callback))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (_gameSimulateHooks.Exists(x => x.Callback.Equals(callback)))
        {
            _logger.LogError("You are already installed this hook<post>!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _gameSimulateHooks.Edit(x =>
        {
            var index = x.FindIndex(y => y.Priority < priority);
            x.Insert(index < 0 ? x.Count : index, new GameSimulateHookInfo(callback, priority));
        });
    }

    public void RemoveGameFrameHook(Action<bool, bool, bool>? pre, Action<bool, bool, bool>? post)
    {
        if (pre is not null)
        {
            if (_framePreHooks.Find(x => x.Callback.Equals(pre)) is { } preHook)
            {
                _framePreHooks.Remove(preHook);
            }
            else
            {
                _logger.LogError("Couldn't found hook<pre>\n{stackTrace}", Environment.StackTrace);
            }
        }

        if (post is not null)
        {
            if (_framePostHooks.Find(x => x.Callback.Equals(post)) is { } postHook)
            {
                _framePostHooks.Remove(postHook);
            }
            else
            {
                _logger.LogError("Couldn't found hook<post>\n{stackTrace}", Environment.StackTrace);
            }
        }
    }

    public void RemoveEntityThinkHook(Action? pre, Action? post)
    {
        if (pre is not null)
        {
            if (_entityPreHooks.Find(x => x.Callback.Equals(pre)) is { } preHook)
            {
                _entityPreHooks.Remove(preHook);
            }
            else
            {
                _logger.LogError("Couldn't found hook<pre>\n{stackTrace}", Environment.StackTrace);
            }
        }

        if (post is not null)
        {
            if (_entityPostHooks.Find(x => x.Callback.Equals(post)) is { } postHook)
            {
                _entityPostHooks.Remove(postHook);
            }
            else
            {
                _logger.LogError("Couldn't found hook<post>\n{stackTrace}", Environment.StackTrace);
            }
        }
    }

    public void RemoveServerGameSimulateHook(Action callback)
    {
        if (_gameSimulateHooks.Find(x => x.Callback.Equals(callback)) is { } hook)
        {
            _gameSimulateHooks.Remove(hook);
        }
        else
        {
            _logger.LogError("Couldn't found hook<post>\n{stackTrace}", Environment.StackTrace);
        }
    }

    public double EngineTime()
        => Bridges.Natives.Core.GetEngineTime();

    public string? GetAddonName()
        => Marshal.PtrToStringUTF8(Game.GetAddonName());

    public string? GetMapName()
        => Marshal.PtrToStringUTF8(Game.GetMapName());

    public IWeaponData? FindWeaponVDataByName(string name)
    {
        var pointer = Game.FindWeaponVDataByName(name);

        return pointer == nint.Zero ? null : CreateNativeObject<IWeaponData>(pointer);
    }

    public partial T? CreateNativeObject<T>(nint ptr) where T : class, INativeObject;

    private FrozenDictionary<string, string?> ParseCommandLine()
    {
        var arguments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var commandLine = Marshal.PtrToStringAnsi(Bridges.Natives.Core.GetCommandLine()) ?? string.Empty;
        var parts       = SplitCommandLine(commandLine).Skip(1).ToArray();

        for (var i = 0; i < parts.Length; i++)
        {
            var key = parts[i].Trim();

            if (i + 1 >= parts.Length)
            {
                SetCommandLineArgument(arguments, key, null);

                continue;
            }

            var next = parts[i + 1];

            // 下一行是新的命令
            if (next.Length > 0 && (next[0] == '+' || next[0] == '-'))
            {
                SetCommandLineArgument(arguments, key, null);

                continue;
            }

            SetCommandLineArgument(arguments, key, next.Trim());
            i++;
        }

        var sb = new StringBuilder();

        foreach (var (k, v) in arguments)
        {
            sb.AppendLine($"{k.PadRight(32)} = {v ?? "<null>"}");
        }

        _logger.LogInformation("Parse CommandLine:\n{commands}", sb.ToString());

        return arguments.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    // - 参数与 tier0 ParamValue 一致取首次出现 (sub_18005ED10 @ tier0.dll 20261015)
    private static void SetCommandLineArgument(Dictionary<string, string?> arguments, string key, string? value)
    {
        if (key.StartsWith('-'))
        {
            arguments.TryAdd(key, value);
        }
        else
        {
            arguments[key] = value;
        }
    }

    // 与 tier0 CCommandLine 的切分一致 (sub_18005E930 @ tier0.dll 20261015): GetCmdLine 返回其切分后重拼的串, 无转义
    private static List<string> SplitCommandLine(string commandLine)
    {
        var results = new List<string>();
        var span    = commandLine.AsSpan();
        var len     = span.Length;
        var i       = 0;

        while (i < len)
        {
            while (i < len && IsCommandLineSpace(span[i]))
            {
                i++;
            }

            if (i >= len)
            {
                break;
            }

            if (span[i] == '"')
            {
                var quoteStart = ++i;

                while (i < len && span[i] != '"')
                {
                    i++;
                }

                results.Add(span[quoteStart..i].ToString());
                i++;

                continue;
            }

            var start = i;

            while (i < len && !IsCommandLineSpace(span[i]))
            {
                i++;
            }

            results.Add(span[start..i].ToString());
        }

        return results;
    }

    private static bool IsCommandLineSpace(char c)
        => c is ' ' or >= '\t' and <= '\r';

#endregion

#region DedicatedServerWorkshopManager

    public bool AddWorkshopMap(ulong sharedFileId, string mapName, string path)
        => Game.AddWorkshopMap(sharedFileId, mapName, path);

    public bool WorkshopMapExists(ulong sharedFileId)
        => Game.WorkshopMapExists(sharedFileId);

    public bool RemoveWorkshopMap(ulong sharedFileId)
        => Game.RemoveWorkshopMap(sharedFileId);

    public unsafe List<(ulong PublishFileId, string Name)> ListWorkshopMaps()
    {
        ref var vector = ref Unsafe.AsRef<CUtlVector<WorkshopMap>>(Game.ListWorkshopMaps().ToPointer());
        var     list   = new List<(ulong PublishFileId, string Name)>(vector.Count);

        foreach (var map in vector)
        {
            list.Add((map.PublishFileId, map.Name));
        }

        return list;
    }

#endregion

    ///////////////////////////////////////////////////////////////////////////////////////

    public void InitMainThread()
        => _mainThreadId = Environment.CurrentManagedThreadId;

    public bool IsOnMainThread()
        => Environment.CurrentManagedThreadId == (_mainThreadId != 0 ? _mainThreadId : _bootThreadId);

    private void Shutdown()
    {
        _logger.LogInformation("Shutdown SharpCore");

        RunStoppedTimers();
        DiscardQueuedActions();

        if (_timers.IsEmpty)
        {
            RunStoppedTimers();
            DiscardQueuedActions();

            return;
        }

        var timers = _timers.Values.ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            try
            {
                var x = timers[i];

                if (_assemblyManager.IsDelegateUnloaded(x.Callback))
                {
                    continue;
                }

                if (!x.IsInitialized)
                {
                    x.Initialize(1.0);
                }

                if (x.Flags.HasFlag(GameTimerFlags.ForceCallBeforeShutdown) && x.TryFinish())
                {
                    x.ForceRun();
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occurred while timer shutdown.");
            }
        }

        _timers.Clear();

        RunStoppedTimers();
        DiscardQueuedActions();
    }

    public void OnModuleUnload(IModSharpModule module)
    {
        RunStoppedTimers();

        var assembly = module.GetType().Assembly;

        _assemblyManager.MarkAssemblyUnloaded(assembly);
        ClearUnloadedTimers(assembly);
        _scriptManager.OnModuleUnload(assembly);
    }

    public void OnModuleUnloaded(IModSharpModule module)
        => _assemblyManager.InvokeUnloadCleanup();

    public void OnModuleLoadFailure(Assembly assembly)
    {
        _assemblyManager.MarkAssemblyUnloaded(assembly);
        ClearUnloadedTimers(null);
        _assemblyManager.InvokeUnloadCleanup();
    }

    private void ClearLeakedRegistrations()
    {
        _gameListeners.Edit(x => _assemblyManager.ClearLeakedListeners(x, "GameListener"));
        _steamListeners.Edit(x => _assemblyManager.ClearLeakedListeners(x, "SteamListener"));

        _framePreHooks.Edit(x => _assemblyManager.ClearLeakedCallbacks(x, "GameFramePreHook", y => y.Callback));
        _framePostHooks.Edit(x => _assemblyManager.ClearLeakedCallbacks(x, "GameFramePostHook", y => y.Callback));
        _entityPreHooks.Edit(x => _assemblyManager.ClearLeakedCallbacks(x, "EntityThinkPreHook", y => y.Callback));
        _entityPostHooks.Edit(x => _assemblyManager.ClearLeakedCallbacks(x, "EntityThinkPostHook", y => y.Callback));
        _gameSimulateHooks.Edit(x => _assemblyManager.ClearLeakedCallbacks(x, "ServerGameSimulateHook", y => y.Callback));
    }

    ///////////////////////////////////////////////////////////////////////////////////////

    private void EnqueueAction(Action action)
    {
        if (action.HasSingleTarget)
        {
            _actionQueue.Enqueue(new AssemblyAction(action, action));

            return;
        }

        foreach (var callback in action.GetInvocationList())
        {
            var single = (Action) callback;

            _actionQueue.Enqueue(new AssemblyAction(single, single));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueAction(Action action, Delegate owner, Action? onDiscard)
        => _actionQueue.Enqueue(new AssemblyAction(action, owner, onDiscard));

    private void ClearMapTimer()
    {
        RunStoppedTimers();

        // Stop timer only current map

        var timers = _timers.Where(x => x.Value.Flags.HasFlag(GameTimerFlags.StopOnMapEnd)
                                        || x.Value.Flags.HasFlag(GameTimerFlags.StopOnRoundEnd))
                            .Select(x => x.Key)
                            .ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            if (_timers.TryRemove(timers[i], out var timer) && timer.Flags.HasFlag(GameTimerFlags.ForceCallOnStop))
            {
                ForceRunOnStop(timer);
            }
        }

        RunStoppedTimers();
    }

    private void ClearLeakedMapTimers()
    {
        var timers = _timers.Where(x => x.Value.Flags.HasFlag(GameTimerFlags.StopOnMapEnd)
                                        || x.Value.Flags.HasFlag(GameTimerFlags.StopOnRoundEnd))
                            .Select(x => x.Key)
                            .ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            if (_timers.TryRemove(timers[i], out var timer))
            {
                timer.TryFinish();

                _logger.LogWarning(
                    "Map timer pushed after map cleanup, removed at game shutdown without running: {detail} ({flags})",
                    timer.Callback.GetMethodSignature(),
                    timer.Flags);
            }
        }
    }

    private void ClearLeakedRoundTimers()
    {
        var timers = _timers.Where(x => x.Value.Flags.HasFlag(GameTimerFlags.StopOnRoundEnd))
                            .Select(x => x.Key)
                            .ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            if (_timers.TryRemove(timers[i], out var timer))
            {
                timer.TryFinish();

                _logger.LogWarning(
                    "Round timer pushed after round cleanup, removed at round restart without running: {detail} ({flags})",
                    timer.Callback.GetMethodSignature(),
                    timer.Flags);
            }
        }
    }

    private void ClearUnloadedTimers(Assembly? forceRunAnchor)
    {
        var timers = _timers.Where(x => _assemblyManager.IsDelegateUnloaded(x.Value.Callback))
                            .Select(x => x.Key)
                            .ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            if (!_timers.TryRemove(timers[i], out var timer))
            {
                continue;
            }

            if (forceRunAnchor is null
                || !timer.Flags.HasFlag(GameTimerFlags.ForceCallBeforeShutdown)
                || !_assemblyManager.IsDelegateOwnedBy(timer.Callback, forceRunAnchor))
            {
                continue;
            }

            try
            {
                timer.ForceRun();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occurred while timer removed with flag {f}", timer.Flags);
            }
        }
    }

    private void ForceRunOnStop(AssemblyTimer timer)
    {
        if (!timer.TryFinish())
        {
            return;
        }

        try
        {
            timer.ForceRun();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "An error occurred while timer removed with flag {f}", timer.Flags);
        }
    }

    private void RunStoppedTimers()
    {
        while (_stoppedTimers.TryDequeue(out var timer))
        {
            _timers.TryRemove(timer.UniqueId, out _);

            if (!_assemblyManager.IsDelegateUnloaded(timer.Callback))
            {
                ForceRunOnStop(timer);
            }
        }
    }

    private void DiscardQueuedActions()
    {
        while (_actionQueue.TryDequeue(out var action))
        {
            DiscardAction(action.OnDiscard);
        }
    }

    private void DiscardAction(Action? onDiscard)
    {
        if (onDiscard is null)
        {
            return;
        }

        try
        {
            onDiscard.Invoke();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while discarding frame action");
        }
    }

    private readonly record struct AssemblyAction(Action Action, Delegate Owner, Action? OnDiscard = null);

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private readonly unsafe struct WorkshopMap
    {
        [FieldOffset(0)]
        private readonly byte* _pName;

        [FieldOffset(8)]
        public readonly ulong PublishFileId;

        public string Name => NativeString.ReadString(_pName);
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct CGameSystemFactory
    {
        private nint                m_pVTable;
        public  CGameSystemFactory* m_pNext;
        public  Pointer<byte>       m_pszName;
        public  nint                m_pInstance;
    }
}
