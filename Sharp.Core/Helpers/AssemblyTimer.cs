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
using System.Reflection;
using Sharp.Shared.Enums;

namespace Sharp.Core.Helpers;

internal abstract class AssemblyTimer
{
    public Guid           UniqueId      { get; init; }
    public Assembly       Assembly      { get; init; }
    public GameTimerFlags Flags         { get; private set; }
    public bool           IsInitialized => _lastCallTime > 0;

    private readonly double _interval;
    private          double _lastCallTime;

    protected AssemblyTimer(Assembly assembly,
        double                       interval,
        GameTimerFlags               flags)
    {
        UniqueId = Guid.NewGuid();
        Assembly = assembly;
        Flags    = flags;

        _interval = interval;
    }

    public bool RunThink(double currentTime)
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Timer not initialized!");
        }

        var nextCallTime = _lastCallTime + _interval;

        if (nextCallTime > currentTime)
        {
            return false;
        }

        _lastCallTime += _interval;

        if (Run() is TimerAction.Stop)
        {
            Flags &= ~GameTimerFlags.Repeatable;
        }

        return true;
    }

    public void Initialize(double time)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Timer already initialized!");
        }

        _lastCallTime = time;
    }

    public void ForceRun()
        => Run();

    protected abstract TimerAction Run();
}

internal sealed class AssemblyFunctionTimer : AssemblyTimer
{
    private readonly Func<TimerAction> _callback;

    public AssemblyFunctionTimer(Assembly assembly,
        double                            interval,
        GameTimerFlags                    flags,
        Func<TimerAction>                 callback) : base(assembly, interval, flags)
        => _callback = callback;

    protected override TimerAction Run()
        => _callback();
}

internal sealed class AssemblyActionTimer : AssemblyTimer
{
    private readonly Action _callback;

    public AssemblyActionTimer(Assembly assembly,
        double                          interval,
        GameTimerFlags                  flags,
        Action                          callback) : base(assembly, interval, flags)
        => _callback = callback;

    protected override TimerAction Run()
    {
        _callback();

        return TimerAction.Continue;
    }
}
