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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Units;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Event
{
    public static partial void HookEvent(string name);

    public static partial IntPtr CreateEvent(string name, bool force);

    public static partial void FireEvent(IntPtr @event, bool serverOnly);

    public static partial void FireToClient(IntPtr @event, int slot);

    public static partial void FireToClients(IntPtr @event);

    public static partial void FreeEvent(IntPtr @event);

    public static partial IntPtr CloneEvent(IntPtr @event);

    public static partial bool FindListener(PlayerSlot slot, string name);

    public static partial sbyte* GetName(IntPtr @event);

    public static partial sbyte* GetString(IntPtr @event, string key);

    public static partial int GetInt(IntPtr @event, string key, int defaultValue);

    public static partial ulong GetUInt64(IntPtr @event, string key, ulong defaultValue);

    public static partial nint GetPlayerController(IntPtr @event, string key);

    public static partial nint GetPlayerPawn(IntPtr @event, string key);

    public static partial float GetFloat(IntPtr @event, string key, float defaultValue);

    public static partial void SetString(IntPtr @event, string key, string value);

    public static partial void SetInt(IntPtr @event, string key, int value);

    public static partial void SetFloat(IntPtr @event, string key, float value);

    public static partial void SetUInt64(IntPtr @event, string key, ulong value);

    public static partial void SetPlayerPawn(IntPtr @event, string key, IBaseEntity pawn);

    public static partial void SetPlayerSlot(IntPtr @event, string key, int slot);
}
