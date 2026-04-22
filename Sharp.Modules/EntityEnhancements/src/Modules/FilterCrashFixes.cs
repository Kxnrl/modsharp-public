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
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Hooks;

namespace Sharp.Modules.EntityEnhancements.Modules;

internal sealed unsafe class FilterCrashFixes : IEnhancement
{
    private static delegate* unmanaged<nint, InputData*, void> _sTrampoline;
    private static FilterCrashFixes?                           _sInstance;

    private readonly ILogger<FilterCrashFixes> _logger;
    private readonly ISharedSystem             _sharedSystem;
    private readonly IDetourHook               _hook;

    public FilterCrashFixes(ISharedSystem sharedSystem)
    {
        _logger       = sharedSystem.GetLoggerFactory().CreateLogger<FilterCrashFixes>();
        _sharedSystem = sharedSystem;
        _hook         = sharedSystem.GetHookManager().CreateDetourHook();

        if (_sTrampoline != null)
        {
            throw new InvalidOperationException("Double Hook!");
        }

        _sInstance = this;
    }

    public void Init()
    {
        _hook.Prepare(_sharedSystem.GetSchemaManager().GetDataMapInputFunc("CBaseFilter", "InputTestActivator"),
                      (nint) (delegate* unmanaged<nint, InputData*, void>) (&Hook));

        if (!_hook.Install())
        {
            _logger.LogError("{n} init failed", GetType().Name);
        }
        else
        {
            _sTrampoline = (delegate* unmanaged<nint, InputData*, void>) _hook.Trampoline;
        }
    }

    public void Shutdown()
    {
        _hook.Uninstall();
        _hook.Dispose();
    }

    [UnmanagedCallersOnly]
    private static void Hook(nint pEntity, InputData* pInput)
    {
        if (pInput->pActivator == nint.Zero)
        {
            return;
        }

        _sTrampoline(pEntity, pInput);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputData
    {
        [FieldOffset(0)]
        public nint pActivator;

        [FieldOffset(8)]
        public nint pCaller;
    }
}
