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
using Sharp.Shared.Enums;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Core
{
    public static partial string GetCommandLine();

    public static partial IntPtr GetCoreBridge();

    public static partial nint GetGameDirectory();

    public static partial IntPtr GetGlobals();

    public static partial IntPtr GetIServer();

    public static partial double GetEngineTime();

    public static partial IntPtr GetGameRules();

    public static partial void FatalError(string message);

    public static partial void LogWarning(string message);

    public static partial void LogMessage(string message);

    public static partial void LogColorText(byte r, byte g, byte b, string message);

    public static partial nint GetGameData();

    public static partial nint FindStringTable(string name);

    public static partial nint CreateInlineHook();

    public static partial nint CreateMidFuncHook();

    public static partial void RemoveInlineHook(nint ptr);

    public static partial void RemoveMidFuncHook(nint ptr);

    public static partial IntPtr FindPattern(string module, string pattern);

    public static partial IntPtr GetVTableByClass(string module, string className);

    public static partial nint InstallVirtualHook(nint vTable, int index, nint hook);

    public static partial nint GetLibraryModule(string module);

    public static partial nint FindValveInterface(string module, string name);

    public static partial bool SetMemoryAccess(nint address, long size, MemoryAccess access, MemoryAccess* originalAccess);

    public static partial void RejectConnection(nint pNetAdr, NetworkDisconnectionReason reason);

    public static partial uint MakeStringToken(string str);
}
