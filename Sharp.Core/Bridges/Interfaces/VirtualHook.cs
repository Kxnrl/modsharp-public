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
using Sharp.Core.Helpers;
using Sharp.Shared.Hooks;

namespace Sharp.Core.Bridges.Interfaces;

internal class VirtualHook : IVirtualHook
{
    private nint _vTable = nint.Zero;
    private int  _offset = -1;
    private nint _hook   = nint.Zero;

    public nint Trampoline { get; private set; } = nint.Zero;

#region Natives

    public bool Install()
    {
        if (_vTable == nint.Zero || _offset == -1 || _hook == nint.Zero)
        {
            return false;
        }

        Trampoline = Natives.Core.InstallVirtualHook(_vTable, _offset, _hook);

        return true;
    }

    public void Uninstall()
    {
        if (Trampoline == nint.Zero)
        {
            return;
        }

        Natives.Core.InstallVirtualHook(_vTable, _offset, Trampoline);
        Trampoline = nint.Zero;
    }

#endregion

    public void Prepare(string module, string @class, string function, nint hookFn)
    {
        var vTable = Natives.Core.GetVTableByClass(module, @class);

        if (vTable == nint.Zero)
        {
            throw new EntryPointNotFoundException($"VTable not found for {module}.{@class}");
        }

        var offset = CoreGameData.Core.GetVFuncIndex($"{@class}::{function}");

        if (offset == -1)
        {
            throw new EntryPointNotFoundException($"GameData {@class}::{function} not found");
        }

        _vTable = vTable;
        _offset = offset;
        _hook   = hookFn;
    }

    public void Prepare(string module, string @class, int offset, nint hookFn)
    {
        var vTable = Natives.Core.GetVTableByClass(module, @class);

        if (vTable == nint.Zero)
        {
            throw new EntryPointNotFoundException($"VTable not found for {module}.{@class}");
        }

        _vTable = vTable;
        _offset = offset;
        _hook   = hookFn;
    }

    public void Prepare(nint vTable, int offset, nint hookFn)
    {
        _vTable = vTable;
        _offset = offset;
        _hook   = hookFn;
    }
}
