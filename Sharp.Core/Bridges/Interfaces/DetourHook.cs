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
using Sharp.Core.CStrike;
using Sharp.Core.Helpers;
using Sharp.Generator;
using Sharp.Shared.Hooks;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal partial class DetourHook : NativeObject, IDetourHook
{
    private bool _installed;

#region Natives

    public DetourHook() : base(nint.Zero)
        => _this = Natives.Core.CreateInlineHook();

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        MarkAsDisposed();
        Natives.Core.RemoveInlineHook(_this);
    }

    public partial void PrepareNative(nint pTargetFn, nint pHookFn);

    public partial bool InstallNative();

    public partial void UninstallNative();

    // ReSharper disable once MemberCanBePrivate.Global
    public partial nint GetTrampoline();

#endregion

    public void Prepare(string gamedata, IntPtr hookFn)
    {
        CheckDisposed();

        var address = CoreGameData.Core.GetAddress(gamedata);

        if (address == nint.Zero)
        {
            throw new EntryPointNotFoundException($"GameData {gamedata} not found");
        }

        Prepare(address, hookFn);
    }

    /// <exception cref="InvalidOperationException">The hook is already installed</exception>
    public void Prepare(nint pTargetFn, nint pHookFn)
    {
        CheckDisposed();

        if (_installed)
        {
            throw new InvalidOperationException("Detour hook is already installed, uninstall it before preparing again");
        }

        PrepareNative(pTargetFn, pHookFn);
    }

    public bool Install()
    {
        CheckDisposed();

        if (!InstallNative())
        {
            return false;
        }

        _installed = true;

        return true;
    }

    public void Uninstall()
    {
        CheckDisposed();

        UninstallNative();
        _installed = false;
    }

    public nint Trampoline
    {
        get
        {
            CheckDisposed();

            return GetTrampoline();
        }
    }
}
