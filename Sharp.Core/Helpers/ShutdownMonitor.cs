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
using System.Threading;

namespace Sharp.Core.Helpers;

internal interface IShutdownMonitor
{
    public CancellationToken Token { get; }

    void RegisterShutdownEvent(Action callback, int priority = 0);
}

internal sealed class ShutdownMonitor : IShutdownMonitor
{
    private record OrderedCallback(Action Callback, int Priority);

    public CancellationToken Token => _source.Token;

    public void RegisterShutdownEvent(Action callback, int priority = 0)
    {
        _events.Add(new OrderedCallback(callback, priority));
        _events.Sort((x, y) => y.Priority.CompareTo(x.Priority));
    }

    private readonly CancellationTokenSource _source = new ();

    private readonly List<OrderedCallback> _events = [];

    internal void Shutdown()
    {
        foreach (var (e, _) in _events)
        {
            try
            {
                e();
            }
            catch (Exception ex)
            {
                Bootstrap.CoreLogger.Error(ex, "An error occurred while executing shutdown event");
            }
        }

        _source.Cancel();
    }
}
