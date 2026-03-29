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

using System.Runtime.InteropServices;
using Sharp.Core.Bridges.Interfaces;

namespace Sharp.Core.Bridges;

internal static unsafe class CoreBridge
{
    private static ModSharpInterface* _coreBridgeStruct;

    private static ModSharpInterface* GetCoreBridge()
    {
        if (_coreBridgeStruct is null)
        {
            _coreBridgeStruct = (ModSharpInterface*) Natives.Core.GetCoreBridge();
        }

        return _coreBridgeStruct;
    }

    private static FullFileSystem?              _fullFileSystem;
    private static SteamApi?                    _steamApi;
    private static NetworkingStringTableHelper? _networkingStringTableHelper;
    private static KeyValuesHelper?             _keyValuesHelper;
    private static MemAlloc?                    _memAlloc;
    private static KeyValues3Helper?            _keyValues3Helper;

    public static FullFileSystem FullFileSystemInstance
        => _fullFileSystem ??= FullFileSystem.Create(GetCoreBridge()->FullFileSystem)!;

    public static SteamApi SteamApiInstance => _steamApi ??= SteamApi.Create(GetCoreBridge()->SteamApi)!;

    public static NetworkingStringTableHelper NetworkingStringTableHelperInstance
        => _networkingStringTableHelper ??= NetworkingStringTableHelper.Create(GetCoreBridge()->NetworkingStringTableHelper)!;

    public static KeyValuesHelper KeyValuesHelperInstance
        => _keyValuesHelper ??= KeyValuesHelper.Create(GetCoreBridge()->KeyValuesHelper)!;

    public static MemAlloc MemAlloc => _memAlloc ??= MemAlloc.Create(GetCoreBridge()->MemAllocator)!;

    public static KeyValues3Helper KeyValues3HelperInstance
        => _keyValues3Helper ??= KeyValues3Helper.Create(GetCoreBridge()->KeyValues3Helper)!;

    public static nint Engine          => GetCoreBridge()->EngineServer;
    public static nint ValveFileSystem => GetCoreBridge()->FullFileSystem;

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ModSharpInterface
    {
        public nint FullFileSystem              { get; init; }
        public nint SteamApi                    { get; init; }
        public nint NetworkingStringTableHelper { get; init; }
        public nint KeyValuesHelper             { get; init; }
        public nint MemAllocator                { get; init; }
        public nint KeyValues3Helper            { get; init; }
        public nint EngineServer                { get; init; }
    }
}
