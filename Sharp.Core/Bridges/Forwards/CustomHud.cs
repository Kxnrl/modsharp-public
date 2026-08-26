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

using System.Runtime.InteropServices;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Bridges.Forwards;

internal static class CustomHud
{
    public delegate void DelegateOnCustomHudClicked(nint player, nint layout, string buttonId);

    public static event DelegateOnCustomHudClicked? OnCustomHudClicked;

    [UnmanagedCallersOnly]
    public static unsafe void OnCustomHudClickedExport(nint player, nint layout, sbyte* pButtonId)
    {
        if (OnCustomHudClicked is null || player == nint.Zero || layout == nint.Zero || pButtonId == null)
        {
            return;
        }

        OnCustomHudClicked.Invoke(player, layout, Utils.ReadString(pButtonId));
    }
}
