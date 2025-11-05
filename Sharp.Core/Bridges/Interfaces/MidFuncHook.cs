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
using Sharp.Core.CStrike;
using Sharp.Core.Helpers;
using Sharp.Generator;
using Sharp.Shared.Hooks;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal partial class MidFuncHook : NativeObject, IMidFuncHook
{
    public MidFuncHook() : base(nint.Zero)
        => _this = Natives.Core.CreateMidFuncHook();

    public void Dispose()
        => Natives.Core.RemoveMidFuncHook(_this);

    public IntPtr Trampoline => throw new NotSupportedException("YOU SHOULD NEVER CALL THIS WITH MidFuncHook");

    public partial void Prepare(nint pTargetFn, nint pHookFn);

    public partial bool Install();

    public partial void Uninstall();

    // ReSharper disable once MemberCanBePrivate.Global
    public partial nint GetTrampoline();

    public void Prepare(string gamedata, nint hookFn)
    {
        var address = CoreGameData.Core.GetAddress(gamedata);

        if (address == nint.Zero)
        {
            throw new EntryPointNotFoundException($"GameData {gamedata} not found");
        }

        Prepare(address, hookFn);
    }
}
