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

    void OnModuleUnload(IModSharpModule module);
}

// ReSharper disable ForCanBeConvertedToForeach
internal partial class SharpCore : ISharpCore
{
    private record GameFrameHookInfo(Action<bool, bool, bool> Callback, int Priority);

    private record EntityThinkHookInfo(Action Callback, int Priority);

    private record GameSimulateHookInfo(Action Callback, int Priority);

    private readonly ILogger<SharpCore> _logger;

    private readonly List<IGameListener>                       _gameListeners;
    private readonly List<ISteamListener>                      _steamListeners;
    private readonly List<GameFrameHookInfo>                   _framePreHooks;
    private readonly List<GameFrameHookInfo>                   _framePostHooks;
    private readonly List<EntityThinkHookInfo>                 _entityPreHooks;
    private readonly List<EntityThinkHookInfo>                 _entityPostHooks;
    private readonly List<GameSimulateHookInfo>                _gameSimulateHooks;
    private readonly ConcurrentQueue<AssemblyAction>           _actionQueue;
    private readonly ConcurrentDictionary<Guid, AssemblyTimer> _timers;
    private readonly string                                    _gamePath;
    private readonly FrozenDictionary<string, string?>         _commandLines;

    private int          _mainThreadId;
    private IDisposable? _logContext;
    private nint         _precacheContext;
    private double       _lastFrameCalled;
    private double       _totalFrameTime;

    private NetworkServer? _sv;
    private GlobalVars?    _globalVars;
    private GameRules?     _gameRules;

    public SharpCore(ILogger<SharpCore> logger, IShutdownMonitor shutdownMonitor)
    {
        _logger = logger;

        shutdownMonitor.RegisterShutdownEvent(Shutdown);

        _gameListeners     = [];
        _steamListeners    = [];
        _framePreHooks     = [];
        _framePostHooks    = [];
        _entityPreHooks    = [];
        _entityPostHooks   = [];
        _gameSimulateHooks = [];
        _actionQueue       = [];
        _timers            = [];

        _precacheContext = nint.Zero;
        _commandLines    = ParseCommandLine();

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
    }

#region Game Hook and Forward

