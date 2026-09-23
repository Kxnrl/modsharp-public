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
    // native bool comes back in al; byte keeps it 1 byte wide since this assembly has runtime marshalling enabled
    private static delegate* unmanaged<nint, nint, nint, byte> _sFilterModelTrampoline;
    private static delegate* unmanaged<nint, nint, nint, byte> _sFilterContextTrampoline;
    private static delegate* unmanaged<nint, nint, nint, byte> _sFilterMassGreaterTrampoline;

    private readonly ILogger<FilterCrashFixes> _logger;
    private readonly IGameData                 _gameData;
    private readonly IVirtualHook              _filterModelHook;
    private readonly IVirtualHook              _filterContextHook;
    private readonly IVirtualHook              _filterMassGreaterHook;

    public FilterCrashFixes(ISharedSystem sharedSystem)
    {
        if (_sFilterModelTrampoline != null || _sFilterContextTrampoline != null || _sFilterMassGreaterTrampoline != null)
        {
            throw new InvalidOperationException("Double Hook!");
        }

        var hooks = sharedSystem.GetHookManager();

        _logger                = sharedSystem.GetLoggerFactory().CreateLogger<FilterCrashFixes>();
        _gameData              = sharedSystem.GetModSharp().GetGameData();
        _filterModelHook       = hooks.CreateVirtualHook();
        _filterContextHook     = hooks.CreateVirtualHook();
        _filterMassGreaterHook = hooks.CreateVirtualHook();
    }

    public void Init()
    {
        var offset = _gameData.GetVFuncIndex("CBaseFilter", "PassesFilterImpl");

        _sFilterModelTrampoline = Install(_filterModelHook,
                                          "CFilterModel",
                                          offset,
                                          (nint) (delegate* unmanaged<nint, nint, nint, byte>) (&FilterModelHook));

        _sFilterContextTrampoline = Install(_filterContextHook,
                                            "CFilterContext",
                                            offset,
                                            (nint) (delegate* unmanaged<nint, nint, nint, byte>) (&FilterContextHook));

        _sFilterMassGreaterTrampoline = Install(_filterMassGreaterHook,
                                                "CFilterMassGreater",
                                                offset,
                                                (nint) (delegate* unmanaged<nint, nint, nint, byte>) (&FilterMassGreaterHook));
    }

    public void Shutdown()
    {
        _filterModelHook.Uninstall();
        _filterContextHook.Uninstall();
        _filterMassGreaterHook.Uninstall();

        _sFilterModelTrampoline       = null;
        _sFilterContextTrampoline     = null;
        _sFilterMassGreaterTrampoline = null;
    }

    private delegate* unmanaged<nint, nint, nint, byte> Install(IVirtualHook hook, string className, int offset, nint hookFn)
    {
        try
        {
            hook.Prepare("server", className, offset, hookFn);

            if (hook.Install())
            {
                return (delegate* unmanaged<nint, nint, nint, byte>) hook.Trampoline;
            }

            _logger.LogError("{n} init failed", className);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{n} init failed", className);
        }

        return null;
    }

    [UnmanagedCallersOnly]
    private static byte FilterModelHook(nint pFilter, nint pCaller, nint pEntity)
    {
        if (pEntity == nint.Zero)
        {
            return 0;
        }

        return _sFilterModelTrampoline(pFilter, pCaller, pEntity);
    }

    [UnmanagedCallersOnly]
    private static byte FilterContextHook(nint pFilter, nint pCaller, nint pEntity)
    {
        if (pEntity == nint.Zero)
        {
            return 0;
        }

        return _sFilterContextTrampoline(pFilter, pCaller, pEntity);
    }

    [UnmanagedCallersOnly]
    private static byte FilterMassGreaterHook(nint pFilter, nint pCaller, nint pEntity)
    {
        if (pEntity == nint.Zero)
        {
            return 0;
        }

        return _sFilterMassGreaterTrampoline(pFilter, pCaller, pEntity);
    }
}
