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

using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.AdminCommands;

internal class InterfaceBridge
{
    public InterfaceBridge(string sharpPath, ISharedSystem sharedSystem)
    {
        SharpPath = sharpPath;

        ModSharp             = sharedSystem.GetModSharp();
        ConVarManager        = sharedSystem.GetConVarManager();
        EventManager         = sharedSystem.GetEventManager();
        ClientManager        = sharedSystem.GetClientManager();
        EntityManager        = sharedSystem.GetEntityManager();
        FileManager          = sharedSystem.GetFileManager();
        HookManager          = sharedSystem.GetHookManager();
        SchemaManager        = sharedSystem.GetSchemaManager();
        TransmitManager      = sharedSystem.GetTransmitManager();
        EconItemManager      = sharedSystem.GetEconItemManager();
        SoundManager         = sharedSystem.GetSoundManager();
        LibraryModuleManager = sharedSystem.GetLibraryModuleManager();
        LoggerFactory        = sharedSystem.GetLoggerFactory();
        SteamAPi             = ModSharp.GetSteamGameServer();
    }

    public string SharpPath { get; }

    public IModSharp             ModSharp             { get; }
    public IConVarManager        ConVarManager        { get; }
    public IEventManager         EventManager         { get; }
    public IClientManager        ClientManager        { get; }
    public IEntityManager        EntityManager        { get; }
    public IFileManager          FileManager          { get; }
    public IHookManager          HookManager          { get; }
    public ISchemaManager        SchemaManager        { get; }
    public ITransmitManager      TransmitManager      { get; }
    public ISteamApi             SteamAPi             { get; }
    public IEconItemManager      EconItemManager      { get; }
    public ISoundManager         SoundManager         { get; }
    public ILibraryModuleManager LibraryModuleManager { get; }
    public ILoggerFactory        LoggerFactory        { get; }
}