    private void OnServerInit()
    {
        _sv         = NetworkServer.Create(Bridges.Natives.Core.GetIServer());
        _globalVars = GlobalVars.Create(Bridges.Natives.Core.GetGlobals());

        _logContext?.Dispose();
        _logContext = LogContext.PushProperty("MapName", $"| {GetMapName() ?? "<empty>"}");

        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnServerInit();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnServerInit),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private void OnServerSpawn()
    {
        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnServerSpawn();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnServerSpawn),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private void OnServerActivate()
    {
        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnServerActivate();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnServerActivate),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private void OnGameInit()
    {
        _gameRules = GameRules.Create(Bridges.Natives.Core.GetGameRules());

        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnGameInit();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameInit),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private void OnGameShutdown()
    {
        ClearMapTimer();

        _gameRules?.MarkAsDisposed();
        _gameRules = null;

        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnGameShutdown();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameShutdown),
                                 _gameListeners[i].GetType().Name);
            }
        }

        _sv?.MarkAsDisposed();
        _globalVars?.MarkAsDisposed();

        _logContext?.Dispose();
        _logContext = null;

        _sv         = null;
        _globalVars = null;
    }

    private void OnGamePostInit()
    {
        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnGamePostInit();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGamePostInit),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private void OnGamePreShutdown()
    {
        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnGamePreShutdown();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGamePreShutdown),
                                 _gameListeners[i].GetType().Name);
            }
        }

        ClearMapTimer();
    }

    private void OnGameActivate()
    {
        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnGameActivate();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameActivate),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private void OnGameDeactivate()
    {
        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnGameDeactivate();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGameDeactivate),
                                 _gameListeners[i].GetType().Name);
            }
        }

        ClearMapTimer();
    }

    private void OnResourcePrecache(nint pContext)
    {
        _precacheContext = pContext;

        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnResourcePrecache();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnResourcePrecache),
                                 _gameListeners[i].GetType().Name);
            }
        }

        _precacheContext = nint.Zero;
    }

    private void OnRoundRestart()
    {
        // Stop timer only current round
        {
            var timers = _timers.Where(x => x.Value.Flags.HasFlag(GameTimerFlags.StopOnRoundEnd))
                                .Select(x => x.Key)
                                .ToArray();

            for (var i = 0; i < timers.Length; i++)
            {
                var x = timers[i];

                if (_timers.Remove(x, out var timer) && timer.Flags.HasFlag(GameTimerFlags.ForceCallOnStop))
                {
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
        }

        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnRoundRestart();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnRoundRestart),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private void OnRoundRestarted()
    {
        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                _gameListeners[i].OnRoundRestarted();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnRoundRestarted),
                                 _gameListeners[i].GetType().Name);
            }
        }
    }

    private ECommandAction OnConsoleSay(string message)
    {
        var action = ECommandAction.Skipped;

        for (var i = 0; i < _gameListeners.Count; i++)
        {
            try
            {
                var low = _gameListeners[i].ConsoleSay(message);

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

        for (var i = 0; i < _entityPreHooks.Count; i++)
        {
            try
            {
                _entityPreHooks[i].Callback();
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

        for (var i = 0; i < _entityPostHooks.Count; i++)
        {
            try
            {
                _entityPostHooks[i].Callback();
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

        for (var i = 0; i < _gameSimulateHooks.Count; i++)
        {
            try
            {
                _gameSimulateHooks[i].Callback();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling hook callback {name}", nameof(OnServerGamePostSimulate));
            }
        }
    }

    private void OnGameFramePre(bool simulating, bool firstTick, bool lastTick)
    {
        if (_framePreHooks.Count > 0)
        {
            for (var i = 0; i < _framePreHooks.Count; i++)
            {
                try
                {
                    _framePreHooks[i].Callback(simulating, firstTick, lastTick);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while calling hook callback {name}", nameof(OnGameFramePre));
                }
            }
        }

        // if game does not run frame
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

            if (!_timers.IsEmpty)
            {
                var timers = _timers.Values.ToArray();

                for (var i = 0; i < timers.Length; i++)
                {
                    var timer  = timers[i];
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
                            _timers.Remove(timer.UniqueId, out _);
                        }
                    }
                }
            }
        }
    }

    private void OnGameFramePost(bool simulating, bool firstTick, bool lastTick)
    {
        if (_framePostHooks.Count > 0)
        {
            for (var i = 0; i < _framePostHooks.Count; i++)
            {
                try
                {
                    _framePostHooks[i].Callback(simulating, firstTick, lastTick);
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

        while (_actionQueue.TryDequeue(out var action))
        {
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
        for (var i = 0; i < _steamListeners.Count; i++)
        {
            try
            {
                _steamListeners[i].OnGroupStatusResult(steamId, groupId, member, officer);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnGroupStatusResult),
                                 _steamListeners[i].GetType().Name);
            }
        }
    }

    private void OnSteamServersConnected()
    {
        for (var i = 0; i < _steamListeners.Count; i++)
        {
            try
            {
                _steamListeners[i].OnSteamServersConnected();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnSteamServersConnected),
                                 _steamListeners[i].GetType().Name);
            }
        }
    }

    private void OnSteamServersDisconnected(SteamApiResult reason)
    {
        for (var i = 0; i < _steamListeners.Count; i++)
        {
            try
            {
                _steamListeners[i].OnSteamServersDisconnected(reason);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnSteamServersDisconnected),
                                 _steamListeners[i].GetType().Name);
            }
        }
    }

    private void OnSteamServersConnectFailure(SteamApiResult reason, bool stillRetrying)
    {
        for (var i = 0; i < _steamListeners.Count; i++)
        {
            try
            {
                _steamListeners[i].OnSteamServersConnectFailure(reason, stillRetrying);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnSteamServersConnectFailure),
                                 _steamListeners[i].GetType().Name);
            }
        }
    }

    private void OnDownloadItemResult(ulong sharedFileId, SteamApiResult result)
    {
        for (var i = 0; i < _steamListeners.Count; i++)
        {
            try
            {
                _steamListeners[i].OnDownloadItemResult(sharedFileId, result);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnDownloadItemResult),
                                 _steamListeners[i].GetType().Name);
            }
        }
    }

    private void OnItemInstalled(ulong sharedFileId)
    {
        for (var i = 0; i < _steamListeners.Count; i++)
        {
            try
            {
                _steamListeners[i].OnItemInstalled(sharedFileId);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling listener<{s}> {name}",
                                 nameof(OnItemInstalled),
                                 _steamListeners[i].GetType().Name);
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

        var source   = new TaskCompletionSource<T>();
        var assembly = action.Method.Module.Assembly;

        EnqueueAction(assembly,
                      () =>
                      {
                          if (cancellationToken.IsCancellationRequested)
                          {
                              source.SetCanceled(CancellationToken.None);

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
                      });

        return source.Task;
    }

    public Task InvokeFrameActionAsync(Action action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source   = new TaskCompletionSource();
        var assembly = action.Method.Module.Assembly;

        EnqueueAction(assembly,
                      () =>
                      {
                          if (cancellationToken.IsCancellationRequested)
                          {
                              source.SetCanceled(CancellationToken.None);

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
                      });

        return source.Task;
    }

    public Guid PushTimer(Action action, double interval, GameTimerFlags flags = GameTimerFlags.None)
    {
        var timer = new AssemblyActionTimer(action.Method.Module.Assembly, interval, flags, action);

        if (!_timers.TryAdd(timer.UniqueId, timer))
        {
            throw new InvalidOperationException($"Could not push timer {timer.UniqueId}");
        }

        return timer.UniqueId;
    }

    public Guid PushTimer(Func<TimerAction> action, double interval, GameTimerFlags flags = GameTimerFlags.None)
    {
        var timer = new AssemblyFunctionTimer(action.Method.Module.Assembly, interval, flags, action);

        if (!_timers.TryAdd(timer.UniqueId, timer))
        {
            throw new InvalidOperationException($"Could not push timer {timer.UniqueId}");
        }

        return timer.UniqueId;
    }

    public void StopTimer(Guid uniqueId)
    {
        if (_timers.Remove(uniqueId, out var timer) && timer.Flags.HasFlag(GameTimerFlags.ForceCallOnStop))
        {
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

        var factory = (CGameSystemFactory*) GetGameData().GetAddress("GameSystemFactory::m_FactoryList");

        while (factory is not null)
        {
            if (NativeString.CStringCmp(factory->m_pszName.Value, pName) == 0)
            {
                return GameSystem.Create(factory->m_pInstance);
            }

            factory = factory->m_pNext;
        }

        return null;
    }

    public IntPtr FindValveInterface(string module, string name)
        => Bridges.Natives.Core.FindValveInterface(module, name);

    public nint GetNativeFunctionPointer(string name)
        => Adapter.NativeFunctions.GetValueOrDefault(name, nint.Zero);

    public IntPtr FindPattern(string module, string pattern)
        => Bridges.Natives.Core.FindPattern(module, pattern);

    public IntPtr GetVTableByClass(string module, string className)
        => Bridges.Natives.Core.GetVTableByClass(module, className);

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

    public bool SetMemoryAccess(nint pMemory, long size, MemoryAccess access)
        => Bridges.Natives.Core.SetMemoryAccess(pMemory, size, access);

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

    public byte[]? FindResourceDataBlockInfo(string filePath, string pathId)
    {
        if (!filePath.EndsWith("_c"))
        {
            filePath += "_c";
        }

        var span = Game.FindResourceDataBlockInfo(filePath, pathId);

        return span.Length == 0 ? null : span.AsSpan().ToArray();
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
        => Game.PrintChannelAll(HudPrintChannel.Chat, message);

    public void PrintToChat(CStrikeTeam team, string message)
        => Game.PrintChannelTeam(HudPrintChannel.Chat, team, message);

    public void PrintChannelAll(HudPrintChannel channel, string message)
        => Game.PrintChannelAll(channel, message);

    public unsafe void PrintChannelFilter(HudPrintChannel channel, string message, RecipientFilter receiver)
        => Game.PrintChannelFilter(channel, message, &receiver);

    public void PrintChannelTeam(HudPrintChannel channel, CStrikeTeam team, string message)
        => Game.PrintChannelTeam(channel, team, message);

    public void RadioTextTeam(CStrikeTeam team,
        PlayerSlot                        slot,
        string                            name,
        string?                           params1,
        string?                           params2,
        string?                           params3,
        string?                           params4)
        => Game.RadioMessageTeam(team, slot, name, params1, params2, params3, params4);

    public void RadioTextAll(PlayerSlot slot, string name, string? params1, string? params2, string? params3, string? params4)
        => Game.RadioMessageAll(slot, name, params1, params2, params3, params4);

    public unsafe bool SendNetMessage<T>(RecipientFilter filter, T data) where T : IMessage
    {
        var bytes = data.ToByteArray();
        var size  = bytes.Length;

        fixed (byte* pBytes = bytes)
        {
            return Net.SendNetMessage(&filter, data.GetType().Name, pBytes, size);
        }
    }

    public void HookNetMessage(ProtobufNetMessageType msgId)
        => Net.HookNetMessage(msgId);

    public void UnhookNetMessage(ProtobufNetMessageType msgId)
        => Net.UnhookNetMessage(msgId);

#endregion

#region Listener

    public void InstallGameListener(IGameListener listener)
    {
        if (listener.ListenerVersion != IGameListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_gameListeners.Any(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _gameListeners.Add(listener);
        _gameListeners.Sort((x, y) => y.ListenerPriority.CompareTo(x.ListenerPriority));
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
        if (listener.ListenerVersion != ISteamListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        if (_steamListeners.Any(x => x.Equals(listener)))
        {
            _logger.LogError("You are already install listener!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _steamListeners.Add(listener);
        _steamListeners.Sort((x, y) => y.ListenerPriority.CompareTo(x.ListenerPriority));
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
        GameFrameHookInfo? preHook  = null;
        GameFrameHookInfo? postHook = null;

        if (pre is not null)
        {
            if (_framePreHooks.Any(x => x.Callback.Equals(pre)))
            {
                _logger.LogError("You are already installed this hook<pre>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            preHook = new GameFrameHookInfo(pre, prePriority);
        }

        if (post is not null)
        {
            if (_framePostHooks.Any(x => x.Callback.Equals(post)))
            {
                _logger.LogError("You are already installed this hook<post>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            postHook = new GameFrameHookInfo(post, postPriority);
        }

        if (preHook is not null)
        {
            _framePreHooks.Add(preHook);
            _framePreHooks.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        if (postHook is not null)
        {
            _framePostHooks.Add(postHook);
            _framePostHooks.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }
    }

    public void InstallEntityThinkHook(Action? pre, Action? post, int prePriority = 0, int postPriority = 0)
    {
        EntityThinkHookInfo? preHook  = null;
        EntityThinkHookInfo? postHook = null;

        if (pre is not null)
        {
            if (_entityPreHooks.Any(x => x.Callback.Equals(pre)))
            {
                _logger.LogError("You are already installed this hook<pre>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            preHook = new EntityThinkHookInfo(pre, prePriority);
        }

        if (post is not null)
        {
            if (_entityPostHooks.Any(x => x.Callback.Equals(post)))
            {
                _logger.LogError("You are already installed this hook<post>!\n{stackTrace}", Environment.StackTrace);

                return;
            }

            postHook = new EntityThinkHookInfo(post, postPriority);
        }

        if (preHook is not null)
        {
            _entityPreHooks.Add(preHook);
            _entityPreHooks.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        if (postHook is not null)
        {
            _entityPostHooks.Add(postHook);
            _entityPostHooks.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }
    }

    public void InstallServerGameSimulateHook(Action callback, int priority = 0)
    {
        if (_gameSimulateHooks.Any(x => x.Callback.Equals(callback)))
        {
            _logger.LogError("You are already installed this hook<post>!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _gameSimulateHooks.Add(new GameSimulateHookInfo(callback, priority));
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

        var parts = Bridges.Natives.Core.GetCommandLine().Split(" ").Skip(1).ToArray();

        for (var i = 1; i < parts.Length; i++)
        {
            var key = parts[i].Trim();

            if (i + 1 >= parts.Length)
            {
                arguments[key] = null;

                continue;
            }

            var next = parts[i + 1];

            // 下一行是新的命令
            if (next[0] == '+' || next[0] == '-')
            {
                arguments[key] = null;

                continue;
            }

            arguments[key] = next.Trim();
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

#endregion

#region Dual Addon

    public void DualAddonPurgeCheck()
        => Game.DualAddonPurgeCheck();

    public void DualAddonOverrideCheck(SteamID steamId, double time)
        => Game.DualAddonOverrideCheck(steamId, time);

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

    private void Shutdown()
    {
        _logger.LogInformation("Shutdown SharpCore");

        if (_timers.IsEmpty)
        {
            return;
        }

        var timers = _timers.Values.ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            try
            {
                var x = timers[i];

                if (!x.IsInitialized)
                {
                    x.Initialize(1.0);
                }

                if (x.Flags.HasFlag(GameTimerFlags.ForceCallBeforeShutdown))
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
    }

    public void OnModuleUnload(IModSharpModule module)
    {
        var assembly = module.GetType().Assembly;

        ClearAssemblyTimer(assembly);
        ClearAssemblyAction(assembly);
    }

    ///////////////////////////////////////////////////////////////////////////////////////

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueAction(Action action)
    {
        var assembly = action.Method.Module.Assembly;
        EnqueueAction(assembly, action);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueAction(Assembly assembly, Action action)
        => _actionQueue.Enqueue(new AssemblyAction(assembly, action));

    private void ClearMapTimer()
    {
        // Stop timer only current map

        var timers = _timers.Where(x => x.Value.Flags.HasFlag(GameTimerFlags.StopOnMapEnd))
                            .Select(x => x.Key)
                            .ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            if (_timers.Remove(timers[i], out var timer) && timer.Flags.HasFlag(GameTimerFlags.ForceCallOnStop))
            {
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
    }

    private void ClearAssemblyTimer(Assembly assembly)
    {
        var timers = _timers.Where(x => x.Value.Assembly.Equals(assembly))
                            .Select(x => x.Key)
                            .ToArray();

        for (var i = 0; i < timers.Length; i++)
        {
            if (_timers.Remove(timers[i], out var timer) && timer.Flags.HasFlag(GameTimerFlags.ForceCallBeforeShutdown))
            {
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
    }

    private void ClearAssemblyAction(Assembly assembly)
    {
        if (_actionQueue.Count == 0)
        {
            return;
        }

        var backup = new List<AssemblyAction>(_actionQueue.Count);

        while (_actionQueue.TryDequeue(out var item))
        {
            if (item.Assembly.Equals(assembly))
            {
                continue;
            }

            backup.Add(item);
        }

        if (backup.Count == 0)
        {
            return;
        }

        foreach (var item in backup)
        {
            _actionQueue.Enqueue(item);
        }
    }

    private readonly record struct AssemblyAction(Assembly Assembly, Action Action);

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
