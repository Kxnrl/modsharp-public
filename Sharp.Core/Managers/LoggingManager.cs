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
using Microsoft.Extensions.Logging;
using Sharp.Core.Logging;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;

namespace Sharp.Core.Managers;

internal interface ICoreLoggingManager : ILoggingManager;

internal class LoggingManager : ICoreLoggingManager
{
    private readonly ILogger<LoggingManager> _logger;
    private readonly ICoreAssemblyManager    _assemblyManager;
    private readonly ISharpCore              _sharpCore;
    private readonly RuntimeLogDispatcher    _dispatcher;

    private readonly List<RuntimeLogDispatcher.Registration> _registrations;

    public LoggingManager(ILogger<LoggingManager> logger,
        ICoreAssemblyManager                      assemblyManager,
        ISharpCore                                sharpCore,
        RuntimeLogDispatchSink                    sink)
    {
        _logger          = logger;
        _assemblyManager = assemblyManager;
        _sharpCore       = sharpCore;
        _dispatcher      = new RuntimeLogDispatcher(logger, sink);
        _registrations   = [];

        _assemblyManager.RegisterUnloadCleanup(ClearLeakedRegistrations);
    }

    public void InstallLoggingListener(ILoggingListener listener)
    {
        EnsureMainThread();

        if (_assemblyManager.IsAssemblyUnloaded(listener.GetType().Assembly))
        {
            _logger.LogError("Install rejected, module already unloaded!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        if (listener.ListenerVersion != ILoggingListener.ApiVersion)
        {
            throw new InvalidOperationException("Your listener api version mismatch");
        }

        var minimumLevel = listener.RuntimeMinimumLevel;

        if (!Enum.IsDefined(minimumLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(listener),
                                                  minimumLevel,
                                                  "RuntimeMinimumLevel is not a defined LogLevel");
        }

        if (_registrations.Exists(x => ReferenceEquals(x.Listener, listener)))
        {
            _logger.LogError("You are already install listener!");

            return;
        }

        _registrations.Add(new RuntimeLogDispatcher.Registration(listener, listener.ListenerPriority, (int) minimumLevel));
        _registrations.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        _dispatcher.Publish([.. _registrations]);
    }

    public void RemoveLoggingListener(ILoggingListener listener)
    {
        EnsureMainThread();

        var index = _registrations.FindIndex(x => ReferenceEquals(x.Listener, listener));

        if (index < 0)
        {
            _logger.LogError("You have not install listener yet!\n{stackTrace}", Environment.StackTrace);

            return;
        }

        _registrations[index].Removed = true;
        _registrations.RemoveAt(index);
        _dispatcher.Publish([.. _registrations]);
    }

    private void EnsureMainThread()
    {
        if (!_sharpCore.IsOnMainThread())
        {
            throw new InvalidOperationException("Logging listeners can only be installed or removed on the main thread.");
        }
    }

    private void ClearLeakedRegistrations()
    {
        var snapshot = _registrations.ToArray();

        _assemblyManager.ClearLeaked(_registrations,
                                     "LoggingListener",
                                     x => x.Listener.GetType().Assembly,
                                     x => x.Listener.GetType().FullName ?? x.Listener.GetType().Name);

        if (_registrations.Count == snapshot.Length)
        {
            return;
        }

        foreach (var registration in snapshot)
        {
            if (!_registrations.Contains(registration))
            {
                registration.Removed = true;
            }
        }

        _dispatcher.Publish([.. _registrations]);
    }
}
